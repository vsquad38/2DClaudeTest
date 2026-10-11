using UnityEngine;

// Trigger collider on the sword; damages any monster it touches during a swing.
public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 9f;

    Vector2 direction;

    public void Begin(Vector2 swingDirection)
    {
        direction = swingDirection;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        var enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;

        Vector2 away = ((Vector2)(other.transform.position - transform.parent.position)).normalized;
        if (away == Vector2.zero) away = direction;
        enemy.GetComponent<Health>().TakeDamage(damage, away * knockbackForce);
    }
}
