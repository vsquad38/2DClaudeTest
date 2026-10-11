using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

// Top-down 4-direction movement with a sword swing.
// Move: WASD / arrow keys / left stick. Attack: Space, J or gamepad south button.
[RequireComponent(typeof(Rigidbody2D), typeof(Health))]
public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 6f;
    public SwordHitbox sword;
    // Fired with each swing while health is at least beamHealthFraction of max.
    public SwordBeam beamPrefab;
    [Range(0f, 1f)] public float beamHealthFraction = 0.5f;
    // Below this fraction of max health the music switches to the danger theme.
    [Range(0f, 1f)] public float dangerHealthFraction = 0.25f;
    // Crystal Sword bolts burn bushes away on this tilemap.
    public Tilemap walls;
    public TileBase bushTile;
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

    // Set by RoomManager while the screen scrolls: ignore input and play the walk cycle.
    public bool Frozen { get; set; }

    public bool SwordUpgraded => sword != null && sword.upgraded;

    public void UpgradeSword(Sprite upgradedSprite)
    {
        sword.upgraded = true;
        sword.damage = 2;
        if (upgradedSprite != null) swordRenderer.sprite = upgradedSprite;
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        anim = GetComponent<PlayerAnimator>();
        health.SetMax(GameSession.MaxHearts); // hearts refill each level and grow as you clear levels
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
        if (health.IsDead || GameHUD.Paused) return;

        AudioManager.SetDanger(HealthFraction < dangerHealthFraction);

        if (Frozen)
        {
            input = Vector2.zero;
            if (anim != null) anim.UpdateVisual(facing, true, false);
            return;
        }

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
        if (health.IsDead || health.IsStunned || Frozen) return;
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
        if (HealthFraction >= beamHealthFraction) FireBeam(swingFacing);

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

    float HealthFraction => (float)health.Current / health.Max;

    // At half health or more, the sword also shoots a lightning bolt that deals the same damage as the sword.
    // One bolt on screen at a time, or two with the Crystal Sword.
    void FireBeam(Vector2 direction)
    {
        int maxBolts = sword.upgraded ? 2 : 1;
        if (beamPrefab == null || SwordBeam.ActiveCount >= maxBolts) return;
        var beam = Instantiate(beamPrefab, (Vector2)transform.position + direction * 0.9f, Quaternion.identity);
        beam.Launch(direction, sword.damage, sword.upgraded, walls, bushTile);
        AudioManager.Play(Sfx.Zap);
    }

    void SetSwordAngle(float angle)
    {
        var rot = Quaternion.Euler(0f, 0f, angle);
        sword.transform.localRotation = rot;
        sword.transform.localPosition = rot * Vector3.up * swordReach;
    }

    void OnDamaged()
    {
        GameSession.AddDamage(health.LastDamage);
        if (!health.IsDead) AudioManager.Play(Sfx.Hurt);
    }

    // Roguelike: dying ends the run. The HUD shows the game-over screen and starts a new run from there.
    void OnDied()
    {
        rb.linearVelocity = Vector2.zero;
        sword.gameObject.SetActive(false);
        transform.rotation = Quaternion.Euler(0, 0, 90);
        AudioManager.StopMusic();
        AudioManager.Play(Sfx.GameOver);
        GameHUD.OnPlayerDied();
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
