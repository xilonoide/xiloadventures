using System.Net.Http;
using NAudio.Wave;
using XiloAdventures.Engine;

namespace XiloAdventures.Terminal.Player.Audio;

/// <summary>
/// Gestor de sonido para el player de consola Windows.
/// Usa NAudio para reproducción de audio.
/// </summary>
public class WindowsSoundManager : SoundManager
{
    private bool _initialized;
    private bool _soundAvailable;

    // Reproductores de audio
    private WaveOutEvent? _musicPlayer;
    private AudioFileReader? _musicReader;
    private string? _currentMusicPath;

    // Reproductor de voz TTS
    private WaveOutEvent? _voicePlayer;
    private RawSourceWaveStream? _voiceStream;

    // Rutas temporales para la música actual
    private string? _currentWorldMusicPath;
    private string? _currentRoomMusicPath;

    // Cliente HTTP para TTS
    private static HttpClient? _ttsHttpClient;
    private static HttpClient TtsHttpClient => _ttsHttpClient ??= new HttpClient
    {
        BaseAddress = new Uri("http://localhost:5002/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    // Cache de voces
    private readonly Dictionary<string, byte[]> _voiceCache = new();
    private readonly object _voiceCacheLock = new();

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
            // Verificar que podemos crear un reproductor de audio
            using var testPlayer = new WaveOutEvent();
            _soundAvailable = true;
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
        PlayMusicWithNAudio(musicId, musicBase64, ref _currentWorldMusicPath);
    }

    public override void PlayRoomMusic(string? musicId, string? musicBase64, string? worldMusicIdFallback, string? worldMusicBase64Fallback)
    {
        if (!SoundEnabled) return;

        // La música de sala reemplaza a la de mundo
        if (!string.IsNullOrWhiteSpace(musicId) || !string.IsNullOrWhiteSpace(musicBase64))
        {
            PlayMusicWithNAudio(musicId, musicBase64, ref _currentRoomMusicPath);
        }
        else
        {
            // No hay música de sala, reproducir música de mundo
            PlayMusicWithNAudio(worldMusicIdFallback, worldMusicBase64Fallback, ref _currentWorldMusicPath);
        }
    }

    private void PlayMusicWithNAudio(string? musicId, string? musicBase64, ref string? currentPath)
    {
        if (!_soundAvailable) return;

        var path = EnsureAudioFile(musicId, musicBase64);
        if (path == null || !File.Exists(path))
        {
            StopMusicInternal();
            currentPath = null;
            return;
        }

        // Si ya es la misma pista, no reiniciar
        if (_currentMusicPath == path) return;

        currentPath = path;
        _currentMusicPath = path;

        try
        {
            // Detener música anterior
            StopMusicInternal();

            // Crear nuevo reproductor
            _musicReader = new AudioFileReader(path);
            _musicPlayer = new WaveOutEvent();
            _musicPlayer.Init(_musicReader);

            // Aplicar volumen
            _musicReader.Volume = MasterVolume * MusicVolume;

            // Configurar loop
            _musicPlayer.PlaybackStopped += (s, e) =>
            {
                if (_musicReader != null && _musicPlayer != null && SoundEnabled)
                {
                    try
                    {
                        _musicReader.Position = 0;
                        _musicPlayer.Play();
                    }
                    catch { }
                }
            };

            _musicPlayer.Play();
        }
        catch
        {
            // Error al reproducir, ignorar
        }
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

    private void StopMusicInternal()
    {
        try
        {
            _musicPlayer?.Stop();
            _musicPlayer?.Dispose();
            _musicReader?.Dispose();
        }
        catch { }
        finally
        {
            _musicPlayer = null;
            _musicReader = null;
        }
    }

    public override void StopMusic()
    {
        StopMusicInternal();
        _currentMusicPath = null;
        _currentWorldMusicPath = null;
        _currentRoomMusicPath = null;
    }

    public override void StopRoomMusic()
    {
        // Si hay música de mundo, volver a ella
        if (!string.IsNullOrEmpty(_currentWorldMusicPath) && File.Exists(_currentWorldMusicPath))
        {
            try
            {
                StopMusicInternal();
                _currentMusicPath = _currentWorldMusicPath;

                _musicReader = new AudioFileReader(_currentWorldMusicPath);
                _musicPlayer = new WaveOutEvent();
                _musicPlayer.Init(_musicReader);
                _musicReader.Volume = MasterVolume * MusicVolume;

                _musicPlayer.PlaybackStopped += (s, e) =>
                {
                    if (_musicReader != null && _musicPlayer != null && SoundEnabled)
                    {
                        try
                        {
                            _musicReader.Position = 0;
                            _musicPlayer.Play();
                        }
                        catch { }
                    }
                };

                _musicPlayer.Play();
            }
            catch { }
        }
        else
        {
            StopMusicInternal();
        }
        _currentRoomMusicPath = null;
    }

    public override void StopWorldMusic()
    {
        // Solo parar si no hay música de sala
        if (string.IsNullOrEmpty(_currentRoomMusicPath))
        {
            StopMusicInternal();
        }
        _currentWorldMusicPath = null;
    }

    public override void StopVoice()
    {
        try
        {
            _voicePlayer?.Stop();
            _voicePlayer?.Dispose();
            _voiceStream?.Dispose();
        }
        catch { }
        finally
        {
            _voicePlayer = null;
            _voiceStream = null;
        }
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
        if (!_soundAvailable)
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

        // Reproducir con NAudio
        PlayVoiceFromBytes(wavBytes);
    }

    private void PlayVoiceFromBytes(byte[] wavBytes)
    {
        try
        {
            // Detener voz anterior
            StopVoice();

            // Crear stream desde bytes WAV
            var ms = new MemoryStream(wavBytes);

            // El TTS de Coqui devuelve WAV 22050Hz 16-bit mono
            var waveFormat = new WaveFormat(22050, 16, 1);

            // Saltar cabecera WAV (44 bytes)
            ms.Position = 44;

            _voiceStream = new RawSourceWaveStream(ms, waveFormat);
            _voicePlayer = new WaveOutEvent();
            _voicePlayer.Init(_voiceStream);

            // Aplicar volumen (NAudio no tiene volumen directo en WaveOutEvent,
            // pero podemos usar un VolumeWaveProvider si es necesario)
            _voicePlayer.Volume = MasterVolume * VoiceVolume;

            _voicePlayer.Play();
        }
        catch
        {
            // Error al reproducir voz, ignorar
            StopVoice();
        }
    }

    public override void RefreshVolumes()
    {
        if (_musicReader != null)
        {
            try
            {
                _musicReader.Volume = MasterVolume * MusicVolume;
            }
            catch { }
        }

        if (_voicePlayer != null)
        {
            try
            {
                _voicePlayer.Volume = MasterVolume * VoiceVolume;
            }
            catch { }
        }
    }

    /// <summary>
    /// Libera los recursos
    /// </summary>
    public override void Dispose()
    {
        StopMusicInternal();
        StopVoice();
        _ttsHttpClient?.Dispose();
        _ttsHttpClient = null;
    }
}
