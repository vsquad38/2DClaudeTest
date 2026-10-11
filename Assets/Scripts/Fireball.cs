using UnityEngine;

// Boss projectile: flies straight, hurts the player, and is destroyed by walls or a sword swing.
[RequireComponent(typeof(Rigidbody2D))]
public class Fireball : MonoBehaviour
{
    public int damage = 1;
    public float knockback = 6f;
    public float lifetime = 4f;
    public float spinSpeed = 720f;

    Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        Destroy(gameObject, lifetime);
    }

    public void Launch(Vector2 velocity)
    {
        rb.linearVelocity = velocity;
    }

    void Update()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.deltaTime);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<SwordHitbox>() != null)
        {
            Destroy(gameObject); // slashed out of the air
            return;
        }

        var player = other.GetComponent<PlayerController>();
        if (player != null)
        {
            player.GetComponent<Health>().TakeDamage(damage, rb.linearVelocity.normalized * knockback);
            Destroy(gameObject);
            return;
        }

        if (other.attachedRigidbody != null && other.attachedRigidbody.bodyType == RigidbodyType2D.Static)
            Destroy(gameObject); // hit a wall, bush or water
    }
}
