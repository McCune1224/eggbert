using Godot;

public partial class AudioManager : Node
{
    private static AudioManager _instance;
    public static AudioManager Instance => _instance;


    public const string MUSIC_BUS = "MUSIC";
	public const string SFX_BUS = "SFX";

    private int _musicAudioPlayerCount = 2;
    private int _currentMusicPlayerIndex = 0;
    private Godot.Collections.Array<AudioStreamPlayer> _musicAudioPlayers = new();
    private float _musicFadeDuration = 0.5f;

    private AudioStreamPlayer _ambientPlayer;

    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
        }
        else
        {
            GameLogger.Warn("Audio", "Multiple instances detected — freeing duplicate");
            QueueFree();
            return;
        }

        ProcessMode = Node.ProcessModeEnum.Always;
        for (int i = 0; i < _musicAudioPlayerCount; i++)
        {
            AudioStreamPlayer audioPlayer = new();
            audioPlayer.Name = $"MusicPlayer{i}";
            AddChild(audioPlayer);
            _musicAudioPlayers.Add(audioPlayer);
            audioPlayer.VolumeDb = -40;
        }

        _ambientPlayer = new AudioStreamPlayer();
        _ambientPlayer.Name = "AmbientPlayer";
        _ambientPlayer.Bus = MUSIC_BUS;
        AddChild(_ambientPlayer);
    }

    public void PlayMusic(AudioStream audio, bool loop = false)
    {
        string name = audio?.ResourcePath.GetFile() ?? "null";
        if (audio == _musicAudioPlayers[_currentMusicPlayerIndex].Stream)
        {
            GameLogger.Debug("Audio", $"PlayMusic: '{name}' already playing — skipped");
            return;
        }
        GameLogger.Info("Audio", $"PlayMusic: '{name}' loop={loop}");

        _currentMusicPlayerIndex = (_currentMusicPlayerIndex + 1) % 2;
        AudioStreamPlayer current = _musicAudioPlayers[_currentMusicPlayerIndex];
        current.Bus = MUSIC_BUS;
        current.Stream = audio;
        PlayFadeIn(current, _musicFadeDuration);

        AudioStreamPlayer old = _musicAudioPlayers[(_currentMusicPlayerIndex + 1) % _musicAudioPlayerCount];
        FadeOutStop(old, _musicFadeDuration);
    }

    public void PlayAmbience(AudioStream ambience)
    {
        string name = ambience?.ResourcePath.GetFile() ?? "null";
        GameLogger.Debug("Audio", $"PlayAmbience: '{name}'");
        _ambientPlayer.Stream = ambience;
        _ambientPlayer.Play();
    }

    public void StopAmbience()
    {
        GameLogger.Debug("Audio", "StopAmbience");
        _ambientPlayer.Stop();
        _ambientPlayer.Stream = null;
    }

    public void PlaySfx(AudioStream sfx, float volumeDb = 0f)
    {
        GameLogger.Debug("Audio", $"PlaySfx: '{sfx?.ResourcePath.GetFile() ?? "null"}' vol={volumeDb}");
        // ponytail: one-shot player, pool if hundreds fire per frame
        var player = new AudioStreamPlayer();
        player.Bus = SFX_BUS;
        player.Stream = sfx;
        player.VolumeDb = volumeDb;
        AddChild(player);
        player.Finished += () => player.QueueFree();
        player.Play();
    }

    private void PlayFadeIn(AudioStreamPlayer player, float duration)
    {
        player.Play();
        Tween tween = CreateTween();
        tween.TweenProperty(player, "volume_db", 0, duration);
    }

    private async void FadeOutStop(AudioStreamPlayer player, float duration)
    {
        Tween tween = CreateTween();
        tween.TweenProperty(player, "volume_db", -40, duration);
        await ToSignal(tween, Tween.SignalName.Finished);
        player.Stop();
    }
}
