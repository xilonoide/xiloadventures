namespace XiloAdventures.Engine.Models.Enums;

/// <summary>
/// Tipos de objetos del juego que determinan su comportamiento y uso.
/// </summary>
public enum ObjectType
{
    /// <summary>Objeto sin tipo específico.</summary>
    Ninguno,
    /// <summary>Arma equipable para combate.</summary>
    Arma,
    /// <summary>Armadura equipable para defensa.</summary>
    Armadura,
    /// <summary>Alimento consumible que reduce el hambre.</summary>
    Comida,
    /// <summary>Bebida consumible que reduce la sed.</summary>
    Bebida,
    /// <summary>Llave para abrir cerraduras.</summary>
    Llave
}
