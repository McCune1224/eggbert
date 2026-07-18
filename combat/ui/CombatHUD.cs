using Godot;
using System.Collections.Generic;

public partial class CombatHUD : CanvasLayer
{
    private ColorRect _playerBarBg;
    private ColorRect _playerBarFill;
    private HealthComponent _playerHC;

    private struct EnemyBar
    {
        public HealthComponent HC;
        public Label NameLabel;
        public ColorRect Bg;
        public ColorRect Fill;
    }

    private List<EnemyBar> _enemyBars = new();

    private static readonly Color BarBgColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
    private static readonly Color PlayerBarColor = new Color(0.2f, 0.8f, 0.2f);
    private static readonly Color EnemyBarColor = new Color(0.8f, 0.2f, 0.2f);
    private static readonly Color LowHpColor = new Color(0.9f, 0.9f, 0.1f);
    private static readonly Color CriticalHpColor = new Color(0.9f, 0.2f, 0.1f);

    private const int BarWidth = 140;
    private const int BarHeight = 12;
    private const int EnemyBarWidth = 110;
    private const int EnemyBarHeight = 8;
    private const int EnemyRowHeight = 16;

    public override void _Ready()
    {
        Layer = 128;

        int sideMargin = 12;
        int topMargin = 10;

        var playerLabel = new Label
        {
            Text = "HP",
            Position = new Vector2(sideMargin, topMargin - 14)
        };
        playerLabel.AddThemeFontSizeOverride("font_size", 11);
        AddChild(playerLabel);

        _playerBarBg = new ColorRect
        {
            Position = new Vector2(sideMargin, topMargin),
            Size = new Vector2(BarWidth, BarHeight),
            Color = BarBgColor
        };
        AddChild(_playerBarBg);

        _playerBarFill = new ColorRect
        {
            Position = new Vector2(sideMargin, topMargin),
            Size = new Vector2(BarWidth, BarHeight),
            Color = PlayerBarColor
        };
        AddChild(_playerBarFill);

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
        int sideMargin = 12;
        int startY = 50;
        int index = _enemyBars.Count;

        int x = 640 - sideMargin - EnemyBarWidth;
        int y = startY + index * EnemyRowHeight;

        var label = new Label
        {
            Text = name,
            Position = new Vector2(x, y - 11)
        };
        label.AddThemeFontSizeOverride("font_size", 9);
        AddChild(label);

        var bg = new ColorRect
        {
            Position = new Vector2(x, y),
            Size = new Vector2(EnemyBarWidth, EnemyBarHeight),
            Color = BarBgColor
        };
        AddChild(bg);

        var fill = new ColorRect
        {
            Position = new Vector2(x, y),
            Size = new Vector2(EnemyBarWidth, EnemyBarHeight),
            Color = EnemyBarColor
        };
        AddChild(fill);

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
            bar.NameLabel.Modulate = new Color(0.5f, 0.5f, 0.5f, 0.5f);
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
