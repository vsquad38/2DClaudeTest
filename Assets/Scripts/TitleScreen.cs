using UnityEngine;
using UnityEngine.SceneManagement;

// Title screen menu: Play, Instructions, Options, Exit. Works with keyboard, gamepad and mouse.
// The UI is laid out on a virtual 720-pixel-tall canvas and scaled to the window.
public class TitleScreen : MonoBehaviour
{
    public string gameTitle = "Crystal Sword Quest";
    public string subtitle = "A tiny top-down adventure";
    public Texture2D titleIcon;
    public AudioClip music;
    public AudioClip moveSound;
    public AudioClip selectSound;
    [Range(0f, 1f)] public float musicMix = 0.35f;
    [Range(0f, 1f)] public float sfxMix = 0.8f;

    const float VirtualHeight = 720f;
    const float ItemHeight = 56f;

    enum Page { Main, Instructions, Options }

    static readonly string[] MainItems = { "Play", "Instructions", "Options", "Exit" };
    static readonly string[] OptionLabels = { "Music Volume", "Sound Volume", "HUD Size", "Fullscreen", "Back" };
    const int BackRow = 4;

    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);
    static readonly Color Crystal = new Color(0.6f, 0.92f, 1f);

    Page page;
    int selected;
    bool leaving;
    Vector2 lastMouse;
    AudioSource musicSource;
    AudioSource sfxSource;

    GUIStyle titleStyle, subtitleStyle, itemStyle, headerStyle, bodyStyle, hintStyle, valueStyle;

    void Awake()
    {
        Time.timeScale = 1f;
        musicSource = gameObject.AddComponent<AudioSource>();
        musicSource.loop = true;
        musicSource.playOnAwake = false;
        sfxSource = gameObject.AddComponent<AudioSource>();
        sfxSource.playOnAwake = false;
        ApplyVolumes();
        if (!Application.isEditor) Screen.fullScreen = GameSettings.Fullscreen;
    }

    void Start()
    {
        if (music == null) return;
        musicSource.clip = music;
        musicSource.Play();
    }

    void ApplyVolumes()
    {
        musicSource.volume = musicMix * GameSettings.MusicVolume;
        sfxSource.volume = sfxMix * GameSettings.SfxVolume;
    }

    int ItemCount => page == Page.Main ? MainItems.Length : page == Page.Options ? OptionLabels.Length : 1;

    // ---------- Input ----------

    void Update()
    {
        if (leaving) return;

        if (MenuInput.Up) Move(-1);
        if (MenuInput.Down) Move(1);
        if (page == Page.Options)
        {
            if (MenuInput.Left) Adjust(selected, -1);
            if (MenuInput.Right) Adjust(selected, 1);
        }

        if (MenuInput.Confirm) Activate(selected);
        else if (MenuInput.Back && page != Page.Main) GoTo(Page.Main);
    }

    void Move(int delta)
    {
        selected = (selected + delta + ItemCount) % ItemCount;
        Play(moveSound);
    }

    void Activate(int index)
    {
        Play(selectSound);
        switch (page)
        {
            case Page.Main:
                if (index == 0) StartGame();
                else if (index == 1) GoTo(Page.Instructions);
                else if (index == 2) GoTo(Page.Options);
                else Quit();
                break;
            case Page.Instructions:
                GoTo(Page.Main);
                break;
            case Page.Options:
                if (index == BackRow) GoTo(Page.Main);
                else Adjust(index, 1, wrap: true);
                break;
        }
    }

    // Changes an option. Volumes and HUD size move in 10% steps; fullscreen toggles.
    void Adjust(int row, int dir, bool wrap = false)
    {
        if (row == 0 || row == 1)
        {
            float current = row == 0 ? GameSettings.MusicVolume : GameSettings.SfxVolume;
            float next = Mathf.Round(current * 10f + dir) / 10f;
            if (wrap && next > 1.001f) next = 0f;
            next = Mathf.Clamp01(next);
            if (row == 0) GameSettings.MusicVolume = next;
            else GameSettings.SfxVolume = next;
            ApplyVolumes();
            Play(row == 1 ? selectSound : moveSound); // preview the new sound level
        }
        else if (row == 2)
        {
            float next = Mathf.Round(GameSettings.HudScale * 10f + dir) / 10f;
            if (wrap && next > GameSettings.MaxHudScale + 0.001f) next = GameSettings.MinHudScale;
            GameSettings.HudScale = next;
            Play(moveSound);
        }
        else if (row == 3)
        {
            GameSettings.Fullscreen = !GameSettings.Fullscreen;
            Play(moveSound);
        }
    }

    void GoTo(Page next)
    {
        // Returning to the main menu re-selects the item you came from.
        selected = next == Page.Main ? (page == Page.Instructions ? 1 : page == Page.Options ? 2 : 0) : 0;
        if (next == Page.Instructions) selected = 0;
        page = next;
    }

    void StartGame()
    {
        GameSession.StartNewRun();
        leaving = true;
        Invoke(nameof(LoadGame), 0.25f); // let the select sound play
    }

    void LoadGame() => SceneManager.LoadScene(GameSettings.GameScene);

    void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Play(AudioClip clip)
    {
        if (clip != null) sfxSource.PlayOneShot(clip);
    }

    // ---------- Drawing ----------

    void OnGUI()
    {
        float scale = Screen.height / VirtualHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;
        EnsureStyles();

        switch (page)
        {
            case Page.Main: DrawMain(width); break;
            case Page.Instructions: DrawInstructions(width); break;
            case Page.Options: DrawOptions(width); break;
        }

        lastMouse = Event.current.mousePosition;
    }

    void DrawMain(float width)
    {
        // Title with a drop shadow, flanked by crystal swords.
        var titleRect = new Rect(0, 70, width, 100);
        Shadowed(titleRect, gameTitle, titleStyle, Gold);
        if (titleIcon != null)
        {
            float textWidth = titleStyle.CalcSize(new GUIContent(gameTitle)).x;
            float iconSize = 72f;
            float y = titleRect.y + (titleRect.height - iconSize) / 2f;
            GUI.DrawTexture(new Rect(width / 2f - textWidth / 2f - iconSize - 16, y, iconSize, iconSize), titleIcon);
            GUI.DrawTexture(new Rect(width / 2f + textWidth / 2f + 16, y, iconSize, iconSize), titleIcon);
        }
        Shadowed(new Rect(0, 170, width, 40), subtitle, subtitleStyle, Crystal);
        if (GameSession.HighScore > 0)
            Shadowed(new Rect(0, 210, width, 34), $"High Score {GameSession.HighScore:N0}    Best Level {GameSession.BestLevel}", hintStyle, Gold);

        float menuWidth = 360f;
        float top = 270f;
        Panel(new Rect(width / 2f - menuWidth / 2f - 20, top - 20, menuWidth + 40, MainItems.Length * ItemHeight + 40));
        for (int i = 0; i < MainItems.Length; i++)
            Item(new Rect(width / 2f - menuWidth / 2f, top + i * ItemHeight, menuWidth, ItemHeight), MainItems[i], i);

        Hint(width, "Arrow keys / W S to choose    Enter to select    or use the mouse");
    }

    void DrawInstructions(float width)
    {
        float panelWidth = Mathf.Min(900f, width - 60f);
        var panel = new Rect(width / 2f - panelWidth / 2f, 24, panelWidth, 580);
        Panel(panel);
        Shadowed(new Rect(panel.x, panel.y + 16, panel.width, 50), "How to Play", headerStyle, Gold);

        string text =
            "<b>Goal</b>\n" +
            "Explore the nine rooms, find the boss's lair (a different corner room each level) and defeat it to clear the level. Each level the world " +
            "returns with stronger, more aggressive monsters and a tougher boss, and you gain a heart. One life: " +
            "if you fall, the run is over.\n" +
            "A <color=#9be9ff><b>Crystal Sword</b></color> is hidden somewhere each level, in a different spot each time. " +
            "With it, the boss falls in far fewer hits and your lightning burns through bushes.\n" +
            "Score: kills x level, minus 100 for every heart of damage taken.\n\n" +
            "<b>Controls</b>\n" +
            "Move:  WASD, Arrow keys or Left stick\n" +
            "Attack:  Space, J or the A button\n" +
            "Pause:  Esc or Start\n\n" +
            "<b>Tips</b>\n" +
            "Walk through a doorway to move to the next room.\n" +
            "Monsters sometimes drop hearts that restore your health.\n" +
            "At half health or more, your sword also shoots a lightning bolt that deals the same damage.\n" +
            "The boss charges and breathes fire. Slash fireballs out of the air!";
        GUI.Label(new Rect(panel.x + 40, panel.y + 80, panel.width - 80, panel.height - 100), text, bodyStyle);

        Item(new Rect(width / 2f - 150, panel.yMax + 8, 300, ItemHeight), "Back", 0);
        Hint(width, "Enter or Esc to go back");
    }

    void DrawOptions(float width)
    {
        float panelWidth = 640f;
        var panel = new Rect(width / 2f - panelWidth / 2f, 90, panelWidth, 490);
        Panel(panel);
        Shadowed(new Rect(panel.x, panel.y + 16, panel.width, 50), "Options", headerStyle, Gold);

        string[] values =
        {
            Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%",
            Mathf.RoundToInt(GameSettings.SfxVolume * 100f) + "%",
            Mathf.RoundToInt(GameSettings.HudScale * 100f) + "%",
            GameSettings.Fullscreen ? "On" : "Off",
        };

        float top = panel.y + 100;
        for (int i = 0; i < OptionLabels.Length; i++)
        {
            var row = new Rect(panel.x + 40, top + i * (ItemHeight + 12), panel.width - 80, ItemHeight);
            if (i == OptionLabels.Length - 1)
            {
                Item(new Rect(width / 2f - 150, row.y + 12, 300, ItemHeight), "Back", i);
                continue;
            }

            bool isSelected = i == selected;
            HoverAndClick(row, i, activate: false);
            if (isSelected) Highlight(row);
            itemStyle.alignment = TextAnchor.MiddleLeft;
            Shadowed(new Rect(row.x + 20, row.y, row.width, row.height), OptionLabels[i], itemStyle, isSelected ? Gold : Color.white);
            itemStyle.alignment = TextAnchor.MiddleCenter;

            // Value with clickable < > arrows.
            var valueRect = new Rect(row.xMax - 240, row.y, 220, row.height);
            var leftArrow = new Rect(valueRect.x, row.y, 44, row.height);
            var rightArrow = new Rect(valueRect.xMax - 44, row.y, 44, row.height);
            Shadowed(leftArrow, "<", valueStyle, isSelected ? Gold : Color.gray);
            Shadowed(valueRect, values[i], valueStyle, Color.white);
            Shadowed(rightArrow, ">", valueStyle, isSelected ? Gold : Color.gray);
            if (Clicked(leftArrow)) { selected = i; Adjust(i, -1); }
            else if (Clicked(rightArrow)) { selected = i; Adjust(i, 1); }
            else if (Clicked(row)) { selected = i; Adjust(i, 1, wrap: true); }
        }

        Hint(width, "Left / Right to change    Esc to go back" + (Application.isEditor ? "    (Fullscreen applies in builds)" : ""));
    }

    // A menu item: highlighted when selected; hover selects it, click activates it.
    void Item(Rect rect, string label, int index)
    {
        HoverAndClick(rect, index, activate: true);
        bool isSelected = index == selected;
        if (isSelected) Highlight(rect);
        Shadowed(rect, isSelected ? $"> {label} <" : label, itemStyle, isSelected ? Gold : Color.white);
    }

    void HoverAndClick(Rect rect, int index, bool activate)
    {
        var e = Event.current;
        if (leaving || !rect.Contains(e.mousePosition)) return;
        if (e.type == EventType.Repaint && e.mousePosition != lastMouse && selected != index)
        {
            selected = index;
            Play(moveSound);
        }
        if (activate && e.type == EventType.MouseDown && e.button == 0)
        {
            selected = index;
            Activate(index);
            e.Use();
        }
    }

    bool Clicked(Rect rect)
    {
        var e = Event.current;
        if (leaving || e.type != EventType.MouseDown || e.button != 0 || !rect.Contains(e.mousePosition)) return false;
        e.Use();
        return true;
    }

    void Highlight(Rect rect)
    {
        var old = GUI.color;
        GUI.color = new Color(1f, 0.85f, 0.3f, 0.18f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Panel(Rect rect)
    {
        var old = GUI.color;
        GUI.color = new Color(0.03f, 0.05f, 0.08f, 0.72f);
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = new Color(1f, 0.85f, 0.3f, 0.5f);
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 2), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 2, rect.width, 2), Texture2D.whiteTexture);
        GUI.color = old;
    }

    void Hint(float width, string text)
    {
        Shadowed(new Rect(0, VirtualHeight - 48, width, 32), text, hintStyle, new Color(1f, 1f, 1f, 0.8f));
    }

    static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * color.a);
        GUI.Label(new Rect(rect.x + 3, rect.y + 3, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = old;
    }

    void EnsureStyles()
    {
        if (titleStyle != null) return;
        titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 72, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        subtitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Italic, alignment = TextAnchor.MiddleCenter };
        itemStyle = new GUIStyle(GUI.skin.label) { fontSize = 34, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        headerStyle = new GUIStyle(GUI.skin.label) { fontSize = 44, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
        bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, wordWrap = true, richText = true };
        bodyStyle.normal.textColor = Color.white;
        hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, alignment = TextAnchor.MiddleCenter };
        valueStyle = new GUIStyle(GUI.skin.label) { fontSize = 30, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
    }
}
