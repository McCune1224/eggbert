using Godot;

public class DialogVoice
{
    public AudioStream BlipStream { get; set; }
    public float BasePitch { get; set; } = 1f;
    public string SpeakerName { get; set; } = "";

    public DialogVoice(AudioStream blipStream = null, float basePitch = 1f, string speakerName = "")
    {
        BlipStream = blipStream;
        BasePitch = basePitch;
        SpeakerName = speakerName;
    }
}
