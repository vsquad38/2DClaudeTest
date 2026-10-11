using UnityEngine;

// Draws the player's hearts, monsters remaining, a room minimap, and a win/lose message.
public class GameHUD : MonoBehaviour
{
    public Health playerHealth;
    public Texture2D heartFull;
    public Texture2D heartEmpty;
    public float heartSize = 32f;
    public Vector2 minimapCell = new Vector2(22, 13);

    static readonly Color Unvisited = new Color(0f, 0f, 0f, 0.45f);
    static readonly Color VisitedColor = new Color(0.65f, 0.65f, 0.6f, 0.9f);
    static readonly Color CurrentColor = new Color(0.35f, 0.85f, 0.35f, 1f);

    GUIStyle bigText;
    GUIStyle smallText;
    bool wonPlayed;

    void Update()
    {
        if (wonPlayed || playerHealth == null || playerHealth.IsDead) return;
        if (Enemy.All.Count == 0)
        {
            wonPlayed = true;
            AudioManager.StopMusic();
            AudioManager.Play(Sfx.Victory);
        }
    }

    void OnGUI()
    {
        if (playerHealth == null) return;

        for (int i = 0; i < playerHealth.maxHealth; i++)
        {
            var tex = i < playerHealth.Current ? heartFull : heartEmpty;
            GUI.DrawTexture(new Rect(12 + i * (heartSize + 4), 12, heartSize, heartSize), tex);
        }

        if (bigText == null)
        {
            bigText = new GUIStyle(GUI.skin.label) { fontSize = 48, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            bigText.normal.textColor = Color.white;
            smallText = new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperRight };
        }

        int monstersLeft = Enemy.All.Count;
        DrawMinimap(out float minimapLeft);
        GUI.Label(new Rect(minimapLeft - 220, 12, 208, 32), $"Monsters: {monstersLeft}", smallText);

        var center = new Rect(0, 0, Screen.width, Screen.height);
        if (playerHealth.IsDead) GUI.Label(center, "GAME OVER", bigText);
        else if (monstersLeft == 0) GUI.Label(center, "YOU WIN!", bigText);
    }

    void DrawMinimap(out float left)
    {
        left = Screen.width;
        var rooms = RoomManager.Instance;
        if (rooms == null || rooms.Visited == null) return;

        const float gap = 2f;
        float width = rooms.cols * (minimapCell.x + gap) - gap;
        left = Screen.width - 12 - width;

        var old = GUI.color;
        for (int r = 0; r < rooms.rows; r++)
        {
            for (int c = 0; c < rooms.cols; c++)
            {
                bool current = rooms.Current == new Vector2Int(c, r);
                GUI.color = current ? CurrentColor : rooms.Visited[r * rooms.cols + c] ? VisitedColor : Unvisited;
                var rect = new Rect(left + c * (minimapCell.x + gap), 12 + r * (minimapCell.y + gap), minimapCell.x, minimapCell.y);
                GUI.DrawTexture(rect, Texture2D.whiteTexture);
            }
        }
        GUI.color = old;
    }
}
