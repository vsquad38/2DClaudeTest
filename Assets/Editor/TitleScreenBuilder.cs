using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

// Tools > Build Title Screen: creates Assets/Scenes/TitleScreen.unity with a pixel-art backdrop and the
// title menu, and makes it the first scene in the build. Run Tools > Build Zelda Board first (it makes the art).
public static class TitleScreenBuilder
{
    const string ScenePath = "Assets/Scenes/TitleScreen.unity";
    const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    const string TemplatePath = "Assets/Settings/Scenes/URP2DSceneTemplate.unity";
    const string Art = "Assets/Art/";
    const string Audio = "Assets/Audio/";

    // Backdrop layout, 26 x 15 tiles centred on the camera. Same characters as the room layouts.
    static readonly string[] Backdrop =
    {
        "##########################",
        "#bb....................bb#",
        "#b......................b#",
        "#........................#",
        "#.www..................b.#",
        "#wwwww...................#",
        "#.www....................#",
        "#........................#",
        "#........................#",
        "#.b....................b.#",
        "#........................#",
        "#........................#",
        "#b......................b#",
        "#bb....................bb#",
        "##########################",
    };

    [MenuItem("Tools/Build Title Screen")]
    public static void Build()
    {
        if (AssetDatabase.LoadAssetAtPath<Tile>(Art + "GrassTile.asset") == null)
        {
            Debug.LogError("Title screen: art not found. Run Tools > Build Zelda Board first.");
            return;
        }

        // Generate (or refresh) the menu sounds alongside the rest of the audio.
        var clips = ChiptuneSynth.GenerateAll();

        var current = EditorSceneManager.GetActiveScene();
        if (current.isDirty && !string.IsNullOrEmpty(current.path)) EditorSceneManager.SaveScene(current);
        string previousScene = current.path;

        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            AssetDatabase.CopyAsset(TemplatePath, ScenePath); // camera + 2D global light
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        foreach (var name in new[] { "Title Backdrop", "Title Characters", "Title Screen" })
        {
            var old = GameObject.Find(name);
            if (old != null) Object.DestroyImmediate(old);
        }

        // --- Camera ---
        var cam = Camera.main;
        cam.orthographic = true;
        cam.orthographicSize = 7.2f;
        cam.transform.position = new Vector3(0, 0, -10);
        cam.backgroundColor = new Color(0.08f, 0.08f, 0.1f);

        // --- Backdrop tilemaps ---
        var grass = AssetDatabase.LoadAssetAtPath<Tile>(Art + "GrassTile.asset");
        var wall = AssetDatabase.LoadAssetAtPath<Tile>(Art + "WallTile.asset");
        var bush = AssetDatabase.LoadAssetAtPath<Tile>(Art + "BushTile.asset");
        var water = AssetDatabase.LoadAssetAtPath<Tile>(Art + "WaterTile.asset");

        var backdrop = new GameObject("Title Backdrop", typeof(Grid));
        backdrop.transform.position = new Vector3(0f, -0.5f, 0f); // centre the odd number of rows on the camera
        var ground = CreateTilemap(backdrop.transform, "Ground", 0);
        var props = CreateTilemap(backdrop.transform, "Props", 1);
        int rows = Backdrop.Length, cols = Backdrop[0].Length;
        for (int r = 0; r < rows; r++)
        {
            for (int c = 0; c < cols; c++)
            {
                var cell = new Vector3Int(c - cols / 2, rows / 2 - r, 0);
                ground.SetTile(cell, grass);
                char ch = c < Backdrop[r].Length ? Backdrop[r][c] : '.';
                if (ch == '#') props.SetTile(cell, wall);
                else if (ch == 'b') props.SetTile(cell, bush);
                else if (ch == 'w') props.SetTile(cell, water);
            }
        }

        // --- Characters: the hero and the Crystal Sword on the left, the boss and its monsters on the right ---
        var chars = new GameObject("Title Characters").transform;
        Sprite S(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art + name + ".png");

        var heroStand = S("Player_Side_Stand");
        Character(chars, "Hero", new Vector3(-8f, -4.5f), 2f, 6f, S("Player_Side_WalkA"), heroStand, S("Player_Side_WalkB"), heroStand);
        var sword = Character(chars, "Crystal Sword", new Vector3(-6.4f, -4.4f), 1.6f, 0f, S("CrystalSword"));
        sword.transform.rotation = Quaternion.Euler(0, 0, -90f);
        Character(chars, "Slime", new Vector3(-9.5f, -1.8f), 1.5f, 3f, S("Slime"), S("Slime_Squash"));

        var boss = Character(chars, "Boss", new Vector3(8f, -3.6f), 4f, 4f, S("Boss"), S("Boss_Step"));
        boss.GetComponent<SpriteRenderer>().flipX = true;
        Character(chars, "Moblin", new Vector3(9.5f, 1.5f), 1.5f, 6f, S("Moblin_WalkA"), S("Moblin"), S("Moblin_WalkB"), S("Moblin")).GetComponent<SpriteRenderer>().flipX = true;
        // Kept below the title text and beside the menu panel.
        Character(chars, "Bat", new Vector3(6.2f, 1.8f), 1.5f, 10f, S("Bat"), S("Bat_Flap"));
        Character(chars, "Bat 2", new Vector3(-6.5f, 2.6f), 1.5f, 10f, S("Bat"), S("Bat_Flap"));

        // --- Menu ---
        var menu = new GameObject("Title Screen").AddComponent<TitleScreen>();
        menu.titleIcon = AssetDatabase.LoadAssetAtPath<Texture2D>(Art + "CrystalSword.png");
        menu.music = clips.music;
        menu.moveSound = clips.menuMove;
        menu.selectSound = clips.menuSelect;

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        // Title first, then the game.
        var others = EditorBuildSettings.scenes.Where(s => s.path != ScenePath && s.path != GameScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true), new EditorBuildSettingsScene(GameScenePath, true) }
            .Concat(others).ToArray();

        Debug.Log("Title screen built and set as the first scene in Build Settings.");

        if (!string.IsNullOrEmpty(previousScene) && previousScene != ScenePath)
            EditorSceneManager.OpenScene(previousScene, OpenSceneMode.Single);
    }

    static GameObject Character(Transform parent, string name, Vector3 pos, float scale, float fps, params Sprite[] frames)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = Vector3.one * scale;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = frames[0];
        sr.sortingOrder = 5;
        if (frames.Length > 1)
        {
            var anim = go.AddComponent<SpriteAnimator>();
            anim.frames = frames;
            anim.fps = fps;
        }
        return go;
    }

    static Tilemap CreateTilemap(Transform parent, string name, int order)
    {
        var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(parent, false);
        go.GetComponent<TilemapRenderer>().sortingOrder = order;
        return go.GetComponent<Tilemap>();
    }
}
