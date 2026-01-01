using System;
using System.Collections.Generic;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine.Models;

/// <summary>
/// Puerta bidireccional entre dos salas. Puede estar abierta o cerrada
/// y opcionalmente tener una cerradura controlada por llaves.
/// </summary>
public class Door
{
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>Nombre visible de la puerta (por ejemplo "Puerta de la cocina").</summary>
    public string Name { get; set; } = "puerta";

    /// <summary>Descripción opcional para el narrador/editor.</summary>
    public string? Description { get; set; } = "una puerta cualquiera";

    /// <summary>Id de la sala A (una de las dos salas que conecta la puerta).</summary>
    public string? RoomIdA { get; set; }

    /// <summary>Id de la sala B (la otra sala que conecta la puerta).</summary>
    public string? RoomIdB { get; set; }

    /// <summary>Indica si actualmente la puerta está abierta.</summary>
    public bool IsOpen { get; set; }

    /// <summary>Indica si la puerta está bloqueada (necesita llave para abrir).</summary>
    public bool IsLocked { get; set; }

    /// <summary>
    /// ID del objeto (tipo Key) necesario para abrir esta puerta.
    /// </summary>
    public string? KeyObjectId { get; set; }

    /// <summary>
    /// Desde qué lado se puede abrir/cerrar la puerta. Por defecto ambos lados.
    /// Afecta a los comandos de abrir/cerrar, no al movimiento una vez abierta.
    /// </summary>
    public DoorOpenSide OpenFromSide { get; set; } = DoorOpenSide.Both;

    /// <summary>Género gramatical (el/la) para mensajes.</summary>
    public GrammaticalGender Gender { get; set; } = GrammaticalGender.Feminine;

    /// <summary>Indica si el nombre es plural (las puertas).</summary>
    public bool IsPlural { get; set; } = false;

    /// <summary>Indica si el género y plural fueron establecidos manualmente (no sobrescribir con IA).</summary>
    public bool GenderAndPluralSetManually { get; set; } = false;

    /// <summary>
    /// Indica si la puerta y sus salidas son visibles para el jugador.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Lista de requisitos de misiones para que la puerta sea visible.
    /// Si hay requisitos, la puerta solo será visible si se cumplen todos.
    /// </summary>
    public List<QuestRequirement> RequiredQuests { get; set; } = new();
}
