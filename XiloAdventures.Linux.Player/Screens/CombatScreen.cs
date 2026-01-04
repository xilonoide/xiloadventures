using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Linux.Player.Screens;

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

            if (_state.ActiveCombat?.IsPlayerTurn == true)
            {
                ProcessPlayerTurn();
            }
            else
            {
                ProcessEnemyTurn();
            }
        }

        return _result;
    }

    private void RunInitiativePhase()
    {
        ConsoleRenderer.Clear();
        ConsoleRenderer.DrawTopBorder(Width);
        ConsoleRenderer.DrawTitle("COMBATE - Iniciativa", Width);
        ConsoleRenderer.DrawSeparator(Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        // Tirada del jugador
        ConsoleRenderer.DrawLine("Tirando dados de iniciativa...", Width);
        ConsoleRenderer.DrawEmptyLine(Width);

        var playerRoll = _combatEngine.RollPlayerInitiative();
        ConsoleRenderer.DrawLine(
            $"Tu tirada: {Colors.Cyan}{playerRoll.DiceValue}{Colors.Reset} + {playerRoll.StatBonus} (destreza) = {Colors.Bold}{playerRoll.Total}{Colors.Reset}",
            Width);

        var npcRoll = _combatEngine.RollNpcInitiative();
        ConsoleRenderer.DrawLine(
            $"{_enemy.Name}: {Colors.Red}{npcRoll.DiceValue}{Colors.Reset} + {npcRoll.StatBonus} = {Colors.Bold}{npcRoll.Total}{Colors.Reset}",
            Width);

        ConsoleRenderer.DrawEmptyLine(Width);

        var playerFirst = _combatEngine.ResolveInitiative();
        if (playerFirst)
        {
            ConsoleRenderer.DrawLine($"{Colors.Green}Tu empiezas!{Colors.Reset}", Width);
            AddLog("Ganas la iniciativa. Tu turno primero.");
        }
        else
        {
            ConsoleRenderer.DrawLine($"{Colors.Red}{_enemy.Name} empieza!{Colors.Reset}", Width);
            AddLog($"{_enemy.Name} gana la iniciativa.");
        }

        ConsoleRenderer.DrawEmptyLine(Width);
        ConsoleRenderer.DrawBottomBorder(Width);

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

        // Menú de acciones (solo si es turno del jugador)
        if (combat?.IsPlayerTurn == true)
        {
            ConsoleRenderer.DrawSeparator(Width, thin: true);
            ConsoleRenderer.DrawLine($"{Colors.Bold}Que quieres hacer?{Colors.Reset}", Width);
            ConsoleRenderer.DrawEmptyLine(Width);
            ConsoleRenderer.DrawLine($"  [1] Atacar          [2] Defender", Width, Colors.Cyan);
            ConsoleRenderer.DrawLine($"  [3] Habilidad       [4] Huir", Width, Colors.Cyan);
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
                AttemptFlee();
                break;

            default:
                // Opción inválida, no hacer nada
                break;
        }
    }

    private void ExecutePlayerAttack()
    {
        AddLog("Atacas al enemigo...");
        var result = _combatEngine.ExecutePlayerAttack();

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
    }

    private void ExecutePlayerDefend()
    {
        _combatEngine.SetPlayerAction(CombatAction.Defend);
        AddLog("Te preparas para defender el proximo ataque.");

        // Avanzar turno
        ProcessEnemyTurn();
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

            AddLog($"Usas {selectedAbility.Name}!");
            var result = _combatEngine.ExecuteMagicAttack(selectedAbility);

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
        }
    }

    private void AttemptFlee()
    {
        AddLog("Intentas huir...");

        if (_combatEngine.AttemptFlee())
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

    private void ProcessEnemyTurn()
    {
        if (_combatEnded) return;

        Render();
        Console.WriteLine();
        ConsoleRenderer.WriteLine($"  {_enemy.Name} ataca...", Colors.Red);
        System.Threading.Thread.Sleep(1000);

        var result = _combatEngine.ExecuteNpcTurn();

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
    }

    private void OnLogEntryAdded(object? sender, CombatLogEntry entry)
    {
        // El log ya se maneja manualmente
    }

    private void OnCombatEnded(object? sender, CombatEndEventArgs args)
    {
        _combatEnded = true;

        if (args.Reason == CombatEndReason.Victory)
        {
            _result = CombatResult.Victory;
            AddLog($"Has derrotado a {_enemy.Name}!");

            // Mostrar pantalla de victoria
            Render();
            Console.WriteLine();
            ConsoleRenderer.WriteLine($"  {Colors.Green}VICTORIA!{Colors.Reset}", Colors.Bold);

            _input.WaitForEnter();

            // Disparar evento de fin de combate
            _engine.TriggerCombatEndEvent(_enemy.Id, CombatEndReason.Victory);
        }
        else if (args.Reason == CombatEndReason.Defeat)
        {
            _result = CombatResult.Defeat;
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
