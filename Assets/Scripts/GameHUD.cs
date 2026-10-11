using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Draws the player's hearts, sword, a room minimap, the boss health bar, messages, and the win/lose screen.
public class GameHUD : MonoBehaviour
{
    public Health playerHealth;
    public Texture2D heartFull;
    public Texture2D heartEmpty;
    public Texture2D upgradedSwordIcon;
    public float heartSize = 32f;
    public Vector2 minimapCell = new Vector2(22, 13);
    public float messageDuration = 3f;

    static readonly Color Unvisited = new Color(0f, 0f, 0f, 0.45f);
    static readonly Color VisitedColor = new Color(0.65f, 0.65f, 0.6f, 0.9f);
    static readonly Color CurrentColor = new Color(0.35f, 0.85f, 0.35f, 1f);
    static readonly Color BossRoomColor = new Color(0.75f, 0.15f, 0.15f, 0.9f);

    static GameHUD instance;

    PlayerController player;
    GUIStyle bigText;
    GUIStyle smallText;
    GUIStyle messageText;
    bool won;
    string message;
    float messageUntil;

    public static void ShowMessage(string text)
    {
        if (instance == null) return;
        instance.message = text;
        instance.messageUntil = Time.unscaledTime + instance.messageDuration;
    }

    void Awake()
    {
        instance = this;
        Time.timeScale = 1f;
        if (playerHealth != null) player = playerHealth.GetComponent<PlayerController>();
    }

    void Update()
    {
        if (!won && Boss.Defeated && playerHealth != null && !playerHealth.IsDead)
        {
            won = true;
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Victory);
            Time.timeScale = 0f; // freeze the world; the game is over
        }

        if (won)
        {
            var kb = Keyboard.current;
            var pad = Gamepad.current;
            bool restart = (kb != null && (kb.enterKey.wasPressedThisFrame || kb.rKey.wasPressedThisFrame))
                || (pad != null && pad.startButton.wasPressedThisFrame);
            if (restart)
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
        }
    }

    void OnGUI()
    {
        if (playerHealth == null) return;

        if (bigText == null)
        {
            bigText = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            bigText.normal.textColor = Color.white;
            smallText = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.MiddleCenter };
            smallText.normal.textColor = Color.white;
            messageText = new GUIStyle(GUI.skin.box) { fontSize = 24, alignment = TextAnchor.MiddleCenter };
            messageText.normal.textColor = Color.white;
        }

        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            var tex = i < playerHealth.Current ? heartFull : heartEmpty;
            GUI.DrawTexture(new Rect(12 + i * (heartSize + 4), 12, heartSize, heartSize), tex);
        }

        if (player != null && player.SwordUpgraded && upgradedSwordIcon != null)
        {
            float x = 12 + playerHealth.maxHealth * (heartSize + 4) + 8;
            GUI.DrawTexture(new Rect(x, 12, heartSize, heartSize), upgradedSwordIcon);
        }

        DrawMinimap();
        DrawBossBar();

        if (!string.IsNullOrEmpty(message) && Time.unscaledTime < messageUntil)
            GUI.Label(new Rect(Screen.width / 2f - 260, Screen.height - 90, 520, 50), message, messageText);

        var center = new Rect(0, 0, Screen.width, Screen.height);
        if (playerHealth.IsDead)
        {
            GUI.Label(center, "GAME OVER", bigText);
        }
        else if (won)
        {
            GUI.Label(center, "YOU WIN!", bigText);
            GUI.Label(new Rect(0, Screen.height / 2f + 40, Screen.width, 40), "The boss is defeated. Press Enter to play again.", smallText);
        }
    }

    void DrawBossBar()
    {
        var boss = Boss.Instance;
        if (boss == null || !boss.gameObject.activeInHierarchy) return;

        float width = Mathf.Min(420f, Screen.width - 40f);
        var back = new Rect(Screen.width / 2f - width / 2f, Screen.height - 40, width, 18);
        float fill = (float)boss.Health.Current / boss.Health.maxHealth;

        var old = GUI.color;
        GUI.color = new Color(0f, 0f, 0f, 0.7f);
        GUI.DrawTexture(new Rect(back.x - 2, back.y - 2, back.width + 4, back.height + 4), Texture2D.whiteTexture);
        GUI.color = new Color(0.85f, 0.15f, 0.15f);
        GUI.DrawTexture(new Rect(back.x, back.y, back.width * fill, back.height), Texture2D.whiteTexture);
        GUI.color = old;
        GUI.Label(new Rect(back.x, back.y - 28, back.width, 24), "BOSS", smallText);
    }

    void DrawMinimap()
    {
        var rooms = RoomManager.Instance;
        if (rooms == null || rooms.Visited == null) return;

        const float gap = 2f;
        float width = rooms.cols * (minimapCell.x + gap) - gap;
        float left = Screen.width - 12 - width;

        var old = GUI.color;
        for (int r = 0; r < rooms.rows; r++)
        {
            for (int c = 0; c < rooms.cols; c++)
            {
                int index = r * rooms.cols + c;
                bool current = rooms.Current == new Vector2Int(c, r);
                Color color = rooms.Visited[index] ? VisitedColor : Unvisited;
                if (index == rooms.bossRoomIndex && !Boss.Defeated) color = BossRoomColor;
                if (current) color = CurrentColor;
                GUI.color = color;
                var rect = new Rect(left + c * (minimapCell.x + gap), 12 + r * (minimapCell.y + gap), minimapCell.x, minimapCell.y);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }
        }
        GUI.color = old;
    }
}
