using UnityEngine;

public class DragonProjectile : MonoBehaviour
{
    [Header("Settings")]
    public float speed         = 18f;
    public float damage        = 20f;
    public float lifetime      = 6f;
    public float homingStrength = 5f;

    [Header("FX")]
    public GameObject hitEffectPrefab;

    private Transform target;

    void Start()
    {
        var rb = GetComponent<Rigidbody>();
        if (rb) rb.useGravity = false;
        if (Application.isPlaying)
            Destroy(gameObject, lifetime);
    }

    public void SetTarget(Transform playerTransform)
    {
        target = playerTransform;
        if (target != null)
            transform.rotation = Quaternion.LookRotation((target.position - transform.position).normalized);
    }

    void Update()
    {
        if (target != null && homingStrength > 0f)
        {
            Vector3 dir = (target.position - transform.position).normalized;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), homingStrength * Time.deltaTime);
        }
        transform.position += transform.forward * speed * Time.deltaTime;
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<DragonProjectile>() != null) return;
        if (other.GetComponentInParent<DragonAttack>() != null) return;

        PlayerHealth health = other.GetComponent<PlayerHealth>()
                           ?? other.GetComponentInParent<PlayerHealth>()
                           ?? other.GetComponentInChildren<PlayerHealth>();

        if (health != null)
        {
            health.TakeDamage(damage);
            SpawnHitEffect();
            Despawn();
            return;
        }

        if (!other.isTrigger)
        {
            SpawnHitEffect();
            Despawn();
        }
    }

    void SpawnHitEffect()
    {
        if (hitEffectPrefab)
            Instantiate(hitEffectPrefab, transform.position, Quaternion.identity);
    }

    void Despawn()
    {
        if (Application.isPlaying)
        {
            Destroy(gameObject);
            return;
        }

        DestroyImmediate(gameObject);
    }
}
