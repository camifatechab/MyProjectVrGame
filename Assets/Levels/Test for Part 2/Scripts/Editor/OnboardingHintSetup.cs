/// <summary>
/// Editor utility — run once to configure all OnboardingHint GameObjects.
/// Menu: Tools > Setup Onboarding Hints
/// Delete this script after running.
/// </summary>
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

public class OnboardingHintSetup : MonoBehaviour
{
    [MenuItem("Tools/Setup Onboarding Hints")]
    static void SetupHints()
    {
        // ── Rover position ───────────────────────────────────────────────
        Vector3 roverPos = new Vector3(12.5f, -7.42f, 28.8f);

        // ── Player/Jetpack spawn position ────────────────────────────────
        Vector3 jetpackPos = new Vector3(24.1f, -7.948f, -27.7f);

        // ── Parent container ─────────────────────────────────────────────
        GameObject parent = GameObject.Find("OnboardingHints");
        if (parent == null)
        {
            parent = new GameObject("OnboardingHints");
            Undo.RegisterCreatedObjectUndo(parent, "Create OnboardingHints");
        }

        // ── Create hints ─────────────────────────────────────────────────
        OnboardingHint hintMount     = CreateHint(parent, "HintMount",    roverPos,   2.5f, OnboardingHint.DismissMode.RoverMounted,    "Both grips to mount",                              false);
        OnboardingHint hintControls  = CreateHint(parent, "HintControls", roverPos,   2.5f, OnboardingHint.DismissMode.RoverMoved,       "Right grip = forward  ·  Left grip = reverse\nRotate hand = steer", true);
        OnboardingHint hintDismount  = CreateHint(parent, "HintDismount", roverPos,   2.5f, OnboardingHint.DismissMode.RoverDismounted,  "Release both grips to dismount",                   true);
        OnboardingHint hintJetpack   = CreateHint(parent, "HintJetpack",  jetpackPos, 3.0f, OnboardingHint.DismissMode.JetpackFired,     "Both grips + arms down = jetpack",                 false);

        // ── Wire up chains via serialized field ───────────────────────────
        // HintMount → enables HintControls on dismiss
        SerializedObject soMount = new SerializedObject(hintMount);
        soMount.FindProperty("nextHint").objectReferenceValue = hintControls.gameObject;
        soMount.ApplyModifiedProperties();

        // HintControls → enables HintDismount on dismiss
        SerializedObject soControls = new SerializedObject(hintControls);
        soControls.FindProperty("nextHint").objectReferenceValue = hintDismount.gameObject;
        soControls.ApplyModifiedProperties();

        // Mark scene dirty
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene());

        Debug.Log("<color=green>✓ Onboarding hints set up! HintMount → HintControls → HintDismount chain ready. HintJetpack independent.</color>");
    }

    static OnboardingHint CreateHint(GameObject parent, string objName, Vector3 pos, float radius,
        OnboardingHint.DismissMode mode, string text, bool startInactive)
    {
        // Reuse existing if already present
        Transform existing = parent.transform.Find(objName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(objName);
        go.transform.SetParent(parent.transform);
        go.transform.position = pos;

        if (existing == null)
            Undo.RegisterCreatedObjectUndo(go, "Create " + objName);

        // SphereCollider
        SphereCollider col = go.GetComponent<SphereCollider>();
        if (col == null) col = Undo.AddComponent<SphereCollider>(go);
        col.isTrigger = true;
        col.radius = radius;

        // OnboardingHint
        OnboardingHint hint = go.GetComponent<OnboardingHint>();
        if (hint == null) hint = Undo.AddComponent<OnboardingHint>(go);

        // Set serialized fields
        SerializedObject so = new SerializedObject(hint);
        so.FindProperty("hintText").stringValue = text;
        so.FindProperty("dismissMode").enumValueIndex = (int)mode;
        so.FindProperty("startInactive").boolValue = startInactive;
        so.ApplyModifiedProperties();

        go.SetActive(!startInactive);

        return hint;
    }
}
#endif
