using UnityEngine;

public class RockfallSpawner : MonoBehaviour
{
    public Rigidbody[] rocks;
    public float interval = 1.5f;
    public float startDelay = 1f;

    private void Start()
    {
        InvokeRepeating(nameof(DropRandomRock), startDelay, interval);
    }

    void DropRandomRock()
    {
        int i = Random.Range(0, rocks.Length);
        rocks[i].gameObject.SetActive(true);
        rocks[i].isKinematic = false;
    }
}
