using System.Collections;
using UnityEngine;

// The boss: stalks the player, then either winds up and charges or fires a spread of fireballs.
// Takes a fixed number of sword hits to beat, fewer with the upgraded sword. Beating it wins the game.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class Boss : MonoBehaviour
{
    public static Boss Instance { get; private set; }
    public static bool Defeated { get; private set; }

    public int hitsWithBasicSword = 10;
    public int hitsWithUpgradedSword = 3;

    public float walkSpeed = 1.4f;
    public float chargeSpeed = 8f;
    public float walkTime = 2f;
    public int contactDamage = 1;
    public float contactKnockback = 10f;

    public GameObject fireballPrefab;
    public int fireballsPerVolley = 5;
    public float fireballSpread = 60f;
    public float fireballSpeed = 5f;

    public Sprite[] walkFrames;
    public Sprite attackFrame;
    public Rect bounds;

    Rigidbody2D rb;
    Health health;
    SpriteRenderer sr;
    Transform player;
    Vector3 spawnPosition;
    bool hitWall;

    public Health Health => health != null ? health : (health = GetComponent<Health>());

    void Awake()
    {
        Instance = this;
        Defeated = false;
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        sr = GetComponent<SpriteRenderer>();
        spawnPosition = transform.position;
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnEnable()
    {
        StartCoroutine(Brain());
    }

    public void ResetToSpawn()
    {
        transform.position = spawnPosition;
        if (rb != null)
        {
            rb.position = spawnPosition;
            rb.linearVelocity = Vector2.zero;
        }
    }

    // Damage per sword hit so that the boss always falls in exactly the configured number of hits.
    public int DamagePerHit(bool upgradedSword)
    {
        int hits = upgradedSword ? hitsWithUpgradedSword : hitsWithBasicSword;
        return Mathf.CeilToInt((float)Health.maxHealth / Mathf.Max(1, hits));
    }

    IEnumerator Brain()
    {
        if (player == null)
        {
            var p = FindAnyObjectByType<PlayerController>();
            if (p != null) player = p.transform;
        }

        // A moment to take in the room before the fight starts.
        rb.linearVelocity = Vector2.zero;
        yield return new WaitForSeconds(1f);

        while (true)
        {
            yield return Walk();
            if (Random.value < 0.5f) yield return Charge();
            else yield return FireballVolley();
        }
    }

    IEnumerator Walk()
    {
        for (float t = 0f; t < walkTime; t += Time.fixedDeltaTime)
        {
            Vector2 dir = ToPlayer();
            Move(dir * walkSpeed);
            sr.sprite = walkFrames[(int)(t * 5f) % walkFrames.Length];
            yield return new WaitForFixedUpdate();
        }
    }

    IEnumerator Charge()
    {
        // Wind-up: stop, shake and glow so the player can react.
        sr.sprite = attackFrame;
        Vector2 dir = ToPlayer();
        for (float t = 0f; t < 0.6f; t += Time.fixedDeltaTime)
        {
            Move(Vector2.zero);
            sr.color = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.3f), Mathf.PingPong(t * 8f, 1f));
            yield return new WaitForFixedUpdate();
        }
        sr.color = Color.white;

        hitWall = false;
        for (float t = 0f; t < 0.7f && !hitWall; t += Time.fixedDeltaTime)
        {
            Move(dir * chargeSpeed);
            yield return new WaitForFixedUpdate();
        }

        Move(Vector2.zero);
        sr.sprite = walkFrames[0];
        yield return new WaitForSeconds(hitWall ? 1f : 0.5f); // dazed after slamming into a wall
    }

    IEnumerator FireballVolley()
    {
        Move(Vector2.zero);
        sr.sprite = attackFrame;
        yield return new WaitForSeconds(0.4f);

        Vector2 aim = ToPlayer();
        if (fireballPrefab != null)
        {
            AudioManager.Play(Sfx.Fireball);
            for (int i = 0; i < fireballsPerVolley; i++)
            {
                float k = fireballsPerVolley == 1 ? 0.5f : (float)i / (fireballsPerVolley - 1);
                float angle = Mathf.Lerp(-fireballSpread / 2f, fireballSpread / 2f, k);
                Vector2 dir = Quaternion.Euler(0, 0, angle) * aim;
                var fb = Instantiate(fireballPrefab, (Vector2)transform.position + dir * 1.2f, Quaternion.identity);
                fb.GetComponent<Fireball>().Launch(dir * fireballSpeed);
            }
        }

        yield return new WaitForSeconds(0.5f);
        sr.sprite = walkFrames[0];
    }

    void Move(Vector2 velocity)
    {
        if (health.IsStunned) return;
        // Stay inside the arena.
        if (bounds.width > 0f && !bounds.Contains(rb.position))
            velocity = (bounds.center - rb.position).normalized * walkSpeed;
        rb.linearVelocity = velocity;
        if (velocity.x != 0f) sr.flipX = velocity.x < 0f;
    }

    Vector2 ToPlayer()
    {
        if (player == null) return Vector2.down;
        Vector2 d = (Vector2)(player.position - transform.position);
        return d.sqrMagnitude > 0.001f ? d.normalized : Vector2.down;
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<PlayerController>() == null) hitWall = true;
        HurtPlayer(collision);
    }

    void OnCollisionStay2D(Collision2D collision) => HurtPlayer(collision);

    void HurtPlayer(Collision2D collision)
    {
        if (health.IsDead) return;
        var target = collision.collider.GetComponent<PlayerController>();
        if (target == null) return;
        Vector2 away = ((Vector2)(target.transform.position - transform.position)).normalized;
        target.GetComponent<Health>().TakeDamage(contactDamage, away * contactKnockback);
    }

    void OnDamaged()
    {
        AudioManager.Play(health.IsDead ? Sfx.EnemyDie : Sfx.Hit);
    }

    void OnDied()
    {
        Defeated = true;
        Destroy(gameObject);
    }
}
