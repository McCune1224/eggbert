using Godot;

/// <summary>
/// Deltarune-style graze meter (docs/combat-ui-design.md §3.2 C6).
/// Enemy bullets passing close to the player (but not hitting) fill a meter;
/// a full meter triggers a screen-wide shockwave that damages every enemy.
/// Graze radius grows with equipment (Graze Charm → CombatStats.GrazeRadiusBonus).
/// Rendered as a bottom-center bar; added as a child of the CombatHUD CanvasLayer.
/// </summary>
public partial class GrazeSystem : Node2D
{
    private const float BaseRadius = 24f;
    private const float PointsPerGraze = 8f;
    private const float MaxValue = 100f;
    private const float MeterWidth = 120f;
    private const float MeterHeight = 5f;

    private float _value = 0f;
    private CombatHUD _hud;

    public override void _Ready()
    {
        _hud = GetParentOrNull<CombatHUD>();
        ZIndex = 10;
        GameLogger.Debug("Combat", "GrazeSystem: ready");
    }

    public override void _Process(double delta)
    {
        if (GameController.Instance?.CurrentLevel is not CombatArena) return;
        var player = Player.Instance;
        if (player == null) return;

        float radius = BaseRadius + CombatStats.GrazeRadiusBonus;

        foreach (Node node in GetTree().GetNodesInGroup("bullet"))
        {
            if (node is not RedBullet bullet || !GodotObject.IsInstanceValid(bullet)) continue;
            if (bullet.Reflected || bullet.HasBeenGrazed) continue;

            if (bullet.GlobalPosition.DistanceTo(player.GlobalPosition) <= radius)
            {
                bullet.HasBeenGrazed = true;
                _value = Mathf.Min(MaxValue, _value + PointsPerGraze);
                GameLogger.Debug("Combat", $"Graze! +{PointsPerGraze} — {_value:0}/{MaxValue:0}");
                if (_value >= MaxValue)
                    TriggerShockwave();
            }
        }

        QueueRedraw();
    }

    /// <summary>Full meter: shockwave that damages every living enemy.</summary>
    private void TriggerShockwave()
    {
        _value = 0f;
        int dmg = 10 + Equipment.Instance.TotalAttackBoost;
        int hits = 0;

        foreach (Node node in GetTree().GetNodesInGroup("enemy"))
        {
            if (node is Node2D enemyNode && GodotObject.IsInstanceValid(enemyNode))
            {
                var health = enemyNode.GetNodeOrNull<HealthComponent>("HealthComponent");
                if (health != null && !health.IsDead)
                {
                    health.TakeDamage(dmg, this);
                    hits++;
                }
            }
        }

        GameLogger.Info("Combat", $"GRAZE shockwave — {hits} enemy(ies) hit for {dmg}");
        _hud?.ShowBanner("GRAZE!", new Color(0.4f, 0.95f, 1f));
        Player.Instance?.Camera?.Shake(5f, 0.25f);
    }

    public override void _Draw()
    {
        // Bottom-center meter bar.
        var pos = new Vector2(320f - MeterWidth / 2f, 344f);
        DrawRect(new Rect2(pos, new Vector2(MeterWidth, MeterHeight)), new Color(0.1f, 0.1f, 0.15f, 0.9f));

        float fill = MeterWidth * Mathf.Clamp(_value / MaxValue, 0f, 1f);
        if (fill > 0.5f)
            DrawRect(new Rect2(pos, new Vector2(fill, MeterHeight)), new Color(0.4f, 0.95f, 1f));

        if (fill < MeterWidth - 0.5f)
            DrawRect(new Rect2(pos + new Vector2(fill - 1f, -1f), new Vector2(2f, MeterHeight + 2f)), new Color(0.9f, 0.98f, 1f, 0.8f));
    }
}
