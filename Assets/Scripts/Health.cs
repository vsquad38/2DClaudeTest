using System;
using System.Collections;
using UnityEngine;

// Shared hit points, invulnerability frames, hit flash and knockback for the player and monsters.
[RequireComponent(typeof(Rigidbody2D))]
public class Health : MonoBehaviour
{
    public int maxHealth = 3;
    public float invulnerableTime = 0.4f;
    public float stunTime = 0.15f;

    public int Current { get; private set; }
    public bool IsDead => Current <= 0;
    public bool IsStunned => Time.time < stunnedUntil;

    public event Action Damaged;
    public event Action Died;

    Rigidbody2D rb;
    SpriteRenderer sr;
    float invulnerableUntil;
    float stunnedUntil;

    void Awake()
    {
        Current = maxHealth;
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    public bool TakeDamage(int amount, Vector2 knockback)
    {
        if (IsDead || Time.time < invulnerableUntil) return false;

        Current = Mathf.Max(0, Current - amount);
        invulnerableUntil = Time.time + invulnerableTime;
        stunnedUntil = Time.time + stunTime;
        rb.linearVelocity = knockback;

        StopAllCoroutines();
        StartCoroutine(Flash());
        Damaged?.Invoke();
        if (IsDead) Died?.Invoke();
        return true;
    }

    public void Heal(int amount)
    {
        Current = Mathf.Min(maxHealth, Current + amount);
    }

    IEnumerator Flash()
    {
        if (sr == null) yield break;
        while (Time.time < invulnerableUntil)
        {
            sr.color = new Color(1f, 0.4f, 0.4f, 0.5f);
            yield return new WaitForSeconds(0.05f);
            sr.color = Color.white;
            yield return new WaitForSeconds(0.05f);
        }
        sr.color = Color.white;
    }
}
