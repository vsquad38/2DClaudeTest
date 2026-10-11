using UnityEngine;

// Restores health when the player walks over it.
public class HeartPickup : MonoBehaviour
{
    public int healAmount = 1;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() == null) return;
        other.GetComponent<Health>().Heal(healAmount);
        AudioManager.Play(Sfx.Pickup);
        Destroy(gameObject);
    }
}
