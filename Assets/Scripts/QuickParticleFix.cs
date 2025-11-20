using UnityEngine;

public class QuickParticleFix : MonoBehaviour
{
    void Start()
    {
        // Automatically fix all particle systems when the game starts
        FixParticles();
    }

    [ContextMenu("Fix Particles Now")]
    void FixParticles()
    {
        Debug.Log("<color=cyan>=== Particle Velocity Fix Starting ===</color>");
        
        ParticleSystem[] allPS = FindObjectsOfType<ParticleSystem>();
        int fixedCount = 0;
        
        foreach (ParticleSystem ps in allPS)
        {
            var vel = ps.velocityOverLifetime;
            if (!vel.enabled) continue;
            
            var xMode = vel.x.mode;
            var yMode = vel.y.mode;
            var zMode = vel.z.mode;
            
            if (xMode != yMode || yMode != zMode)
            {
                Debug.Log("Fixing particle system: " + ps.gameObject.name);
                
                var x = vel.x;
                x.mode = ParticleSystemCurveMode.Constant;
                vel.x = x;
                
                var y = vel.y;
                y.mode = ParticleSystemCurveMode.Constant;
                vel.y = y;
                
                var z = vel.z;
                z.mode = ParticleSystemCurveMode.Constant;
                vel.z = z;
                
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log("<color=green>✓ FIXED " + fixedCount + " particle systems with velocity curve errors!</color>");
        }
        else
        {
            Debug.Log("<color=yellow>No particle systems needed fixing. All velocity curves are consistent.</color>");
        }
    }
}
