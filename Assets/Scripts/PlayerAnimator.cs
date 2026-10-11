using UnityEngine;

// Picks the player's sprite from facing direction and state.
// Each frame set is: [0] stand, [1] walk A, [2] walk B, [3] attack. Side frames face right and are flipped for left.
[RequireComponent(typeof(SpriteRenderer))]
public class PlayerAnimator : MonoBehaviour
{
    public Sprite[] down;
    public Sprite[] up;
    public Sprite[] side;
    public float walkFps = 8f;

    static readonly int[] WalkCycle = { 1, 0, 2, 0 };

    SpriteRenderer sr;
    float walkTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void UpdateVisual(Vector2 facing, bool moving, bool attacking)
    {
        Sprite[] set = facing.y > 0.5f ? up : facing.y < -0.5f ? down : side;
        if (set == null || set.Length < 4) return;

        sr.flipX = facing.x < -0.5f;

        int frame = 0;
        if (attacking)
        {
            frame = 3;
        }
        else if (moving)
        {
            frame = WalkCycle[(int)(walkTimer * walkFps) % WalkCycle.Length];
            walkTimer += Time.deltaTime;
        }
        else
        {
            walkTimer = 0f;
        }
        sr.sprite = set[frame];
    }
}
