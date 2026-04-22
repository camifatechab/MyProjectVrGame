using System.Collections;
using UnityEngine;

public class RockfallSpawner : MonoBehaviour
{
    [Header("Rock Settings")]
    public GameObject rockPrefab;
    public int poolSize = 10;
    public float dropIntervalMin = 1.5f;
    public float dropIntervalMax = 3.5f;
    public float startDelay = 1f;
    public float rockLifetime = 5f;

    [Header("Spawn Area")]
    public Vector3 areaSize = new Vector3(5f, 0f, 5f);

    [Header("Damage")]
    [Tooltip("Damage dealt to the player on direct rock hit.")]
    public float damage = 15f;

    private GameObject[] pool;
    private RockInstance[] instances;
    private int poolIndex;

    private struct RockInstance
    {
        public Rigidbody rb;
        public RockDamage damager;
        public int activeToken;
    }

    private void Start()
    {
        CreatePool();
        StartCoroutine(SpawnLoop());
    }

    void CreatePool()
    {
        pool      = new GameObject[poolSize];
        instances = new RockInstance[poolSize];

        for (int i = 0; i < poolSize; i++)
        {
            GameObject rock = Instantiate(rockPrefab);
            EnsurePhysicsSetup(rock, i);
            rock.SetActive(false);
            pool[i] = rock;
        }
    }

    IEnumerator SpawnLoop()
    {
        yield return new WaitForSeconds(startDelay);
        while (true)
        {
            DropRock();
            yield return new WaitForSeconds(Random.Range(dropIntervalMin, dropIntervalMax));
        }
    }

    void DropRock()
    {
        int slot = poolIndex;
        poolIndex = (poolIndex + 1) % poolSize;

        instances[slot].activeToken++;
        int myToken = instances[slot].activeToken;

        Vector3 spawnPos = transform.position + new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            0f,
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f));

        GameObject rock = pool[slot];
        rock.transform.position = spawnPos;
        rock.transform.rotation = Random.rotation;

        Rigidbody rb = instances[slot].rb;
        rb.linearVelocity  = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        instances[slot].damager.damage = damage;
        rock.SetActive(true);

        StartCoroutine(DisableAfter(slot, myToken, rock));
    }

    IEnumerator DisableAfter(int slot, int token, GameObject rock)
    {
        yield return new WaitForSeconds(rockLifetime);
        if (instances[slot].activeToken == token && rock != null)
            rock.SetActive(false);
    }

    void EnsurePhysicsSetup(GameObject rock, int slot)
    {
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb == null) rb = rock.AddComponent<Rigidbody>();
        instances[slot].rb = rb;

        MeshCollider mc = rock.GetComponent<MeshCollider>();
        if (mc != null && mc.sharedMesh != null && !mc.convex)
            mc.convex = true;

        RockDamage damager = rock.GetComponent<RockDamage>();
        if (damager == null) damager = rock.AddComponent<RockDamage>();
        instances[slot].damager = damager;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, 1f, areaSize.z));
    }
}

public class RockDamage : MonoBehaviour
{
    public float damage = 15f;

    private void OnCollisionEnter(Collision col)
    {
        PlayerHealth health = col.gameObject.GetComponent<PlayerHealth>()
                           ?? col.gameObject.GetComponentInParent<PlayerHealth>();
        if (health != null)
            health.TakeDamage(damage);
    }
}
