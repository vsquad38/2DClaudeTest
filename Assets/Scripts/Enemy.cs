using UnityEngine;

// Simple monster AI: wanders randomly and chases the player when close. Hurts the player on contact.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class Enemy : MonoBehaviour
{
    public enum Behaviour { Wander, Chase, Erratic }

    public Behaviour behaviour = Behaviour.Wander;
    public float moveSpeed = 1.5f;
    public float chaseRange = 4f;
    public int contactDamage = 1;
    public float contactKnockback = 8f;
    public float changeDirectionTime = 1.5f;
    public GameObject dropOnDeath;
    [Range(0f, 1f)] public float dropChance = 0.3f;

    Rigidbody2D rb;
    Health health;
    SpriteRenderer sr;
    Transform player;
    Vector2 wanderDir;
    float nextDirectionChange;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        sr = GetComponent<SpriteRenderer>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
    }

    void OnDamaged()
    {
        AudioManager.Play(health.IsDead ? Sfx.EnemyDie : Sfx.Hit);
    }

    void Start()
    {
        var p = FindAnyObjectByType<PlayerController>();
        if (p != null) player = p.transform;
        PickWanderDirection();
    }

    void FixedUpdate()
    {
        if (health.IsStunned) return;

        Vector2 dir = wanderDir;
        bool playerAlive = player != null && !player.GetComponent<Health>().IsDead;
        bool playerNear = playerAlive && Vector2.Distance(player.position, transform.position) < chaseRange;

        if (behaviour == Behaviour.Chase && playerNear)
        {
            dir = ((Vector2)(player.position - transform.position)).normalized;
        }
        else if (Time.time >= nextDirectionChange)
        {
            PickWanderDirection();
            dir = wanderDir;
        }

        rb.linearVelocity = dir * moveSpeed;
        if (dir.x != 0f) sr.flipX = dir.x < 0f;
    }

    void PickWanderDirection()
    {
        if (behaviour == Behaviour.Erratic)
        {
            wanderDir = Random.insideUnitCircle.normalized;
            nextDirectionChange = Time.time + Random.Range(0.3f, 0.8f);
        }
        else
        {
            Vector2[] dirs = { Vector2.up, Vector2.down, Vector2.left, Vector2.right, Vector2.zero };
            wanderDir = dirs[Random.Range(0, dirs.Length)];
            nextDirectionChange = Time.time + Random.Range(0.6f, changeDirectionTime);
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        // Bounce off walls by picking a new direction.
        if (collision.collider.GetComponent<PlayerController>() == null)
            nextDirectionChange = 0f;
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

    void OnDied()
    {
        if (dropOnDeath != null && Random.value < dropChance)
            Instantiate(dropOnDeath, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }
}
