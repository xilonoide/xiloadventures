using System.Net.Http;
using XiloAdventures.Engine;

namespace XiloAdventures.Linux.Player.Audio;

/// <summary>
/// Gestor de sonido para el player de consola Linux.
/// Usa SDL2 via Sdl2AudioPlayer para reproducción de audio.
/// </summary>
public class LinuxSoundManager : SoundManager
{
    private bool _initialized;
    private bool _soundAvailable;
    private Sdl2AudioPlayer? _sdl2Player;

    // Rutas temporales para la música actual
    private string? _currentWorldMusicPath;
    private string? _currentRoomMusicPath;

    // Cliente HTTP para TTS
    // En Docker (modo pruebas): acceder al host Windows via host.docker.internal
    // En Linux nativo: los servicios corren localmente en localhost
    private static string TtsHost => File.Exists("/.dockerenv") ? "host.docker.internal" : "localhost";

    private static HttpClient? _ttsHttpClient;
    private static HttpClient TtsHttpClient => _ttsHttpClient ??= new HttpClient
    {
        BaseAddress = new Uri($"http://{TtsHost}:5002/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Cache de voces
    private readonly Dictionary<string, byte[]> _voiceCache = new();
    private readonly object _voiceCacheLock = new();
    private readonly HashSet<string> _visitedRoomsForTts = new();

    /// <summary>
    /// Inicializa el sistema de sonido.
    /// Retorna true si el sonido está disponible.
    /// </summary>
    public bool Initialize()
    {
        if (_initialized)
            return _soundAvailable;

        _initialized = true;

        try
        {
            _sdl2Player = new Sdl2AudioPlayer();
            _soundAvailable = _sdl2Player.Initialize();

            if (!_soundAvailable)
            {
                SoundEnabled = false;
            }
        }
        catch
        {
            _soundAvailable = false;
            SoundEnabled = false;
        }

        return _soundAvailable;
    }

    public override void PlayWorldMusic(string? musicId, string? musicBase64)
    {
        if (!SoundEnabled) return;
        PlayMusicWithSdl2(musicId, musicBase64, ref _currentWorldMusicPath);
    }

    public override void PlayRoomMusic(string? musicId, string? musicBase64, string? worldMusicIdFallback, string? worldMusicBase64Fallback)
    {
        if (!SoundEnabled) return;

        // La música de sala reemplaza a la de mundo (SDL2 solo reproduce una pista a la vez)
        if (!string.IsNullOrWhiteSpace(musicId) || !string.IsNullOrWhiteSpace(musicBase64))
        {
            PlayMusicWithSdl2(musicId, musicBase64, ref _currentRoomMusicPath);
        }
        else
        {
            // No hay música de sala, reproducir música de mundo
            PlayMusicWithSdl2(worldMusicIdFallback, worldMusicBase64Fallback, ref _currentWorldMusicPath);
        }
    }

    private void PlayMusicWithSdl2(string? musicId, string? musicBase64, ref string? currentPath)
    {
        if (_sdl2Player == null || !_soundAvailable) return;

        var path = EnsureAudioFile(musicId, musicBase64);
        if (path == null || !File.Exists(path))
        {
            _sdl2Player.StopMusic();
            currentPath = null;
            return;
        }

        // Si ya es la misma pista, no reiniciar
        if (currentPath == path) return;

        currentPath = path;
        _sdl2Player.PlayMusic(path, loop: true);
    }

    /// <summary>
    /// Garantiza que tenemos un archivo de audio físico para reproducir.
    /// </summary>
    private string? EnsureAudioFile(string? musicId, string? musicBase64)
    {
        if (!string.IsNullOrWhiteSpace(musicBase64))
        {
            try
            {
                var bytes = Convert.FromBase64String(musicBase64);

                string fileName;
                if (!string.IsNullOrWhiteSpace(musicId))
                {
                    fileName = musicId;
                }
                else
                {
                    var basePart = musicBase64.Length > 16
                        ? musicBase64.Substring(0, 16)
                        : musicBase64;

                    foreach (var c in Path.GetInvalidFileNameChars())
                    {
                        basePart = basePart.Replace(c, '_');
                    }

                    fileName = $"music_{basePart}.mp3";
                }

                var path = Path.Combine(Path.GetTempPath(), fileName);

                if (!File.Exists(path))
                {
                    File.WriteAllBytes(path, bytes);
                }

                return path;
            }
            catch
            {
                return null;
            }
        }

        if (!string.IsNullOrWhiteSpace(musicId))
        {
            var path = Path.Combine(Path.GetTempPath(), musicId);
            return File.Exists(path) ? path : null;
        }

        return null;
    }

    public override void StopMusic()
    {
        _sdl2Player?.StopMusic();
        _currentWorldMusicPath = null;
        _currentRoomMusicPath = null;
    }

    public override void StopRoomMusic()
    {
        // Si hay música de mundo, volver a ella
        if (!string.IsNullOrEmpty(_currentWorldMusicPath) && File.Exists(_currentWorldMusicPath))
        {
            _sdl2Player?.PlayMusic(_currentWorldMusicPath, loop: true);
        }
        else
        {
            _sdl2Player?.StopMusic();
        }
        _currentRoomMusicPath = null;
    }

    public override void StopWorldMusic()
    {
        // Solo parar si no hay música de sala
        if (string.IsNullOrEmpty(_currentRoomMusicPath))
        {
            _sdl2Player?.StopMusic();
        }
        _currentWorldMusicPath = null;
    }

    public override void StopVoice()
    {
        _sdl2Player?.StopVoice();
    }

    public override async Task PlayRoomDescriptionAsync(string roomId, string? text)
    {
        if (!SoundEnabled || SuppressVoicePlayback)
            return;

        if (string.IsNullOrWhiteSpace(text))
        {
            StopVoice();
            return;
        }

        if (MasterVolume <= 0f || VoiceVolume <= 0f)
        {
            StopVoice();
            return;
        }

        await PlayRoomDescriptionWithTtsAsync(roomId, text);
    }

    private async Task PlayRoomDescriptionWithTtsAsync(string roomId, string text)
    {
        if (_sdl2Player == null || !_soundAvailable)
            return;

        byte[]? wavBytes = null;

        // Buscar en cache
        if (!string.IsNullOrWhiteSpace(roomId))
        {
            lock (_voiceCacheLock)
            {
                if (_voiceCache.TryGetValue(roomId, out var cached))
                {
                    wavBytes = cached;
                }
            }
        }

        // Si no está en cache, obtener del servicio TTS
        if (wavBytes == null)
        {
            try
            {
                var encodedText = Uri.EscapeDataString(text);
                using var response = await TtsHttpClient
                    .GetAsync($"api/tts?text={encodedText}", HttpCompletionOption.ResponseHeadersRead)
                    .ConfigureAwait(false);

                response.EnsureSuccessStatusCode();

                await using var networkStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false);
                using var ms = new MemoryStream();
                await networkStream.CopyToAsync(ms).ConfigureAwait(false);
                wavBytes = ms.ToArray();

                // Guardar en cache
                if (wavBytes.Length > 0 && !string.IsNullOrWhiteSpace(roomId))
                {
                    lock (_voiceCacheLock)
                    {
                        _voiceCache[roomId] = wavBytes;
                    }
                }
            }
            catch
            {
                // TTS no disponible, continuar sin voz
                return;
            }
        }

        if (wavBytes == null || wavBytes.Length == 0)
            return;

        // Reproducir con SDL2
        StopVoice();
        _sdl2Player.PlayVoiceFromBytes(wavBytes);
    }

    public override void RefreshVolumes()
    {
        if (_sdl2Player != null)
        {
            _sdl2Player.SetMasterVolume(MasterVolume);
            _sdl2Player.SetMusicVolume(MusicVolume);
            _sdl2Player.SetVoiceVolume(VoiceVolume);
        }
    }

    /// <summary>
    /// Libera los recursos
    /// </summary>
    public override void Dispose()
    {
        _sdl2Player?.Dispose();
        _sdl2Player = null;
    }
}
