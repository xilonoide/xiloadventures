using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Terminal.Player.Screens;

/// <summary>
/// Resultado del combate
/// </summary>
public enum CombatResult
{
    Victory,
    Defeat,
    Fled
}

/// <summary>
/// Pantalla de combate por turnos
/// </summary>
public class CombatScreen
{
    private static int Width => ConsoleRenderer.ScreenWidth;

    private readonly GameEngine _engine;
    private readonly GameState _state;
    private readonly WorldModel _world;
    private readonly Npc _enemy;
    private readonly ConsoleInput _input;
    private readonly CombatEngine _combatEngine;
    private readonly List<string> _combatLog = new();

    private bool _combatEnded;
    private CombatResult _result;

    public CombatScreen(GameEngine engine, GameState state, WorldModel world, Npc enemy, ConsoleInput input)
    {
        _engine = engine;
        _state = state;
        _world = world;
        _enemy = enemy;
        _input = input;
        _combatEngine = new CombatEngine(state);
    }

    /// <summary>
    /// Ejecuta el combate y retorna el resultado
    /// </summary>
    public CombatResult Run()
    {
        // Iniciar combate
        _combatEngine.StartCombat(_enemy.Id);

        // Suscribirse a eventos
        _combatEngine.LogEntryAdded += OnLogEntryAdded;
        _combatEngine.CombatEnded += OnCombatEnded;

        // Fase de iniciativa
        RunInitiativePhase();

        // Loop de combate
        while (!_combatEnded)
        {
            Render();

            // Usar la fase del combate para determinar quién actúa
            var phase = _state.ActiveCombat?.Phase;
            if (phase == CombatPhase.PlayerAction)
            {
                ProcessPlayerTurn();
            }
            else if (phase == CombatPhase.NpcAction)
            {
                ProcessEnemyTurn();
            }
        }

        return _result;
    }

    private void RunInitiativePhase()
    {
        var random = new Random();

        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("COMBATE - Iniciativa", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Tirada del jugador
        ConsoleRenderer.DrawLine($"  {Colors.Cyan}Tu iniciativa{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var playerDicePos = Console.CursorTop;

        // Animación del dado del jugador
        for (int i = 0; i < 10; i++)
        {
            Console.SetCursorPosition(0, playerDicePos);
            var fakeDice = random.Next(1, 21);
            ConsoleRenderer.DrawLine($"     [ {Colors.Cyan}{fakeDice,2}{Colors.Reset} ]", Width);
            Thread.Sleep(80);
        }

        var playerRoll = _combatEngine.RollPlayerInitiative();

        // Resultado final del jugador
        Console.SetCursorPosition(0, playerDicePos);
        var playerDiceColor = playerRoll.IsCritical ? Colors.Yellow : (playerRoll.IsFumble ? Colors.Red : Colors.Cyan);
        ConsoleRenderer.DrawLine($"     [ {playerDiceColor}{playerRoll.DiceValue,2}{Colors.Reset} ]  {(playerRoll.IsCritical ? "CRITICO!" : playerRoll.IsFumble ? "PIFIA!" : "")}", Width);
        Thread.Sleep(500);

        // Desglose del jugador
        var playerEquipBonus = playerRoll.EquipmentBonus > 0 ? $" + {playerRoll.EquipmentBonus}" : "";
        ConsoleRenderer.DrawLine($"     {playerRoll.DiceValue} + {playerRoll.StatBonus} (DES){playerEquipBonus} = {Colors.Bold}{playerRoll.Total}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(800);

        // Tirada del NPC
        ConsoleRenderer.DrawLine($"  {Colors.Red}Iniciativa de {_enemy.Name}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var npcDicePos = Console.CursorTop;

        // Animación del dado del NPC
        for (int i = 0; i < 10; i++)
        {
            Console.SetCursorPosition(0, npcDicePos);
            var fakeDice = random.Next(1, 21);
            ConsoleRenderer.DrawLine($"     [ {Colors.Red}{fakeDice,2}{Colors.Reset} ]", Width);
            Thread.Sleep(80);
        }

        var npcRoll = _combatEngine.RollNpcInitiative();

        // Resultado final del NPC
        Console.SetCursorPosition(0, npcDicePos);
        var npcDiceColor = npcRoll.IsCritical ? Colors.Yellow : (npcRoll.IsFumble ? Colors.Red : Colors.Red);
        ConsoleRenderer.DrawLine($"     [ {npcDiceColor}{npcRoll.DiceValue,2}{Colors.Reset} ]  {(npcRoll.IsCritical ? "CRITICO!" : npcRoll.IsFumble ? "PIFIA!" : "")}", Width);
        Thread.Sleep(500);

        // Desglose del NPC
        var npcEquipBonus = npcRoll.EquipmentBonus > 0 ? $" + {npcRoll.EquipmentBonus}" : "";
        ConsoleRenderer.DrawLine($"     {npcRoll.DiceValue} + {npcRoll.StatBonus} (DES){npcEquipBonus} = {Colors.Bold}{npcRoll.Total}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(800);

        // Comparación
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine($"{playerRoll.Total}  vs  {npcRoll.Total}", Width, Colors.Bold);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(500);

        // Resultado
        var playerFirst = _combatEngine.ResolveInitiative();
        if (playerFirst)
        {
            ConsoleRenderer.DrawCenteredLine($"{Colors.Green}TU EMPIEZAS!{Colors.Reset}", Width);
            AddLog("Ganas la iniciativa. Tu turno primero.");
        }
        else
        {
            ConsoleRenderer.DrawCenteredLine($"{Colors.Red}{_enemy.Name.ToUpper()} EMPIEZA!{Colors.Reset}", Width);
            AddLog($"{_enemy.Name} gana la iniciativa.");
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

        Thread.Sleep(2000);
        _input.WaitForEnter();
    }

    private void Render()
    {
        ConsoleRenderer.Clear();

        var combat = _state.ActiveCombat;
        var enemyStats = _enemy.Stats;

        // Encabezado
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle($"COMBATE - Ronda {combat?.RoundNumber ?? 1}", Width);
        ConsoleRenderer.DrawSeparator(Width);

        // Enemigo
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawLine($"{Colors.Enemy}{_enemy.Name}{Colors.Reset}", Width);
        var enemyMaxHealth = enemyStats.MaxHealth;
        ConsoleRenderer.DrawStatBar("*", "Salud", enemyStats.CurrentHealth, enemyMaxHealth, Colors.Health, Width, barWidth: 20);

        // Separador
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine("--- VS ---", Width, Colors.Gray);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Jugador
        var playerStats = _state.Player.DynamicStats;
        var maxHealth = playerStats.MaxHealth;
        var maxMana = playerStats.MaxMana;

        ConsoleRenderer.DrawLine($"{Colors.Green}{_state.Player.Name ?? "Aventurero"}{Colors.Reset}", Width);
        ConsoleRenderer.DrawStatBar("*", "Salud", playerStats.Health, maxHealth, Colors.Health, Width, barWidth: 20);
        ConsoleRenderer.DrawStatBar("*", "Mana", playerStats.Mana, maxMana, Colors.Mana, Width, barWidth: 20);

        // Log de combate
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawLine($"{Colors.Bold}Registro:{Colors.Reset}", Width);

        var logLines = _combatLog.TakeLast(4);
        foreach (var log in logLines)
        {
            ConsoleRenderer.DrawWrappedText($"  {log}", Width, Colors.Gray);
        }

        // Menú de acciones (solo si es fase de acción del jugador)
        if (combat?.Phase == CombatPhase.PlayerAction)
        {
            ConsoleRenderer.DrawSeparator(Width, thin: true);
            ConsoleRenderer.DrawLine($"{Colors.Bold}Que quieres hacer?{Colors.Reset}", Width);
            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawLine($"  [1] Atacar          [2] Defender", Width, Colors.Cyan);
            ConsoleRenderer.DrawLine($"  [3] Habilidad       [4] Objeto        [5] Huir", Width, Colors.Cyan);
        }

        ConsoleRenderer.DrawBottomBorder(Width);
    }

    private void ProcessPlayerTurn()
    {
        var option = _input.ReadLine();

        switch (option.Trim())
        {
            case "1":
                ExecutePlayerAttack();
                break;

            case "2":
                ExecutePlayerDefend();
                break;

            case "3":
                ShowAbilitiesMenu();
                break;

            case "4":
                ShowItemsMenu();
                break;

            case "5":
                AttemptFlee();
                break;

            default:
                // Opción inválida, no hacer nada
                break;
        }
    }

    private void ExecutePlayerAttack()
    {
        _combatEngine.SetPlayerAction(CombatAction.Attack);
        var result = _combatEngine.ExecutePlayerAttack();

        // Mostrar animación de dados
        ShowAttackDiceAnimation(
            "Tu ataque",
            _state.ActiveCombat?.LastPlayerRoll,
            $"Defensa de {_enemy.Name}",
            _state.ActiveCombat?.LastNpcRoll,
            result,
            isPlayerAttacking: true);

        if (result.WasCritical)
        {
            AddLog($"CRITICO! Infliges {result.FinalDamage} de dano!");
        }
        else if (!result.Hit)
        {
            AddLog("Fallas el ataque.");
        }
        else
        {
            AddLog($"Infliges {result.FinalDamage} de dano.");
        }

        // Mostrar pantalla de fin si el combate terminó
        ShowCombatEndScreen();
    }

    private void ExecutePlayerDefend()
    {
        _combatEngine.SetPlayerAction(CombatAction.Defend);

        // Mostrar pantalla de defensa
        ShowDefendAnimation();

        AddLog("Te preparas para defender el proximo ataque (+5 defensa).");

        // Avanzar turno - el enemigo ataca
        ProcessEnemyTurn();
    }

    private void ShowDefendAnimation()
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("POSTURA DEFENSIVA", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Animación de escudo
        var frames = new[] { "  (  )", " ( O )", "( O O )", "[=O=O=]", "[=O=O=]", "[=O=O=]" };
        var pos = Console.CursorTop;

        foreach (var frame in frames)
        {
            Console.SetCursorPosition(0, pos);
            ConsoleRenderer.DrawCenteredLine($"{Colors.Cyan}{frame}{Colors.Reset}", Width);
            Thread.Sleep(150);
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine($"{Colors.Green}+5 Defensa este turno{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine("Preparado para el ataque enemigo...", Width, Colors.Gray);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

        Thread.Sleep(2000);
    }

    private void ShowAbilitiesMenu()
    {
        var abilities = _combatEngine.GetPlayerAbilities();

        if (!abilities.Any())
        {
            AddLog("No tienes habilidades disponibles.");
            return;
        }

        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("Habilidades", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var index = 1;
        foreach (var ability in abilities)
        {
            var manaCost = ability.ManaCost > 0 ? $" ({ability.ManaCost} mana)" : "";
            ConsoleRenderer.DrawLine($"  [{index}] {ability.Name}{manaCost}", Width, Colors.Cyan);
            if (!string.IsNullOrEmpty(ability.Description))
            {
                ConsoleRenderer.DrawWrappedText($"      {ability.Description}", Width, Colors.Gray);
            }
            index++;
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawLine("  [0] Volver", Width, Colors.Gray);
        ConsoleRenderer.DrawBottomBorder(Width);

        var option = _input.ReadLine();

        if (option == "0" || string.IsNullOrWhiteSpace(option))
            return;

        if (int.TryParse(option, out int abilityIndex) &&
            abilityIndex >= 1 && abilityIndex <= abilities.Count)
        {
            var selectedAbility = abilities[abilityIndex - 1];

            if (_state.Player.DynamicStats.Mana < selectedAbility.ManaCost)
            {
                AddLog("No tienes suficiente mana.");
                return;
            }

            _combatEngine.SetPlayerAction(CombatAction.UseAbility, selectedAbility.Id);
            var result = _combatEngine.ExecuteMagicAttack(selectedAbility);

            // Mostrar animación de dados para habilidad mágica
            ShowAttackDiceAnimation(
                $"{selectedAbility.Name}",
                _state.ActiveCombat?.LastPlayerRoll,
                $"Defensa de {_enemy.Name}",
                _state.ActiveCombat?.LastNpcRoll,
                result,
                isPlayerAttacking: true,
                isMagic: true);

            if (result.WasCritical)
            {
                AddLog($"CRITICO! Infliges {result.FinalDamage} de dano magico!");
            }
            else if (!result.Hit)
            {
                AddLog("El hechizo falla.");
            }
            else
            {
                AddLog($"Infliges {result.FinalDamage} de dano magico.");
            }

            // Mostrar pantalla de fin si el combate terminó
            ShowCombatEndScreen();
        }
    }

    private void ShowItemsMenu()
    {
        // Obtener objetos del inventario
        var inventoryItems = _state.InventoryObjectIds
            .Select(id => _state.Objects.FirstOrDefault(o => o.Id == id))
            .Where(o => o != null)
            .ToList();

        if (!inventoryItems.Any())
        {
            AddLog("No tienes objetos en el inventario.");
            return;
        }

        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("Inventario", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var index = 1;
        foreach (var item in inventoryItems)
        {
            var typeInfo = item!.Type != ObjectType.Ninguno ? $" ({item.Type})" : "";
            ConsoleRenderer.DrawLine($"  [{index}] {item.Name}{typeInfo}", Width, Colors.Cyan);
            if (!string.IsNullOrEmpty(item.Description))
            {
                var shortDesc = item.Description.Length > 50
                    ? item.Description.Substring(0, 47) + "..."
                    : item.Description;
                ConsoleRenderer.DrawWrappedText($"      {shortDesc}", Width, Colors.Gray);
            }
            index++;
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawLine("  [0] Volver", Width, Colors.Gray);
        ConsoleRenderer.DrawBottomBorder(Width);

        var option = _input.ReadLine();

        if (option == "0" || string.IsNullOrWhiteSpace(option))
            return;

        if (int.TryParse(option, out int itemIndex) &&
            itemIndex >= 1 && itemIndex <= inventoryItems.Count)
        {
            var selectedItem = inventoryItems[itemIndex - 1]!;

            // Mostrar animación de uso de objeto
            ShowUseItemAnimation(selectedItem);

            // Usar el objeto
            _combatEngine.SetPlayerAction(CombatAction.UseItem, selectedItem.Id);
            _combatEngine.UseItem(selectedItem.Id);

            AddLog($"Usas {selectedItem.Name}.");

            // El enemigo ataca después de usar un objeto
            ProcessEnemyTurn();
        }
    }

    private void ShowUseItemAnimation(GameObject item)
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("USANDO OBJETO", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Animación simple
        var frames = new[] { "[ . ]", "[ o ]", "[ O ]", "[ * ]", "[ + ]" };
        var pos = Console.CursorTop;

        foreach (var frame in frames)
        {
            Console.SetCursorPosition(0, pos);
            ConsoleRenderer.DrawCenteredLine($"{Colors.Yellow}{frame}{Colors.Reset}", Width);
            Thread.Sleep(150);
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine($"{Colors.Green}{item.Name}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

        Thread.Sleep(2000);
    }

    private void AttemptFlee()
    {
        _combatEngine.SetPlayerAction(CombatAction.Flee);

        // Mostrar animación de huida
        var success = _combatEngine.AttemptFlee();
        ShowFleeAnimation(success);

        if (success)
        {
            AddLog("Logras escapar!");
            _combatEnded = true;
            _result = CombatResult.Fled;
        }
        else
        {
            AddLog("No consigues escapar!");
            ProcessEnemyTurn();
        }
    }

    private void ShowFleeAnimation(bool success)
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("INTENTANDO HUIR", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Animación de correr
        var runFrames = new[] { " o    ", "  o   ", "   o  ", "    o ", "     o" };
        var pos = Console.CursorTop;

        for (int i = 0; i < 2; i++)
        {
            foreach (var frame in runFrames)
            {
                Console.SetCursorPosition(0, pos);
                ConsoleRenderer.DrawCenteredLine($"{Colors.Yellow}{frame}{Colors.Reset}", Width);
                Thread.Sleep(100);
            }
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Resultado
        if (success)
        {
            ConsoleRenderer.DrawCenteredLine($"{Colors.Green}ESCAPAS!{Colors.Reset}", Width);
        }
        else
        {
            ConsoleRenderer.DrawCenteredLine($"{Colors.Red}BLOQUEADO!{Colors.Reset}", Width);
            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawCenteredLine("El enemigo te alcanza...", Width, Colors.Gray);
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

        Thread.Sleep(2400);
    }

    private void ProcessEnemyTurn()
    {
        if (_combatEnded) return;

        var result = _combatEngine.ExecuteNpcTurn();

        // Mostrar animación de dados
        ShowAttackDiceAnimation(
            $"Ataque de {_enemy.Name}",
            _state.ActiveCombat?.LastNpcRoll,
            "Tu defensa",
            _state.ActiveCombat?.LastPlayerRoll,
            result,
            isPlayerAttacking: false);

        if (result.WasCritical)
        {
            AddLog($"CRITICO! {_enemy.Name} te inflige {result.FinalDamage} de dano!");
        }
        else if (!result.Hit)
        {
            AddLog($"{_enemy.Name} falla su ataque.");
        }
        else
        {
            AddLog($"{_enemy.Name} te inflige {result.FinalDamage} de dano.");
        }

        // Mostrar pantalla de fin si el combate terminó
        ShowCombatEndScreen();
    }

    private void ShowAttackDiceAnimation(
        string attackerLabel,
        DiceRollResult? attackRoll,
        string defenderLabel,
        DiceRollResult? defenseRoll,
        DamageResult result,
        bool isPlayerAttacking,
        bool isMagic = false)
    {
        if (attackRoll == null || defenseRoll == null) return;

        var random = new Random();
        var attackColor = isPlayerAttacking ? Colors.Cyan : Colors.Red;
        var defenseColor = isPlayerAttacking ? Colors.Red : Colors.Cyan;

        // Pantalla de tirada de dados
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("TIRADA DE DADOS", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Animación del dado de ataque
        ConsoleRenderer.DrawLine($"  {attackColor}{attackerLabel}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var dicePos = Console.CursorTop;
        for (int i = 0; i < 10; i++)
        {
            Console.SetCursorPosition(0, dicePos);
            var fakeDice = random.Next(1, 21);
            ConsoleRenderer.DrawLine($"     [ {attackColor}{fakeDice,2}{Colors.Reset} ]", Width);
            Thread.Sleep(80);
        }

        // Resultado final del ataque
        Console.SetCursorPosition(0, dicePos);
        var attackDiceColor = attackRoll.IsCritical ? Colors.Yellow : (attackRoll.IsFumble ? Colors.Red : attackColor);
        ConsoleRenderer.DrawLine($"     [ {attackDiceColor}{attackRoll.DiceValue,2}{Colors.Reset} ]  {(attackRoll.IsCritical ? "CRITICO!" : attackRoll.IsFumble ? "PIFIA!" : "")}", Width);
        Thread.Sleep(500);

        // Desglose del ataque
        var statName = isMagic ? "INT" : "FUE";
        var equipBonus = attackRoll.EquipmentBonus > 0 ? $" + {attackRoll.EquipmentBonus}" : "";
        ConsoleRenderer.DrawLine($"     {attackRoll.DiceValue} + {attackRoll.StatBonus} ({statName}){equipBonus} = {Colors.Bold}{attackRoll.Total}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(800);

        // Animación del dado de defensa
        ConsoleRenderer.DrawLine($"  {defenseColor}{defenderLabel}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        dicePos = Console.CursorTop;
        for (int i = 0; i < 10; i++)
        {
            Console.SetCursorPosition(0, dicePos);
            var fakeDice = random.Next(1, 21);
            ConsoleRenderer.DrawLine($"     [ {defenseColor}{fakeDice,2}{Colors.Reset} ]", Width);
            Thread.Sleep(80);
        }

        // Resultado final de defensa
        Console.SetCursorPosition(0, dicePos);
        var defenseDiceColor = defenseRoll.IsCritical ? Colors.Yellow : (defenseRoll.IsFumble ? Colors.Red : defenseColor);
        ConsoleRenderer.DrawLine($"     [ {defenseDiceColor}{defenseRoll.DiceValue,2}{Colors.Reset} ]", Width);
        Thread.Sleep(500);

        // Desglose de defensa
        var defEquipBonus = defenseRoll.EquipmentBonus > 0 ? $" + {defenseRoll.EquipmentBonus}" : "";
        ConsoleRenderer.DrawLine($"     {defenseRoll.DiceValue} + {defenseRoll.StatBonus} (DEF){defEquipBonus} = {Colors.Bold}{defenseRoll.Total}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(800);

        // Comparación y resultado
        ConsoleRenderer.DrawSeparator(Width, thin: true);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawCenteredLine($"{attackRoll.Total}  vs  {defenseRoll.Total}", Width, Colors.Bold);
        ConsoleRenderer.DrawEmptyLine(Width);
        Thread.Sleep(500);

        // Resultado final
        string resultText;
        string resultColor;

        if (result.WasCritical)
        {
            resultText = $"GOLPE CRITICO! {result.FinalDamage} de dano!";
            resultColor = Colors.Yellow;
        }
        else if (!result.Hit)
        {
            resultText = isPlayerAttacking ? "FALLO!" : "BLOQUEADO!";
            resultColor = isPlayerAttacking ? Colors.Red : Colors.Green;
        }
        else
        {
            resultText = $"IMPACTO! {result.FinalDamage} de dano";
            resultColor = isPlayerAttacking ? Colors.Green : Colors.Red;
        }

        ConsoleRenderer.DrawCenteredLine($"{resultColor}{resultText}{Colors.Reset}", Width);
        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

        Thread.Sleep(3000);
    }

    private void OnLogEntryAdded(object? sender, CombatLogEntry entry)
    {
        // El log ya se maneja manualmente
    }

    private void OnCombatEnded(object? sender, CombatEndEventArgs args)
    {
        // Solo establecer flags, la pantalla se muestra después de la animación
        _combatEnded = true;
        _result = args.Reason == CombatEndReason.Victory ? CombatResult.Victory : CombatResult.Defeat;
    }

    private void ShowCombatEndScreen()
    {
        if (!_combatEnded) return;

        if (_result == CombatResult.Victory)
        {
            AddLog($"Has derrotado a {_enemy.Name}!");

            // Mostrar pantalla de victoria
            Render();
            Console.WriteLine();
            ConsoleRenderer.WriteLine($"  {Colors.Green}VICTORIA!{Colors.Reset}", Colors.Bold);

            _input.WaitForEnter();

            // Disparar evento de fin de combate
            _engine.TriggerCombatEndEvent(_enemy.Id, CombatEndReason.Victory);
        }
        else if (_result == CombatResult.Defeat)
        {
            AddLog("Has sido derrotado...");

            Render();
            Console.WriteLine();
            ConsoleRenderer.WriteLine($"  {Colors.Red}DERROTA{Colors.Reset}", Colors.Bold);
            _input.WaitForEnter();

            _engine.TriggerCombatEndEvent(_enemy.Id, CombatEndReason.Defeat);
        }
    }

    private void AddLog(string message)
    {
        _combatLog.Add(message);

        // Mantener solo los últimos 10 mensajes
        while (_combatLog.Count > 10)
        {
            _combatLog.RemoveAt(0);
        }
    }
}
