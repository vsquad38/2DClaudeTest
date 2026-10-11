using UnityEngine;

// Player options, saved between sessions with PlayerPrefs.
public static class GameSettings
{
    public const string TitleScene = "TitleScreen";
    public const string GameScene = "SampleScene";

    const string MusicKey = "MusicVolume";
    const string SfxKey = "SfxVolume";
    const string FullscreenKey = "Fullscreen";

    // 0..1 multipliers on top of the AudioManager's mix levels.
    public static float MusicVolume
    {
        get => PlayerPrefs.GetFloat(MusicKey, 1f);
        set { PlayerPrefs.SetFloat(MusicKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static float SfxVolume
    {
        get => PlayerPrefs.GetFloat(SfxKey, 1f);
        set { PlayerPrefs.SetFloat(SfxKey, Mathf.Clamp01(value)); PlayerPrefs.Save(); }
    }

    public static bool Fullscreen
    {
        get => PlayerPrefs.GetInt(FullscreenKey, 1) == 1;
        set
        {
            PlayerPrefs.SetInt(FullscreenKey, value ? 1 : 0);
            PlayerPrefs.Save();
            Screen.fullScreen = value;
        }
    }
}
