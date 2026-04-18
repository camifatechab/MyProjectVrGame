using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class UIDebugDump
{
    [MenuItem("Tools/UI/Inspect CP6 Sequence Wiring")]
    public static void Inspect()
    {
        var sb = new StringBuilder();

        CP06JetpackSequence seq = Object.FindFirstObjectByType<CP06JetpackSequence>(FindObjectsInactive.Include);
        sb.AppendLine("[CP06JetpackSequence]");
        if (seq == null)
        {
            sb.AppendLine("  NOT FOUND in scene");
        }
        else
        {
            sb.AppendLine($"  host: {PathOf(seq.transform)}  enabled={seq.enabled}  active={seq.gameObject.activeInHierarchy}");
            sb.AppendLine($"    rover                = {Nameof(seq.rover)}");
            sb.AppendLine($"    roverRespawn         = {Nameof(seq.roverRespawn)}");
            sb.AppendLine($"    jetpackController    = {Nameof(seq.jetpackController)}");
            sb.AppendLine($"    playerRespawnManager = {Nameof(seq.playerRespawnManager)}");
            sb.AppendLine($"    jetpackPickup        = {Nameof(seq.jetpackPickup)}");
            sb.AppendLine($"    jetpackHighlight     = {Nameof(seq.jetpackHighlight)}");
            sb.AppendLine($"    lockDismountUntilActivated = {seq.lockDismountUntilActivated}");
        }

        // Trigger collider on the host
        if (seq != null)
        {
            Collider c = seq.GetComponent<Collider>();
            if (c == null) sb.AppendLine("  ⚠ no Collider on CP6 — OnTriggerEnter will never fire");
            else sb.AppendLine($"  collider: type={c.GetType().Name}  isTrigger={c.isTrigger}  bounds={c.bounds.size}  enabled={c.enabled}");

            // Rigidbody? Either CP6 or the rover needs a rigidbody for trigger events.
            Rigidbody rb = seq.GetComponent<Rigidbody>();
            sb.AppendLine($"  rigidbody on CP6: {(rb == null ? "none" : "yes (kinematic=" + rb.isKinematic + ")")}");
        }

        // RoverPhysicsController must have a rigidbody; verify
        RoverPhysicsController rover = Object.FindFirstObjectByType<RoverPhysicsController>(FindObjectsInactive.Include);
        sb.AppendLine("\n[RoverPhysicsController]");
        if (rover == null) sb.AppendLine("  NOT FOUND");
        else
        {
            sb.AppendLine($"  host: {PathOf(rover.transform)}");
            Rigidbody rb = rover.GetComponent<Rigidbody>();
            sb.AppendLine($"  rigidbody: {(rb == null ? "none ⚠" : "yes")}");
            sb.AppendLine($"  colliders on rover body: {rover.GetComponentsInChildren<Collider>(true).Length}");
            // Is SetDismountEnabled method present?
            MethodInfo mi = typeof(RoverPhysicsController).GetMethod("SetDismountEnabled", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            sb.AppendLine($"  SetDismountEnabled method: {(mi == null ? "MISSING ⚠" : "present")}");
            MethodInfo mi2 = typeof(RoverPhysicsController).GetMethod("SetExternalFlightLock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        }

        // AutoJetpackController
        AutoJetpackController jp = Object.FindFirstObjectByType<AutoJetpackController>(FindObjectsInactive.Include);
        sb.AppendLine("\n[AutoJetpackController]");
        if (jp == null) sb.AppendLine("  NOT FOUND");
        else
        {
            sb.AppendLine($"  host: {PathOf(jp.transform)}  enabled={jp.enabled}");
            MethodInfo mi = typeof(AutoJetpackController).GetMethod("SetExternalFlightLock", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            sb.AppendLine($"  SetExternalFlightLock method: {(mi == null ? "MISSING ⚠" : "present")}");
        }

        // Exit prompt controller presence
        CP06RoverExitPromptController exit = Object.FindFirstObjectByType<CP06RoverExitPromptController>(FindObjectsInactive.Include);
        sb.AppendLine("\n[CP06RoverExitPromptController]");
        if (exit == null) sb.AppendLine("  NOT FOUND ⚠");
        else sb.AppendLine($"  host: {PathOf(exit.transform)}  enabled={exit.enabled}");

        // Prompt UI on CP6
        CP06JetpackPromptUI promptUI = Object.FindFirstObjectByType<CP06JetpackPromptUI>(FindObjectsInactive.Include);
        sb.AppendLine("\n[CP06JetpackPromptUI]");
        if (promptUI == null) sb.AppendLine("  NOT FOUND ⚠");
        else sb.AppendLine($"  host: {PathOf(promptUI.transform)}  enabled={promptUI.enabled}");

        // Jetpack pickup
        JetpackPickupInteractable pickup = Object.FindFirstObjectByType<JetpackPickupInteractable>(FindObjectsInactive.Include);
        sb.AppendLine("\n[JetpackPickupInteractable]");
        if (pickup == null) sb.AppendLine("  NOT FOUND ⚠");
        else
        {
            sb.AppendLine($"  host: {PathOf(pickup.transform)}  enabled={pickup.enabled}");
            sb.AppendLine($"  pickupEnabledOnStart={pickup.pickupEnabledOnStart}");
        }

        JetpackPickupHighlighter hl = Object.FindFirstObjectByType<JetpackPickupHighlighter>(FindObjectsInactive.Include);
        sb.AppendLine("\n[JetpackPickupHighlighter]");
        if (hl == null) sb.AppendLine("  NOT FOUND ⚠");
        else sb.AppendLine($"  host: {PathOf(hl.transform)}  enabled={hl.enabled}");

        foreach (var line in sb.ToString().Split('\n'))
            if (!string.IsNullOrWhiteSpace(line)) Debug.Log("[WIREDUMP] " + line.TrimEnd('\r'));
    }

    private static string Nameof(Object o)
    {
        if (o == null) return "<null> ⚠";
        if (o is Component comp) return $"{comp.GetType().Name} on {PathOf(comp.transform)}";
        return o.name;
    }

    private static string PathOf(Transform t)
    {
        if (t == null) return "<null>";
        var stack = new Stack<string>();
        while (t != null) { stack.Push(t.name); t = t.parent; }
        return string.Join("/", stack);
    }
}
