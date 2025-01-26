using Godot;
using System.Collections.Generic;

public partial class CombatScene : Node2D
{
    [Export]
    private int _opponentCount = 1;
    //WARNING: I don't think it'll matter for preformance to use a list. 
    //Not many opponents will be on the screen...
    private List<CombatOpponent> _opponents;

    // Called when the node entes the scene tree for the first time.
    public override void _Ready()
    {
        _opponents = new List<CombatOpponent>(_opponentCount);
    }

    // Called every frame. 'delta' is the elapsed time since the previous frame.
    public override void _Process(double delta)
    {
    }
}
