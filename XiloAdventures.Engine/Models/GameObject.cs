using System.Collections.Generic;
using System.ComponentModel;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Representa un objeto interactivo en el mundo del juego.
/// </summary>
public class GameObject
{
    /// <summary>
    /// Identificador único del objeto.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Nombre del objeto que se muestra al jugador.
    /// </summary>
    public string Name { get; set; } = "Objeto sin nombre";

    /// <summary>
    /// Descripción detallada del objeto.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Tipo del objeto que determina su comportamiento.
    /// </summary>
    public ObjectType Type { get; set; } = ObjectType.Ninguno;

    /// <summary>
    /// Indica si el objeto se puede leer (libro, carta, pergamino, cartel, etc.).
    /// </summary>
    public bool CanRead { get; set; } = false;

    /// <summary>
    /// Contenido de texto legible (solo si CanRead es true).
    /// Se muestra al usar el comando "leer" sobre el objeto.
    /// </summary>
    public string? TextContent { get; set; }

    /// <summary>
    /// Género gramatical del objeto (para artículos: el/la).
    /// </summary>
    public GrammaticalGender Gender { get; set; } = GrammaticalGender.Masculine;

    /// <summary>
    /// Si el nombre del objeto es plural (para artículos: los/las).
    /// </summary>
    public bool IsPlural { get; set; } = false;

    /// <summary>
    /// Indica si el género y plural fueron establecidos manualmente (no sobrescribir con IA).
    /// </summary>
    public bool GenderAndPluralSetManually { get; set; } = false;

    /// <summary>
    /// Indica si el jugador puede coger el objeto.
    /// </summary>
    public bool CanTake { get; set; }

    #region Container Properties

    /// <summary>
    /// Indica si el objeto es un contenedor.
    /// </summary>
    public bool IsContainer { get; set; }

    /// <summary>
    /// IDs de los objetos contenidos dentro de este contenedor.
    /// </summary>
    public List<string> ContainedObjectIds { get; set; } = new();

    /// <summary>
    /// Si el contenedor se puede abrir/cerrar.
    /// </summary>
    public bool IsOpenable { get; set; }

    /// <summary>
    /// Estado actual del contenedor (abierto/cerrado).
    /// </summary>
    public bool IsOpen { get; set; } = true;

    /// <summary>
    /// Si el contenedor está bloqueado.
    /// </summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// ID del objeto (tipo Llave) necesario para abrir este contenedor.
    /// </summary>
    public string? KeyId { get; set; }

    /// <summary>
    /// Si el contenido es visible sin abrir (ej: estante vs cofre).
    /// </summary>
    public bool ContentsVisible { get; set; }

    /// <summary>
    /// Capacidad máxima del contenedor en centímetros cúbicos (cm³). -1 = ilimitado.
    /// </summary>
    public double MaxCapacity { get; set; } = -1;

    #endregion

    #region Physical Properties

    /// <summary>
    /// Volumen del objeto en centímetros cúbicos (cm³).
    /// </summary>
    public double Volume { get; set; } = 0;

    /// <summary>
    /// Peso del objeto en gramos.
    /// </summary>
    public int Weight { get; set; } = 0;

    /// <summary>
    /// Precio del objeto en monedas.
    /// </summary>
    public int Price { get; set; } = 0;

    #endregion

    #region Combat Statistics

    /// <summary>
    /// Modificador de ataque (daño adicional). Solo para tipo Arma.
    /// </summary>
    public int AttackBonus { get; set; } = 0;

    /// <summary>
    /// Manos requeridas para usar el arma (1 o 2). Solo para tipo Arma.
    /// Un arma de 2 manos ocupa ambos slots de mano (derecha e izquierda).
    /// </summary>
    public int HandsRequired { get; set; } = 1;

    /// <summary>
    /// Modificador de defensa (reducción de daño). Solo para tipo Armadura.
    /// </summary>
    public int DefenseBonus { get; set; } = 0;

    /// <summary>
    /// Durabilidad máxima del objeto. -1 = indestructible.
    /// </summary>
    public int MaxDurability { get; set; } = -1;

    /// <summary>
    /// Durabilidad actual. Cuando llega a 0, el objeto se rompe.
    /// </summary>
    public int CurrentDurability { get; set; } = -1;

    /// <summary>
    /// Modificador de iniciativa (afecta orden de turnos en combate).
    /// </summary>
    public int InitiativeBonus { get; set; } = 0;

    /// <summary>
    /// Tipo de daño para armas (Físico, Mágico, Perforante).
    /// </summary>
    public DamageType DamageType { get; set; } = DamageType.Physical;

    #endregion

    /// <summary>
    /// Sala inicial donde se encuentra el objeto.
    /// </summary>
    public string? RoomId { get; set; }

    /// <summary>
    /// Controla si el jugador puede ver / interactuar con el objeto en la sala.
    /// </summary>
    public bool Visible { get; set; } = true;

    #region Consumable Properties

    /// <summary>
    /// Cantidad de puntos que reduce de la necesidad correspondiente (hambre para comida, sed para bebida).
    /// Solo aplica a objetos de tipo Comida o Bebida.
    /// </summary>
    public int NutritionAmount { get; set; } = 10;

    #endregion

    #region Light Source Properties

    /// <summary>
    /// Indica si el objeto es una fuente de luz (vela, antorcha, lámpara, etc.).
    /// </summary>
    public bool IsLightSource { get; set; }

    /// <summary>
    /// Indica si el objeto luminoso está actualmente encendido.
    /// </summary>
    public bool IsLit { get; set; }

    /// <summary>
    /// Turnos de luminosidad restantes. -1 = infinito.
    /// </summary>
    public int LightTurnsRemaining { get; set; } = -1;

    /// <summary>
    /// Si el objeto luminoso se puede apagar.
    /// </summary>
    public bool CanExtinguish { get; set; }

    /// <summary>
    /// Si el objeto luminoso se puede encender.
    /// </summary>
    public bool CanIgnite { get; set; }

    /// <summary>
    /// ID del objeto necesario para encender este objeto luminoso.
    /// Si es null o vacío, se puede encender sin ningún objeto.
    /// </summary>
    public string? IgniterObjectId { get; set; }

    #endregion

    #region Crafting Properties

    /// <summary>
    /// Lista de ingredientes necesarios para fabricar este objeto.
    /// Si está vacía, el objeto no se puede fabricar.
    /// </summary>
    public List<CraftingIngredient> CraftingRecipe { get; set; } = new();

    #endregion
}
