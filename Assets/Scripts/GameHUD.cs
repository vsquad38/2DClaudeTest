using UnityEngine;
using UnityEngine.SceneManagement;

// Draws the hearts, sword, level and score, minimap, boss health bar, messages and level banner,
// plus the pause menu, the level-clear screen and the game-over screen.
// The always-on HUD lives inside the room's top and bottom wall rows so it never covers the playing field:
// hearts + sword and Level/Score/Best along the top wall, minimap and boss bar along the bottom wall.
// Overlays (banner, messages, menus, result screens) use a virtual canvas ReferenceHeight pixels tall,
// scaled to the window (times the HUD Size option).
public class GameHUD : MonoBehaviour
{
    public Health playerHealth;
    public Texture2D heartFull;
    public Texture2D heartEmpty;
    public Texture2D upgradedSwordIcon;
    public float messageDuration = 3f;
    public float bannerDuration = 2.5f;

    const float ReferenceHeight = 600f;

    static readonly Color Unvisited = new Color(0f, 0f, 0f, 0.45f);
    static readonly Color VisitedColor = new Color(0.65f, 0.65f, 0.6f, 0.9f);
    static readonly Color CurrentColor = new Color(0.35f, 0.85f, 0.35f, 1f);
    static readonly Color BossRoomColor = new Color(0.75f, 0.15f, 0.15f, 0.9f);
    static readonly Color Gold = new Color(1f, 0.85f, 0.3f);

    static readonly string[] PauseItems = { "Resume", "Quit to Title" };

    static GameHUD instance;

    PlayerController player;
    GUIStyle bigText, smallText, messageText, statText, menuText, statusText;
    bool levelCleared;
    bool gameOver;
    bool newHighScore;
    float screenInputTime; // ignore input briefly when an end screen appears, so a held key doesn't skip it
    bool paused;
    int pauseSelected;
    string message;
    float messageUntil;
    string bannerTitle, bannerSubtitle;
    float bannerUntil;

    // Size of the virtual canvas for the current frame, and the height of one wall row on it.
    float W, H;
    float strip;

    public static bool Paused => instance != null && instance.paused;

    public static void ShowMessage(string text)
    {
        if (instance == null) return;
        instance.message = text;
        instance.messageUntil = Time.unscaledTime + instance.messageDuration;
    }

    public static void ShowBanner(string title, string subtitle)
    {
        if (instance == null) return;
        instance.bannerTitle = title;
        instance.bannerSubtitle = subtitle;
        instance.bannerUntil = Time.unscaledTime + instance.bannerDuration;
    }

    // Roguelike: the run is over. Records the high score and shows the summary.
    public static void OnPlayerDied()
    {
        if (instance == null) return;
        instance.gameOver = true;
        instance.paused = false;
        instance.newHighScore = GameSession.EndRun();
        instance.screenInputTime = Time.unscaledTime + 1.5f;
    }

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        if (playerHealth != null) player = playerHealth.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!levelCleared && !gameOver && Boss.Defeated && playerHealth != null && !playerHealth.IsDead)
        {
            levelCleared = true;
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Victory);
            Time.timeScale = 0f; // freeze the world while the results are up
            screenInputTime = Time.unscaledTime + 1f;
        }

        if (levelCleared || gameOver)
        {
            if (Time.unscaledTime < screenInputTime) return;
            if (MenuInput.Confirm)
            {
                if (levelCleared) GameSession.AdvanceLevel();
                else GameSession.StartNewRun();
                LoadScene(GameSettings.GameScene);
            }
            else if (MenuInput.Back)
            {
                GameSession.EndRun();
                LoadScene(GameSettings.TitleScene);
            }
            return;
        }

        if (playerHealth == null || playerHealth.IsDead) return;

        if (paused)
        {
            if (MenuInput.Up || MenuInput.Down) pauseSelected = 1 - pauseSelected;
            if (MenuInput.Confirm) ChoosePauseItem(pauseSelected);
            else if (MenuInput.Pause) SetPaused(false);
        }
        else if (MenuInput.Pause)
        {
            SetPaused(true);
        }
    }

    void SetPaused(bool value)
    {
        paused = value;
        pauseSelected = 0;
        Time.timeScale = value ? 0f : 1f;
    }

    void ChoosePauseItem(int index)
    {
        if (index == 0)
        {
            SetPaused(false);
            return;
        }
        GameSession.EndRun(); // quitting ends the run; the score still counts toward the high score
        LoadScene(GameSettings.TitleScene);
    }

    static void LoadScene(string sceneName)
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(sceneName);
    }

    // ---------- Drawing ----------

    void OnGUI()
    {
        if (playerHealth == null) return;

        float scale = Screen.height / ReferenceHeight * GameSettings.HudScale;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        W = Screen.width / scale;
        H = Screen.height / scale;
        strip = H * WallRowFraction();
        EnsureStyles();

        DrawHearts();
        DrawScore();
        DrawMinimap();
        DrawBossBar();

        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
        {
            float width = Mathf.Min(560f, W - 40f);
            GUI.Label(new Rect(W / 2f - width / 2f, H - strip - 64, width, 52), message, messageText);
        }

        if (gameOver) DrawGameOver();
        else if (levelCleared) DrawLevelClear();
        else if (paused) DrawPauseMenu();
        else DrawBanner();
    }

    // Fraction of the screen height taken by one wall row at the top (and bottom) of the room.
    // The camera shows a little more than the room, so this is (half view - half room + 1 tile) / view.
    static float WallRowFraction()
    {
        var cam = Camera.main;
        var rooms = RoomManager.Instance;
        if (cam == null || rooms == null) return 0.08f;
        float row = cam.orthographicSize - (rooms.roomSize.y / 2f - 1f);
        return Mathf.Clamp(row / (2f * cam.orthographicSize), 0.04f, 0.15f);
    }

    // Hearts and the Crystal Sword icon, sized to fit inside the top wall row.
    void DrawHearts()
    {
        float size = strip * 0.72f;
        float y = (strip - size) / 2f;
        float step = size + 3f;
        for (int i = 0; i < playerHealth.Max; i++)
        {
            var tex = i < playerHealth.Current ? heartFull : heartEmpty;
            GUI.DrawTexture(new Rect(8 + i * step, y, size, size), tex);
        }

        if (player != null && player.SwordUpgraded && upgradedSwordIcon != null)
            GUI.DrawTexture(new Rect(8 + playerHealth.Max * step + 6, y, size, size), upgradedSwordIcon);
    }

    // Level / Score / Best: one small line at the right end of the top wall row.
    void DrawScore()
    {
        statusText.fontSize = Mathf.Max(9, Mathf.RoundToInt(strip * 0.4f));
        string gold = "#ffd94d";
        string text = $"Level <color={gold}>{GameSession.Level}</color>    " +
                      $"Score <color={gold}>{GameSession.Score:N0}</color>    " +
                      $"Best <color={gold}>{Mathf.Max(GameSession.HighScore, GameSession.Score):N0}</color>";
        float width = statusText.CalcSize(new GUIContent(text)).x + 16f;
        var box = new Rect(W - 8 - width, strip * 0.14f, width, strip * 0.72f);

        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(box, text, statusText);
    }

    void DrawBanner()
    {
        if (string.IsNullOrEmpty(bannerTitle) || Time.unscaledTime >= bannerUntil) return;
        float remaining = bannerUntil - Time.unscaledTime;
        float alpha = Mathf.Clamp01(remaining / 0.5f); // fade out over the last half second
        Shadowed(new Rect(0, H * 0.26f, W, 80), bannerTitle, bigText, new Color(Gold.r, Gold.g, Gold.b, alpha));
        Shadowed(new Rect(0, H * 0.26f + 76, W, 40), bannerSubtitle, smallText, new Color(1f, 1f, 1f, alpha));
    }

    void DrawLevelClear()
    {
        var panel = CenteredPanel(620, 400);
        float y = panel.y + 22;
        Shadowed(new Rect(panel.x, y, panel.width, 64), $"LEVEL {GameSession.Level} CLEAR!", bigText, Gold);
        y += 82;
        y = Stat(panel, y, "Kills", GameSession.Kills.ToString("N0"));
        y = Stat(panel, y, "Damage taken", $"{GameSession.DamageTaken} (-{GameSession.DamageTaken * GameSession.DamagePenalty:N0})");
        y = Stat(panel, y, "Score", GameSession.Score.ToString("N0"));
        y += 14;
        string reward = GameSession.MaxHearts < GameSession.MaxHeartsCap ? "+1 heart. " : "";
        Shadowed(new Rect(panel.x + 10, y, panel.width - 20, 34), $"Next: Level {GameSession.Level + 1}. {reward}Monsters grow stronger...", smallText, Color.white);
        y += 46;
        if (Time.unscaledTime >= screenInputTime)
            Shadowed(new Rect(panel.x, y, panel.width, 34), "Enter: continue      Esc: end run", smallText, Gold);
    }

    void DrawGameOver()
    {
        var panel = CenteredPanel(620, 480);
        float y = panel.y + 22;
        Shadowed(new Rect(panel.x, y, panel.width, 64), "GAME OVER", bigText, new Color(1f, 0.35f, 0.3f));
        y += 82;
        y = Stat(panel, y, "Reached level", GameSession.Level.ToString());
        y = Stat(panel, y, "Kills", GameSession.Kills.ToString("N0"));
        y = Stat(panel, y, "Damage taken", $"{GameSession.DamageTaken} (-{GameSession.DamageTaken * GameSession.DamagePenalty:N0})");
        y = Stat(panel, y, "Final score", GameSession.Score.ToString("N0"));
        y = Stat(panel, y, "High score", GameSession.HighScore.ToString("N0"));
        y += 6;
        if (newHighScore)
            Shadowed(new Rect(panel.x, y, panel.width, 34), "NEW HIGH SCORE!", smallText, Gold);
        y += 44;
        if (Time.unscaledTime >= screenInputTime)
            Shadowed(new Rect(panel.x, y, panel.width, 34), "Enter: new run      Esc: title screen", smallText, Gold);
    }

    float Stat(Rect panel, float y, string label, string value)
    {
        statText.alignment = TextAnchor.MiddleLeft;
        Shadowed(new Rect(panel.x + 50, y, panel.width - 100, 36), label, statText, Color.white);
        statText.alignment = TextAnchor.MiddleRight;
        Shadowed(new Rect(panel.x + 50, y, panel.width - 100, 36), value, statText, Gold);
        return y + 40;
    }

    Rect CenteredPanel(float width, float height)
    {
        width = Mathf.Min(width, W - 20f);
        height = Mathf.Min(height, H - 20f);
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        var panel = new Rect(W / 2f - width / 2f, H / 2f - height / 2f, width, height);
        GUI.color = new Color(0.03f, 0.05f, 0.08f, 0.88f);
        GUI.DrawTexture(panel, Texture2D.whiteTexture);
        GUI.color = new Color(Gold.r, Gold.g, Gold.b, 0.6f);
        GUI.DrawTexture(new Rect(panel.x, panel.y, panel.width, 3), Texture2D.whiteTexture);
        GUI.DrawTexture(new Rect(panel.x, panel.yMax - 3, panel.width, 3), Texture2D.whiteTexture);
        GUI.color = old;
        return panel;
    }

    void DrawPauseMenu()
    {
        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(0, 0, W, H), Texture2D.whiteTexture);
        GUI.color = old;

        Shadowed(new Rect(0, H / 2f - 130, W, 70), "PAUSED", bigText, Color.white);
        for (int i = 0; i < PauseItems.Length; i++)
        {
            var rect = new Rect(W / 2f - 180, H / 2f - 30 + i * 58, 360, 52);
            var e = Event.current;
            if (rect.Contains(e.mousePosition))
            {
                pauseSelected = i;
                if (e.type == EventType.MouseDown && e.button == 0)
                {
                    e.Use();
                    ChoosePauseItem(i);
                    return;
                }
            }
            bool isSelected = i == pauseSelected;
            Shadowed(rect, isSelected ? $"> {PauseItems[i]} <" : PauseItems[i], menuText, isSelected ? Gold : Color.white);
        }
        Shadowed(new Rect(0, H / 2f + 100, W, 34), "Quitting ends the run (your score still counts).", smallText, Color.white);
    }

    // Boss health bar with an inline "BOSS" label, in the middle of the bottom wall row.
    void DrawBossBar()
    {
        var boss = Boss.Instance;
        if (boss == null || !boss.gameObject.activeInHierarchy) return;

        statusText.fontSize = Mathf.Max(9, Mathf.RoundToInt(strip * 0.4f));
        const string label = "BOSS";
        float labelWidth = statusText.CalcSize(new GUIContent(label)).x + 10f;
        float barWidth = Mathf.Min(360f, W * 0.35f);
        float barHeight = strip * 0.36f;
        float left = W / 2f - (labelWidth + barWidth) / 2f;
        float centerY = H - strip / 2f;
        var bar = new Rect(left + labelWidth, centerY - barHeight / 2f, barWidth, barHeight);
        float fill = (float)boss.Health.Current / boss.Health.Max;

        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.6f);
        GUI.DrawTexture(new Rect(left - 6, centerY - strip * 0.36f, labelWidth + barWidth + 12, strip * 0.72f), Texture2D.whiteTexture);
        GUI.color = new Color(0.25f, 0.05f, 0.05f);
        GUI.DrawTexture(bar, Texture2D.whiteTexture);
        GUI.color = new Color(0.9f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(bar.x, bar.y, bar.width * fill, bar.height), Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(new Rect(left, centerY - strip / 2f, labelWidth, strip), label, statusText);
    }

    // Minimap in the bottom-right corner, sized to fit inside the bottom wall row.
    void DrawMinimap()
    {
        var rooms = RoomManager.Instance;
        if (rooms == null || rooms.Visited == null) return;

        const float gap = 2f;
        float cellH = (strip * 0.84f - gap * (rooms.rows - 1)) / rooms.rows;
        var minimapCell = new Vector2(cellH * 1.7f, cellH);
        float width = rooms.cols * (minimapCell.x + gap) - gap;
        float height = rooms.rows * (minimapCell.y + gap) - gap;
        float left = W - 8 - width;
        float top = H - strip / 2f - height / 2f;

        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.55f);
        GUI.DrawTexture(new Rect(left - 3, top - 3, width + 6, height + 6), Texture2D.whiteTexture);

        for (int r = 0; r < rooms.rows; r++)
        {
            for (int c = 0; c < rooms.cols; c++)
            {
                int index = r * rooms.cols + c;
                bool current = rooms.Current == new Vector2Int(c, r);
                Color color = rooms.Visited[index] ? VisitedColor : Unvisited;
                // The lair moves every level, so it only shows once you've found it.
                if (index == rooms.bossRoomIndex && !Boss.Defeated && rooms.Visited[index]) color = BossRoomColor;
                if (current) color = CurrentColor;
                GUI.color = color;
                var rect = new Rect(left + c * (minimapCell.x + gap), top + r * (minimapCell.y + gap), minimapCell.x, minimapCell.y);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }
        }
        GUI.color = old;
    }

    static void Shadowed(Rect rect, string text, GUIStyle style, Color color)
    {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0f, 0f, 0f, 0.85f * color.a);
        GUI.Label(new Rect(rect.x + 2, rect.y + 2, rect.width, rect.height), text, style);
        style.normal.textColor = color;
        GUI.Label(rect, text, style);
        style.normal.textColor = old;
    }

    void EnsureStyles()
    {
        if (bigText != null) return;
        bigText = new GUIStyle(GUI.skin.label) { fontSize = 56, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        smallText = new GUIStyle(GUI.skin.label) { fontSize = 24, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        menuText = new GUIStyle(GUI.skin.label) { fontSize = 34, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        messageText = new GUIStyle(GUI.skin.box) { fontSize = 26, alignment = TextAnchor.MiddleCenter, wordWrap = true };
        messageText.normal.textColor = Color.white;
        statText = new GUIStyle(GUI.skin.label) { fontSize = 28, fontStyle = FontStyle.Bold };
        statusText = new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter, richText = true };
        statusText.normal.textColor = Color.white;
    }
}
