using System.Runtime.InteropServices;

namespace XiloAdventures.Linux.Player.Audio;

/// <summary>
/// Reproductor de audio usando SDL2 para Linux.
/// Solo se usa cuando se ejecuta en Linux (WSL o nativo).
/// Usa P/Invoke directo a SDL2 para mayor compatibilidad.
/// </summary>
public class Sdl2AudioPlayer : IDisposable
{
    // Registrar el resolver de bibliotecas nativas en el constructor estático
    static Sdl2AudioPlayer()
    {
        NativeLibrary.SetDllImportResolver(typeof(Sdl2AudioPlayer).Assembly, ResolveSdl2Library);
    }

    private static IntPtr ResolveSdl2Library(string libraryName, System.Reflection.Assembly assembly, DllImportSearchPath? searchPath)
    {
        if (libraryName != "SDL2")
            return IntPtr.Zero;

        // Intentar cargar con diferentes nombres según la plataforma
        string[] libraryNames;

        if (OperatingSystem.IsWindows())
        {
            libraryNames = new[] { "SDL2.dll", "SDL2" };
        }
        else if (OperatingSystem.IsMacOS())
        {
            libraryNames = new[] { "libSDL2.dylib", "libSDL2-2.0.dylib", "SDL2" };
        }
        else // Linux
        {
            libraryNames = new[]
            {
                "libSDL2-2.0.so.0",
                "libSDL2-2.0.so",
                "libSDL2.so.0",
                "libSDL2.so",
                "SDL2"
            };
        }

        foreach (var name in libraryNames)
        {
            if (NativeLibrary.TryLoad(name, assembly, searchPath, out IntPtr handle))
            {
                return handle;
            }
        }

        return IntPtr.Zero;
    }

    private bool _initialized;
    private bool _disposed;
    private IntPtr _audioDevice;
    private GCHandle _callbackHandle;

    // Estado de reproducción de música
    private byte[]? _currentMusicData;
    private int _musicPosition;
    private bool _musicPlaying;
    private bool _musicLooping = true;
    private float _musicVolume = 1.0f;
    private float _masterVolume = 1.0f;

    // Estado de reproducción de voz (TTS)
    private byte[]? _currentVoiceData;
    private int _voicePosition;
    private bool _voicePlaying;
    private float _voiceVolume = 1.0f;

    private readonly object _lock = new();

    // Delegate para el callback de audio
    private delegate void SDL_AudioCallback(IntPtr userdata, IntPtr stream, int len);
    private SDL_AudioCallback? _audioCallback;

    public bool Initialize()
    {
        if (_initialized)
            return true;

        try
        {
            // Inicializar SDL con audio
            if (SDL_Init(SDL_INIT_AUDIO) < 0)
            {
                var error = Marshal.PtrToStringAnsi(SDL_GetError());
                Console.WriteLine($"Error inicializando SDL: {error}");
                return false;
            }

            // Configurar callback
            _audioCallback = AudioCallback;
            _callbackHandle = GCHandle.Alloc(_audioCallback);

            // Configurar formato de audio
            var desiredSpec = new SDL_AudioSpec
            {
                freq = 44100,
                format = AUDIO_S16LSB,
                channels = 2,
                samples = 4096,
                callback = Marshal.GetFunctionPointerForDelegate(_audioCallback),
                userdata = IntPtr.Zero
            };

            var obtainedSpec = new SDL_AudioSpec();
            _audioDevice = SDL_OpenAudioDevice(IntPtr.Zero, 0, ref desiredSpec, ref obtainedSpec, 0);

            if (_audioDevice == IntPtr.Zero)
            {
                var error = Marshal.PtrToStringAnsi(SDL_GetError());
                Console.WriteLine($"Error abriendo dispositivo de audio: {error}");
                SDL_Quit();
                return false;
            }

            // Iniciar reproducción (unpause)
            SDL_PauseAudioDevice(_audioDevice, 0);

            _initialized = true;
            return true;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error inicializando SDL2: {ex.Message}");
            return false;
        }
    }

    private void AudioCallback(IntPtr userdata, IntPtr stream, int len)
    {
        // Limpiar buffer con silencio
        unsafe
        {
            var ptr = (byte*)stream;
            for (int i = 0; i < len; i++)
            {
                ptr[i] = 0;
            }
        }

        lock (_lock)
        {
            // Mezclar música y voz
            MixMusicToStream(stream, len);
            MixVoiceToStream(stream, len);
        }
    }

    private void MixMusicToStream(IntPtr stream, int len)
    {
        if (!_musicPlaying || _currentMusicData == null)
            return;

        var bytesToCopy = Math.Min(len, _currentMusicData.Length - _musicPosition);
        if (bytesToCopy <= 0)
        {
            if (_musicLooping)
            {
                _musicPosition = 0;
                bytesToCopy = Math.Min(len, _currentMusicData.Length);
            }
            else
            {
                _musicPlaying = false;
                return;
            }
        }

        // Reducir volumen de música si hay voz (talkover)
        var musicVol = _musicVolume * _masterVolume;
        if (_voicePlaying)
        {
            musicVol *= 0.3f; // Reducir al 30% cuando hay voz
        }

        unsafe
        {
            var outPtr = (short*)stream;
            for (int i = 0; i < bytesToCopy; i += 2)
            {
                if (_musicPosition + i + 1 >= _currentMusicData.Length)
                    break;

                short sample = (short)(_currentMusicData[_musicPosition + i] |
                                       (_currentMusicData[_musicPosition + i + 1] << 8));
                sample = (short)(sample * musicVol);

                // Mezclar (añadir al stream existente)
                int index = i / 2;
                int mixed = outPtr[index] + sample;
                outPtr[index] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
            }
        }

        _musicPosition += bytesToCopy;
    }

    private void MixVoiceToStream(IntPtr stream, int len)
    {
        if (!_voicePlaying || _currentVoiceData == null)
            return;

        var bytesToCopy = Math.Min(len, _currentVoiceData.Length - _voicePosition);
        if (bytesToCopy <= 0)
        {
            _voicePlaying = false;
            _currentVoiceData = null;
            _voicePosition = 0;
            return;
        }

        var voiceVol = _voiceVolume * _masterVolume;

        unsafe
        {
            var outPtr = (short*)stream;
            for (int i = 0; i < bytesToCopy; i += 2)
            {
                if (_voicePosition + i + 1 >= _currentVoiceData.Length)
                    break;

                short sample = (short)(_currentVoiceData[_voicePosition + i] |
                                       (_currentVoiceData[_voicePosition + i + 1] << 8));
                sample = (short)(sample * voiceVol);

                // Mezclar (añadir al stream existente)
                int index = i / 2;
                int mixed = outPtr[index] + sample;
                outPtr[index] = (short)Math.Clamp(mixed, short.MinValue, short.MaxValue);
            }
        }

        _voicePosition += bytesToCopy;
    }

    public void PlayMusic(string filePath, bool loop = true)
    {
        if (!_initialized || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        try
        {
            // Cargar archivo WAV
            var audioData = LoadWavFile(filePath);
            if (audioData == null)
                return;

            lock (_lock)
            {
                _currentMusicData = audioData;
                _musicPosition = 0;
                _musicLooping = loop;
                _musicPlaying = true;
            }
        }
        catch
        {
            // Ignorar errores de reproducción
        }
    }

    public void StopMusic()
    {
        lock (_lock)
        {
            _musicPlaying = false;
            _currentMusicData = null;
            _musicPosition = 0;
        }
    }

    public void SetMusicVolume(float volume)
    {
        _musicVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void SetMasterVolume(float volume)
    {
        _masterVolume = Math.Clamp(volume, 0f, 1f);
    }

    public void SetVoiceVolume(float volume)
    {
        _voiceVolume = Math.Clamp(volume, 0f, 1f);
    }

    /// <summary>
    /// Reproduce un archivo WAV como voz (TTS).
    /// </summary>
    public void PlayVoice(string filePath)
    {
        if (!_initialized || string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            return;

        try
        {
            var audioData = LoadWavFile(filePath);
            if (audioData == null)
                return;

            lock (_lock)
            {
                _currentVoiceData = audioData;
                _voicePosition = 0;
                _voicePlaying = true;
            }
        }
        catch
        {
            // Ignorar errores de reproducción
        }
    }

    /// <summary>
    /// Reproduce datos WAV directamente desde memoria.
    /// </summary>
    public void PlayVoiceFromBytes(byte[] wavData)
    {
        if (!_initialized || wavData == null || wavData.Length == 0)
            return;

        try
        {
            // Convertir WAV a PCM raw
            var audioData = ExtractPcmFromWav(wavData);
            if (audioData == null)
                return;

            lock (_lock)
            {
                _currentVoiceData = audioData;
                _voicePosition = 0;
                _voicePlaying = true;
            }
        }
        catch
        {
            // Ignorar errores de reproducción
        }
    }

    public void StopVoice()
    {
        lock (_lock)
        {
            _voicePlaying = false;
            _currentVoiceData = null;
            _voicePosition = 0;
        }
    }

    public bool IsVoicePlaying
    {
        get
        {
            lock (_lock)
            {
                return _voicePlaying;
            }
        }
    }

    private byte[]? ExtractPcmFromWav(byte[] wavData)
    {
        try
        {
            // Verificar header WAV (RIFF....WAVEfmt )
            if (wavData.Length < 44)
                return wavData; // Demasiado pequeño, asumir raw PCM

            // Verificar "RIFF" al inicio
            if (wavData[0] != 'R' || wavData[1] != 'I' || wavData[2] != 'F' || wavData[3] != 'F')
                return wavData; // No es WAV, asumir raw PCM

            // Verificar "WAVE" en posición 8
            if (wavData[8] != 'W' || wavData[9] != 'A' || wavData[10] != 'V' || wavData[11] != 'E')
                return wavData;

            // Buscar el chunk "data"
            int pos = 12;
            while (pos < wavData.Length - 8)
            {
                var chunkId = System.Text.Encoding.ASCII.GetString(wavData, pos, 4);
                var chunkSize = BitConverter.ToInt32(wavData, pos + 4);

                if (chunkId == "data")
                {
                    // Encontrado el chunk de datos
                    var dataStart = pos + 8;
                    var dataLength = Math.Min(chunkSize, wavData.Length - dataStart);
                    var pcmData = new byte[dataLength];
                    Array.Copy(wavData, dataStart, pcmData, 0, dataLength);
                    return pcmData;
                }

                pos += 8 + chunkSize;
                // Alinear a 2 bytes (WAV usa alineación de 2 bytes para chunks)
                if (chunkSize % 2 != 0)
                    pos++;
            }

            // No se encontró chunk data, devolver todo después del header
            if (wavData.Length > 44)
            {
                var pcmData = new byte[wavData.Length - 44];
                Array.Copy(wavData, 44, pcmData, 0, pcmData.Length);
                return pcmData;
            }

            return wavData;
        }
        catch
        {
            return wavData;
        }
    }

    private byte[]? LoadWavFile(string filePath)
    {
        try
        {
            // Intentar cargar WAV usando SDL2
            IntPtr audioBuffer;
            uint audioLength;
            var spec = new SDL_AudioSpec();

            var result = SDL_LoadWAV(filePath, ref spec, out audioBuffer, out audioLength);
            if (result == IntPtr.Zero)
            {
                // No es un WAV válido, intentar leer como raw PCM
                return File.ReadAllBytes(filePath);
            }

            // Copiar datos
            var data = new byte[audioLength];
            Marshal.Copy(audioBuffer, data, 0, (int)audioLength);

            // Liberar buffer de SDL
            SDL_FreeWAV(audioBuffer);

            return data;
        }
        catch
        {
            try
            {
                return File.ReadAllBytes(filePath);
            }
            catch
            {
                return null;
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        try
        {
            StopMusic();

            if (_audioDevice != IntPtr.Zero)
            {
                SDL_CloseAudioDevice(_audioDevice);
                _audioDevice = IntPtr.Zero;
            }

            if (_callbackHandle.IsAllocated)
            {
                _callbackHandle.Free();
            }

            SDL_Quit();
        }
        catch
        {
            // Ignorar errores al cerrar
        }
    }

    #region SDL2 P/Invoke

    private const uint SDL_INIT_AUDIO = 0x00000010;
    private const ushort AUDIO_S16LSB = 0x8010;

    [StructLayout(LayoutKind.Sequential)]
    private struct SDL_AudioSpec
    {
        public int freq;
        public ushort format;
        public byte channels;
        public byte silence;
        public ushort samples;
        public ushort padding;
        public uint size;
        public IntPtr callback;
        public IntPtr userdata;
    }

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern int SDL_Init(uint flags);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_Quit();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_GetError();

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr SDL_OpenAudioDevice(
        IntPtr device,
        int iscapture,
        ref SDL_AudioSpec desired,
        ref SDL_AudioSpec obtained,
        int allowed_changes);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_CloseAudioDevice(IntPtr dev);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_PauseAudioDevice(IntPtr dev, int pause_on);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
    private static extern IntPtr SDL_LoadWAV(
        string file,
        ref SDL_AudioSpec spec,
        out IntPtr audio_buf,
        out uint audio_len);

    [DllImport("SDL2", CallingConvention = CallingConvention.Cdecl)]
    private static extern void SDL_FreeWAV(IntPtr audio_buf);

    #endregion
}
