using UnityEngine;

/// <summary>
/// Attach to any falling rock GameObject.
/// Deals damage to the player on collision.
/// </summary>
public class FallingRock : MonoBehaviour
{
    [Tooltip("Damage dealt to the player on impact.")]
    public float damage = 25f;

    [Tooltip("Minimum impact speed (m/s) required to deal damage. Prevents damage from resting contact.")]
    public float minImpactSpeed = 2f;

    private void OnCollisionEnter(Collision col)
    {
        if (col.relativeVelocity.magnitude < minImpactSpeed)
            return;

        PlayerHealth health = col.gameObject.GetComponent<PlayerHealth>()
                           ?? col.gameObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(damage);
    }
}
