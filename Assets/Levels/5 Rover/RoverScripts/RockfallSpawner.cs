using UnityEngine;

public class RockfallSpawner : MonoBehaviour
{
    [Header("Rock Settings")]
    public GameObject rockPrefab;
    public int poolSize = 10;
    public float dropInterval = 2f;
    public float startDelay = 1f;

    [Header("Spawn Area")]
    public Vector3 areaSize = new Vector3(5f, 0f, 5f);

    private GameObject[] pool;
    private int poolIndex = 0;

    private void Start()
    {
        CreatePool();
        InvokeRepeating(nameof(DropRock), startDelay, dropInterval);
    }

    void CreatePool()
    {
        pool = new GameObject[poolSize];
        for (int i = 0; i < poolSize; i++)
        {
            GameObject rock = Instantiate(rockPrefab);
            EnsurePhysicsSetup(rock);
            rock.SetActive(false);
            pool[i] = rock;
        }
    }

    void DropRock()
    {
        GameObject rock = pool[poolIndex];
        poolIndex = (poolIndex + 1) % poolSize;

        Vector3 randomOffset = new Vector3(
            Random.Range(-areaSize.x / 2f, areaSize.x / 2f),
            Random.Range(-areaSize.y / 2f, areaSize.y / 2f),
            Random.Range(-areaSize.z / 2f, areaSize.z / 2f)
        );

        rock.transform.position = transform.position + randomOffset;
        rock.transform.rotation = Random.rotation;

        Rigidbody rb = EnsurePhysicsSetup(rock);
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;

        rock.SetActive(true);

        // Disable after 5 seconds
        StartCoroutine(DisableAfterSeconds(rock, 5f));
    }

    System.Collections.IEnumerator DisableAfterSeconds(GameObject obj, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (obj != null)
            obj.SetActive(false);
    }

    Rigidbody EnsurePhysicsSetup(GameObject rock)
    {
        Rigidbody rb = rock.GetComponent<Rigidbody>();
        if (rb == null)
            rb = rock.AddComponent<Rigidbody>();

        MeshCollider meshCollider = rock.GetComponent<MeshCollider>();
        if (meshCollider != null && meshCollider.sharedMesh != null && !meshCollider.convex)
            meshCollider.convex = true;

        return rb;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, areaSize);
    }
}
