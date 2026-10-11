using UnityEngine;
using UnityEngine.Tilemaps;

// Lightning bolt fired from the sword while the player has enough health.
// Deals the same damage as a sword hit, then disappears on hitting a monster, the boss, or a wall.
// With the Crystal Sword it's bigger, glows, leaves a trail, and burns through bushes.
[RequireComponent(typeof(Rigidbody2D))]
public class SwordBeam : MonoBehaviour
{
    public float speed = 11f;
    public float lifetime = 1.5f;
    public float knockbackForce = 9f;

    [Header("Crystal Sword")]
    public Sprite crystalSprite;
    public float crystalScale = 1.5f;
    public SpriteRenderer glow;            // child halo, shown only for the crystal bolt
    public GameObject burnEffectPrefab;    // flames left where a bush burns away
    public float trailInterval = 0.03f;

    public static int ActiveCount { get; private set; }

    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector2 direction;
    int damage;
    bool crystal;
    Tilemap walls;
    TileBase bushTile;
    float nextTrailTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        if (glow != null) glow.enabled = false;
        ActiveCount++;
        Destroy(gameObject, lifetime);
    }

    void OnDestroy() => ActiveCount--;

    public void Launch(Vector2 dir, int swordDamage, bool crystalSword, Tilemap wallTilemap, TileBase bush)
    {
        direction = dir.normalized;
        damage = swordDamage;
        crystal = crystalSword;
        walls = wallTilemap;
        bushTile = bush;
        transform.rotation = Quaternion.FromToRotation(Vector3.up, direction); // art points up
        rb.linearVelocity = direction * speed;

        if (crystal)
        {
            if (crystalSprite != null) sr.sprite = crystalSprite;
            transform.localScale *= crystalScale;
            if (glow != null) glow.enabled = true;
        }
    }

    void Update()
    {
        // Crackle: flicker brightness; the crystal glow pulses.
        sr.color = Color.Lerp(Color.white, new Color(1f, 1f, 1f, 0.75f), Random.value);
        if (crystal && glow != null)
            glow.color = new Color(0.55f, 0.95f, 1f, 0.35f + 0.25f * Mathf.Sin(Time.time * 40f));

        if (crystal && Time.time >= nextTrailTime)
        {
            nextTrailTime = Time.time + trailInterval;
            SpawnAfterImage();
        }

        // Don't fly on into a neighbouring room through a doorway.
        var rooms = RoomManager.Instance;
        if (rooms != null && rooms.RoomAt(transform.position) != rooms.Current)
        {
            Destroy(gameObject);
            return;
        }

        CheckTerrain();
    }

    // Walls, water and bushes come from the walls tilemap: crystal bolts burn bushes and keep going.
    void CheckTerrain()
    {
        if (walls == null) return;
        Vector2 front = (Vector2)transform.position + direction * 0.35f;
        var cell = walls.WorldToCell(front);
        var tile = walls.GetTile(cell);
        if (tile == null) return;

        if (crystal && tile == bushTile) BurnBush(cell);
        else Destroy(gameObject);
    }

    void BurnBush(Vector3Int cell)
    {
        walls.SetTile(cell, null); // the tilemap collider updates itself, opening the way
        if (burnEffectPrefab != null) Instantiate(burnEffectPrefab, walls.GetCellCenterWorld(cell), Quaternion.identity);
        AudioManager.Play(Sfx.Burn);
    }

    void SpawnAfterImage()
    {
        var ghost = new GameObject("BoltTrail");
        ghost.transform.SetPositionAndRotation(transform.position, transform.rotation);
        ghost.transform.localScale = transform.localScale;
        var ghostSr = ghost.AddComponent<SpriteRenderer>();
        ghostSr.sprite = sr.sprite;
        ghostSr.sortingOrder = sr.sortingOrder - 1;
        ghostSr.color = new Color(0.55f, 0.95f, 1f, 0.5f);
        ghost.AddComponent<FadeOut>().duration = 0.18f;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var boss = other.GetComponent<Boss>();
        if (boss != null)
        {
            boss.Health.TakeDamage(boss.DamagePerHit(crystal), direction * knockbackForce * 0.3f);
            Destroy(gameObject);
            return;
        }

        var enemy = other.GetComponent<Enemy>();
        if (enemy != null)
        {
            enemy.GetComponent<Health>().TakeDamage(damage, direction * knockbackForce);
            Destroy(gameObject);
        }
    }
}
