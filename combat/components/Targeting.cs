using Godot;

static class CombatTargeter
{
    public static Vector2 GetPlayerPosition()
    {
        return Player.Instance.GlobalPosition;
    }
}
