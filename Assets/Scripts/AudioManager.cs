using UnityEngine;

public enum Sfx { Swing, Hit, EnemyDie, Hurt, Pickup, Victory, GameOver, Fireball, ItemGet }

// Plays background music and one-shot sound effects. Call AudioManager.Play(Sfx.X) from anywhere.
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    public AudioClip music;
    public AudioClip bossMusic;
    public AudioClip swing;
    public AudioClip hit;
    public AudioClip enemyDie;
    public AudioClip hurt;
    public AudioClip pickup;
    public AudioClip victory;
    public AudioClip gameOver;
    public AudioClip fireball;
    public AudioClip itemGet;

    [Range(0f, 1f)] public float musicVolume = 0.35f;
    [Range(0f, 1f)] public float sfxVolume = 0.8f;

    AudioSource musicSource;
    AudioSource sfxSource;

    void Awake()
    {
        Instance = this;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        musicSource.volume = musicVolume;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        sfxSource.volume = sfxVolume;
    }

    void Start()
    {
        // The RoomManager may already have picked a track (e.g. starting in the boss room).
        if (!musicSource.isPlaying) PlayTrack(music);
    }

    // Switches between the overworld theme and the boss theme.
    public static void SetBossMusic(bool boss)
    {
        if (Instance == null) return;
        Instance.PlayTrack(boss ? Instance.bossMusic : Instance.music);
    }

    void PlayTrack(AudioClip clip)
    {
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
        if (Instance != null) Instance.musicSource.Stop();
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
            default: return null;
        }
    }
}
