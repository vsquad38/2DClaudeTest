using UnityEditor;
using UnityEditor.SceneManagement;

// Makes Play in the editor always start from the title screen, like a built game does,
// no matter which scene is open. Toggle with Tools > Play From Title Screen.
[InitializeOnLoad]
public static class PlayFromTitleScreen
{
    const string MenuPath = "Tools/Play From Title Screen";
    const string PrefKey = "CrystalSwordQuest.PlayFromTitleScreen";
    const string TitleScenePath = "Assets/Scenes/TitleScreen.unity";

    static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    static PlayFromTitleScreen()
    {
        Apply();
        // Re-apply right before entering Play mode, in case the scene asset wasn't loadable yet at startup.
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Apply();
        };
    }

    [MenuItem(MenuPath)]
    static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
    }

    [MenuItem(MenuPath, true)]
    static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    static void Apply()
    {
        EditorSceneManager.playModeStartScene = Enabled
            ? AssetDatabase.LoadAssetAtPath<SceneAsset>(TitleScenePath)
            : null;
    }
}
