using UnityEngine;

// Fades a sprite out (optionally after a delay, optionally growing) and then destroys it.
// Used for the lightning bolt's after-images and the bush-burning flames.
public class FadeOut : MonoBehaviour
{
    public float duration = 0.3f;
    public float delay;
    public float grow;

    SpriteRenderer sr;
    Color startColor;
    Vector3 baseScale;
    float elapsed;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        startColor = sr != null ? sr.color : Color.white;
        baseScale = transform.localScale;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        float k = Mathf.Clamp01((elapsed - delay) / Mathf.Max(0.01f, duration));
        if (sr != null) sr.color = new Color(startColor.r, startColor.g, startColor.b, startColor.a * (1f - k));
        transform.localScale = baseScale * (1f + grow * elapsed);
        if (elapsed >= delay + duration) Destroy(gameObject);
    }
}
