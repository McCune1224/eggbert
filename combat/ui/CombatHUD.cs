using Godot;
using System.Collections.Generic;

public partial class CombatHUD : CanvasLayer
{
    private PanelContainer _playerPanel;
    private ColorRect _playerBarBg;
    private ColorRect _playerBarFill;
    private Label _playerLabel;
    private HealthComponent _playerHC;

    private PanelContainer _enemyPanel;
    private VBoxContainer _enemyList;
    private struct EnemyBar
    {
        public HealthComponent HC;
        public Label NameLabel;
        public ColorRect Bg;
        public ColorRect Fill;
    }
    private List<EnemyBar> _enemyBars = new();

    private static readonly Color BarBgColor = new Color(0.1f, 0.1f, 0.15f, 0.9f);
    private static readonly Color PlayerBarColor = new Color(0.9098039f, 0.72156864f, 0.3764706f);
    private static readonly Color EnemyBarColor = new Color(0.8784314f, 0.40784314f, 0.40784314f);
    private static readonly Color LowHpColor = new Color(0.91f, 0.45f, 0.1f);
    private static readonly Color CriticalHpColor = new Color(0.8784314f, 0.40784314f, 0.40784314f);

    private const int BarWidth = 140;
    private const int BarHeight = 12;
    private const int EnemyBarWidth = 110;
    private const int EnemyBarHeight = 8;
    private const int EnemyRowHeight = 16;

    public override void _Ready()
    {
        Layer = 128;

        const int sideMargin = 8;
        const int topMargin = 8;

        // Player HP panel (HudPanel) wrapping the label + bar.
        _playerPanel = new PanelContainer
        {
            ThemeTypeVariation = "HudPanel",
            Position = new Vector2(sideMargin, topMargin)
        };
        var playerVBox = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var playerRow = new HBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore,
            CustomMinimumSize = new Vector2(BarWidth + 8, 0)
        };
        _playerLabel = new Label
        {
            Text = "HP",
            ThemeTypeVariation = "HudLabel",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _playerLabel.AddThemeFontSizeOverride("font_size", 11);
        playerRow.AddChild(_playerLabel);
        playerVBox.AddChild(playerRow);

        var barContainer = new Control
        {
            CustomMinimumSize = new Vector2(BarWidth, BarHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _playerBarBg = new ColorRect
        {
            Position = Vector2.Zero,
            Size = new Vector2(BarWidth, BarHeight),
            Color = BarBgColor,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _playerBarFill = new ColorRect
        {
            Position = Vector2.Zero,
            Size = new Vector2(BarWidth, BarHeight),
            Color = PlayerBarColor,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        barContainer.AddChild(_playerBarBg);
        barContainer.AddChild(_playerBarFill);
        playerVBox.AddChild(barContainer);
        _playerPanel.AddChild(playerVBox);
        AddChild(_playerPanel);

        // Enemy bars panel (HudPanel) — top-right, auto-grows with rows.
        const int enemyPanelWidth = EnemyBarWidth + 16;  // bar + padding
        _enemyPanel = new PanelContainer
        {
            ThemeTypeVariation = "HudPanel",
            Position = new Vector2(640 - sideMargin - enemyPanelWidth, topMargin),
            CustomMinimumSize = new Vector2(enemyPanelWidth, 0)
        };
        _enemyList = new VBoxContainer
        {
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        _enemyList.AddThemeConstantOverride("separation", 2);
        _enemyPanel.AddChild(_enemyList);
        AddChild(_enemyPanel);

        GameLogger.Debug("Combat", "CombatHUD: _Ready");
    }

    public void SetPlayerHealthComponent(HealthComponent hc)
    {
        _playerHC = hc;
        UpdatePlayerBar();
        hc.Damaged += OnPlayerDamaged;
        hc.Healed += OnPlayerHealed;
    }

    public void AddEnemy(string name, HealthComponent hc)
    {
        int index = _enemyBars.Count;
        int y = index * EnemyRowHeight;  // tracked for legacy callers / debug

        // Row: name label above the bar; everything inside the themed panel.
        var row = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };

        var label = new Label
        {
            Text = name,
            ThemeTypeVariation = "HudLabel",
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        row.AddChild(label);

        var barContainer = new Control
        {
            CustomMinimumSize = new Vector2(EnemyBarWidth, EnemyBarHeight),
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var bg = new ColorRect
        {
            Position = Vector2.Zero,
            Size = new Vector2(EnemyBarWidth, EnemyBarHeight),
            Color = BarBgColor,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        var fill = new ColorRect
        {
            Position = Vector2.Zero,
            Size = new Vector2(EnemyBarWidth, EnemyBarHeight),
            Color = EnemyBarColor,
            MouseFilter = Control.MouseFilterEnum.Ignore
        };
        barContainer.AddChild(bg);
        barContainer.AddChild(fill);
        row.AddChild(barContainer);

        _enemyList.AddChild(row);

        var entry = new EnemyBar { HC = hc, NameLabel = label, Bg = bg, Fill = fill };
        _enemyBars.Add(entry);

        UpdateEnemyBar(entry);

        hc.Damaged += (amount, source) => UpdateEnemyBarSafe(entry);
        hc.Healed += (amount) => UpdateEnemyBarSafe(entry);

        GameLogger.Debug("Combat", $"CombatHUD: added enemy bar '{name}' — {_enemyBars.Count} total");
    }

    private void UpdatePlayerBar()
    {
        if (_playerHC == null) return;
        float pct = (float)_playerHC.CurrentHP / _playerHC.MaxHP;
        _playerBarFill.Size = new Vector2(BarWidth * pct, BarHeight);

        Color newColor = pct <= 0.25f ? CriticalHpColor :
                         pct <= 0.5f ? LowHpColor :
                         PlayerBarColor;
        if (newColor != _playerBarFill.Color)
            GameLogger.Debug("Combat", $"CombatHUD: player HP threshold — {_playerHC.CurrentHP}/{_playerHC.MaxHP} ({pct*100:F0}%)");
        _playerBarFill.Color = newColor;
    }

    private void UpdateEnemyBar(EnemyBar bar)
    {
        if (bar.HC == null) return;
        float pct = (float)bar.HC.CurrentHP / bar.HC.MaxHP;
        bar.Fill.Size = new Vector2(EnemyBarWidth * pct, EnemyBarHeight);

        bar.Fill.Color = pct <= 0.25f ? CriticalHpColor :
                         pct <= 0.5f ? LowHpColor :
                         EnemyBarColor;

        if (bar.HC.IsDead)
            bar.NameLabel.Modulate = new Color(0.53333336f, 0.53333336f, 0.53333336f, 1f);
    }

    private void UpdateEnemyBarSafe(EnemyBar bar)
    {
        if (IsInsideTree())
            UpdateEnemyBar(bar);
    }

    private void OnPlayerDamaged(int amount, Node source) { if (IsInsideTree()) { UpdatePlayerBar(); GameLogger.Debug("Combat", $"CombatHUD: player took {amount} DMG — HP={_playerHC?.CurrentHP ?? -1}"); } }
    private void OnPlayerHealed(int amount) { if (IsInsideTree()) { UpdatePlayerBar(); GameLogger.Debug("Combat", $"CombatHUD: player healed {amount} — HP={_playerHC?.CurrentHP ?? -1}"); } }

    public override void _ExitTree()
    {
        if (_playerHC != null)
        {
            _playerHC.Damaged -= OnPlayerDamaged;
            _playerHC.Healed -= OnPlayerHealed;
        }
        GameLogger.Debug("Combat", "CombatHUD: _ExitTree");
    }
}
