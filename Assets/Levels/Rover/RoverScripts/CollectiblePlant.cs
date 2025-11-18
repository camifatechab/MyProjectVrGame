/*using UnityEngine;

public class CollectiblePlant : MonoBehaviour
{
    public GameObject collectEffect;
    public AudioClip collectSound;
    private bool collected = false; // Prevents double collection

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return; // already collected, skip

        if (other.CompareTag("Rover"))
        {
            collected = true;

            PlantManager.Instance.CollectPlant();

            if (collectEffect)
                Instantiate(collectEffect, transform.position, Quaternion.identity);
            if (collectSound)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            /*PlantManager.Instance?.CollectPlant();
            Destroy(gameObject);//

            // Hide or disable plant
            gameObject.SetActive(false);
        }
    }
}*/

using UnityEngine;

public class CollectiblePlant : MonoBehaviour
{
    public GameObject collectEffect;    // prefab (must be a prefab in Project)
    public AudioClip collectSound;
    private bool collected = false;

    private void OnTriggerEnter(Collider other)
    {
        if (collected) return;

        if (other.CompareTag("Rover"))
        {
            collected = true;

            PlantManager.Instance.CollectPlant();

            if (collectEffect)
            {
                // Instantiate as GameObject so we can control it
                GameObject fx = Instantiate(collectEffect, transform.position + Vector3.up * 0.2f, Quaternion.identity);

                // Make sure the instance is active
                fx.SetActive(true);

                // Try to find ParticleSystem(s) and force them to use World simulation and play
                ParticleSystem[] systems = fx.GetComponentsInChildren<ParticleSystem>(true);
                foreach (var ps in systems)
                {
                    var main = ps.main;
                    // set simulation space to World so it doesn't get affected by parent transforms or later changes
                    main.simulationSpace = ParticleSystemSimulationSpace.World;
                    // ensure Play On Awake doesn't prevent runtime start
                    if (!ps.isPlaying)
                        ps.Play(true);
                }

                Debug.Log("Spawned FX: " + fx.name + " with " + systems.Length + " particle systems.");
            }
            else
            {
                Debug.LogWarning("CollectEffect not assigned on " + name);
            }

            if (collectSound)
                AudioSource.PlayClipAtPoint(collectSound, transform.position);

            // disable plant after spawning FX
            gameObject.SetActive(false);
        }
    }
}

