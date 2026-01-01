using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using XiloAdventures.Engine;
using XiloAdventures.Engine.Models;
using XiloAdventures.Engine.Models.Enums;

namespace XiloAdventures.Wpf.Common.Windows;

/// <summary>
/// Ventana de combate por turnos.
/// </summary>
public partial class CombatWindow : Window
{
    private readonly CombatEngine _combatEngine;
    private readonly GameState _gameState;
    private readonly Npc _enemy;
    private readonly List<GameObject> _playerInventory;
    private readonly bool _magicEnabled;
    private int _lastPlayerDiceResult;
    private CombatAbility? _selectedDefenseAbility;
    private bool _initiativeResolved = false;
    private CombatAction _pendingAction = CombatAction.None;
    private CombatAbility? _pendingAbility;
    private bool _isProcessingForcedFlee = false;

    /// <summary>
    /// Evento disparado cuando el combate termina.
    /// </summary>
    public event Action<CombatEndReason>? CombatEnded;

    public CombatWindow(
        CombatEngine combatEngine,
        GameState gameState,
        Npc enemy,
        IEnumerable<GameObject> playerInventory,
        bool magicEnabled = false)
    {
        InitializeComponent();

        _combatEngine = combatEngine;
        _gameState = gameState;
        _enemy = enemy;
        _playerInventory = playerInventory.ToList();
        _magicEnabled = magicEnabled;

        // Suscribirse a eventos del motor de combate
        _combatEngine.LogEntryAdded += OnLogEntryAdded;
        _combatEngine.CombatEnded += OnCombatEnded;

        // Configurar dados
        NpcDice.IsRollEnabled = false;
        PlayerDice.IsRollEnabled = false;

        // Inicializar UI
        InitializeUI();

        // Iniciar combate
        StartCombatAsync();
    }

    private void InitializeUI()
    {
        // Información del NPC
        NpcNameText.Text = _enemy.Name;
        UpdateNpcHealth();

        // Información del jugador
        PlayerNameText.Text = _gameState.Player.Name ?? "Jugador";
        UpdatePlayerStats();
        UpdateEquipmentText();
        UpdateCombatStatsDisplay();

        // Ocultar elementos de magia si no está habilitada
        if (!_magicEnabled)
        {
            ManaSection.Visibility = Visibility.Collapsed;
            UseAbilityButton.Visibility = Visibility.Collapsed;
        }

        // Estado inicial de botones
        SetActionButtonsEnabled(false);

        // Limpiar log y textos auxiliares
        CombatLogText.Inlines.Clear();
        ClearRollCalculations();
        ClearActionResult();
    }

    private void ClearRollCalculations()
    {
        PlayerRollCalcText.Text = "";
        PlayerRollResultText.Text = "";
        NpcRollCalcText.Text = "";
        NpcRollResultText.Text = "";
    }

    private void ShowPlayerRollCalculation(string calculation)
    {
        // Separar fórmula y resultado (formato: "12 + 3 (estado) + 0 (equipo) = 15")
        var parts = calculation.Split(" = ");
        if (parts.Length == 2)
        {
            PlayerRollCalcText.Text = parts[0];
            PlayerRollResultText.Text = parts[1];
        }
        else
        {
            PlayerRollCalcText.Text = calculation;
            PlayerRollResultText.Text = "";
        }
    }

    private void ShowNpcRollCalculation(string calculation)
    {
        // Separar fórmula y resultado (formato: "12 + 3 (estado) + 0 (equipo) = 15")
        var parts = calculation.Split(" = ");
        if (parts.Length == 2)
        {
            NpcRollCalcText.Text = parts[0];
            NpcRollResultText.Text = parts[1];
        }
        else
        {
            NpcRollCalcText.Text = calculation;
            NpcRollResultText.Text = "";
        }
    }

    private void ClearActionResult()
    {
        ActionResultText.Text = "";
    }

    private void ShowActionResult(string result, bool isSuccess)
    {
        ActionResultText.Text = result;
        ActionResultText.Foreground = new SolidColorBrush(
            isSuccess ? Color.FromRgb(144, 238, 144) : Color.FromRgb(255, 107, 107));
    }

    private void ShowPlayerAttackPreview()
    {
        var statBonus = _gameState.Player.Strength / 5;
        var equipBonus = 0;

        // Buscar arma equipada en ambas manos
        var rightHandId = _gameState.Player.EquippedRightHandId;
        var leftHandId = _gameState.Player.EquippedLeftHandId;
        if (!string.IsNullOrEmpty(rightHandId))
        {
            var weapon = _playerInventory.FirstOrDefault(o => o.Id == rightHandId && o.Type == ObjectType.Arma);
            if (weapon != null)
                equipBonus = weapon.AttackBonus;
        }
        if (equipBonus == 0 && !string.IsNullOrEmpty(leftHandId) && leftHandId != rightHandId)
        {
            var weapon = _playerInventory.FirstOrDefault(o => o.Id == leftHandId && o.Type == ObjectType.Arma);
            if (weapon != null)
                equipBonus = weapon.AttackBonus;
        }

        PlayerRollCalcText.Text = $"1d20 + {statBonus} (estado) + {equipBonus} (equipo)";
        PlayerRollResultText.Text = "";
    }

    private void ShowPlayerMagicAttackPreview(CombatAbility ability)
    {
        var statBonus = _gameState.Player.Intelligence / 5;
        var abilityBonus = ability.AttackValue;

        PlayerRollCalcText.Text = $"1d20 + {statBonus} (estado) + {abilityBonus} ({ability.Name})";
        PlayerRollResultText.Text = "";
    }

    private void UpdateNpcHealth()
    {
        var current = _enemy.Stats.CurrentHealth;
        var max = _enemy.Stats.MaxHealth;
        NpcHealthBar.Maximum = max;
        NpcHealthBar.Value = current;
        NpcHealthText.Text = $"{current}/{max}";

        // Color según salud
        if (current <= max * 0.25)
            NpcHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 68, 68)); // Rojo
        else if (current <= max * 0.5)
            NpcHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7)); // Amarillo
        else
            NpcHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // Verde
    }

    private void UpdatePlayerStats()
    {
        var stats = _gameState.Player.DynamicStats;

        // Salud
        PlayerHealthBar.Maximum = stats.MaxHealth;
        PlayerHealthBar.Value = stats.Health;
        PlayerHealthText.Text = $"{stats.Health}/{stats.MaxHealth}";

        // Color según salud
        if (stats.Health <= stats.MaxHealth * 0.25)
            PlayerHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 68, 68));
        else if (stats.Health <= stats.MaxHealth * 0.5)
            PlayerHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(255, 193, 7));
        else
            PlayerHealthBar.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80));

        // Mana
        PlayerManaBar.Maximum = stats.MaxMana;
        PlayerManaBar.Value = stats.Mana;
        PlayerManaText.Text = $"{stats.Mana}/{stats.MaxMana}";
    }

    private void UpdateEquipmentText()
    {
        var parts = new List<string>();

        // Mano derecha
        var rightHandId = _gameState.Player.EquippedRightHandId;
        if (!string.IsNullOrEmpty(rightHandId))
        {
            var item = _playerInventory.FirstOrDefault(o => o.Id == rightHandId);
            if (item != null)
            {
                if (item.Type == ObjectType.Arma)
                    parts.Add($"MD: {item.Name} (+{item.AttackBonus})");
                else if (item.Type == ObjectType.Armadura)
                    parts.Add($"MD: {item.Name} (+{item.DefenseBonus})");
            }
        }

        // Mano izquierda (solo si es diferente de mano derecha - arma de 2 manos muestra solo una vez)
        var leftHandId = _gameState.Player.EquippedLeftHandId;
        if (!string.IsNullOrEmpty(leftHandId) && leftHandId != rightHandId)
        {
            var item = _playerInventory.FirstOrDefault(o => o.Id == leftHandId);
            if (item != null)
            {
                if (item.Type == ObjectType.Arma)
                    parts.Add($"MI: {item.Name} (+{item.AttackBonus})");
                else if (item.Type == ObjectType.Armadura)
                    parts.Add($"MI: {item.Name} (+{item.DefenseBonus})");
            }
        }

        // Torso
        var torsoId = _gameState.Player.EquippedTorsoId;
        if (!string.IsNullOrEmpty(torsoId))
        {
            var armor = _playerInventory.FirstOrDefault(o => o.Id == torsoId);
            if (armor != null)
                parts.Add($"Torso: {armor.Name} (+{armor.DefenseBonus})");
        }

        PlayerEquipText.Text = parts.Count > 0 ? string.Join(" | ", parts) : "Sin equipo";
    }

    private void UpdateCombatStatsDisplay()
    {
        // Estadísticas del jugador (divididas entre 5 para obtener el bonus)
        var playerAtk = _gameState.Player.Strength / 5;
        var playerDef = _gameState.Player.Dexterity / 5;
        var playerMag = _gameState.Player.Intelligence / 5;

        PlayerAttackText.Text = $"ATQ: {playerAtk}";
        PlayerDefenseText.Text = $"DEF: {playerDef}";
        PlayerMagicText.Text = $"MAG: {playerMag}";

        // Estadísticas del NPC (divididas entre 5 para obtener el bonus)
        var npcAtk = _enemy.Stats.Strength / 5;
        var npcDef = _enemy.Stats.Dexterity / 5;
        var npcMag = _enemy.Stats.Intelligence / 5;

        NpcAttackText.Text = $"ATQ: {npcAtk}";
        NpcDefenseText.Text = $"DEF: {npcDef}";
        NpcMagicText.Text = $"MAG: {npcMag}";
    }

    private void UpdateCombatState()
    {
        var state = _gameState.ActiveCombat;
        if (state == null) return;

        RoundText.Text = $"Ronda {state.RoundNumber}";
        PhaseText.Text = GetPhaseText(state.Phase);

        // Determinar de quién es el turno basándose en la fase
        if (state.Phase == CombatPhase.Initiative)
        {
            TurnText.Text = "Tira para iniciativa";
            TurnText.Foreground = new SolidColorBrush(Color.FromRgb(255, 215, 0)); // Dorado
        }
        else if (state.Phase == CombatPhase.PlayerAction || state.Phase == CombatPhase.PlayerRoll)
        {
            TurnText.Text = "Tu turno";
            TurnText.Foreground = new SolidColorBrush(Color.FromRgb(144, 238, 144)); // Verde claro
        }
        else if (state.Phase == CombatPhase.NpcAction || state.Phase == CombatPhase.NpcRoll)
        {
            TurnText.Text = "Turno del enemigo";
            TurnText.Foreground = new SolidColorBrush(Color.FromRgb(255, 107, 107)); // Rojo claro
        }
        else
        {
            TurnText.Text = "";
        }
    }

    private string GetPhaseText(CombatPhase phase) => phase switch
    {
        CombatPhase.Initiative => "Iniciativa",
        CombatPhase.PlayerAction => "Elige acción",
        CombatPhase.PlayerRoll => "Tira el dado",
        CombatPhase.NpcAction => "Enemigo elige",
        CombatPhase.NpcRoll => "Enemigo tira",
        CombatPhase.Resolution => "Resolución",
        CombatPhase.RoundEnd => "Fin de ronda",
        CombatPhase.Victory => "Victoria",
        CombatPhase.Defeat => "Derrota",
        _ => phase.ToString()
    };

    private void SetActionButtonsEnabled(bool enabled)
    {
        AttackButton.IsEnabled = enabled;
        DefendButton.IsEnabled = enabled;
        FleeButton.IsEnabled = enabled;
        UseItemButton.IsEnabled = enabled && _playerInventory.Any(o =>
            o.Type == ObjectType.Comida || o.Type == ObjectType.Bebida);

        // Habilidad disponible si hay mana Y el jugador tiene al menos una habilidad
        var abilities = _combatEngine.GetPlayerAbilities();
        var hasUsableAbility = abilities.Any(a => _gameState.Player.DynamicStats.Mana >= a.ManaCost);
        UseAbilityButton.IsEnabled = enabled && hasUsableAbility;
    }

    private void StartCombatAsync()
    {
        // Iniciar combate en el motor
        _combatEngine.StartCombat(_enemy.Id);

        UpdateCombatState();

        // Fase de iniciativa - habilitar dado del jugador para que haga click
        PlayerDice.UseCriticalColors = false;
        PlayerDice.IsRollEnabled = true;
        PlayerDice.Reset();

        // Suscribirse a eventos del dado
        PlayerDice.RollRequested += OnInitiativeRollRequested;
        PlayerDice.RollCompleted += OnInitiativeRollCompleted;
    }

    private void OnInitiativeRollRequested()
    {
        // Ocultar texto inmediatamente al hacer click
        TurnText.Text = "";
    }

    private async void OnInitiativeRollCompleted(int result)
    {
        // Desuscribirse de los eventos para que no se vuelvan a llamar
        PlayerDice.RollRequested -= OnInitiativeRollRequested;
        PlayerDice.RollCompleted -= OnInitiativeRollCompleted;
        PlayerDice.IsRollEnabled = false;

        // Verificar que el combate sigue activo (podría haberse cerrado la ventana)
        if (_gameState.ActiveCombat == null || !_gameState.ActiveCombat.IsActive)
            return;

        await ProcessInitiativeRoll(result);
    }

    private async void OnActionRollCompleted(int result)
    {
        // Desuscribirse del evento
        PlayerDice.RollCompleted -= OnActionRollCompleted;

        // Ocultar botón cancelar
        CancelActionButton.Visibility = Visibility.Collapsed;
        PlayerDice.IsRollEnabled = false;

        // Verificar que el combate sigue activo (podría haberse cerrado la ventana)
        if (_gameState.ActiveCombat == null || !_gameState.ActiveCombat.IsActive)
            return;

        // Guardar resultado del dado
        _lastPlayerDiceResult = result;

        // Ejecutar la acción pendiente
        var pendingAction = _pendingAction;
        var pendingAbility = _pendingAbility;
        _pendingAction = CombatAction.None;
        _pendingAbility = null;

        switch (pendingAction)
        {
            case CombatAction.Attack:
                await ExecutePlayerTurnAsync();
                break;

            case CombatAction.Flee:
                await ExecutePlayerTurnAsync();
                break;

            case CombatAction.UseAbility:
                if (pendingAbility != null)
                    await ExecuteMagicAttackAsync(pendingAbility);
                break;
        }
    }

    private async Task ProcessInitiativeRoll(int result)
    {
        var state = _gameState.ActiveCombat;
        if (state == null) return;

        // El motor usa el valor del dado visual del jugador
        var playerInit = _combatEngine.RollPlayerInitiative(result);
        ShowPlayerRollCalculation(playerInit.Breakdown);

        // Pequeña pausa antes de la tirada del NPC
        await Task.Delay(500);

        // Tirada del NPC con animación - el motor genera el valor y lo mostramos en el dado
        var npcInit = _combatEngine.RollNpcInitiative();
        await NpcDice.RollAsync(npcInit.DiceValue, useCriticalColors: false);
        ShowNpcRollCalculation(npcInit.Breakdown);

        // Pausa para mostrar resultados
        await Task.Delay(2800);

        // Resolver iniciativa
        var playerWins = _combatEngine.ResolveInitiative();
        _initiativeResolved = true;

        // Mostrar resultado de iniciativa en el centro
        if (playerWins)
            ShowActionResult("¡Ganas la iniciativa!", true);
        else
            ShowActionResult($"{_enemy.Name} gana", false);

        await Task.Delay(1200);
        ClearActionResult();
        ClearRollCalculations();
        await ProcessNextPhaseAsync();
    }

    private async Task ProcessNextPhaseAsync()
    {
        var state = _gameState.ActiveCombat;
        if (state == null || !state.IsActive) return;

        UpdateCombatState();
        UpdateNpcHealth();
        UpdatePlayerStats();
        UpdateEquipmentText();

        switch (state.Phase)
        {
            case CombatPhase.Victory:
            case CombatPhase.Defeat:
                // El combate terminará por el evento
                break;

            case CombatPhase.PlayerAction:
                // Habilitar botones de acción
                SetActionButtonsEnabled(true);
                break;

            case CombatPhase.NpcAction:
                // Turno del NPC
                await ProcessNpcTurnAsync();
                break;

            default:
                // Otros estados, continuar procesando
                break;
        }
    }

    private async Task ExecutePlayerTurnAsync()
    {
        var state = _gameState.ActiveCombat;
        if (state == null) return;

        ClearRollCalculations();
        ClearActionResult();

        switch (state.PlayerAction)
        {
            case CombatAction.Attack:
                // Animación del dado del NPC para defensa - generamos el valor primero
                await Task.Delay(500);
                var npcDefenseValue = new Random().Next(1, 21);
                await NpcDice.RollAsync(npcDefenseValue);

                // Ejecutar ataque con los valores de los dados visuales
                var damageResult = _combatEngine.ExecutePlayerAttack(_lastPlayerDiceResult, npcDefenseValue);

                // Mostrar cálculos de ambos
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");
                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");

                // Mostrar resultado del ataque
                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult("¡Fallo épico!", false);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡CRÍTICO!\n-{damageResult.FinalDamage} HP", true);
                    else
                        ShowActionResult($"¡Impacto!\n-{damageResult.FinalDamage} HP", true);
                }
                else
                {
                    ShowActionResult("Bloqueado", false);
                }

                // Pausa para mostrar resultado
                await Task.Delay(3500);
                ClearActionResult();
                ClearRollCalculations();
                break;

            case CombatAction.Flee:
                var fled = _combatEngine.AttemptFlee();
                if (fled)
                    ShowActionResult("¡Huyes!", true);
                else
                    ShowActionResult("¡No escapas!", false);
                await Task.Delay(1200);
                ClearActionResult();
                break;

            case CombatAction.UseItem:
                if (!string.IsNullOrEmpty(state.SelectedItemId))
                    _combatEngine.UseItem(state.SelectedItemId);
                ShowActionResult("Objeto usado", true);
                await Task.Delay(1000);
                ClearActionResult();
                break;

            case CombatAction.UseAbility:
                // La lógica de habilidades ahora se maneja en UseAbilityButton_Click
                // y ExecuteMagicAttackAsync
                break;
        }

        await Task.Delay(500);
        await ProcessNextPhaseAsync();
    }

    private async Task ProcessNpcTurnAsync()
    {
        var state = _gameState.ActiveCombat;
        if (state == null) return;

        ClearRollCalculations();
        ClearActionResult();

        await Task.Delay(500);

        // Generar valores de dados primero
        var random = new Random();
        var npcAttackValue = random.Next(1, 21);
        var playerDefenseValue = random.Next(1, 21);

        // Comprobar opciones mágicas del NPC
        var npcAbilities = _combatEngine.GetNpcAbilities(); // Solo si NPC.MagicEnabled
        var npcMagicWeapon = _combatEngine.GetNpcMagicWeapon();

        // Decidir tipo de ataque:
        // - Si tiene habilidades mágicas: 50% chance de usarlas
        // - Si no tiene habilidades pero tiene arma mágica: 50% chance de usarla
        // - En otro caso: ataque físico
        bool usesMagicAbility = npcAbilities.Any() && random.Next(100) < 50;
        bool usesMagicWeapon = !usesMagicAbility && npcMagicWeapon != null && random.Next(100) < 50;
        CombatAbility? npcMagicAbility = null;

        if (usesMagicAbility)
        {
            npcMagicAbility = npcAbilities[random.Next(npcAbilities.Count)];
        }

        // Mostrar la acción elegida por el enemigo
        if (usesMagicAbility && npcMagicAbility != null)
        {
            TurnText.Text = $"Usa {npcMagicAbility.Name}";
        }
        else if (usesMagicWeapon && npcMagicWeapon != null)
        {
            TurnText.Text = $"Usa {npcMagicWeapon.Name}";
        }
        else
        {
            TurnText.Text = "Te ataca";
        }

        // Animación del dado del NPC (ataque)
        await NpcDice.RollAsync(npcAttackValue);
        await Task.Delay(500);

        // Animación del dado del jugador (defensa)
        await PlayerDice.RollAsync(playerDefenseValue);

        DamageResult damageResult;

        // Determinar el tipo de combate según las combinaciones de ataque/defensa
        if (usesMagicAbility && npcMagicAbility != null)
        {
            // NPC usa ataque mágico
            if (_selectedDefenseAbility != null)
            {
                // Jugador usa defensa mágica contra ataque mágico
                var ability = _selectedDefenseAbility;
                _selectedDefenseAbility = null;

                damageResult = _combatEngine.ExecuteMagicDefenseVsMagicAttack(
                    ability, npcMagicAbility, npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"¡{npcMagicAbility.Name}\nfalla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡HECHIZO CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"¡{npcMagicAbility.Name}!\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult($"¡{ability.Name}!\nBloqueado", true);
                }
            }
            else
            {
                // Jugador usa defensa normal contra ataque mágico
                damageResult = _combatEngine.ExecuteNpcMagicAttack(npcMagicAbility, npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"¡{npcMagicAbility.Name}\nfalla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡HECHIZO CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"¡{npcMagicAbility.Name}!\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult("¡Resistes!", true);
                }
            }
        }
        else if (usesMagicWeapon && npcMagicWeapon != null)
        {
            // NPC usa arma mágica
            if (_selectedDefenseAbility != null)
            {
                // Jugador usa defensa mágica contra arma mágica
                var ability = _selectedDefenseAbility;
                _selectedDefenseAbility = null;

                // Usar defensa mágica (la habilidad defensiva) contra el ataque del arma mágica
                damageResult = _combatEngine.ExecuteMagicDefense(ability, npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"¡{npcMagicWeapon.Name}\nfalla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡GOLPE MÁGICO CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"¡{npcMagicWeapon.Name}!\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult($"¡{ability.Name}!\nBloqueado", true);
                }
            }
            else
            {
                // Jugador usa defensa normal contra arma mágica
                damageResult = _combatEngine.ExecuteNpcMagicWeaponAttack(npcMagicWeapon, npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"¡{npcMagicWeapon.Name}\nfalla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡GOLPE MÁGICO CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"¡{npcMagicWeapon.Name}!\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult("¡Resistes!", true);
                }
            }
        }
        else
        {
            // NPC usa ataque físico
            if (_selectedDefenseAbility != null)
            {
                // Jugador usa defensa mágica contra ataque físico
                var ability = _selectedDefenseAbility;
                _selectedDefenseAbility = null;

                damageResult = _combatEngine.ExecuteMagicDefense(ability, npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"{_enemy.Name}\n¡Falla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"Te golpea\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult($"¡{ability.Name}!\nBloqueado", true);
                }
            }
            else
            {
                // Jugador usa defensa normal contra ataque físico
                damageResult = _combatEngine.ExecuteNpcTurn(npcAttackValue, playerDefenseValue);

                ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");
                ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");

                await Task.Delay(300);
                if (damageResult.WasFumble)
                {
                    ShowActionResult($"{_enemy.Name}\n¡Falla!", true);
                }
                else if (damageResult.Hit)
                {
                    if (damageResult.WasCritical)
                        ShowActionResult($"¡CRÍTICO!\n-{damageResult.FinalDamage} HP", false);
                    else
                        ShowActionResult($"Te golpea\n-{damageResult.FinalDamage} HP", false);
                }
                else
                {
                    ShowActionResult("¡Bloqueas!", true);
                }
            }
        }

        UpdatePlayerStats();

        // Pausa para mostrar resultado
        await Task.Delay(3500);
        ClearActionResult();
        ClearRollCalculations();

        await Task.Delay(300);
        await ProcessNextPhaseAsync();
    }

    private void EndCombat(CombatEndReason reason)
    {
        SetActionButtonsEnabled(false);
        CombatEnded?.Invoke(reason);
        DialogResult = reason == CombatEndReason.Victory;
        Close();
    }

    private void OnLogEntryAdded(object? sender, CombatLogEntry entry)
    {
        Dispatcher.Invoke(() =>
        {
            // Si es un header de ronda, añadir salto de línea extra antes
            bool isRoundHeader = entry.LogType == CombatLogType.System && entry.Message.Contains("--- Ronda");

            if (CombatLogText.Inlines.Count > 0)
            {
                CombatLogText.Inlines.Add(new Run(Environment.NewLine));
            }

            // Añadir salto de línea extra antes del header de ronda para separarlo visualmente
            if (isRoundHeader && CombatLogText.Inlines.Count > 0)
            {
                CombatLogText.Inlines.Add(new Run(Environment.NewLine));
            }

            // Determinar si el mensaje debe ser en negrita
            var isBold = entry.LogType is CombatLogType.Hit or CombatLogType.Miss
                or CombatLogType.Critical or CombatLogType.Fumble
                or CombatLogType.Victory or CombatLogType.Defeat;

            var run = new Run(entry.Message);
            if (isBold)
                run.FontWeight = FontWeights.Bold;

            CombatLogText.Inlines.Add(run);

            // Si es un header de ronda, añadir la línea divisoria justo después
            if (isRoundHeader)
            {
                CombatLogText.Inlines.Add(new Run(Environment.NewLine + "────────────────────────────────"));
            }

            CombatLogScrollViewer.ScrollToEnd();
        });
    }

    private void OnCombatEnded(object? sender, CombatEndEventArgs args)
    {
        Dispatcher.Invoke(async () =>
        {
            UpdateCombatState();
            UpdateNpcHealth();
            UpdatePlayerStats();

            await Task.Delay(1500);
            EndCombat(args.Reason);
        });
    }

    #region Button Handlers

    private void AttackButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState.ActiveCombat?.Phase != CombatPhase.PlayerAction) return;

        SetActionButtonsEnabled(false);
        _combatEngine.SetPlayerAction(CombatAction.Attack);
        TurnText.Text = "";

        // Mostrar previsualización del cálculo
        ShowPlayerAttackPreview();

        // Preparar dado para click manual
        _pendingAction = CombatAction.Attack;
        PlayerDice.UseCriticalColors = true;
        PlayerDice.Reset();
        PlayerDice.IsRollEnabled = true;
        PlayerDice.RollCompleted += OnActionRollCompleted;

        // Mostrar botón cancelar
        CancelActionButton.Visibility = Visibility.Visible;
    }

    private async void DefendButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState.ActiveCombat?.Phase != CombatPhase.PlayerAction) return;

        SetActionButtonsEnabled(false);
        _combatEngine.SetPlayerAction(CombatAction.Defend);
        TurnText.Text = "";

        // El motor ya cambia la fase a NpcAction
        await ProcessNextPhaseAsync();
    }

    private void FleeButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState.ActiveCombat?.Phase != CombatPhase.PlayerAction) return;

        SetActionButtonsEnabled(false);
        _combatEngine.SetPlayerAction(CombatAction.Flee);
        TurnText.Text = "";

        // Mostrar previsualización (huir usa Destreza)
        var statBonus = _gameState.Player.Dexterity / 5;
        PlayerRollCalcText.Text = $"1d20 + {statBonus} (estado)";
        PlayerRollResultText.Text = "";

        // Preparar dado para click manual
        _pendingAction = CombatAction.Flee;
        PlayerDice.UseCriticalColors = true;
        PlayerDice.Reset();
        PlayerDice.IsRollEnabled = true;
        PlayerDice.RollCompleted += OnActionRollCompleted;

        // Mostrar botón cancelar
        CancelActionButton.Visibility = Visibility.Visible;
    }

    private async void UseItemButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState.ActiveCombat?.Phase != CombatPhase.PlayerAction) return;

        // Buscar objetos consumibles
        var consumables = _playerInventory
            .Where(o => o.Type == ObjectType.Comida || o.Type == ObjectType.Bebida)
            .ToList();

        if (!consumables.Any()) return;

        // Por ahora, usar el primer objeto disponible (simplificado)
        var item = consumables.First();

        SetActionButtonsEnabled(false);
        TurnText.Text = "";

        // Aplicar efecto del objeto (simplificado: restaurar salud)
        var healAmount = 25;
        _gameState.Player.DynamicStats.Health = Math.Min(
            _gameState.Player.DynamicStats.MaxHealth,
            _gameState.Player.DynamicStats.Health + healAmount);

        // Remover objeto del inventario
        _gameState.InventoryObjectIds.Remove(item.Id);
        _playerInventory.Remove(item);

        _combatEngine.UseItem(item.Id);
        UpdatePlayerStats();

        await ProcessNextPhaseAsync();
    }

    private async void UseAbilityButton_Click(object sender, RoutedEventArgs e)
    {
        if (_gameState.ActiveCombat?.Phase != CombatPhase.PlayerAction) return;

        var abilities = _combatEngine.GetPlayerAbilities();
        if (!abilities.Any()) return;

        // Abrir ventana de selección de habilidades
        var selectionWindow = new AbilitySelectionWindow(abilities, _gameState.Player.DynamicStats.Mana)
        {
            Owner = this
        };

        if (selectionWindow.ShowDialog() != true || selectionWindow.SelectedAbility == null)
            return;

        var selectedAbility = selectionWindow.SelectedAbility;

        if (selectedAbility.AbilityType == AbilityType.Attack)
        {
            // Habilidad de ataque: mostrar previsualización y esperar click en dado
            SetActionButtonsEnabled(false);
            _combatEngine.SetPlayerAction(CombatAction.UseAbility, selectedAbility.Id);
            TurnText.Text = "";

            // Mostrar previsualización del cálculo mágico
            ShowPlayerMagicAttackPreview(selectedAbility);

            // Preparar dado para click manual
            _pendingAction = CombatAction.UseAbility;
            _pendingAbility = selectedAbility;
            PlayerDice.UseCriticalColors = true;
            PlayerDice.Reset();
            PlayerDice.IsRollEnabled = true;
            PlayerDice.RollCompleted += OnActionRollCompleted;

            // Mostrar botón cancelar
            CancelActionButton.Visibility = Visibility.Visible;
        }
        else
        {
            // Habilidad de defensa: se usará cuando el NPC ataque
            _selectedDefenseAbility = selectedAbility;
            SetActionButtonsEnabled(false);
            TurnText.Text = "";

            // Mostrar mensaje de habilidad defensiva seleccionada
            ShowActionResult($"{selectedAbility.Name}\npreparada", true);
            await Task.Delay(1000);
            ClearActionResult();

            // Pasar al turno del NPC, que usará la defensa mágica
            _gameState.ActiveCombat!.Phase = CombatPhase.NpcAction;
            await ProcessNextPhaseAsync();
        }
    }

    private void CancelActionButton_Click(object sender, RoutedEventArgs e)
    {
        // Desuscribir evento de dado
        PlayerDice.RollCompleted -= OnActionRollCompleted;
        PlayerDice.IsRollEnabled = false;
        PlayerDice.Reset();

        // Limpiar acción pendiente
        _pendingAction = CombatAction.None;
        _pendingAbility = null;

        // Ocultar botón cancelar
        CancelActionButton.Visibility = Visibility.Collapsed;

        // Limpiar previsualización
        ClearRollCalculations();

        // Restaurar fase a PlayerAction para que pueda elegir otra acción
        if (_gameState.ActiveCombat != null)
        {
            _gameState.ActiveCombat.Phase = CombatPhase.PlayerAction;
            _gameState.ActiveCombat.PlayerAction = CombatAction.None;
        }

        UpdateCombatState();
        SetActionButtonsEnabled(true);
    }

    private async Task ExecuteMagicAttackAsync(CombatAbility ability)
    {
        var state = _gameState.ActiveCombat;
        if (state == null) return;

        ClearRollCalculations();
        ClearActionResult();

        // Animación del dado del NPC para defensa
        await Task.Delay(500);
        var npcDefenseValue = new Random().Next(1, 21);
        await NpcDice.RollAsync(npcDefenseValue);

        // Ejecutar ataque mágico con los valores de los dados
        var damageResult = _combatEngine.ExecuteMagicAttack(ability, _lastPlayerDiceResult, npcDefenseValue);

        // Mostrar cálculos
        ShowPlayerRollCalculation(state.LastPlayerRoll?.Breakdown ?? "");
        ShowNpcRollCalculation(state.LastNpcRoll?.Breakdown ?? "");

        // Mostrar resultado del ataque
        await Task.Delay(300);
        if (damageResult.WasFumble)
        {
            ShowActionResult("¡Fallo mágico!", false);
        }
        else if (damageResult.Hit)
        {
            if (damageResult.WasCritical)
                ShowActionResult($"¡CRÍTICO!\n-{damageResult.FinalDamage} HP", true);
            else
                ShowActionResult($"¡{ability.Name}!\n-{damageResult.FinalDamage} HP", true);
        }
        else
        {
            ShowActionResult("Resistido", false);
        }

        UpdatePlayerStats();

        // Pausa para mostrar resultado
        await Task.Delay(3500);
        ClearActionResult();
        ClearRollCalculations();

        await Task.Delay(500);
        await ProcessNextPhaseAsync();
    }

    #endregion

    protected override async void OnClosing(System.ComponentModel.CancelEventArgs e)
    {
        // Si ya estamos procesando huida forzada, permitir cierre
        if (_isProcessingForcedFlee)
        {
            base.OnClosing(e);
            return;
        }

        // Si el combate sigue activo y ya se resolvió la iniciativa
        if (_gameState.ActiveCombat != null && _gameState.ActiveCombat.IsActive && _initiativeResolved)
        {
            // Cancelar el cierre para mostrar la animación de huida
            e.Cancel = true;
            _isProcessingForcedFlee = true;

            // Deshabilitar botones
            SetActionButtonsEnabled(false);

            // Mostrar previsualización de huida
            var statBonus = _gameState.Player.Dexterity / 5;
            PlayerRollCalcText.Text = $"1d20 + {statBonus} (estado)";
            PlayerRollResultText.Text = "";

            // Configurar acción de huida
            _combatEngine.SetPlayerAction(CombatAction.Flee);

            // Tirada del dado del jugador con animación
            var diceResult = await PlayerDice.RollAsync();

            // Ejecutar huida
            var fled = _combatEngine.AttemptFlee();

            // Mostrar resultado
            if (fled)
                ShowActionResult("¡Huyes!", true);
            else
                ShowActionResult("¡No escapas!", false);

            // Esperar para que el jugador vea el resultado
            await Task.Delay(2000);

            // Ahora sí cerrar
            CombatEnded?.Invoke(CombatEndReason.Fled);
            Close();
            return;
        }

        base.OnClosing(e);
    }

    protected override void OnClosed(EventArgs e)
    {
        // Desuscribirse de eventos
        _combatEngine.LogEntryAdded -= OnLogEntryAdded;
        _combatEngine.CombatEnded -= OnCombatEnded;

        // Si el combate sigue activo y no se procesó huida forzada
        if (_gameState.ActiveCombat != null && _gameState.ActiveCombat.IsActive && !_isProcessingForcedFlee)
        {
            // Antes de iniciativa: cancelar combate completamente
            _gameState.ActiveCombat = null;
        }

        base.OnClosed(e);
    }
}
