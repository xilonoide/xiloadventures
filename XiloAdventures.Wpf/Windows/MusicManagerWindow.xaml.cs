using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using NAudio.Wave;
using XiloAdventures.Engine.Models;
using XiloAdventures.Wpf.Common.Windows;

namespace XiloAdventures.Wpf.Windows;

public partial class MusicManagerWindow : Window
{
    private readonly WorldModel _world;
    private readonly ObservableCollection<MusicAssetViewModel> _musicItems = new();
    private WaveOutEvent? _currentPlayer;
    private Mp3FileReader? _currentReader;

    public MusicManagerWindow(WorldModel world)
    {
        InitializeComponent();

        _world = world;

        MusicListView.ItemsSource = _musicItems;

        LoadMusicList();

        Closing += (s, e) => StopCurrentMusic();
    }

    private void LoadMusicList()
    {
        _musicItems.Clear();

        foreach (var music in _world.Musics)
        {
            _musicItems.Add(new MusicAssetViewModel(music));
        }

        UpdateEmptyMessage();
    }

    private void UpdateEmptyMessage()
    {
        if (_musicItems.Count == 0)
        {
            EmptyMessageText.Visibility = Visibility.Visible;
            MusicListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyMessageText.Visibility = Visibility.Collapsed;
            MusicListView.Visibility = Visibility.Visible;
        }
    }

    private void AddMusicButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio (*.mp3)|*.mp3|Todos los archivos (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Title = "Seleccionar archivos de música",
            Multiselect = true
        };

        if (dlg.ShowDialog() == true)
        {
            const long MaxAudioBytes = 20L * 1024 * 1024; // 20 MB
            var addedCount = 0;
            var errors = new List<string>();

            foreach (var filePath in dlg.FileNames)
            {
                try
                {
                    var fileInfo = new FileInfo(filePath);

                    if (fileInfo.Length > MaxAudioBytes)
                    {
                        errors.Add($"{Path.GetFileName(filePath)}: Archivo demasiado grande ({fileInfo.Length / (1024 * 1024)} MB)");
                        continue;
                    }

                    var fileName = Path.GetFileName(filePath);

                    // Verificar si ya existe una música con el mismo nombre
                    if (_world.Musics.Any(m => m.Id.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"{fileName}: Ya existe una música con este nombre");
                        continue;
                    }

                    var bytes = File.ReadAllBytes(filePath);
                    var base64 = Convert.ToBase64String(bytes);

                    // Obtener duración usando NAudio
                    double duration = 0;
                    try
                    {
                        using var reader = new AudioFileReader(filePath);
                        duration = reader.TotalTime.TotalSeconds;
                    }
                    catch
                    {
                        // Si no se puede leer la duración, se deja en 0
                    }

                    var musicAsset = new MusicAsset
                    {
                        Id = fileName,
                        Base64 = base64,
                        SizeBytes = fileInfo.Length,
                        DurationSeconds = duration
                    };

                    _world.Musics.Add(musicAsset);
                    _musicItems.Add(new MusicAssetViewModel(musicAsset));
                    addedCount++;
                }
                catch (Exception ex)
                {
                    errors.Add($"{Path.GetFileName(filePath)}: {ex.Message}");
                }
            }

            UpdateEmptyMessage();

            // Mostrar resumen si hubo errores
            if (errors.Count > 0)
            {
                var message = $"Se añadieron {addedCount} archivo(s) correctamente.\n\nErrores:\n" + string.Join("\n", errors);
                new AlertWindow(message, "Resultado")
                {
                    Owner = this
                }.ShowDialog();
            }
        }
    }

    private void PlayMusicButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not MusicAssetViewModel viewModel)
            return;

        try
        {
            // Detener cualquier reproducción actual
            StopCurrentMusic();

            // Obtener el MusicAsset
            var musicAsset = _world.Musics.FirstOrDefault(m => m.Id == viewModel.Id);
            if (musicAsset == null || string.IsNullOrEmpty(musicAsset.Base64))
                return;

            // Convertir Base64 a bytes y crear stream
            var bytes = Convert.FromBase64String(musicAsset.Base64);
            var stream = new MemoryStream(bytes);

            // Crear reader y player
            _currentReader = new Mp3FileReader(stream);
            _currentPlayer = new WaveOutEvent();
            _currentPlayer.Init(_currentReader);
            _currentPlayer.Play();

            // Cuando termine de reproducir, limpiar
            _currentPlayer.PlaybackStopped += OnPlaybackStopped;
        }
        catch (Exception ex)
        {
            new AlertWindow($"Error al reproducir música: {ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        StopCurrentMusic();
    }

    private void DeleteMusicButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not MusicAssetViewModel viewModel)
            return;

        var confirmWindow = new ConfirmWindow(
            $"¿Estás seguro de que quieres eliminar la música '{viewModel.Id}'?\n\n" +
            "Esta acción no se puede deshacer. Las salas que usen esta música dejarán de reproducirla.",
            "Confirmar eliminación")
        {
            Owner = this
        };

        if (confirmWindow.ShowDialog() == true)
        {
            var musicToRemove = _world.Musics.FirstOrDefault(m => m.Id == viewModel.Id);
            if (musicToRemove != null)
            {
                _world.Musics.Remove(musicToRemove);
                _musicItems.Remove(viewModel);

                UpdateEmptyMessage();
            }
        }
    }

    private void StopCurrentMusic()
    {
        if (_currentPlayer != null)
        {
            // Desuscribir el evento antes de detener para evitar llamadas recursivas
            _currentPlayer.PlaybackStopped -= OnPlaybackStopped;
            _currentPlayer.Stop();
            _currentPlayer.Dispose();
            _currentPlayer = null;
        }

        if (_currentReader != null)
        {
            _currentReader.Dispose();
            _currentReader = null;
        }
    }

    private void MusicListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (MusicListView.View is not GridView gridView || gridView.Columns.Count == 0)
            return;

        // Calcular el ancho disponible (ancho total - anchos fijos - margen para scrollbar)
        var totalWidth = MusicListView.ActualWidth - 20; // 20 para scrollbar y márgenes
        var fixedWidth = 100 + 100 + 50 + 50; // Tamaño + Duración + Play + Eliminar
        var nameColumnWidth = totalWidth - fixedWidth;

        if (nameColumnWidth > 100) // Ancho mínimo
        {
            gridView.Columns[0].Width = nameColumnWidth;
        }
    }
}

/// <summary>
/// ViewModel para mostrar la información de MusicAsset en el ListView.
/// </summary>
public class MusicAssetViewModel
{
    private readonly MusicAsset _music;

    public MusicAssetViewModel(MusicAsset music)
    {
        _music = music;
    }

    public string Id => _music.Id;

    public string SizeFormatted
    {
        get
        {
            var sizeInKb = _music.SizeBytes / 1024.0;
            if (sizeInKb < 1024)
                return $"{sizeInKb:F1} KB";

            var sizeInMb = sizeInKb / 1024.0;
            return $"{sizeInMb:F1} MB";
        }
    }

    public string DurationFormatted
    {
        get
        {
            var totalSeconds = (int)_music.DurationSeconds;
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }
    }
}
