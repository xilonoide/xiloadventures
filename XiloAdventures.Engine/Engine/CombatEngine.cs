using System;
using System.Linq;
using XiloAdventures.Engine.Interfaces;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Engine;

/// <summary>
/// Motor de combate por turnos con sistema de dados D20.
/// </summary>
public class CombatEngine : ICombatEngine
{
    private readonly GameState _state;
    private readonly Random _random = new();

    /// <summary>
    /// Evento que se dispara cuando el combate termina.
    /// </summary>
    public event EventHandler<CombatEndEventArgs>? CombatEnded;

    /// <summary>
    /// Evento que se dispara cuando hay una entrada nueva en el log.
    /// </summary>
    public event EventHandler<CombatLogEntry>? LogEntryAdded;

    public CombatEngine(GameState state)
    {
        _state = state;
    }

    /// <summary>
    /// Inicia un combate con el NPC especificado.
    /// </summary>
    public CombatState StartCombat(string npcId)
    {
        var npc = _state.Npcs.FirstOrDefault(n => n.Id == npcId);
        if (npc == null)
            throw new ArgumentException($"NPC no encontrado: {npcId}");

        if (npc.IsCorpse)
            throw new InvalidOperationException($"{npc.Name} ya está muerto.");

        var combat = new CombatState
        {
            IsActive = true,
            EnemyNpcId = npcId,
            Phase = CombatPhase.Initiative,
            RoundNumber = 1
        };

        _state.ActiveCombat = combat;

        AddLogEntry(combat, $"¡Comienza el combate contra {npc.Name}!", false, CombatLogType.System);
        AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);

        return combat;
    }

    /// <summary>
    /// Realiza la tirada de iniciativa del jugador.
    /// </summary>
    /// <param name="diceValue">Valor del dado (1-20). Si es null, se genera aleatoriamente.</param>
    public DiceRollResult RollPlayerInitiative(int? diceValue = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.Initiative)
            throw new InvalidOperationException("No hay combate en fase de iniciativa.");

        var roll = diceValue ?? RollD20();
        var statBonus = _state.Player.Dexterity / 5;
        var equipBonus = GetPlayerInitiativeBonus();

        combat.LastPlayerRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = equipBonus
        };

        AddLogEntry(combat, $"Tu iniciativa: {combat.LastPlayerRoll.Breakdown}", true, CombatLogType.Normal);

        return combat.LastPlayerRoll;
    }

    /// <summary>
    /// Realiza la tirada de iniciativa del NPC.
    /// </summary>
    /// <param name="diceValue">Valor del dado (1-20). Si es null, se genera aleatoriamente.</param>
    public DiceRollResult RollNpcInitiative(int? diceValue = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.Initiative)
            throw new InvalidOperationException("No hay combate en fase de iniciativa.");

        var npc = GetCurrentEnemy();
        var roll = diceValue ?? RollD20();
        var statBonus = npc.Stats.Dexterity / 5;

        combat.LastNpcRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = 0
        };

        AddLogEntry(combat, $"Iniciativa de {npc.Name}: {combat.LastNpcRoll.Breakdown}", false, CombatLogType.Normal);

        return combat.LastNpcRoll;
    }

    /// <summary>
    /// Resuelve la iniciativa y determina quién actúa primero.
    /// </summary>
    public bool ResolveInitiative()
    {
        var combat = _state.ActiveCombat;
        if (combat?.LastPlayerRoll == null || combat.LastNpcRoll == null)
            throw new InvalidOperationException("Faltan tiradas de iniciativa.");

        // El jugador gana empates
        combat.IsPlayerTurn = combat.LastPlayerRoll.Total >= combat.LastNpcRoll.Total;
        combat.Phase = combat.IsPlayerTurn ? CombatPhase.PlayerAction : CombatPhase.NpcAction;

        var npc = GetCurrentEnemy();
        if (combat.IsPlayerTurn)
            AddLogEntry(combat, "¡Ganas la iniciativa! Tu turno.", true, CombatLogType.System);
        else
            AddLogEntry(combat, $"{npc.Name} gana la iniciativa.", false, CombatLogType.System);

        return combat.IsPlayerTurn;
    }

    /// <summary>
    /// Establece la acción del jugador para este turno.
    /// </summary>
    public void SetPlayerAction(CombatAction action, string? itemOrAbilityId = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.PlayerAction)
            throw new InvalidOperationException("No es el turno del jugador.");

        combat.PlayerAction = action;
        combat.SelectedItemId = action == CombatAction.UseItem ? itemOrAbilityId : null;
        combat.SelectedAbilityId = action == CombatAction.UseAbility ? itemOrAbilityId : null;
        combat.PlayerDefending = action == CombatAction.Defend;

        if (action == CombatAction.Defend)
        {
            AddLogEntry(combat, "Adoptas una postura defensiva (+5 defensa).", true, CombatLogType.Normal);
            // Pasar al turno del NPC
            combat.Phase = CombatPhase.NpcAction;
        }
        else if (action == CombatAction.Flee)
        {
            combat.Phase = CombatPhase.PlayerRoll;
        }
        else
        {
            combat.Phase = CombatPhase.PlayerRoll;
        }
    }

    /// <summary>
    /// Ejecuta la tirada de ataque del jugador.
    /// </summary>
    /// <param name="playerAttackDice">Valor del dado de ataque del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="npcDefenseDice">Valor del dado de defensa del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecutePlayerAttack(int? playerAttackDice = null, int? npcDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.PlayerRoll)
            throw new InvalidOperationException("No es momento de tirar dados.");

        var npc = GetCurrentEnemy();

        // Tirada de ataque del jugador
        var attackRoll = RollAttack(true, playerAttackDice);
        combat.LastPlayerRoll = attackRoll;

        // Tirada de defensa del NPC
        var defenseRoll = RollDefense(false, npcDefenseDice);
        combat.LastNpcRoll = defenseRoll;

        // Calcular daño
        var result = CalculateDamage(attackRoll, defenseRoll, true);

        // Log
        AddLogEntry(combat, $"Tu ataque: {attackRoll.Breakdown}", true, CombatLogType.Normal);
        AddLogEntry(combat, $"Defensa de {npc.Name}: {defenseRoll.Breakdown}", false, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, "¡Fallo épico! Tu ataque falla completamente.", true, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡GOLPE CRÍTICO! Causas {result.FinalDamage} de daño.", true, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"¡Impacto! Causas {result.FinalDamage} de daño.", true, CombatLogType.Hit);

            // Aplicar daño al NPC
            npc.Stats.CurrentHealth -= result.FinalDamage;
            if (npc.Stats.CurrentHealth < 0) npc.Stats.CurrentHealth = 0;

            // Desgastar arma
            WearWeapon();
        }
        else
        {
            AddLogEntry(combat, $"{npc.Name} bloquea tu ataque.", true, CombatLogType.Miss);
        }

        // Desgastar armadura del NPC (no aplica, NPCs no tienen armadura con durabilidad)

        // Verificar victoria
        if (npc.Stats.CurrentHealth <= 0)
        {
            combat.Phase = CombatPhase.Victory;
            HandleVictory();
        }
        else
        {
            combat.Phase = CombatPhase.NpcAction;
        }

        return result;
    }

    /// <summary>
    /// Intenta huir del combate.
    /// </summary>
    public bool AttemptFlee()
    {
        var combat = _state.ActiveCombat;
        if (combat == null)
            throw new InvalidOperationException("No hay combate activo.");

        // Probabilidad base 50% + Destreza/10
        var chance = 50 + (_state.Player.Dexterity / 10);
        var roll = _random.Next(1, 101);
        var success = roll <= chance;

        if (success)
        {
            AddLogEntry(combat, $"¡Huyes del combate! (tirada: {roll}, necesitabas ≤{chance})", true, CombatLogType.Fled);
            EndCombat(CombatEndReason.Fled);
        }
        else
        {
            AddLogEntry(combat, $"¡No consigues huir! (tirada: {roll}, necesitabas ≤{chance})", true, CombatLogType.Miss);
            // El NPC ataca con ventaja
            combat.Phase = CombatPhase.NpcAction;
        }

        return success;
    }

    /// <summary>
    /// Ejecuta el turno del NPC (IA simple: siempre ataca).
    /// </summary>
    /// <param name="npcAttackDice">Valor del dado de ataque del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="playerDefenseDice">Valor del dado de defensa del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecuteNpcTurn(int? npcAttackDice = null, int? playerDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.NpcAction)
            throw new InvalidOperationException("No es el turno del NPC.");

        var npc = GetCurrentEnemy();

        // IA simple: huir si salud < 20%, sino atacar
        if (npc.Stats.CurrentHealth < npc.Stats.MaxHealth * 0.2)
        {
            // El NPC intenta huir
            var fleeChance = 30 + (npc.Stats.Dexterity / 10);
            if (_random.Next(1, 101) <= fleeChance)
            {
                AddLogEntry(combat, $"{npc.Name} huye del combate.", false, CombatLogType.Fled);
                EndCombat(CombatEndReason.EnemyFled);
                return new DamageResult();
            }
        }

        // Tirada de ataque del NPC
        var attackRoll = RollAttack(false, npcAttackDice);
        combat.LastNpcRoll = attackRoll;

        // Tirada de defensa del jugador
        var defenseRoll = RollDefense(true, playerDefenseDice);
        combat.LastPlayerRoll = defenseRoll;

        // Calcular daño
        var result = CalculateDamage(attackRoll, defenseRoll, false);

        // Log
        AddLogEntry(combat, $"Ataque de {npc.Name}: {attackRoll.Breakdown}", false, CombatLogType.Normal);
        AddLogEntry(combat, $"Tu defensa: {defenseRoll.Breakdown}", true, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, $"{npc.Name} falla estrepitosamente.", false, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡{npc.Name} te golpea críticamente! Recibes {result.FinalDamage} de daño.", false, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"{npc.Name} te golpea. Recibes {result.FinalDamage} de daño.", false, CombatLogType.Hit);

            // Aplicar daño al jugador
            _state.Player.DynamicStats.Health -= result.FinalDamage;
            if (_state.Player.DynamicStats.Health < 0) _state.Player.DynamicStats.Health = 0;

            // Desgastar armadura del jugador
            WearArmor();
        }
        else
        {
            AddLogEntry(combat, "Bloqueas el ataque.", true, CombatLogType.Miss);
        }

        // Verificar derrota
        if (_state.Player.DynamicStats.Health <= 0)
        {
            combat.Phase = CombatPhase.Defeat;
            HandleDefeat();
        }
        else
        {
            // Siguiente ronda
            combat.RoundNumber++;
            combat.PlayerDefending = false;
            combat.Phase = CombatPhase.PlayerAction;
            AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);
        }

        return result;
    }

    /// <summary>
    /// Usa un objeto durante el combate.
    /// </summary>
    public bool UseItem(string objectId)
    {
        var combat = _state.ActiveCombat;
        if (combat == null)
            throw new InvalidOperationException("No hay combate activo.");

        var obj = _state.Objects.FirstOrDefault(o => o.Id == objectId);
        if (obj == null || !_state.InventoryObjectIds.Contains(objectId))
        {
            AddLogEntry(combat, "No tienes ese objeto.", true, CombatLogType.Normal);
            return false;
        }

        // Lógica básica de uso de objetos en combate
        // La lógica real se implementará en GameEngine con scripts
        AddLogEntry(combat, $"Usas {obj.Name}.", true, CombatLogType.Normal);

        // El NPC ataca después
        combat.Phase = CombatPhase.NpcAction;
        return true;
    }

    /// <summary>
    /// Ejecuta un ataque mágico con una habilidad.
    /// </summary>
    /// <param name="ability">La habilidad de ataque a usar.</param>
    /// <param name="playerAttackDice">Valor del dado de ataque del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="npcDefenseDice">Valor del dado de defensa del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecuteMagicAttack(CombatAbility ability, int? playerAttackDice = null, int? npcDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.PlayerRoll)
            throw new InvalidOperationException("No es momento de tirar dados.");

        if (ability.AbilityType != AbilityType.Attack)
            throw new InvalidOperationException("La habilidad no es de tipo ataque.");

        // Verificar mana
        if (_state.Player.DynamicStats.Mana < ability.ManaCost)
            throw new InvalidOperationException("No tienes suficiente mana.");

        // Consumir mana
        _state.Player.DynamicStats.Mana -= ability.ManaCost;

        var npc = GetCurrentEnemy();

        // Tirada de ataque mágico: D20 + Inteligencia/5 + AbilityAttackValue
        var roll = playerAttackDice ?? RollD20();
        var statBonus = _state.Player.Intelligence / 5;
        var attackRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = ability.AttackValue,
            AdditionalBonus = 0
        };
        combat.LastPlayerRoll = attackRoll;

        // Tirada de defensa del NPC
        var defenseRoll = RollDefense(false, npcDefenseDice);
        combat.LastNpcRoll = defenseRoll;

        // Calcular daño
        var result = CalculateMagicDamage(attackRoll, defenseRoll, ability);

        // Log
        AddLogEntry(combat, $"¡{ability.Name}! {attackRoll.Breakdown}", true, CombatLogType.Normal);
        AddLogEntry(combat, $"Defensa de {npc.Name}: {defenseRoll.Breakdown}", false, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, "¡Fallo mágico! Tu hechizo falla.", true, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡HECHIZO CRÍTICO! Causas {result.FinalDamage} de daño mágico.", true, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"¡Impacto mágico! Causas {result.FinalDamage} de daño.", true, CombatLogType.Hit);

            // Aplicar daño al NPC
            npc.Stats.CurrentHealth -= result.FinalDamage;
            if (npc.Stats.CurrentHealth < 0) npc.Stats.CurrentHealth = 0;

            // Aplicar curación si la habilidad la tiene
            if (ability.Healing > 0)
            {
                var healAmount = ability.Healing;
                _state.Player.DynamicStats.Health = Math.Min(
                    _state.Player.DynamicStats.MaxHealth,
                    _state.Player.DynamicStats.Health + healAmount);
                AddLogEntry(combat, $"Recuperas {healAmount} de salud.", true, CombatLogType.Normal);
            }
        }
        else
        {
            AddLogEntry(combat, $"{npc.Name} resiste tu hechizo.", true, CombatLogType.Miss);
        }

        // Verificar victoria
        if (npc.Stats.CurrentHealth <= 0)
        {
            combat.Phase = CombatPhase.Victory;
            HandleVictory();
        }
        else
        {
            combat.Phase = CombatPhase.NpcAction;
        }

        return result;
    }

    /// <summary>
    /// Ejecuta una defensa mágica contra el ataque del NPC.
    /// </summary>
    /// <param name="ability">La habilidad de defensa a usar.</param>
    /// <param name="npcAttackDice">Valor del dado de ataque del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="playerDefenseDice">Valor del dado de defensa del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecuteMagicDefense(CombatAbility ability, int? npcAttackDice = null, int? playerDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.NpcAction)
            throw new InvalidOperationException("No es el turno del NPC.");

        if (ability.AbilityType != AbilityType.Defense)
            throw new InvalidOperationException("La habilidad no es de tipo defensa.");

        // Verificar mana
        if (_state.Player.DynamicStats.Mana < ability.ManaCost)
            throw new InvalidOperationException("No tienes suficiente mana.");

        // Consumir mana
        _state.Player.DynamicStats.Mana -= ability.ManaCost;

        var npc = GetCurrentEnemy();

        // Tirada de ataque del NPC
        var attackRoll = RollAttack(false, npcAttackDice);
        combat.LastNpcRoll = attackRoll;

        // Tirada de defensa mágica: D20 + Inteligencia/5 + AbilityDefenseValue
        var roll = playerDefenseDice ?? RollD20();
        var statBonus = _state.Player.Intelligence / 5;
        var defenseRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = ability.DefenseValue,
            AdditionalBonus = 0
        };
        combat.LastPlayerRoll = defenseRoll;

        // Calcular daño
        var result = CalculateDamage(attackRoll, defenseRoll, false);

        // Log
        AddLogEntry(combat, $"Ataque de {npc.Name}: {attackRoll.Breakdown}", false, CombatLogType.Normal);
        AddLogEntry(combat, $"¡{ability.Name}! {defenseRoll.Breakdown}", true, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, $"{npc.Name} falla estrepitosamente.", false, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡{npc.Name} te golpea críticamente! Recibes {result.FinalDamage} de daño.", false, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"{npc.Name} te golpea. Recibes {result.FinalDamage} de daño.", false, CombatLogType.Hit);

            // Aplicar daño al jugador
            _state.Player.DynamicStats.Health -= result.FinalDamage;
            if (_state.Player.DynamicStats.Health < 0) _state.Player.DynamicStats.Health = 0;
        }
        else
        {
            AddLogEntry(combat, "¡Tu barrera mágica bloquea el ataque!", true, CombatLogType.Miss);
        }

        // Aplicar curación si la habilidad la tiene
        if (ability.Healing > 0)
        {
            var healAmount = ability.Healing;
            _state.Player.DynamicStats.Health = Math.Min(
                _state.Player.DynamicStats.MaxHealth,
                _state.Player.DynamicStats.Health + healAmount);
            AddLogEntry(combat, $"Tu barrera te cura {healAmount} de salud.", true, CombatLogType.Normal);
        }

        // Verificar derrota
        if (_state.Player.DynamicStats.Health <= 0)
        {
            combat.Phase = CombatPhase.Defeat;
            HandleDefeat();
        }
        else
        {
            // Siguiente ronda
            combat.RoundNumber++;
            combat.PlayerDefending = false;
            combat.Phase = CombatPhase.PlayerAction;
            AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);
        }

        return result;
    }

    /// <summary>
    /// Obtiene las habilidades del jugador desde el estado del juego.
    /// </summary>
    public System.Collections.Generic.List<CombatAbility> GetPlayerAbilities()
    {
        return _state.Abilities
            .Where(a => _state.Player.AbilityIds.Contains(a.Id))
            .ToList();
    }

    /// <summary>
    /// Obtiene las habilidades de ataque del NPC actual (solo si NPC.MagicEnabled es true).
    /// </summary>
    public System.Collections.Generic.List<CombatAbility> GetNpcAbilities()
    {
        var npc = GetCurrentEnemy();
        if (!npc.MagicEnabled)
            return new System.Collections.Generic.List<CombatAbility>();

        return _state.Abilities
            .Where(a => npc.AbilityIds.Contains(a.Id) && a.AbilityType == AbilityType.Attack)
            .ToList();
    }

    /// <summary>
    /// Obtiene la mejor arma del NPC de su inventario (la de mayor AttackBonus).
    /// </summary>
    public GameObject? GetNpcBestWeapon()
    {
        var npc = GetCurrentEnemy();
        return _state.Objects
            .Where(o => npc.Inventory.Any(i => i.ObjectId == o.Id) && o.Type == ObjectType.Arma)
            .OrderByDescending(o => o.AttackBonus)
            .FirstOrDefault();
    }

    /// <summary>
    /// Obtiene la mejor armadura o escudo del NPC (equipado o en inventario, el de mayor DefenseBonus).
    /// </summary>
    public GameObject? GetNpcBestArmor()
    {
        var npc = GetCurrentEnemy();
        var candidates = new List<GameObject>();

        // Objetos equipados
        if (!string.IsNullOrEmpty(npc.EquippedLeftHandId))
        {
            var shield = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedLeftHandId && o.Type == ObjectType.Escudo);
            if (shield != null) candidates.Add(shield);
        }
        if (!string.IsNullOrEmpty(npc.EquippedTorsoId))
        {
            var armor = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedTorsoId && o.Type == ObjectType.Armadura);
            if (armor != null) candidates.Add(armor);
        }
        if (!string.IsNullOrEmpty(npc.EquippedHeadId))
        {
            var helmet = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedHeadId && o.Type == ObjectType.Casco);
            if (helmet != null) candidates.Add(helmet);
        }

        // Objetos en inventario
        candidates.AddRange(_state.Objects
            .Where(o => npc.Inventory.Any(i => i.ObjectId == o.Id) &&
                        (o.Type == ObjectType.Armadura || o.Type == ObjectType.Escudo || o.Type == ObjectType.Casco)));

        return candidates.OrderByDescending(o => o.DefenseBonus).FirstOrDefault();
    }

    /// <summary>
    /// Obtiene el arma mágica del NPC (si tiene alguna en su inventario).
    /// </summary>
    public GameObject? GetNpcMagicWeapon()
    {
        var npc = GetCurrentEnemy();
        return _state.Objects
            .Where(o => npc.Inventory.Any(i => i.ObjectId == o.Id) &&
                        o.Type == ObjectType.Arma &&
                        o.DamageType == DamageType.Magical)
            .OrderByDescending(o => o.AttackBonus)
            .FirstOrDefault();
    }

    /// <summary>
    /// Obtiene la armadura o escudo mágico del NPC (equipado o en inventario).
    /// </summary>
    public GameObject? GetNpcMagicArmor()
    {
        var npc = GetCurrentEnemy();
        var candidates = new List<GameObject>();

        // Objetos equipados
        if (!string.IsNullOrEmpty(npc.EquippedLeftHandId))
        {
            var shield = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedLeftHandId &&
                o.Type == ObjectType.Escudo && o.DamageType == DamageType.Magical);
            if (shield != null) candidates.Add(shield);
        }
        if (!string.IsNullOrEmpty(npc.EquippedTorsoId))
        {
            var armor = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedTorsoId &&
                o.Type == ObjectType.Armadura && o.DamageType == DamageType.Magical);
            if (armor != null) candidates.Add(armor);
        }
        if (!string.IsNullOrEmpty(npc.EquippedHeadId))
        {
            var helmet = _state.Objects.FirstOrDefault(o => o.Id == npc.EquippedHeadId &&
                o.Type == ObjectType.Casco && o.DamageType == DamageType.Magical);
            if (helmet != null) candidates.Add(helmet);
        }

        // Objetos en inventario
        candidates.AddRange(_state.Objects
            .Where(o => npc.Inventory.Any(i => i.ObjectId == o.Id) &&
                        (o.Type == ObjectType.Armadura || o.Type == ObjectType.Escudo || o.Type == ObjectType.Casco) &&
                        o.DamageType == DamageType.Magical));

        return candidates.OrderByDescending(o => o.DefenseBonus).FirstOrDefault();
    }

    /// <summary>
    /// Verifica si el NPC puede usar magia (tiene MagicEnabled, habilidades mágicas o arma mágica).
    /// </summary>
    public bool CanNpcUseMagicAttack()
    {
        var abilities = GetNpcAbilities();
        var magicWeapon = GetNpcMagicWeapon();
        return abilities.Any() || magicWeapon != null;
    }

    /// <summary>
    /// Ejecuta un ataque mágico del NPC usando un arma mágica.
    /// </summary>
    public DamageResult ExecuteNpcMagicWeaponAttack(GameObject magicWeapon, int? npcAttackDice = null, int? playerDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.NpcAction)
            throw new InvalidOperationException("No es el turno del NPC.");

        var npc = GetCurrentEnemy();

        // Tirada de ataque mágico del NPC: D20 + Inteligencia/5 + WeaponAttackBonus
        var roll = npcAttackDice ?? RollD20();
        var statBonus = npc.Stats.Intelligence / 5;
        var attackRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = magicWeapon.AttackBonus,
            AdditionalBonus = 0
        };
        combat.LastNpcRoll = attackRoll;

        // Tirada de defensa del jugador
        var defenseRoll = RollDefense(true, playerDefenseDice);
        combat.LastPlayerRoll = defenseRoll;

        // Calcular daño
        var result = CalculateDamage(attackRoll, defenseRoll, false);

        // Log
        AddLogEntry(combat, $"¡{npc.Name} ataca con {magicWeapon.Name}! {attackRoll.Breakdown}", false, CombatLogType.Normal);
        AddLogEntry(combat, $"Tu defensa: {defenseRoll.Breakdown}", true, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, $"¡El ataque mágico de {npc.Name} falla!", false, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡GOLPE MÁGICO CRÍTICO de {npc.Name}! Recibes {result.FinalDamage} de daño.", false, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"¡{npc.Name} te golpea con magia! Recibes {result.FinalDamage} de daño.", false, CombatLogType.Hit);

            _state.Player.DynamicStats.Health -= result.FinalDamage;
            if (_state.Player.DynamicStats.Health < 0) _state.Player.DynamicStats.Health = 0;
        }
        else
        {
            AddLogEntry(combat, "Resistes el ataque mágico.", true, CombatLogType.Miss);
        }

        // Verificar derrota
        if (_state.Player.DynamicStats.Health <= 0)
        {
            combat.Phase = CombatPhase.Defeat;
            HandleDefeat();
        }
        else
        {
            combat.RoundNumber++;
            combat.PlayerDefending = false;
            combat.Phase = CombatPhase.PlayerAction;
            AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);
        }

        return result;
    }

    /// <summary>
    /// Ejecuta un ataque mágico del NPC contra el jugador.
    /// </summary>
    /// <param name="ability">La habilidad de ataque del NPC.</param>
    /// <param name="npcAttackDice">Valor del dado de ataque del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="playerDefenseDice">Valor del dado de defensa del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecuteNpcMagicAttack(CombatAbility ability, int? npcAttackDice = null, int? playerDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.NpcAction)
            throw new InvalidOperationException("No es el turno del NPC.");

        if (ability.AbilityType != AbilityType.Attack)
            throw new InvalidOperationException("La habilidad no es de tipo ataque.");

        var npc = GetCurrentEnemy();

        // Tirada de ataque mágico del NPC: D20 + Inteligencia/5 + AbilityAttackValue
        var roll = npcAttackDice ?? RollD20();
        var statBonus = npc.Stats.Intelligence / 5;
        var attackRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = ability.AttackValue,
            AdditionalBonus = 0
        };
        combat.LastNpcRoll = attackRoll;

        // Tirada de defensa del jugador
        var defenseRoll = RollDefense(true, playerDefenseDice);
        combat.LastPlayerRoll = defenseRoll;

        // Calcular daño mágico del NPC
        var result = CalculateNpcMagicDamage(attackRoll, defenseRoll, ability, npc);

        // Log
        AddLogEntry(combat, $"¡{npc.Name} lanza {ability.Name}! {attackRoll.Breakdown}", false, CombatLogType.Normal);
        AddLogEntry(combat, $"Tu defensa: {defenseRoll.Breakdown}", true, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, $"¡El hechizo de {npc.Name} falla!", false, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡HECHIZO CRÍTICO de {npc.Name}! Recibes {result.FinalDamage} de daño mágico.", false, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"¡{npc.Name} te golpea con magia! Recibes {result.FinalDamage} de daño.", false, CombatLogType.Hit);

            // Aplicar daño al jugador
            _state.Player.DynamicStats.Health -= result.FinalDamage;
            if (_state.Player.DynamicStats.Health < 0) _state.Player.DynamicStats.Health = 0;
        }
        else
        {
            AddLogEntry(combat, "Resistes el hechizo.", true, CombatLogType.Miss);
        }

        // Verificar derrota
        if (_state.Player.DynamicStats.Health <= 0)
        {
            combat.Phase = CombatPhase.Defeat;
            HandleDefeat();
        }
        else
        {
            // Siguiente ronda
            combat.RoundNumber++;
            combat.PlayerDefending = false;
            combat.Phase = CombatPhase.PlayerAction;
            AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);
        }

        return result;
    }

    /// <summary>
    /// Ejecuta una defensa mágica del jugador contra un ataque mágico del NPC.
    /// </summary>
    /// <param name="playerDefenseAbility">La habilidad de defensa del jugador.</param>
    /// <param name="npcAttackAbility">La habilidad de ataque del NPC.</param>
    /// <param name="npcAttackDice">Valor del dado de ataque del NPC (1-20). Si es null, se genera aleatoriamente.</param>
    /// <param name="playerDefenseDice">Valor del dado de defensa del jugador (1-20). Si es null, se genera aleatoriamente.</param>
    public DamageResult ExecuteMagicDefenseVsMagicAttack(CombatAbility playerDefenseAbility, CombatAbility npcAttackAbility, int? npcAttackDice = null, int? playerDefenseDice = null)
    {
        var combat = _state.ActiveCombat;
        if (combat == null || combat.Phase != CombatPhase.NpcAction)
            throw new InvalidOperationException("No es el turno del NPC.");

        if (playerDefenseAbility.AbilityType != AbilityType.Defense)
            throw new InvalidOperationException("La habilidad del jugador no es de tipo defensa.");

        // Verificar mana del jugador
        if (_state.Player.DynamicStats.Mana < playerDefenseAbility.ManaCost)
            throw new InvalidOperationException("No tienes suficiente mana.");

        // Consumir mana del jugador
        _state.Player.DynamicStats.Mana -= playerDefenseAbility.ManaCost;

        var npc = GetCurrentEnemy();

        // Tirada de ataque mágico del NPC: D20 + Inteligencia/5 + AbilityAttackValue
        var roll = npcAttackDice ?? RollD20();
        var statBonus = npc.Stats.Intelligence / 5;
        var attackRoll = new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = npcAttackAbility.AttackValue,
            AdditionalBonus = 0
        };
        combat.LastNpcRoll = attackRoll;

        // Tirada de defensa mágica del jugador: D20 + Inteligencia/5 + AbilityDefenseValue
        var playerRoll = playerDefenseDice ?? RollD20();
        var playerStatBonus = _state.Player.Intelligence / 5;
        var defenseRoll = new DiceRollResult
        {
            DiceValue = playerRoll,
            StatBonus = playerStatBonus,
            EquipmentBonus = playerDefenseAbility.DefenseValue,
            AdditionalBonus = 0
        };
        combat.LastPlayerRoll = defenseRoll;

        // Calcular daño mágico del NPC
        var result = CalculateNpcMagicDamage(attackRoll, defenseRoll, npcAttackAbility, npc);

        // Log
        AddLogEntry(combat, $"¡{npc.Name} lanza {npcAttackAbility.Name}! {attackRoll.Breakdown}", false, CombatLogType.Normal);
        AddLogEntry(combat, $"¡{playerDefenseAbility.Name}! {defenseRoll.Breakdown}", true, CombatLogType.Normal);

        if (result.WasFumble)
        {
            AddLogEntry(combat, $"¡El hechizo de {npc.Name} falla!", false, CombatLogType.Fumble);
        }
        else if (result.Hit)
        {
            if (result.WasCritical)
                AddLogEntry(combat, $"¡HECHIZO CRÍTICO de {npc.Name}! Recibes {result.FinalDamage} de daño mágico.", false, CombatLogType.Critical);
            else
                AddLogEntry(combat, $"{npc.Name} te golpea con magia. Recibes {result.FinalDamage} de daño.", false, CombatLogType.Hit);

            // Aplicar daño al jugador
            _state.Player.DynamicStats.Health -= result.FinalDamage;
            if (_state.Player.DynamicStats.Health < 0) _state.Player.DynamicStats.Health = 0;
        }
        else
        {
            AddLogEntry(combat, "¡Tu barrera mágica bloquea el hechizo enemigo!", true, CombatLogType.Miss);
        }

        // Aplicar curación si la habilidad defensiva la tiene
        if (playerDefenseAbility.Healing > 0)
        {
            var healAmount = playerDefenseAbility.Healing;
            _state.Player.DynamicStats.Health = Math.Min(
                _state.Player.DynamicStats.MaxHealth,
                _state.Player.DynamicStats.Health + healAmount);
            AddLogEntry(combat, $"Tu barrera te cura {healAmount} de salud.", true, CombatLogType.Normal);
        }

        // Verificar derrota
        if (_state.Player.DynamicStats.Health <= 0)
        {
            combat.Phase = CombatPhase.Defeat;
            HandleDefeat();
        }
        else
        {
            // Siguiente ronda
            combat.RoundNumber++;
            combat.PlayerDefending = false;
            combat.Phase = CombatPhase.PlayerAction;
            AddLogEntry(combat, $"--- Ronda {combat.RoundNumber} ---", false, CombatLogType.System);
        }

        return result;
    }

    #region Private Methods

    private int RollD20() => _random.Next(1, 21);

    private DiceRollResult RollAttack(bool isPlayer, int? diceValue = null)
    {
        var roll = diceValue ?? RollD20();
        int statBonus;
        int equipBonus = 0;

        if (isPlayer)
        {
            // Solo cuenta el arma EQUIPADA
            var weapon = GetEquippedWeapon();

            if (weapon != null)
            {
                // Si el arma es mágica, usar Inteligencia
                statBonus = weapon.DamageType == DamageType.Magical
                    ? _state.Player.Intelligence / 5
                    : _state.Player.Strength / 5;
                equipBonus = weapon.AttackBonus;
            }
            else
            {
                // Sin arma equipada
                statBonus = _state.Player.Strength / 5;
            }
        }
        else
        {
            var npc = GetCurrentEnemy();

            // NPCs usan la mejor arma de su inventario automáticamente
            var npcBestWeapon = GetNpcBestWeapon();

            if (npcBestWeapon != null)
            {
                // Si el arma es mágica, usar Inteligencia
                statBonus = npcBestWeapon.DamageType == DamageType.Magical
                    ? npc.Stats.Intelligence / 5
                    : npc.Stats.Strength / 5;
                equipBonus = npcBestWeapon.AttackBonus;
            }
            else
            {
                statBonus = npc.Stats.Strength / 5;
            }
        }

        return new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = equipBonus
        };
    }

    private DiceRollResult RollDefense(bool isPlayer, int? diceValue = null)
    {
        var roll = diceValue ?? RollD20();
        int statBonus;
        int equipBonus = 0;
        int additionalBonus = 0;

        if (isPlayer)
        {
            // Solo cuenta la armadura EQUIPADA
            var equippedArmor = GetEquippedArmor();

            // Usar el bonus total de defensa de todas las armaduras equipadas
            equipBonus = GetTotalDefenseBonus();

            if (equippedArmor != null)
            {
                // Si la armadura del torso es mágica, usar Inteligencia
                statBonus = equippedArmor.DamageType == DamageType.Magical
                    ? _state.Player.Intelligence / 5
                    : _state.Player.Dexterity / 5;
            }
            else
            {
                // Sin armadura en torso
                statBonus = _state.Player.Dexterity / 5;
            }

            // Bonus por postura defensiva
            if (_state.ActiveCombat?.PlayerDefending == true)
                additionalBonus = 5;
        }
        else
        {
            var npc = GetCurrentEnemy();

            // NPCs usan la mejor armadura de su inventario automáticamente
            var npcBestArmor = GetNpcBestArmor();

            if (npcBestArmor != null)
            {
                // Si la armadura es mágica, usar Inteligencia
                statBonus = npcBestArmor.DamageType == DamageType.Magical
                    ? npc.Stats.Intelligence / 5
                    : npc.Stats.Dexterity / 5;
                equipBonus = npcBestArmor.DefenseBonus;
            }
            else
            {
                statBonus = npc.Stats.Dexterity / 5;
            }
        }

        return new DiceRollResult
        {
            DiceValue = roll,
            StatBonus = statBonus,
            EquipmentBonus = equipBonus,
            AdditionalBonus = additionalBonus
        };
    }

    private DamageResult CalculateDamage(DiceRollResult attack, DiceRollResult defense, bool playerAttacking)
    {
        var result = new DamageResult
        {
            AttackRoll = attack,
            DefenseRoll = defense
        };

        if (attack.IsFumble)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        if (!result.Hit)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        // Daño base = diferencia + stat/10
        var difference = attack.Total - defense.Total;
        int statDamage;

        if (playerAttacking)
        {
            var weapon = GetEquippedWeapon();

            if (weapon != null)
            {
                // Si el arma es mágica, usar Inteligencia
                statDamage = weapon.DamageType == DamageType.Magical
                    ? _state.Player.Intelligence / 10
                    : _state.Player.Strength / 10;
            }
            else
            {
                // Sin arma = daño reducido
                statDamage = _state.Player.Strength / 10;
                difference = Math.Max(1, difference / 2);
            }
        }
        else
        {
            var npc = GetCurrentEnemy();
            var npcBestWeapon = GetNpcBestWeapon();

            if (npcBestWeapon != null)
            {
                // Si el arma es mágica, usar Inteligencia
                statDamage = npcBestWeapon.DamageType == DamageType.Magical
                    ? npc.Stats.Intelligence / 10
                    : npc.Stats.Strength / 10;
            }
            else
            {
                // Sin arma = daño reducido
                statDamage = npc.Stats.Strength / 10;
                difference = Math.Max(1, difference / 2);
            }
        }

        result.BaseDamage = Math.Max(1, difference + statDamage);
        result.FinalDamage = result.BaseDamage;

        // Crítico = daño x2
        if (attack.IsCritical)
            result.FinalDamage *= 2;

        return result;
    }

    private DamageResult CalculateMagicDamage(DiceRollResult attack, DiceRollResult defense, CombatAbility ability)
    {
        var result = new DamageResult
        {
            AttackRoll = attack,
            DefenseRoll = defense
        };

        if (attack.IsFumble)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        if (!result.Hit)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        // Daño mágico = diferencia + daño base de la habilidad + Int/10
        var difference = attack.Total - defense.Total;
        var statDamage = _state.Player.Intelligence / 10;
        var abilityDamage = ability.Damage;

        result.BaseDamage = Math.Max(1, difference + statDamage + abilityDamage);
        result.FinalDamage = result.BaseDamage;

        // Crítico = daño x2
        if (attack.IsCritical)
            result.FinalDamage *= 2;

        return result;
    }

    private DamageResult CalculateNpcMagicDamage(DiceRollResult attack, DiceRollResult defense, CombatAbility ability, Npc npc)
    {
        var result = new DamageResult
        {
            AttackRoll = attack,
            DefenseRoll = defense
        };

        if (attack.IsFumble)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        if (!result.Hit)
        {
            result.BaseDamage = 0;
            result.FinalDamage = 0;
            return result;
        }

        // Daño mágico del NPC = diferencia + daño base de la habilidad + Int/10
        var difference = attack.Total - defense.Total;
        var statDamage = npc.Stats.Intelligence / 10;
        var abilityDamage = ability.Damage;

        result.BaseDamage = Math.Max(1, difference + statDamage + abilityDamage);
        result.FinalDamage = result.BaseDamage;

        // Crítico = daño x2
        if (attack.IsCritical)
            result.FinalDamage *= 2;

        return result;
    }

    private int GetPlayerInitiativeBonus()
    {
        var weapon = GetEquippedWeapon();
        var armor = GetEquippedArmor();
        return (weapon?.InitiativeBonus ?? 0) + (armor?.InitiativeBonus ?? 0);
    }

    private GameObject? GetEquippedWeapon()
    {
        // Buscar arma en mano derecha primero
        var rightHandId = _state.Player.EquippedRightHandId;
        if (!string.IsNullOrEmpty(rightHandId))
        {
            var weapon = _state.Objects.FirstOrDefault(o => o.Id == rightHandId && o.Type == ObjectType.Arma);
            if (weapon != null) return weapon;
        }

        // Si no hay arma en mano derecha, buscar en mano izquierda
        var leftHandId = _state.Player.EquippedLeftHandId;
        if (!string.IsNullOrEmpty(leftHandId) && leftHandId != rightHandId)
        {
            var weapon = _state.Objects.FirstOrDefault(o => o.Id == leftHandId && o.Type == ObjectType.Arma);
            if (weapon != null) return weapon;
        }

        return null;
    }

    private GameObject? GetEquippedArmor()
    {
        // Obtener armadura del torso
        var torsoId = _state.Player.EquippedTorsoId;
        if (string.IsNullOrEmpty(torsoId)) return null;
        return _state.Objects.FirstOrDefault(o => o.Id == torsoId && o.Type == ObjectType.Armadura);
    }

    /// <summary>
    /// Calcula el bonus total de defensa de armaduras y escudos equipados.
    /// </summary>
    private int GetTotalDefenseBonus()
    {
        int bonus = 0;

        // Mano izquierda (escudo) - solo si es diferente de mano derecha
        if (!string.IsNullOrEmpty(_state.Player.EquippedLeftHandId) && _state.Player.EquippedLeftHandId != _state.Player.EquippedRightHandId)
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedLeftHandId);
            if (obj?.Type == ObjectType.Escudo)
                bonus += obj.DefenseBonus;
        }

        // Torso (armadura de cuerpo)
        if (!string.IsNullOrEmpty(_state.Player.EquippedTorsoId))
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedTorsoId);
            if (obj?.Type == ObjectType.Armadura)
                bonus += obj.DefenseBonus;
        }

        // Cabeza (casco)
        if (!string.IsNullOrEmpty(_state.Player.EquippedHeadId))
        {
            var obj = _state.Objects.FirstOrDefault(o => o.Id == _state.Player.EquippedHeadId);
            if (obj?.Type == ObjectType.Casco)
                bonus += obj.DefenseBonus;
        }

        return bonus;
    }

    private Npc GetCurrentEnemy()
    {
        var combat = _state.ActiveCombat;
        if (combat == null)
            throw new InvalidOperationException("No hay combate activo.");

        var npc = _state.Npcs.FirstOrDefault(n => n.Id == combat.EnemyNpcId);
        if (npc == null)
            throw new InvalidOperationException($"NPC no encontrado: {combat.EnemyNpcId}");

        return npc;
    }

    private void WearWeapon()
    {
        var weapon = GetEquippedWeapon();
        if (weapon == null || weapon.MaxDurability < 0) return;

        weapon.CurrentDurability--;
        if (weapon.CurrentDurability <= 0)
        {
            weapon.CurrentDurability = 0;
            // Desequipar el arma de ambas manos si es necesario
            if (_state.Player.EquippedRightHandId == weapon.Id)
                _state.Player.EquippedRightHandId = null;
            if (_state.Player.EquippedLeftHandId == weapon.Id)
                _state.Player.EquippedLeftHandId = null;
            AddLogEntry(_state.ActiveCombat!, $"¡Tu {weapon.Name} se ha roto!", true, CombatLogType.System);
        }
    }

    private void WearArmor()
    {
        var armor = GetEquippedArmor();
        if (armor == null || armor.MaxDurability < 0) return;

        armor.CurrentDurability--;
        if (armor.CurrentDurability <= 0)
        {
            armor.CurrentDurability = 0;
            _state.Player.EquippedTorsoId = null;
            AddLogEntry(_state.ActiveCombat!, $"¡Tu {armor.Name} se ha roto!", true, CombatLogType.System);
        }
    }

    private void HandleVictory()
    {
        var combat = _state.ActiveCombat!;
        var npc = GetCurrentEnemy();

        AddLogEntry(combat, $"¡Has derrotado a {npc.Name}!", true, CombatLogType.Victory);

        // Convertir NPC en cadáver
        npc.IsCorpse = true;
        npc.IsPatrolling = false;
        npc.IsFollowingPlayer = false;

        EndCombat(CombatEndReason.Victory);
    }

    private void HandleDefeat()
    {
        var combat = _state.ActiveCombat!;
        var npc = GetCurrentEnemy();

        AddLogEntry(combat, $"Has sido derrotado por {npc.Name}...", false, CombatLogType.Defeat);

        EndCombat(CombatEndReason.Defeat);
    }

    private void EndCombat(CombatEndReason reason)
    {
        var combat = _state.ActiveCombat;
        if (combat == null) return;

        combat.IsActive = false;

        CombatEnded?.Invoke(this, new CombatEndEventArgs
        {
            Reason = reason,
            EnemyNpcId = combat.EnemyNpcId,
            RoundsPlayed = combat.RoundNumber
        });

        // No limpiamos ActiveCombat aquí para que la UI pueda mostrar el resultado final
    }

    private void AddLogEntry(CombatState combat, string message, bool isPlayerAction, CombatLogType logType)
    {
        var entry = new CombatLogEntry
        {
            Message = message,
            IsPlayerAction = isPlayerAction,
            LogType = logType,
            Timestamp = DateTime.Now
        };

        combat.CombatLog.Add(entry);
        LogEntryAdded?.Invoke(this, entry);
    }

    #endregion
}

/// <summary>
/// Razón por la que terminó el combate.
/// </summary>
public enum CombatEndReason
{
    /// <summary>El jugador ganó.</summary>
    Victory,
    /// <summary>El jugador perdió (muerte).</summary>
    Defeat,
    /// <summary>El jugador huyó.</summary>
    Fled,
    /// <summary>El enemigo huyó.</summary>
    EnemyFled
}

/// <summary>
/// Argumentos del evento de fin de combate.
/// </summary>
public class CombatEndEventArgs : EventArgs
{
    public CombatEndReason Reason { get; set; }
    public string EnemyNpcId { get; set; } = string.Empty;
    public int RoundsPlayed { get; set; }
}
