using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FloatingIslandIntroSequenceTests
{
    private static readonly Type SequenceType = Type.GetType("FloatingIslandIntroSequence, Assembly-CSharp");
    private static readonly Type RideableCreatureType = Type.GetType("RideableCreature, Assembly-CSharp");
    private static readonly Type CrystalCollectibleType = Type.GetType("CrystalCollectible, Assembly-CSharp");
    private static readonly Type PlayerRespawnManagerType = Type.GetType("PlayerRespawnManager, Assembly-CSharp");
    private static readonly Type PlayerHealthType = Type.GetType("PlayerHealth, Assembly-CSharp");
    private static readonly Type RoverPhysicsControllerType = Type.GetType("RoverPhysicsController, Assembly-CSharp");
    private static readonly Type AutoJetpackControllerType = Type.GetType("AutoJetpackController, Assembly-CSharp");
    private static readonly Type XROriginType = Type.GetType("Unity.XR.CoreUtils.XROrigin, Unity.XR.CoreUtils");

    private static readonly Type SequenceStageType =
        SequenceType?.GetNestedType("SequenceStage", BindingFlags.NonPublic);

    private static readonly MethodInfo ResolveRequiredCrystalsMethod =
        SequenceType?.GetMethod("ResolveRequiredCrystals", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo ConfigureSequenceMethod =
        SequenceType?.GetMethod("ConfigureSequence", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo BuildFlightPathsMethod =
        SequenceType?.GetMethod("BuildFlightPaths", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleDragonMountedMethod =
        SequenceType?.GetMethod("HandleDragonMounted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleFlightPathCompletedMethod =
        SequenceType?.GetMethod("HandleFlightPathCompleted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleDragonDismountedMethod =
        SequenceType?.GetMethod("HandleDragonDismounted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo StageField =
        SequenceType?.GetField("stage", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RequiredCrystalsField =
        SequenceType?.GetField("requiredCrystals", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo FirstFlightWaypointsField =
        SequenceType?.GetField("firstFlightWaypoints", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo SecondFlightWaypointsField =
        SequenceType?.GetField("secondFlightWaypoints", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RespawnWaypointField =
        PlayerRespawnManagerType?.GetField("wp01Transform", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RespawnEnabledField =
        PlayerRespawnManagerType?.GetField("respawnEnabled", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo CrystalCollectionEnabledProperty =
        CrystalCollectibleType?.GetProperty("CollectionEnabled", BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo CreatureCanMountField =
        RideableCreatureType?.GetField("canPlayerMount", BindingFlags.Instance | BindingFlags.Public);

    private static readonly PropertyInfo CreatureScriptedSequenceActiveProperty =
        RideableCreatureType?.GetProperty("IsScriptedSequenceActive", BindingFlags.Instance | BindingFlags.Public);

    private static readonly PropertyInfo CreatureIsParkingProperty =
        RideableCreatureType?.GetProperty("IsParking", BindingFlags.Instance | BindingFlags.Public);

    private static readonly PropertyInfo CreatureTotalWaypointsProperty =
        RideableCreatureType?.GetProperty("TotalWaypoints", BindingFlags.Instance | BindingFlags.Public);

    private static readonly PropertyInfo CreatureCurrentWaypointProperty =
        RideableCreatureType?.GetProperty("CurrentWaypoint", BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo CreatureTargetPlatformField =
        RideableCreatureType?.GetField("targetPlatform", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo CreatureForcedDismountTargetField =
        RideableCreatureType?.GetField("forcedDismountTarget", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo CreatureSetScriptedSequenceActiveMethod =
        RideableCreatureType?.GetMethod("SetScriptedSequenceActive", BindingFlags.Instance | BindingFlags.Public);

    private static readonly MethodInfo CreatureSetFlightPathMethod =
        RideableCreatureType?.GetMethod("SetFlightPath", BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo CreatureJetpackControllerField =
        RideableCreatureType?.GetField("jetpackController", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo CreatureInitialJetpackEnabledStateField =
        RideableCreatureType?.GetField("initialJetpackEnabledState", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo CreatureHasInitialJetpackEnabledStateField =
        RideableCreatureType?.GetField("hasInitialJetpackEnabledState", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo RoverCanMountProperty =
        RoverPhysicsControllerType?.GetProperty("CanMount", BindingFlags.Instance | BindingFlags.Public);

    [Test]
    public void ConfigureSequence_DisablesLegacyObjectives_And_DisablesRespawnManager()
    {
        AssertSharedTypes();
        Assert.That(ResolveRequiredCrystalsMethod, Is.Not.Null);
        Assert.That(ConfigureSequenceMethod, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject playerObject = new GameObject("Player");
        GameObject spawnAnchorObject = new GameObject("SpawnAnchor");
        GameObject dragonStartObject = new GameObject("DragonStart");
        GameObject crystalAObject = new GameObject("CrystalA");
        GameObject crystalBObject = new GameObject("CrystalB");
        GameObject wp01Object = new GameObject("WP01");
        GameObject wp02Object = new GameObject("WP02");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component xrOrigin = playerObject.AddComponent(XROriginType);
            Component respawnManager = playerObject.AddComponent(PlayerRespawnManagerType);
            Component crystalA = crystalAObject.AddComponent(CrystalCollectibleType);
            Component crystalB = crystalBObject.AddComponent(CrystalCollectibleType);

            spawnAnchorObject.transform.position = new Vector3(5f, 2f, 3f);
            dragonStartObject.transform.position = new Vector3(0f, 8f, 0f);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "xrOrigin", xrOrigin);
            SetField(sequence, "respawnManager", respawnManager);
            SetField(sequence, "playerSpawnAnchor", spawnAnchorObject.transform);
            SetField(sequence, "dragonStartAnchor", dragonStartObject.transform);
            SetField(sequence, "movePlayerToStartOnPlay", false);
            SetField(sequence, "snapDragonToStartOnPlay", false);
            AddWaypoints(FirstFlightWaypointsField, sequence, wp01Object.transform, wp02Object.transform);
            SetRequiredCrystals(sequence, crystalA, crystalB);

            ResolveRequiredCrystalsMethod.Invoke(sequence, null);
            ConfigureSequenceMethod.Invoke(sequence, null);

            Assert.That(crystalAObject.activeSelf, Is.False, "Legacy crystal objectives should stay disabled for the simplified ride.");
            Assert.That(crystalBObject.activeSelf, Is.False, "Every legacy pickup should stay disabled for the simplified ride.");
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalA), Is.False);
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalB), Is.False);
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.True);
            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.True);
            Assert.That(((Behaviour)respawnManager).enabled, Is.False);
            Assert.That((bool)RespawnEnabledField.GetValue(respawnManager), Is.False);
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(spawnAnchorObject);
            UnityEngine.Object.DestroyImmediate(dragonStartObject);
            UnityEngine.Object.DestroyImmediate(crystalAObject);
            UnityEngine.Object.DestroyImmediate(crystalBObject);
            UnityEngine.Object.DestroyImmediate(wp01Object);
            UnityEngine.Object.DestroyImmediate(wp02Object);
            CleanupDynamicRespawnPoint();
        }
    }

    [Test]
    public void BuildFlightPaths_UsesNumericWaypointOrder_ForFullRide()
    {
        AssertSharedTypes();
        Assert.That(BuildFlightPathsMethod, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject flightPathRootObject = new GameObject("FlightPath");
        GameObject wp10 = new GameObject("WP10_Liftoff");
        GameObject wp02 = new GameObject("WP02_BigDive");
        GameObject wp03 = new GameObject("WP03_ParadiseSweep");
        GameObject wp01 = new GameObject("WP01_Return");
        GameObject wp04 = new GameObject("WP04_Paradise");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);

            wp10.transform.SetParent(flightPathRootObject.transform);
            wp02.transform.SetParent(flightPathRootObject.transform);
            wp03.transform.SetParent(flightPathRootObject.transform);
            wp01.transform.SetParent(flightPathRootObject.transform);
            wp04.transform.SetParent(flightPathRootObject.transform);

            SetField(sequence, "flightPathRoot", flightPathRootObject.transform);

            BuildFlightPathsMethod.Invoke(sequence, null);

            IList ridePath = (IList)FirstFlightWaypointsField.GetValue(sequence);
            IList unusedSecondLeg = (IList)SecondFlightWaypointsField.GetValue(sequence);

            Assert.That(ridePath.Count, Is.EqualTo(5));
            Assert.That(((Transform)ridePath[0]).name, Is.EqualTo("WP01_Return"));
            Assert.That(((Transform)ridePath[1]).name, Is.EqualTo("WP02_BigDive"));
            Assert.That(((Transform)ridePath[2]).name, Is.EqualTo("WP03_ParadiseSweep"));
            Assert.That(((Transform)ridePath[3]).name, Is.EqualTo("WP04_Paradise"));
            Assert.That(((Transform)ridePath[4]).name, Is.EqualTo("WP10_Liftoff"));
            Assert.That(unusedSecondLeg.Count, Is.EqualTo(0), "The simplified ride should not split the path for mid-ride stops.");
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(flightPathRootObject);
            UnityEngine.Object.DestroyImmediate(wp10);
            UnityEngine.Object.DestroyImmediate(wp02);
            UnityEngine.Object.DestroyImmediate(wp03);
            UnityEngine.Object.DestroyImmediate(wp01);
            UnityEngine.Object.DestroyImmediate(wp04);
        }
    }

    [Test]
    public void BuildFlightPaths_CanTemporarilyStartFromWp09_ForTesting()
    {
        AssertSharedTypes();
        Assert.That(BuildFlightPathsMethod, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject flightPathRootObject = new GameObject("FlightPath");
        GameObject wp10 = new GameObject("WP10");
        GameObject wp09 = new GameObject("WP09");
        GameObject wp08 = new GameObject("WP08");
        GameObject wp07 = new GameObject("WP07");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);

            wp10.transform.SetParent(flightPathRootObject.transform);
            wp09.transform.SetParent(flightPathRootObject.transform);
            wp08.transform.SetParent(flightPathRootObject.transform);
            wp07.transform.SetParent(flightPathRootObject.transform);

            SetField(sequence, "flightPathRoot", flightPathRootObject.transform);
            SetField(sequence, "testingStartWaypointName", "WP09");

            BuildFlightPathsMethod.Invoke(sequence, null);

            IList ridePath = (IList)FirstFlightWaypointsField.GetValue(sequence);

            Assert.That(ridePath.Count, Is.EqualTo(2));
            Assert.That(((Transform)ridePath[0]).name, Is.EqualTo("WP09"));
            Assert.That(((Transform)ridePath[1]).name, Is.EqualTo("WP10"));
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(flightPathRootObject);
            UnityEngine.Object.DestroyImmediate(wp10);
            UnityEngine.Object.DestroyImmediate(wp09);
            UnityEngine.Object.DestroyImmediate(wp08);
            UnityEngine.Object.DestroyImmediate(wp07);
        }
    }

    [Test]
    public void RideableCreature_ScriptedSequenceLock_DisablesJetpack_And_RestoresIt_WhenReleased()
    {
        AssertSharedTypes();
        Assert.That(AutoJetpackControllerType, Is.Not.Null);
        Assert.That(CreatureSetScriptedSequenceActiveMethod, Is.Not.Null);
        Assert.That(CreatureJetpackControllerField, Is.Not.Null);
        Assert.That(CreatureInitialJetpackEnabledStateField, Is.Not.Null);
        Assert.That(CreatureHasInitialJetpackEnabledStateField, Is.Not.Null);

        GameObject dragonObject = new GameObject("Dragon");
        GameObject playerObject = new GameObject("Player");

        try
        {
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component jetpack = playerObject.AddComponent(AutoJetpackControllerType);

            CreatureJetpackControllerField.SetValue(dragon, jetpack);
            CreatureInitialJetpackEnabledStateField.SetValue(dragon, true);
            CreatureHasInitialJetpackEnabledStateField.SetValue(dragon, true);

            CreatureSetScriptedSequenceActiveMethod.Invoke(dragon, new object[] { true, true });

            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.True);
            Assert.That(((Behaviour)jetpack).enabled, Is.False);

            CreatureSetScriptedSequenceActiveMethod.Invoke(dragon, new object[] { false, false });

            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.False);
            Assert.That(((Behaviour)jetpack).enabled, Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void HandleDragonMounted_ReappliesFullRideFlightPath()
    {
        AssertSharedTypes();
        Assert.That(HandleDragonMountedMethod, Is.Not.Null);
        Assert.That(CreatureSetFlightPathMethod, Is.Not.Null);
        Assert.That(CreatureCurrentWaypointProperty, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject wp01 = new GameObject("WP01");
        GameObject wp02 = new GameObject("WP02");
        GameObject wp03 = new GameObject("WP03");
        GameObject wp10 = new GameObject("WP10");
        GameObject wrongWp99 = new GameObject("WP99");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);

            SetField(sequence, "dragon", dragon);
            AddWaypoints(FirstFlightWaypointsField, sequence, wp01.transform, wp02.transform, wp03.transform, wp10.transform);
            SetStage(sequence, "WaitingForInitialMount");

            CreatureSetFlightPathMethod.Invoke(dragon, new object[] { new List<Transform> { wp01.transform, wrongWp99.transform }, true });

            HandleDragonMountedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("FlyingToEnd"));
            Assert.That((int)CreatureTotalWaypointsProperty.GetValue(dragon), Is.EqualTo(4));
            Assert.That((Transform)CreatureCurrentWaypointProperty.GetValue(dragon), Is.EqualTo(wp01.transform));
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(wp01);
            UnityEngine.Object.DestroyImmediate(wp02);
            UnityEngine.Object.DestroyImmediate(wp03);
            UnityEngine.Object.DestroyImmediate(wp10);
            UnityEngine.Object.DestroyImmediate(wrongWp99);
        }
    }

    [Test]
    public void HandleFlightPathCompleted_EnablesManualFinalDismount_And_RoverMount()
    {
        AssertSharedTypes();
        Assert.That(HandleFlightPathCompletedMethod, Is.Not.Null);
        Assert.That(RoverCanMountProperty, Is.Not.Null);
        Assert.That(CreatureForcedDismountTargetField, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject roverObject = new GameObject("Rover");
        GameObject roverDismountObject = new GameObject("RoverDismount");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component rover = roverObject.AddComponent(RoverPhysicsControllerType);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "rover", rover);
            SetField(sequence, "roverDismountAnchor", roverDismountObject.transform);
            SetStage(sequence, "FlyingToEnd");
            CreatureSetScriptedSequenceActiveMethod.Invoke(dragon, new object[] { true, true });

            HandleFlightPathCompletedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("WaitingForFinalDismount"));
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.False);
            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.False);
            Assert.That((bool)RoverCanMountProperty.GetValue(rover), Is.True);
            Assert.That(CreatureForcedDismountTargetField.GetValue(dragon), Is.EqualTo(roverDismountObject.transform));
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(roverObject);
            UnityEngine.Object.DestroyImmediate(roverDismountObject);
        }
    }

    [Test]
    public void FinalDismount_CompletesHandoff_And_KeepsRespawnDisabled()
    {
        AssertSharedTypes();
        Assert.That(HandleDragonDismountedMethod, Is.Not.Null);
        Assert.That(RoverCanMountProperty, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject roverObject = new GameObject("Rover");
        GameObject playerObject = new GameObject("Player");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component rover = roverObject.AddComponent(RoverPhysicsControllerType);
            Component respawnManager = playerObject.AddComponent(PlayerRespawnManagerType);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "rover", rover);
            SetField(sequence, "respawnManager", respawnManager);
            SetStage(sequence, "WaitingForFinalDismount");

            HandleDragonDismountedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("Completed"));
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.False);
            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.False);
            Assert.That((bool)RoverCanMountProperty.GetValue(rover), Is.True);
            Assert.That(((Behaviour)respawnManager).enabled, Is.False);
            Assert.That((bool)RespawnEnabledField.GetValue(respawnManager), Is.False);
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(roverObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }

    private static void AssertSharedTypes()
    {
        Assert.That(SequenceType, Is.Not.Null);
        Assert.That(RideableCreatureType, Is.Not.Null);
        Assert.That(CrystalCollectibleType, Is.Not.Null);
        Assert.That(PlayerRespawnManagerType, Is.Not.Null);
        Assert.That(PlayerHealthType, Is.Not.Null);
        Assert.That(SequenceStageType, Is.Not.Null);
        Assert.That(XROriginType, Is.Not.Null);
        Assert.That(StageField, Is.Not.Null);
        Assert.That(RequiredCrystalsField, Is.Not.Null);
        Assert.That(RespawnWaypointField, Is.Not.Null);
        Assert.That(RespawnEnabledField, Is.Not.Null);
        Assert.That(CrystalCollectionEnabledProperty, Is.Not.Null);
        Assert.That(CreatureCanMountField, Is.Not.Null);
        Assert.That(CreatureScriptedSequenceActiveProperty, Is.Not.Null);
        Assert.That(CreatureTotalWaypointsProperty, Is.Not.Null);
        Assert.That(CreatureIsParkingProperty, Is.Not.Null);
        Assert.That(CreatureTargetPlatformField, Is.Not.Null);
        Assert.That(CreatureForcedDismountTargetField, Is.Not.Null);
    }

    private static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = GetPrivateField(fieldName);
        Assert.That(field, Is.Not.Null, $"{target.GetType().Name}.{fieldName} not found");
        field.SetValue(target, value);
    }

    private static FieldInfo GetPrivateField(string fieldName)
    {
        return SequenceType?.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
    }

    private static void SetRequiredCrystals(Component sequence, params Component[] crystals)
    {
        IList crystalList = (IList)RequiredCrystalsField.GetValue(sequence);
        crystalList.Clear();
        for (int i = 0; i < crystals.Length; i++)
            crystalList.Add(crystals[i]);
    }

    private static void AddWaypoints(FieldInfo field, Component sequence, params Transform[] waypoints)
    {
        IList waypointList = (IList)field.GetValue(sequence);
        waypointList.Clear();
        for (int i = 0; i < waypoints.Length; i++)
            waypointList.Add(waypoints[i]);
    }

    private static void SetStage(Component sequence, string stageName)
    {
        object stageValue = Enum.Parse(SequenceStageType, stageName);
        StageField.SetValue(sequence, stageValue);
    }

    private static string GetStageName(Component sequence)
    {
        object value = StageField.GetValue(sequence);
        return value?.ToString();
    }

    private static void CleanupRuntimeAnchors()
    {
        string[] runtimeAnchorNames =
        {
            "PlayerSpawnAnchor_Runtime",
            "RoverLandingAnchor_Runtime",
            "RoverDismountAnchor_Runtime"
        };

        for (int i = 0; i < runtimeAnchorNames.Length; i++)
        {
            GameObject runtimeAnchor = GameObject.Find(runtimeAnchorNames[i]);
            if (runtimeAnchor != null)
                UnityEngine.Object.DestroyImmediate(runtimeAnchor);
        }
    }

    private static void CleanupDynamicRespawnPoint()
    {
        GameObject respawnPoint = GameObject.Find("DynamicRespawnPoint");
        if (respawnPoint != null)
            UnityEngine.Object.DestroyImmediate(respawnPoint);
    }

    private static void AssertVectorApproximately(Vector3 actual, Vector3 expected, string message = null)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.001f), message);
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.001f), message);
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(0.001f), message);
    }
}
