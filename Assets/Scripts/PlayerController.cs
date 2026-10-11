using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Top-down 4-direction movement with a sword swing.
// Move: WASD / arrow keys / left stick. Attack: Space, J or gamepad south button.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public SwordHitbox sword;
    public float swingDuration = 0.2f;
    public float swingCooldown = 0.1f;
    public float swingArc = 140f;
    public float swordReach = 0.8f;

    Rigidbody2D rb;
    Health health;
    PlayerAnimator anim;
    SpriteRenderer swordRenderer;
    Vector2 input;
    Vector2 facing = Vector2.down;
    bool swinging;
    float nextSwingTime;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        anim = GetComponent<PlayerAnimator>();
        health.Damaged += OnDamaged;
        health.Died += OnDied;
        if (sword != null)
        {
            swordRenderer = sword.GetComponent<SpriteRenderer>();
            sword.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        if (health.IsDead) return;

        input = ReadMove();
        // Snap to 4 directions, like the classic games.
        if (input != Vector2.zero)
        {
            input = Mathf.Abs(input.x) > Mathf.Abs(input.y)
                ? new Vector2(Mathf.Sign(input.x), 0f)
                : new Vector2(0f, Mathf.Sign(input.y));
            if (!swinging) facing = input;
        }

        if (AttackPressed() && !swinging && Time.time >= nextSwingTime)
            StartCoroutine(Swing());

        if (anim != null) anim.UpdateVisual(facing, input != Vector2.zero, swinging);
    }

    void FixedUpdate()
    {
        if (health.IsDead || health.IsStunned) return;
        float speed = swinging ? moveSpeed * 0.3f : moveSpeed;
        rb.linearVelocity = input * speed;
    }

    IEnumerator Swing()
    {
        swinging = true;
        Vector2 swingFacing = facing;
        AudioManager.Play(Sfx.Swing);

        // Sword sprite points up; sweep it clockwise through an arc centred on the facing direction.
        // When facing left, sweep the other way so the swing still reads as a forehand slash.
        float baseAngle = Vector2.SignedAngle(Vector2.up, swingFacing);
        float sweep = swingFacing.x < 0f ? -swingArc : swingArc;
        // Draw the sword behind the player when swinging upward.
        swordRenderer.sortingOrder = swingFacing.y > 0.5f ? 4 : 6;

        sword.Begin(swingFacing);
        SetSwordAngle(baseAngle + sweep * 0.5f);
        sword.gameObject.SetActive(true);

        for (float t = 0f; t < swingDuration; t += Time.deltaTime)
        {
            if (health.IsDead) break;
            float k = t / swingDuration;
            float eased = 1f - (1f - k) * (1f - k); // fast start, slow finish
            SetSwordAngle(baseAngle + Mathf.Lerp(sweep * 0.5f, -sweep * 0.5f, eased));
            yield return null;
        }

        sword.gameObject.SetActive(false);
        swinging = false;
        nextSwingTime = Time.time + swingCooldown;
    }

    void SetSwordAngle(float angle)
    {
        var rot = Quaternion.Euler(0f, 0f, angle);
        sword.transform.localRotation = rot;
        sword.transform.localPosition = rot * Vector3.up * swordReach;
    }

    void OnDamaged()
    {
        if (!health.IsDead) AudioManager.Play(Sfx.Hurt);
    }

    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        sword.gameObject.SetActive(false);
        transform.rotation = Quaternion.Euler(0, 0, 90);
        AudioManager.StopMusic();
        AudioManager.Play(Sfx.GameOver);
        StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        yield return new WaitForSeconds(2.5f);
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    static Vector2 ReadMove()
    {
        Vector2 v = Vector2.zero;
        var kb = Keyboard.current;
        if (kb != null)
        {
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) v.x -= 1;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) v.x += 1;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v.y -= 1;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v.y += 1;
        }
        var pad = Gamepad.current;
        if (pad != null && v == Vector2.zero)
        {
            Vector2 stick = pad.leftStick.ReadValue();
            if (stick.magnitude > 0.3f) v = stick;
        }
        return v;
    }

    static bool AttackPressed()
    {
        var kb = Keyboard.current;
        if (kb != null && (kb.spaceKey.wasPressedThisFrame || kb.jKey.wasPressedThisFrame)) return true;
        var pad = Gamepad.current;
        return pad != null && pad.buttonSouth.wasPressedThisFrame;
    }
}
