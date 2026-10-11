using UnityEngine;

// Trigger collider on the sword; damages any monster it touches during a swing.
public class SwordHitbox : MonoBehaviour
{
    public int damage = 1;
    public float knockbackForce = 9f;
    public bool upgraded;

    Vector2 direction;

    public void Begin(Vector2 swingDirection)
    {
        direction = swingDirection;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Vector2 away = ((Vector2)(other.transform.position - transform.parent.position)).normalized;
        if (away == Vector2.zero) away = direction;

        var boss = other.GetComponent<Boss>();
        if (boss != null)
        {
            // The boss is heavy: it barely budges, and its hit count depends on which sword you have.
            boss.Health.TakeDamage(boss.DamagePerHit(upgraded), away * knockbackForce * 0.3f);
            return;
        }

        var enemy = other.GetComponent<Enemy>();
        if (enemy == null) return;
        enemy.GetComponent<Health>().TakeDamage(damage, away * knockbackForce);
    }
}
