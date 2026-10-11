using UnityEngine;

// Loops through sprite frames at a fixed rate (used for monsters).
[RequireComponent(typeof(SpriteRenderer))]
public class SpriteAnimator : MonoBehaviour
{
    public Sprite[] frames;
    public float fps = 4f;

    SpriteRenderer sr;
    float offset;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        // Desync monsters of the same type so they don't all animate in lockstep.
        offset = Random.value * 10f;
    }

    void Update()
    {
        if (frames == null || frames.Length == 0) return;
        sr.sprite = frames[(int)((Time.time + offset) * fps) % frames.Length];
    }
}
