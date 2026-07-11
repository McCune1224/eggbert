using Godot;

[GlobalClass]
public partial class DialogVoiceResource : Resource
{
    [Export] public AudioStream VoiceStream { get; set; }
    [Export] public string SpeakerName { get; set; } = "";
    [Export] public float BasePitch { get; set; } = 1f;
    [Export(PropertyHint.Range, "0.01,0.5,0.01")]
    public float BlipDuration { get; set; } = 0.08f;
    [Export(PropertyHint.Range, "0,15,0.1")]
    public float StartOffset { get; set; } = 0f;
    [Export(PropertyHint.Range, "-12,6,0.1")]
    public float VolumeDb { get; set; } = 0f;
    [Export(PropertyHint.Range, "0,0.5,0.01")]
    public float ConsonantPitchVariance { get; set; } = 0.12f;
    [Export(PropertyHint.Range, "0,6,0.1")]
    public float VolumeVariance { get; set; } = 3f;

    [ExportGroup("Vowel Pitches")]
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelA { get; set; } = 1.00f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelE { get; set; } = 1.10f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelI { get; set; } = 1.20f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelO { get; set; } = 0.90f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelU { get; set; } = 0.85f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelY { get; set; } = 1.05f;

    [ExportGroup("Punctuation Pitches")]
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchPeriod { get; set; } = 0.70f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchQMark { get; set; } = 1.30f;
    [Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchExclam { get; set; } = 1.20f;

    private static AudioStreamWav _defaultBlip;

    public AudioStream GetBlipStream()
    {
        if (VoiceStream != null)
            return VoiceStream;
        EnsureDefaultBlip();
        return _defaultBlip;
    }

    public float GetVowelPitch(char c) => c switch
    {
        'a' or 'A' => VowelA,
        'e' or 'E' => VowelE,
        'i' or 'I' => VowelI,
        'o' or 'O' => VowelO,
        'u' or 'U' => VowelU,
        'y' or 'Y' => VowelY,
        _ => 0f
    };

    public float GetPunctuationPitch(char c) => c switch
    {
        '.' => PitchPeriod,
        '?' => PitchQMark,
        '!' => PitchExclam,
        _ => 1f
    };

    public static bool IsIntonation(char c) => c is '?' or '!' or '.';

    public static bool IsPunctuation(char c) => c is '!' or '.' or ',' or '?' or ';' or ':';

    private static void EnsureDefaultBlip()
    {
        if (_defaultBlip != null) return;
        int sampleRate = 22050;
        float duration = 0.06f;
        float freq = 440f;
        int samples = (int)(sampleRate * duration);
        byte[] data = new byte[samples * 2];
        for (int i = 0; i < samples; i++)
        {
            float t = (float)i / sampleRate;
            float envelope = t < 0.003f
                ? t / 0.003f
                : Mathf.Clamp(1f - (t - 0.003f) / (duration - 0.003f), 0f, 1f);
            float sample = Mathf.Sin(t * freq * Mathf.Tau) * envelope * 0.25f;
            short val = (short)(sample * 32767);
            data[i * 2] = (byte)(val & 0xFF);
            data[i * 2 + 1] = (byte)((val >> 8) & 0xFF);
        }
        _defaultBlip = new AudioStreamWav
        {
            Data = data,
            Format = AudioStreamWav.FormatEnum.Format16Bits,
            MixRate = sampleRate,
            Stereo = false
        };
    }
}
