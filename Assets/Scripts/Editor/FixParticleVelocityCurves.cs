using UnityEngine;
using UnityEditor;

public class FixParticleVelocityCurves : EditorWindow
{
    [MenuItem("Tools/Fix Particle Velocity Curves")]
    static void FixAllParticleSystems()
    {
        // Find all particle systems in the scene
        ParticleSystem[] allParticleSystems = FindObjectsOfType<ParticleSystem>();
        
        int fixedCount = 0;
        int totalCount = allParticleSystems.Length;
        
        Debug.Log($"Found {totalCount} particle systems. Checking for velocity curve issues...");
        
        foreach (ParticleSystem ps in allParticleSystems)
        {
            if (FixParticleSystemVelocity(ps))
            {
                fixedCount++;
                Debug.Log($"✓ Fixed velocity curves on: {ps.gameObject.name}");
            }
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>✓ Fixed {fixedCount} out of {totalCount} particle systems!</color>");
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Fixed velocity curves on {fixedCount} particle system(s).", "OK");
        }
        else
        {
            Debug.Log($"<color=cyan>All {totalCount} particle systems already have consistent velocity curves.</color>");
            EditorUtility.DisplayDialog("No Issues Found", 
                "All particle systems already have consistent velocity curves.", "OK");
        }
    }
    
    static bool FixParticleSystemVelocity(ParticleSystem ps)
    {
        var velocityModule = ps.velocityOverLifetime;
        
        // If the module isn't enabled, skip it
        if (!velocityModule.enabled)
        {
            return false;
        }
        
        bool needsFix = false;
        
        // Check if modes are mixed
        ParticleSystemCurveMode xMode = velocityModule.x.mode;
        ParticleSystemCurveMode yMode = velocityModule.y.mode;
        ParticleSystemCurveMode zMode = velocityModule.z.mode;
        
        // If modes are different, we need to fix
        if (xMode != yMode || yMode != zMode || xMode != zMode)
        {
            needsFix = true;
        }
        
        if (needsFix)
        {
            // Get current values before changing
            float xValue = GetConstantValue(velocityModule.x);
            float yValue = GetConstantValue(velocityModule.y);
            float zValue = GetConstantValue(velocityModule.z);
            
            Debug.Log($"  - {ps.gameObject.name}: Converting mixed modes (X:{xMode}, Y:{yMode}, Z:{zMode}) to Constant mode");
            Debug.Log($"  - Using values: X={xValue}, Y={yValue}, Z={zValue}");
            
            // Set all to Constant mode with their current values
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
            
            // Mark the scene as dirty so Unity saves the changes
            EditorUtility.SetDirty(ps);
            
            return true;
        }
        
        return false;
    }
    
    static float GetConstantValue(ParticleSystem.MinMaxCurve curve)
    {
        // Try to extract a reasonable constant value from different curve modes
        switch (curve.mode)
        {
            case ParticleSystemCurveMode.Constant:
                return curve.constant;
            
            case ParticleSystemCurveMode.Curve:
                // Use the curve's value at time 0
                if (curve.curve != null && curve.curve.keys.Length > 0)
                {
                    return curve.curve.keys[0].value * curve.curveMultiplier;
                }
                return 0f;
            
            case ParticleSystemCurveMode.TwoCurves:
                // Use average of min curve at time 0
                if (curve.curveMin != null && curve.curveMin.keys.Length > 0)
                {
                    return curve.curveMin.keys[0].value * curve.curveMultiplier;
                }
                return 0f;
            
            case ParticleSystemCurveMode.TwoConstants:
                // Use the average of the two constants
                return (curve.constantMin + curve.constantMax) / 2f;
            
            default:
                return 0f;
        }
    }
    
    [MenuItem("Tools/Fix Particle Velocity Curves (Selected Only)")]
    static void FixSelectedParticleSystems()
    {
        GameObject[] selectedObjects = Selection.gameObjects;
        
        if (selectedObjects.Length == 0)
        {
            EditorUtility.DisplayDialog("No Selection", 
                "Please select one or more GameObjects with Particle Systems.", "OK");
            return;
        }
        
        int fixedCount = 0;
        int totalCount = 0;
        
        foreach (GameObject obj in selectedObjects)
        {
            // Check this object and all children
            ParticleSystem[] particleSystems = obj.GetComponentsInChildren<ParticleSystem>(true);
            
            foreach (ParticleSystem ps in particleSystems)
            {
                totalCount++;
                if (FixParticleSystemVelocity(ps))
                {
                    fixedCount++;
                    Debug.Log($"✓ Fixed velocity curves on: {ps.gameObject.name}");
                }
            }
        }
        
        if (totalCount == 0)
        {
            EditorUtility.DisplayDialog("No Particle Systems", 
                "No Particle Systems found in selected objects.", "OK");
            return;
        }
        
        if (fixedCount > 0)
        {
            Debug.Log($"<color=green>✓ Fixed {fixedCount} out of {totalCount} particle systems in selection!</color>");
            EditorUtility.DisplayDialog("Fix Complete", 
                $"Fixed velocity curves on {fixedCount} particle system(s) in selection.", "OK");
        }
        else
        {
            Debug.Log($"<color=cyan>All {totalCount} particle systems in selection already have consistent velocity curves.</color>");
            EditorUtility.DisplayDialog("No Issues Found", 
                "All particle systems in selection already have consistent velocity curves.", "OK");
        }
    }
}
