namespace XiloAdventures.Engine.Models;

/// <summary>
/// Posición de una sala en el mapa del editor visual.
/// Almacena las coordenadas 2D para la representación gráfica del mundo.
/// </summary>
public class MapPosition
{
    /// <summary>
    /// Coordenada X (horizontal) en el mapa del editor.
    /// Valores más altos mueven la sala hacia la derecha.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Coordenada Y (vertical) en el mapa del editor.
    /// Valores más altos mueven la sala hacia abajo.
    /// </summary>
    public double Y { get; set; }
}
