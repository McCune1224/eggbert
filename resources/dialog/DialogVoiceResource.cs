using Godot;

/// <summary>
/// Voice and intonation settings for a dialog speaker.
/// If <see cref="VoiceStream"/> is assigned, it is used for all voiced line audio.
/// When VoiceStream is null, <see cref="GetBlipStream"/> generates a procedural default blip (80ms sine at 440Hz, configured via <see cref="BlipDuration"/>)
/// as a fallback, avoiding the need for a separate .ogg clip for every line.
/// </summary>
[GlobalClass]
public partial class DialogVoiceResource : Resource
{
	/// <summary>Custom audio stream for this speaker. If null, a procedural blip is generated.</summary>
	[Export] public AudioStream VoiceStream { get; set; }
	/// <summary>Displayed as the speaker name in the dialog UI.</summary>
	[Export] public string SpeakerName { get; set; } = "";
	/// <summary>Optional portrait texture shown beside the speaker's dialog text.</summary>
	[Export] public Texture2D Portrait { get; set; }
	/// <summary>Base pitch multiplier for all generated voice audio. Default 1.0 (no pitch shift).</summary>
	[Export] public float BasePitch { get; set; } = 1f;
	/// <summary>Default blip duration in seconds for procedural fallback voice clips.</summary>
	[Export(PropertyHint.Range, "0.01,0.5,0.01")]
	public float BlipDuration { get; set; } = 0.08f;
	/// <summary>Offset in seconds from the start of the audio stream to begin playback.</summary>
	[Export(PropertyHint.Range, "0,15,0.1")]
	public float StartOffset { get; set; } = 0f;
	/// <summary>Volume adjustment in decibels applied to this voice resource.</summary>
	[Export(PropertyHint.Range, "-12,6,0.1")]
	public float VolumeDb { get; set; } = 0f;
	/// <summary>Magnitude of random pitch variation added per consonant hit, creating a natural cadence.</summary>
	[Export(PropertyHint.Range, "0,0.5,0.01")]
	public float ConsonantPitchVariance { get; set; } = 0.12f;
	/// <summary>Magnitude of random volume variation per syllable, simulating natural speech dynamics.</summary>
	[Export(PropertyHint.Range, "0,6,0.1")]
	public float VolumeVariance { get; set; } = 3f;

	[ExportGroup("Vowel Pitches")]
	/// <summary>Pitch multiplier for the vowel 'a'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelA { get; set; } = 1.00f;
	/// <summary>Pitch multiplier for the vowel 'e'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelE { get; set; } = 1.10f;
	/// <summary>Pitch multiplier for the vowel 'i'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelI { get; set; } = 1.20f;
	/// <summary>Pitch multiplier for the vowel 'o'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelO { get; set; } = 0.90f;
	/// <summary>Pitch multiplier for the vowel 'u'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelU { get; set; } = 0.85f;
	/// <summary>Pitch multiplier for the vowel 'y'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float VowelY { get; set; } = 1.05f;

	[ExportGroup("Punctuation Pitches")]
	/// <summary>Pitch multiplier applied when the spoken character ends with a period '.'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchPeriod { get; set; } = 0.70f;
	/// <summary>Pitch multiplier applied when the spoken character ends with a question mark '?'.</summary>
	[Export(PropertyHint.Range, "0.5,2,0.01")] public float PitchQMark { get; set; } = 1.30f;
	/// <summary>Pitch multiplier applied when the spoken character ends with an exclamation mark '!'.</summary>
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
		float duration = 0.08f;
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
