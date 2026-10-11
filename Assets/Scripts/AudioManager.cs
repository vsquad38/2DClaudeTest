using UnityEngine;

public enum Sfx { Swing, Hit, EnemyDie, Hurt, Pickup, Victory, GameOver, Fireball, ItemGet, Zap, Burn }

// Plays background music and one-shot sound effects. Call AudioManager.Play(Sfx.X) from anywhere.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip music;
    public AudioClip bossMusic;
    public AudioClip dangerMusic;
    public AudioClip swing;
    public AudioClip hit;
    public AudioClip enemyDie;
    public AudioClip hurt;
    public AudioClip pickup;
    public AudioClip victory;
    public AudioClip gameOver;
    public AudioClip fireball;
    public AudioClip itemGet;
    public AudioClip zap;
    public AudioClip burn;

    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    AudioSource musicSource;
    AudioSource sfxSource;

    // Which track should be playing: danger (low health) beats boss, which beats the overworld theme.
    bool inBossRoom;
    bool inDanger;
    bool musicStopped; // after a win or game over, nothing restarts the music

    void Awake()
    {
        Instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume * GameSettings.MusicVolume;
        musicSource.pitch = Mathf.Min(1.15f, 1f + 0.03f * (GameSession.Level - 1)); // a little faster each level
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume * GameSettings.SfxVolume;
    }

    void Start()
    {
        // The RoomManager may already have picked a track (e.g. starting in the boss room).
        if (!musicSource.isPlaying) UpdateTrack();
    }

    public static AudioClip CurrentTrack => Instance != null ? Instance.musicSource.clip : null;

    // Entering or leaving the boss room.
    public static void SetBossMusic(bool boss)
    {
        if (Instance == null) return;
        Instance.inBossRoom = boss;
        Instance.UpdateTrack();
    }

    // Player health dropped below (or recovered above) the danger threshold.
    public static void SetDanger(bool danger)
    {
        if (Instance == null || Instance.inDanger == danger) return;
        Instance.inDanger = danger;
        Instance.UpdateTrack();
    }

    void UpdateTrack()
    {
        if (musicStopped) return;
        var clip = inDanger && dangerMusic != null ? dangerMusic : inBossRoom ? bossMusic : music;
        if (clip == null || (musicSource.clip == clip && musicSource.isPlaying)) return;
        musicSource.clip = clip;
        musicSource.Play();
    }

    public static void Play(Sfx sfx)
    {
        if (Instance == null) return;
        var clip = Instance.ClipFor(sfx);
        if (clip != null) Instance.sfxSource.PlayOneShot(clip);
    }

    public static void StopMusic()
    {
        if (Instance == null) return;
        Instance.musicStopped = true;
        Instance.musicSource.Stop();
    }

    AudioClip ClipFor(Sfx sfx)
    {
        switch (sfx)
        {
            case Sfx.Swing: return swing;
            case Sfx.Hit: return hit;
            case Sfx.EnemyDie: return enemyDie;
            case Sfx.Hurt: return hurt;
            case Sfx.Pickup: return pickup;
            case Sfx.Victory: return victory;
            case Sfx.GameOver: return gameOver;
            case Sfx.Fireball: return fireball;
            case Sfx.ItemGet: return itemGet;
            case Sfx.Zap: return zap;
            case Sfx.Burn: return burn;
            default: return null;
        }
    }
}
