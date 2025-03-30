using Godot;
using System.Collections.Generic;

public partial class AudioManager : Node
{

    Godot.Collections.Dictionary<SoundEffectSettings.SoundEffectType, SoundEffectSettings> _soundEffectDict = new();
    [Export]
    Godot.Collections.Array<SoundEffectSettings> SoundEffectSettings;

    public override void _Ready()
    {
        foreach (var sfx in SoundEffectSettings)
        {
            _soundEffectDict[sfx.SoundType] = sfx;
        }
    }

    public void Create2DAudioAtLocation(Vector2 location, SoundEffectSettings.SoundEffectType sound)
    {

    }
}
