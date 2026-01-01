using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;

namespace XiloAdventures.Wpf.Common.Controls;

/// <summary>
/// Control visual de dado D20 con animación de tirada.
/// </summary>
public partial class DiceControl : UserControl
{
    private static readonly Random _random = new();
    private bool _isRolling;
    private bool _isEnabled = true;
    private int? _lastResult;
    private bool _useCriticalColors = true;

    /// <summary>
    /// Evento disparado cuando se completa una tirada.
    /// </summary>
    public event Action<int>? RollCompleted;

    /// <summary>
    /// Evento disparado cuando el usuario hace click para tirar.
    /// </summary>
    public event Action? RollRequested;

    /// <summary>
    /// Indica si el dado está habilitado para tirar.
    /// </summary>
    public bool IsRollEnabled
    {
        get => _isEnabled;
        set
        {
            _isEnabled = value;
            UpdateVisualState();
        }
    }

    /// <summary>
    /// Indica si se usan colores especiales para 20 (crítico) y 1 (fallo).
    /// </summary>
    public bool UseCriticalColors
    {
        get => _useCriticalColors;
        set => _useCriticalColors = value;
    }

    /// <summary>
    /// El último resultado de la tirada (null si no se ha tirado).
    /// </summary>
    public int? LastResult => _lastResult;

    /// <summary>
    /// Indica si el dado está actualmente animándose.
    /// </summary>
    public bool IsRolling => _isRolling;

    public DiceControl()
    {
        InitializeComponent();
        UpdateVisualState();
    }

    /// <summary>
    /// Realiza una tirada del dado con animación.
    /// </summary>
    /// <param name="predeterminedResult">Si se especifica, usa este valor en lugar de uno aleatorio.</param>
    /// <param name="useCriticalColors">Si es false, no usa colores especiales para 20 o 1 (útil para iniciativa).</param>
    /// <returns>El resultado de la tirada (1-20).</returns>
    public async Task<int> RollAsync(int? predeterminedResult = null, bool useCriticalColors = true)
    {
        if (_isRolling) return _lastResult ?? 1;

        _isRolling = true;
        _lastResult = null;

        // Iniciar animación de sacudida
        var shakeStoryboard = (Storyboard)Resources["ShakeAnimation"];
        shakeStoryboard.Begin();

        // Mostrar valores aleatorios durante la animación
        var animationDuration = TimeSpan.FromMilliseconds(1500);
        var startTime = DateTime.Now;
        var updateInterval = 50; // ms

        while (DateTime.Now - startTime < animationDuration)
        {
            var randomValue = _random.Next(1, 21);
            DiceValueText.Text = randomValue.ToString();
            UpdateDiceColor(randomValue, useCriticalColors);
            await Task.Delay(updateInterval);
        }

        // Detener animación de sacudida
        shakeStoryboard.Stop();
        DiceTransform.Angle = 0;

        // Determinar resultado final
        var finalResult = predeterminedResult ?? _random.Next(1, 21);
        _lastResult = finalResult;

        // Mostrar resultado final
        DiceValueText.Text = finalResult.ToString();
        UpdateDiceColor(finalResult, useCriticalColors);

        // Animación de impacto
        var impactStoryboard = (Storyboard)Resources["ImpactAnimation"];
        impactStoryboard.Begin();

        _isRolling = false;

        // Notificar resultado
        RollCompleted?.Invoke(finalResult);

        return finalResult;
    }

    /// <summary>
    /// Muestra un valor específico sin animación.
    /// </summary>
    public void ShowValue(int value)
    {
        _lastResult = value;
        DiceValueText.Text = value.ToString();
        UpdateDiceColor(value);
    }

    /// <summary>
    /// Reinicia el dado al estado inicial.
    /// </summary>
    public void Reset()
    {
        _lastResult = null;
        DiceValueText.Text = "D20";
        UpdateVisualState();
    }

    private void UpdateDiceColor(int value, bool useCriticalColors = true)
    {
        Brush brush;
        if (useCriticalColors)
        {
            brush = value switch
            {
                20 => (Brush)Resources["CriticalDiceColor"],
                1 => (Brush)Resources["FumbleDiceColor"],
                _ => (Brush)Resources["NormalDiceColor"]
            };
        }
        else
        {
            brush = (Brush)Resources["NormalDiceColor"];
        }
        DiceShape.Fill = brush;
    }

    private void UpdateVisualState()
    {
        var glowStoryboard = (Storyboard)Resources["GlowAnimation"];

        // Si hay un resultado, mostrar su color (incluso si está deshabilitado)
        if (_lastResult.HasValue)
        {
            UpdateDiceColor(_lastResult.Value);
            DiceGlowShape.Visibility = Visibility.Collapsed;
            glowStoryboard.Stop();
        }
        else
        {
            DiceShape.Fill = (Brush)Resources["InactiveDiceColor"];

            // Mostrar/ocultar resplandor azul parpadeante
            if (_isEnabled)
            {
                DiceGlowShape.Visibility = Visibility.Visible;
                glowStoryboard.Begin();
            }
            else
            {
                DiceGlowShape.Visibility = Visibility.Collapsed;
                glowStoryboard.Stop();
            }
        }

        Cursor = _isEnabled && !_lastResult.HasValue ? Cursors.Hand : Cursors.Arrow;
    }

    private async void UserControl_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (!_isEnabled || _isRolling) return;

        // Ocultar resplandor inmediatamente al hacer click
        var glowStoryboard = (Storyboard)Resources["GlowAnimation"];
        DiceGlowShape.Visibility = Visibility.Collapsed;
        glowStoryboard.Stop();

        RollRequested?.Invoke();
        await RollAsync(useCriticalColors: _useCriticalColors);
    }
}
