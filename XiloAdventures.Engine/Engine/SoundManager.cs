using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Net.Http;
using NAudio.Wave;

namespace XiloAdventures.Engine;

public class SoundManager : IDisposable
{
    private IWavePlayer? _worldMusicPlayer;
    private AudioFileReader? _worldMusicReader;
    private string? _worldMusicPath;

    private IWavePlayer? _roomMusicPlayer;
    private AudioFileReader? _roomMusicReader;
    private string? _roomMusicPath;

    private IWavePlayer? _voicePlayer;
    private WaveStream? _voiceReader;

    private static readonly HttpClient TtsHttpClient = new()
    {
        BaseAddress = new Uri("http://localhost:5002/")
    };


    private readonly object _voiceCacheLock = new();
    private readonly Dictionary<string, byte[]> _voiceCache = new(StringComparer.OrdinalIgnoreCase);
    // Flags para controlar el bucle de la música y evitar reinicios cuando se detiene explícitamente.
    private bool _worldMusicLoopEnabled;
    private bool _roomMusicLoopEnabled;

    private const float FadeDurationSeconds = 0.5f;
    private const float TalkoverFadeDurationSeconds = 0.5f;
    private const float TalkoverVolumeReduction = 0.50f; // Reducir 50%

    // Volumen objetivo actual de la música (antes del talkover)
    private float _worldMusicTargetVolume = 1.0f;
    private float _roomMusicTargetVolume = 1.0f;
    private bool _talkoverActive = false;

    // Tokens de cancelación para evitar fades simultáneos
    private CancellationTokenSource? _worldMusicFadeCts;
    private CancellationTokenSource? _roomMusicFadeCts;

    public bool SoundEnabled { get; set; } = true;

    /// <summary>
    /// Cuando está activo, se suprime la reproducción de voz (para cargar partidas sin leer la sala).
    /// </summary>
    public bool SuppressVoicePlayback { get; set; } = false;

    /// <summary>
    /// Volumen de la música normalizado (0.0 - 1.0).
    /// </summary>
    public float MusicVolume { get; set; } = 1.0f;

    /// <summary>
    /// Volumen de los efectos normalizado (0.0 - 1.0).
    /// </summary>
    public float EffectsVolume { get; set; } = 1.0f;

    /// <summary>
    /// Volumen maestro normalizado (0.0 - 1.0).
    /// </summary>
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>
    /// Volumen de la voz normalizado (0.0 - 1.0).
    /// </summary>
    public float VoiceVolume { get; set; } = 1.0f;

    public SoundManager()
    {
    }

    /// <summary>
    /// Reproduce la música global del mundo a partir de un identificador de archivo
    /// y/o un contenido en Base64. Si ambos están vacíos se detiene la música global.
    /// </summary>
    public virtual void PlayWorldMusic(string? musicId, string? musicBase64)
    {
        if (!SoundEnabled)
        {
            // No intentar hacer nada si el sonido está desactivado
            return;
        }

        // Si ya tenemos un reproductor de música de mundo, no reiniciamos la pista.
        // Simplemente nos aseguramos de que esté sonando.
        if (_worldMusicPlayer != null)
        {
            if (_worldMusicPlayer.PlaybackState != PlaybackState.Playing)
                _worldMusicPlayer.Play();
            return;
        }

        if (string.IsNullOrWhiteSpace(musicId) && string.IsNullOrWhiteSpace(musicBase64))
        {
            StopWorldMusic();
            return;
        }

        var path = EnsureAudioFile(musicId, musicBase64);
        if (path == null || !File.Exists(path))
        {
            StopWorldMusic();
            return;
        }

        try
        {
            _worldMusicReader = new AudioFileReader(path)
            {
                Volume = 0.0f
            };
            _worldMusicPlayer = new WaveOutEvent();
            _worldMusicPlayer.Init(_worldMusicReader);
            _worldMusicPath = path;

            // Activar bucle para la música de mundo.
            _worldMusicLoopEnabled = true;
            _worldMusicPlayer.PlaybackStopped += WorldMusicPlayerOnPlaybackStopped;

            _worldMusicPlayer.Play();

            // Fade-in suave de la música global del mundo.
            FadeWorldMusicTo(1.0f);
        }
        catch
        {
            StopWorldMusic();
        }
    }


    /// <summary>
    /// Reproduce la música especial de una sala.
    /// - Si la sala tiene música propia: se reproduce la de la sala y, si hay música global, se deja en segundo plano con volumen 0.
    /// - Si la sala NO tiene música propia: se detiene la música de sala (si la hay) y se reestablece la música global (si existe).
    /// </summary>
    public virtual void PlayRoomMusic(string? musicId, string? musicBase64, string? worldMusicIdFallback, string? worldMusicBase64Fallback)
    {
        if (!SoundEnabled)
        {
            // No intentar hacer nada si el sonido está desactivado
            return;
        }

        var hasRoomMusic = !string.IsNullOrWhiteSpace(musicId) || !string.IsNullOrWhiteSpace(musicBase64);

        if (!hasRoomMusic)
        {
            // No hay música especial: hacemos fade-out de la música de sala (si la hay)
            // y restauramos progresivamente el volumen de la música global si existe.
            if (_roomMusicPlayer != null && _roomMusicReader != null)
            {
                FadeOutAndStopRoomMusic();
            }
            else
            {
                StopRoomMusic();
            }

            if (_worldMusicPlayer != null && _worldMusicReader != null)
            {
                FadeWorldMusicTo(1.0f);
            }

            return;
        }

        // Hay música de sala: si la música global existe, la llevamos a volumen 0 con un pequeño fade.
        if (_worldMusicPlayer != null && _worldMusicReader != null)
        {
            FadeWorldMusicTo(0.0f);
        }

        var path = EnsureAudioFile(musicId, musicBase64);
        if (path == null || !File.Exists(path))
        {
            StopRoomMusic();
            if (_worldMusicPlayer != null)
            {
                SetWorldMusicVolume(1.0f);
            }
            return;
        }

        if (_roomMusicPlayer != null &&
            string.Equals(_roomMusicPath, path, StringComparison.OrdinalIgnoreCase))
        {
            if (_roomMusicPlayer.PlaybackState != PlaybackState.Playing)
                _roomMusicPlayer.Play();

            // Si la pista de sala ya es la misma, nos aseguramos de que esté a volumen completo.
            FadeRoomMusicTo(1.0f);
            return;
        }

        // Si hay música de sala distinta sonando, la atenuamos y detenemos antes de iniciar la nueva.
        if (_roomMusicPlayer != null && _roomMusicReader != null)
        {
            FadeOutAndStopRoomMusic();
        }
        else
        {
            StopRoomMusic();
        }

        try
        {
            _roomMusicReader = new AudioFileReader(path)
            {
                Volume = 0.0f
            };
            _roomMusicPlayer = new WaveOutEvent();
            _roomMusicPlayer.Init(_roomMusicReader);
            _roomMusicPath = path;

            // Activar bucle para la música de sala.
            _roomMusicLoopEnabled = true;
            _roomMusicPlayer.PlaybackStopped += RoomMusicPlayerOnPlaybackStopped;

            _roomMusicPlayer.Play();

            // Fade-in suave de la nueva música de sala.
            FadeRoomMusicTo(1.0f);
        }
        catch
        {
            StopRoomMusic();
        }
    }



    private async Task<byte[]?> FetchVoiceBytesAsync(string text)
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
            return ms.ToArray();
        }
        catch
        {
            // Si el servicio TTS no está disponible, retornamos null para que el juego continúe sin voz
            return null;
        }
    }

    public async Task PreloadRoomVoiceAsync(string roomId, string? text)
    {
        if (string.IsNullOrWhiteSpace(roomId) || string.IsNullOrWhiteSpace(text))
            return;

        lock (_voiceCacheLock)
        {
            if (_voiceCache.ContainsKey(roomId))
                return;
        }

        try
        {
            var bytes = await FetchVoiceBytesAsync(text).ConfigureAwait(false);
            if (bytes == null || bytes.Length == 0)
                return;

            lock (_voiceCacheLock)
            {
                _voiceCache[roomId] = bytes;
            }
        }
        catch
        {
            // Si el TTS falla al precargar, no consideramos esto un error fatal.
        }
    }

    public IReadOnlyCollection<string> GetCachedVoiceRoomIds()
    {
        lock (_voiceCacheLock)
        {
            return new List<string>(_voiceCache.Keys);
        }
    }

    public void RemoveVoiceFromCache(string roomId)
    {
        if (string.IsNullOrWhiteSpace(roomId))
            return;

        lock (_voiceCacheLock)
        {
            _voiceCache.Remove(roomId);
        }
    }

    public virtual async Task PlayRoomDescriptionAsync(string roomId, string? text)
    {
        // Si el sonido está desactivado o la voz está suprimida, no hacer nada
        if (!SoundEnabled || SuppressVoicePlayback)
        {
            return;
        }

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

        byte[]? bytes = null;

        if (!string.IsNullOrWhiteSpace(roomId))
        {
            lock (_voiceCacheLock)
            {
                if (_voiceCache.TryGetValue(roomId, out var cachedBytes))
                {
                    bytes = cachedBytes;
                }
            }
        }

        try
        {
            if (bytes == null)
            {
                var fetched = await FetchVoiceBytesAsync(text).ConfigureAwait(false);
                if (fetched == null || fetched.Length == 0)
                {
                    StopVoice();
                    return;
                }

                if (!string.IsNullOrWhiteSpace(roomId))
                {
                    lock (_voiceCacheLock)
                    {
                        _voiceCache[roomId] = fetched;
                    }
                }

                bytes = fetched;
            }

            StopVoice();

            var ms = new MemoryStream(bytes);
            var waveReader = new WaveFileReader(ms);
            var voiceChannel = new WaveChannel32(waveReader)
            {
                PadWithZeroes = false
            };

            var outputDevice = new WaveOutEvent
            {
                DesiredLatency = 200
            };

            _voiceReader = voiceChannel;
            _voicePlayer = outputDevice;

            RefreshVoiceVolume();

            // Aplicar talkover: bajar música mientras se reproduce la voz
            ApplyTalkover();

            _voicePlayer.PlaybackStopped += OnVoicePlaybackStopped;
            _voicePlayer.Init(_voiceReader);
            _voicePlayer.Play();
        }
        catch
        {
            // Si algo falla al generar o reproducir la voz, simplemente paramos la voz.
            StopVoice();
        }
    }

    private void OnVoicePlaybackStopped(object? sender, StoppedEventArgs e)
    {
        // Restaurar volumen de música cuando termina la voz
        RemoveTalkover();
    }

    public Task PlayRoomDescriptionAsync(string? text)
        => PlayRoomDescriptionAsync(string.Empty, text);


    public virtual void StopRoomMusic()
    {
        _roomMusicLoopEnabled = false;

        try
        {
            if (_roomMusicPlayer != null)
            {
                try
                {
                    _roomMusicPlayer.PlaybackStopped -= RoomMusicPlayerOnPlaybackStopped;
                }
                catch
                {
                    // Ignorar
                }
            }

            _roomMusicPlayer?.Stop();
            _roomMusicPlayer?.Dispose();
            _roomMusicReader?.Dispose();
        }
        catch
        {
            // Ignorar
        }
        finally
        {
            _roomMusicPlayer = null;
            _roomMusicReader = null;
            _roomMusicPath = null;
        }
    }

    public virtual void StopWorldMusic()
    {
        _worldMusicLoopEnabled = false;

        try
        {
            if (_worldMusicPlayer != null)
            {
                try
                {
                    _worldMusicPlayer.PlaybackStopped -= WorldMusicPlayerOnPlaybackStopped;
                }
                catch
                {
                    // Ignorar
                }
            }

            _worldMusicPlayer?.Stop();
            _worldMusicPlayer?.Dispose();
            _worldMusicReader?.Dispose();
        }
        catch
        {
            // Ignorar
        }
        finally
        {
            _worldMusicPlayer = null;
            _worldMusicReader = null;
            _worldMusicPath = null;
        }
    }


    public virtual void StopMusic()
    {
        StopRoomMusic();
        StopWorldMusic();
        StopVoice();
    }

    public virtual void StopVoice()
    {
        try
        {
            if (_voicePlayer != null)
            {
                _voicePlayer.PlaybackStopped -= OnVoicePlaybackStopped;
                _voicePlayer.Stop();
            }
        }
        catch
        {
            // Ignorar errores al detener la voz
        }

        _voicePlayer?.Dispose();
        _voiceReader?.Dispose();

        _voicePlayer = null;
        _voiceReader = null;

        // Restaurar volumen de música si había talkover activo
        RemoveTalkover();
    }

    /// <summary>
    /// Aplica el efecto talkover: baja el volumen de la música un 50% en 0.5 segundos.
    /// </summary>
    private void ApplyTalkover()
    {
        if (_talkoverActive) return;
        _talkoverActive = true;

        // Reducir el volumen objetivo actual en un 50%
        if (_worldMusicReader != null && _worldMusicTargetVolume > 0)
        {
            var talkoverVolume = _worldMusicTargetVolume * (1.0f - TalkoverVolumeReduction);
            var effectiveTarget = CalculateEffectiveVolume(talkoverVolume, MasterVolume, MusicVolume);
            FadeWorldMusic(effectiveTarget, TalkoverFadeDurationSeconds);
        }

        if (_roomMusicReader != null && _roomMusicTargetVolume > 0)
        {
            var talkoverVolume = _roomMusicTargetVolume * (1.0f - TalkoverVolumeReduction);
            var effectiveTarget = CalculateEffectiveVolume(talkoverVolume, MasterVolume, MusicVolume);
            FadeRoomMusic(effectiveTarget, TalkoverFadeDurationSeconds);
        }
    }

    /// <summary>
    /// Elimina el efecto talkover: restaura el volumen de la música en 0.5 segundos.
    /// </summary>
    private void RemoveTalkover()
    {
        if (!_talkoverActive) return;
        _talkoverActive = false;

        // Restaurar al volumen objetivo original
        if (_worldMusicReader != null && _worldMusicTargetVolume > 0)
        {
            var effectiveTarget = CalculateEffectiveVolume(_worldMusicTargetVolume, MasterVolume, MusicVolume);
            FadeWorldMusic(effectiveTarget, TalkoverFadeDurationSeconds);
        }

        if (_roomMusicReader != null && _roomMusicTargetVolume > 0)
        {
            var effectiveTarget = CalculateEffectiveVolume(_roomMusicTargetVolume, MasterVolume, MusicVolume);
            FadeRoomMusic(effectiveTarget, TalkoverFadeDurationSeconds);
        }
    }



    private void WorldMusicPlayerOnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (!_worldMusicLoopEnabled)
            return;

        if (_worldMusicReader == null || _worldMusicPlayer == null)
            return;

        try
        {
            _worldMusicReader.Position = 0;
            _worldMusicPlayer.Play();
        }
        catch
        {
            _worldMusicLoopEnabled = false;
        }
    }

    private void RoomMusicPlayerOnPlaybackStopped(object? sender, StoppedEventArgs e)
    {
        if (!_roomMusicLoopEnabled)
            return;

        if (_roomMusicReader == null || _roomMusicPlayer == null)
            return;

        try
        {
            _roomMusicReader.Position = 0;
            _roomMusicPlayer.Play();
        }
        catch
        {
            _roomMusicLoopEnabled = false;
        }
    }

    private float CalculateEffectiveVolume(float targetVolume, float volume1, float volume2)
    {
        var v1 = Math.Clamp(volume1, 0f, 1f);
        var v2 = Math.Clamp(volume2, 0f, 1f);
        return targetVolume * v1 * v2;
    }

    private void FadeWorldMusicTo(float targetVolume)
    {
        if (_worldMusicReader == null)
            return;

        // Guardar el volumen objetivo para poder restaurarlo después del talkover
        _worldMusicTargetVolume = targetVolume;

        // Si hay talkover activo, aplicar la reducción al volumen objetivo
        var adjustedTarget = _talkoverActive ? targetVolume * (1.0f - TalkoverVolumeReduction) : targetVolume;
        var effectiveTarget = CalculateEffectiveVolume(adjustedTarget, MasterVolume, MusicVolume);
        FadeWorldMusic(effectiveTarget, FadeDurationSeconds);
    }


    private void FadeRoomMusicTo(float targetVolume)
    {
        if (_roomMusicReader == null)
            return;

        // Guardar el volumen objetivo para poder restaurarlo después del talkover
        _roomMusicTargetVolume = targetVolume;

        // Si hay talkover activo, aplicar la reducción al volumen objetivo
        var adjustedTarget = _talkoverActive ? targetVolume * (1.0f - TalkoverVolumeReduction) : targetVolume;
        var effectiveTarget = CalculateEffectiveVolume(adjustedTarget, MasterVolume, MusicVolume);
        FadeRoomMusic(effectiveTarget, FadeDurationSeconds);
    }


    private void SetWorldMusicVolume(float volume)
    {
        if (_worldMusicReader != null)
        {
            try
            {
                _worldMusicReader.Volume = CalculateEffectiveVolume(volume, MasterVolume, MusicVolume);
            }
            catch
            {
                // Ignorar problemas de volumen
            }
        }
    }

    private void RefreshVoiceVolume()
    {
        if (_voiceReader is WaveChannel32 voiceChannel)
        {
            try
            {
                if (!SoundEnabled)
                {
                    voiceChannel.Volume = 0f;
                    return;
                }

                voiceChannel.Volume = CalculateEffectiveVolume(1.0f, MasterVolume, VoiceVolume);
            }
            catch
            {
                // Ignorar problemas de volumen de voz
            }
        }
    }

    public virtual void RefreshVolumes()
    {
        if (!SoundEnabled)
        {
            // Si el sonido está desactivado, silenciamos la música sin detenerla.
            if (_worldMusicReader != null)
            {
                try { _worldMusicReader.Volume = 0f; } catch { }
            }
            if (_roomMusicReader != null)
            {
                try { _roomMusicReader.Volume = 0f; } catch { }
            }

            RefreshVoiceVolume();
            return;
        }

        var master = Math.Clamp(MasterVolume, 0f, 1f);
        var music = Math.Clamp(MusicVolume, 0f, 1f);

        var hasRoomMusic = _roomMusicPlayer != null && _roomMusicReader != null;
        var hasWorldMusic = _worldMusicPlayer != null && _worldMusicReader != null;

        if (hasRoomMusic)
        {
            // Estamos en una sala con música especial:
            // - La música de mundo debe permanecer silenciada mientras haya música de sala.
            // - Solo ajustamos el volumen de la música de sala con master + música.
            if (hasWorldMusic)
            {
                // Aseguramos que la música de mundo siga a 0 aunque se haya reactivado el sonido.
                SetWorldMusicVolume(0.0f);
            }

            try
            {
                _roomMusicReader!.Volume = 1.0f * master * music;
            }
            catch
            {
                // Ignorar problemas de volumen
            }
        }
        else if (hasWorldMusic)
        {
            // No hay música especial de sala: aplicamos el volumen a la música de mundo.
            SetWorldMusicVolume(1.0f);
        }

        RefreshVoiceVolume();
    }


    /// <summary>
    /// Termina la música de sala con un pequeño fade-out y la detiene.
    /// </summary>
    private void FadeOutAndStopRoomMusic()
    {
        if (_roomMusicPlayer == null || _roomMusicReader == null)
        {
            StopRoomMusic();
            return;
        }

        var start = _roomMusicReader.Volume;
        var master = Math.Clamp(MasterVolume, 0f, 1f);
        var music = Math.Clamp(MusicVolume, 0f, 1f);
        var effectiveTarget = 0f;

        Task.Run(async () =>
        {
            const int steps = 20;
            var stepDuration = FadeDurationSeconds / steps;

            for (int i = 1; i <= steps; i++)
            {
                var t = i / (float)steps;
                var current = start + (effectiveTarget - start) * t;
                try
                {
                    _roomMusicReader.Volume = current * master * music;
                }
                catch
                {
                    // Ignorar errores de volumen en el fade
                    break;
                }

                await Task.Delay(TimeSpan.FromSeconds(stepDuration));
            }

            StopRoomMusic();
        });
    }

    /// <summary>
    /// Realiza un fade genérico sobre un AudioFileReader.
    /// </summary>
    private void FadeVolume(AudioFileReader reader, float startVolume, float targetVolume, float durationSeconds, CancellationToken cancellationToken = default)
    {
        Task.Run(async () =>
        {
            const int steps = 20;
            var stepDuration = durationSeconds / steps;

            for (int i = 1; i <= steps; i++)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var t = i / (float)steps;
                var current = startVolume + (targetVolume - startVolume) * t;

                try
                {
                    reader.Volume = current;
                }
                catch
                {
                    // Ignorar errores de volumen durante el fade
                    break;
                }

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(stepDuration), cancellationToken);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        });
    }

    /// <summary>
    /// Realiza un fade sobre la música de mundo, cancelando cualquier fade anterior.
    /// </summary>
    private void FadeWorldMusic(float targetVolume, float durationSeconds)
    {
        if (_worldMusicReader == null) return;

        // Cancelar fade anterior
        _worldMusicFadeCts?.Cancel();
        _worldMusicFadeCts = new CancellationTokenSource();

        float start;
        try
        {
            start = _worldMusicReader.Volume;
        }
        catch
        {
            return;
        }

        FadeVolume(_worldMusicReader, start, targetVolume, durationSeconds, _worldMusicFadeCts.Token);
    }

    /// <summary>
    /// Realiza un fade sobre la música de sala, cancelando cualquier fade anterior.
    /// </summary>
    private void FadeRoomMusic(float targetVolume, float durationSeconds)
    {
        if (_roomMusicReader == null) return;

        // Cancelar fade anterior
        _roomMusicFadeCts?.Cancel();
        _roomMusicFadeCts = new CancellationTokenSource();

        float start;
        try
        {
            start = _roomMusicReader.Volume;
        }
        catch
        {
            return;
        }

        FadeVolume(_roomMusicReader, start, targetVolume, durationSeconds, _roomMusicFadeCts.Token);
    }

    /// <summary>
    /// Garantiza que tenemos un archivo de audio físico para reproducir.
    /// - Si musicBase64 viene informado, se decodifica y se guarda en la carpeta de sonido.
    /// - Si sólo viene musicId, se asume que es un nombre de archivo relativo a la carpeta de sonido.
    /// Para evitar reinicios constantes, cuando musicId viene vacío generamos un nombre
    /// estable basado en el propio contenido Base64.
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

    public virtual void Dispose()
    {
        StopMusic();
    }
}


