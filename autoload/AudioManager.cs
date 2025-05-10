using Godot;
using System.Collections.Generic;


public partial class AudioManager : Node
{

    private static AudioManager _instance;
    public static AudioManager Instance => _instance;



    public readonly string MUSIC_BUS = "MUSIC";
    public readonly string SFX_BUS = "SFX";
    private int _musicAudioPlayerCount = 2;
    private int _currentMusicPlayerIndex = 0;
    private List<AudioStreamPlayer> _musicAudioPlayers = new List<AudioStreamPlayer>();
    private float musicFadeDuration = 0.5f;


    public override void _Ready()
    {
        if (_instance == null)
        {
            _instance = this;
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
    }

    public void PlayMusic(AudioStream audio, bool loop = false)
    {
        // Prevents music repeating in same 'areas'
        if (audio == _musicAudioPlayers[_currentMusicPlayerIndex].Stream)
        {
            GD.Print("Audio is current played music, exiting");
            return;
        }
        _currentMusicPlayerIndex++;
        if (_currentMusicPlayerIndex > 1)
        {
            _currentMusicPlayerIndex = 0;
        }
        AudioStreamPlayer currentMusicPlayer = _musicAudioPlayers[_currentMusicPlayerIndex];
        currentMusicPlayer.Bus = MUSIC_BUS;
        currentMusicPlayer.Stream = audio;
        PlayFadeIn(currentMusicPlayer, musicFadeDuration);

        AudioStreamPlayer oldMusicPlayer = _musicAudioPlayers[(_currentMusicPlayerIndex + 1) % _musicAudioPlayerCount];
        FadeOutStop(oldMusicPlayer, musicFadeDuration);
    }


    public void PlayFadeIn(AudioStreamPlayer player, float duration)
    {
        player.Play();
        Tween tween = CreateTween();
        tween.TweenProperty(player, "volume_db", 0, duration);
    }

    public async void FadeOutStop(AudioStreamPlayer player, float duration)
    {
        player.Play();
        Tween tween = CreateTween();
        tween.TweenProperty(player, "volume_db", -40, duration);
        await ToSignal(tween, "finished");
        player.Stop();
    }
}


