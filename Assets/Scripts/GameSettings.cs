using UnityEngine;

// Player options, saved between sessions with PlayerPrefs.
public static class GameSettings
{
    public const string TitleScene = "TitleScreen";
    public const string GameScene = "SampleScene";

    const string MusicKey = "MusicVolume";
    const string SfxKey = "SfxVolume";
    const string FullscreenKey = "Fullscreen";
    const string HudScaleKey = "HudScale";

    public const float MinHudScale = 0.8f;
    public const float MaxHudScale = 1.3f; // above this the end-of-run panels no longer fit on screen

    // Extra size multiplier for the in-game HUD and messages (on top of scaling with the screen).
    public static float HudScale
    {
        get => Mathf.Clamp(PlayerPrefs.GetFloat(HudScaleKey, 1f), MinHudScale, MaxHudScale);
        set { PlayerPrefs.SetFloat(HudScaleKey, Mathf.Clamp(value, MinHudScale, MaxHudScale)); PlayerPrefs.Save(); }
    }

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
