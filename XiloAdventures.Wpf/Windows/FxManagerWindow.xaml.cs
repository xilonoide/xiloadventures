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

public partial class FxManagerWindow : Window
{
    private readonly WorldModel _world;
    private readonly ObservableCollection<FxAssetViewModel> _fxItems = new();
    private WaveOutEvent? _currentPlayer;
    private WaveStream? _currentReader;

    public FxManagerWindow(WorldModel world)
    {
        InitializeComponent();

        _world = world;

        FxListView.ItemsSource = _fxItems;

        LoadFxList();

        Closing += (s, e) => StopCurrentFx();
    }

    private void LoadFxList()
    {
        _fxItems.Clear();

        foreach (var fx in _world.Fxs)
        {
            _fxItems.Add(new FxAssetViewModel(fx));
        }

        UpdateEmptyMessage();
    }

    private void UpdateEmptyMessage()
    {
        if (_fxItems.Count == 0)
        {
            EmptyMessageText.Visibility = Visibility.Visible;
            FxListView.Visibility = Visibility.Collapsed;
        }
        else
        {
            EmptyMessageText.Visibility = Visibility.Collapsed;
            FxListView.Visibility = Visibility.Visible;
        }
    }

    private void AddFxButton_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Audio (*.mp3;*.wav)|*.mp3;*.wav|Todos los archivos (*.*)|*.*",
            InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic),
            Title = "Seleccionar archivos de efectos de sonido",
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

                    // Verificar si ya existe un FX con el mismo nombre
                    if (_world.Fxs.Any(m => m.Id.Equals(fileName, StringComparison.OrdinalIgnoreCase)))
                    {
                        errors.Add($"{fileName}: Ya existe un efecto con este nombre");
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

                    var fxAsset = new FxAsset
                    {
                        Id = fileName,
                        Base64 = base64,
                        SizeBytes = fileInfo.Length,
                        DurationSeconds = duration
                    };

                    _world.Fxs.Add(fxAsset);
                    _fxItems.Add(new FxAssetViewModel(fxAsset));
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

    private void PlayFxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not FxAssetViewModel viewModel)
            return;

        try
        {
            // Detener cualquier reproducción actual
            StopCurrentFx();

            // Obtener el FxAsset
            var fxAsset = _world.Fxs.FirstOrDefault(m => m.Id == viewModel.Id);
            if (fxAsset == null || string.IsNullOrEmpty(fxAsset.Base64))
                return;

            // Convertir Base64 a bytes y crear stream
            var bytes = Convert.FromBase64String(fxAsset.Base64);
            var stream = new MemoryStream(bytes);

            // Crear reader según tipo de archivo
            var extension = Path.GetExtension(fxAsset.Id).ToLowerInvariant();
            if (extension == ".mp3")
            {
                _currentReader = new Mp3FileReader(stream);
            }
            else if (extension == ".wav")
            {
                _currentReader = new WaveFileReader(stream);
            }
            else
            {
                stream.Dispose();
                throw new NotSupportedException($"Formato de archivo no soportado: {extension}");
            }

            // Crear player
            _currentPlayer = new WaveOutEvent();
            _currentPlayer.Init(_currentReader);
            _currentPlayer.Play();

            // Cuando termine de reproducir, limpiar
            _currentPlayer.PlaybackStopped += OnPlaybackStopped;
        }
        catch (Exception ex)
        {
            new AlertWindow($"Error al reproducir efecto: {ex.Message}", "Error")
            {
                Owner = this
            }.ShowDialog();
        }
    }

    private void OnPlaybackStopped(object? sender, StoppedEventArgs args)
    {
        StopCurrentFx();
    }

    private void DeleteFxButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button button)
            return;

        if (button.Tag is not FxAssetViewModel viewModel)
            return;

        var confirmWindow = new ConfirmWindow(
            $"¿Estás seguro de que quieres eliminar el efecto '{viewModel.Id}'?\n\n" +
            "Esta acción no se puede deshacer.",
            "Confirmar eliminación")
        {
            Owner = this
        };

        if (confirmWindow.ShowDialog() == true)
        {
            var fxToRemove = _world.Fxs.FirstOrDefault(m => m.Id == viewModel.Id);
            if (fxToRemove != null)
            {
                _world.Fxs.Remove(fxToRemove);
                _fxItems.Remove(viewModel);

                UpdateEmptyMessage();
            }
        }
    }

    private void StopCurrentFx()
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

    private void FxListView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (FxListView.View is not GridView gridView || gridView.Columns.Count == 0)
            return;

        // Calcular el ancho disponible (ancho total - anchos fijos - margen para scrollbar)
        var totalWidth = FxListView.ActualWidth - 20; // 20 para scrollbar y márgenes
        var fixedWidth = 100 + 100 + 50 + 50; // Tamaño + Duración + Play + Eliminar
        var nameColumnWidth = totalWidth - fixedWidth;

        if (nameColumnWidth > 100) // Ancho mínimo
        {
            gridView.Columns[0].Width = nameColumnWidth;
        }
    }
}

/// <summary>
/// ViewModel para mostrar la información de FxAsset en el ListView.
/// </summary>
public class FxAssetViewModel
{
    private readonly FxAsset _fx;

    public FxAssetViewModel(FxAsset fx)
    {
        _fx = fx;
    }

    public string Id => _fx.Id;

    public string SizeFormatted
    {
        get
        {
            var sizeInKb = _fx.SizeBytes / 1024.0;
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
            var totalSeconds = (int)_fx.DurationSeconds;
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return $"{minutes}:{seconds:D2}";
        }
    }
}
