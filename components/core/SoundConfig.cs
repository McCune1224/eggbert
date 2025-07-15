using System;
using Godot;

public partial class SoundEffectSettings : Resource
{
    public enum SoundEffectType
    {
        // Define your sound effect types here
        UIClick,
        // PlayerWalk,
        PlayerDamage,
        EnemyDamage,
        MenuOpen,
        DefaultDialogChat,
        // DialogAdvance,
        Attack,
        Dodge
    }
    [Export(PropertyHint.Range, "1,20,1")]
    public int limit = 5;
    [Export]
    public SoundEffectType SoundType;
    [Export]
    public AudioStreamMP3 SoundEffect;
    [Export(PropertyHint.Range, "-40,20,1")]
    public int Volume = 0;
    [Export(PropertyHint.Range, "-0.0,4.0,.01")]
    public float PitchScale = 1.0f;
    [Export(PropertyHint.Range, "-1.0,1.0,.01")]
    public float PitchRandomness = 0.0f;

    private int _audioCount = 0;

    public void UpdateAudioCount(int amount)
    {
        _audioCount = Math.Max(0, _audioCount + amount);
    }

    public bool AtLimit()
    {
        return _audioCount == limit;
    }

    public void OnAudioFinished()
    {
        UpdateAudioCount(-1);
    }


}
