using UnityEngine;

// The hidden upgraded sword. Bobs and glints; walking over it upgrades the player's sword.
public class SwordPickup : MonoBehaviour
{
    public Sprite upgradedSwordSprite;
    public string itemName = "Crystal Sword";

    Vector3 basePosition;
    SpriteRenderer sr;

    void Awake()
    {
        basePosition = transform.position;
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        transform.position = basePosition + Vector3.up * Mathf.Sin(Time.time * 3f) * 0.1f;
        if (sr != null) sr.color = Color.Lerp(Color.white, new Color(0.7f, 1f, 1f), Mathf.PingPong(Time.time * 2f, 1f));
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;
        player.UpgradeSword(upgradedSwordSprite);
        AudioManager.Play(Sfx.ItemGet);
        GameHUD.ShowMessage($"You found the {itemName}!");
        Destroy(gameObject);
    }
}
