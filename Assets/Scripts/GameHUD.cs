using UnityEngine;

// Draws the player's hearts and a win/lose message.
public class GameHUD : MonoBehaviour
{
    public Health playerHealth;
    public Texture2D heartFull;
    public Texture2D heartEmpty;
    public float heartSize = 32f;

    GUIStyle bigText;
    bool wonPlayed;

    void Update()
    {
        if (wonPlayed || playerHealth == null || playerHealth.IsDead) return;
        if (FindObjectsByType<Enemy>().Length == 0)
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
        }

        int monstersLeft = FindObjectsByType<Enemy>().Length;
        GUI.Label(new Rect(Screen.width - 220, 12, 208, 32), $"Monsters: {monstersLeft}",
            new GUIStyle(GUI.skin.label) { fontSize = 22, alignment = TextAnchor.UpperRight });

        var center = new Rect(0, 0, Screen.width, Screen.height);
        if (playerHealth.IsDead) GUI.Label(center, "GAME OVER", bigText);
        else if (monstersLeft == 0) GUI.Label(center, "YOU WIN!", bigText);
    }
}
