using UnityEngine;

/// <summary>
/// Simple script to fix particle velocity curve errors at runtime.
/// Attach to any GameObject and it will automatically fix all particle systems in the scene on Start.
/// </summary>
public class ParticleVelocityFixer : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Should this run automatically on Start?")]
    public bool fixOnStart = true;
    
    [Tooltip("Show detailed logs?")]
    public bool verboseLogs = true;
    
    void Start()
    {
        if (fixOnStart)
        {
            FixAllParticleSystemsInScene();
        }
    }
    
    [ContextMenu("Fix All Particle Systems Now")]
    public void FixAllParticleSystemsInScene()
    {
        ParticleSystem[] allParticleSystems = FindObjectsOfType<ParticleSystem>();
        
        int fixedCount = 0;
        int totalCount = allParticleSystems.Length;
        
        Debug.Log($"<color=cyan>Particle Velocity Fixer: Found {totalCount} particle systems</color>");
        
        foreach (ParticleSystem ps in allParticleSystems)
        {
            if (FixParticleSystem(ps))
            {
                fixedCount++;
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>✓ Fixed velocity curves on {fixedCount} particle system(s)!</color>");
        }
        else
        {
            Debug.Log($"<color=green>✓ All {totalCount} particle systems already have consistent velocity curves.</color>");
        }
    }
    
    bool FixParticleSystem(ParticleSystem ps)
    {
        var velocityModule = ps.velocityOverLifetime;
        
        // If the module isn't enabled, skip it
        if (!velocityModule.enabled)
        {
            return false;
        }
        
        // Check if modes are mixed
        ParticleSystemCurveMode xMode = velocityModule.x.mode;
        ParticleSystemCurveMode yMode = velocityModule.y.mode;
        ParticleSystemCurveMode zMode = velocityModule.z.mode;
        
        // If modes are the same, no fix needed
        if (xMode == yMode && yMode == zMode)
        {
            return false;
        }
        
        // Fix needed - get current values
        float xValue = GetConstantValue(velocityModule.x);
        float yValue = GetConstantValue(velocityModule.y);
        float zValue = GetConstantValue(velocityModule.z);
        
        if (verboseLogs)
        {
            Debug.Log($"<color=yellow>Fixing {ps.gameObject.name}:</color> X={xMode}, Y={yMode}, Z={zMode} → All Constant (X={xValue:F2}, Y={yValue:F2}, Z={zValue:F2})");
        }
        
        // Set all to Constant mode
        var newX = velocityModule.x;
        newX.mode = ParticleSystemCurveMode.Constant;
        newX.constant = xValue;
        velocityModule.x = newX;
        
        var newY = velocityModule.y;
        newY.mode = ParticleSystemCurveMode.Constant;
        newY.constant = yValue;
        velocityModule.y = newY;
        
        var newZ = velocityModule.z;
        newZ.mode = ParticleSystemCurveMode.Constant;
        newZ.constant = zValue;
        velocityModule.z = newZ;
        
        return true;
    }
    
    float GetConstantValue(ParticleSystem.MinMaxCurve curve)
    {
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            
            case ParticleSystemCurveMode.Curve:
                if (curve.curve != null && curve.curve.keys.Length > 0)
                {
                    return curve.curve.keys[0].value * curve.curveMultiplier;
                }
                return 0f;
            
            case ParticleSystemCurveMode.TwoCurves:
                if (curve.curveMin != null && curve.curveMin.keys.Length > 0)
                {
                    return curve.curveMin.keys[0].value * curve.curveMultiplier;
                }
                return 0f;
            
            case ParticleSystemCurveMode.TwoConstants:
                return (curve.constantMin + curve.constantMax) / 2f;
            
            default:
                return 0f;
        }
    }
}
