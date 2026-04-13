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

    private static readonly MethodInfo HandleLandingCompletedMethod =
        SequenceType?.GetMethod("HandleLandingCompleted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleDragonMountedMethod =
        SequenceType?.GetMethod("HandleDragonMounted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleCrystalCollectedMethod =
        SequenceType?.GetMethod("HandleCrystalCollected", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo HandleDragonDismountedMethod =
        SequenceType?.GetMethod("HandleDragonDismounted", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo BuildCrystalAnchorsMethod =
        SequenceType?.GetMethod("BuildCrystalAnchors", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo TryTriggerCrystalLandingFromWaypointMethod =
        SequenceType?.GetMethod("TryTriggerCrystalLandingFromWaypoint", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo StageField =
        SequenceType?.GetField("stage", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RequiredCrystalsField =
        SequenceType?.GetField("requiredCrystals", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo FirstFlightWaypointsField =
        SequenceType?.GetField("firstFlightWaypoints", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo SecondFlightWaypointsField =
        SequenceType?.GetField("secondFlightWaypoints", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo CrystalRespawnAnchorField =
        SequenceType?.GetField("crystalRespawnAnchor", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RespawnWaypointField =
        PlayerRespawnManagerType?.GetField("wp01Transform", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo CrystalCollectedField =
        CrystalCollectibleType?.GetField("isCollected", BindingFlags.Instance | BindingFlags.NonPublic);

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
    public void ConfigureSequence_HidesCrystalsUntilLanding_And_SetsSpawnCheckpoint()
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

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component xrOrigin = playerObject.AddComponent(XROriginType);
            Component respawnManager = playerObject.AddComponent(PlayerRespawnManagerType);
            Component playerHealth = playerObject.AddComponent(PlayerHealthType);
            Component crystalA = crystalAObject.AddComponent(CrystalCollectibleType);
            Component crystalB = crystalBObject.AddComponent(CrystalCollectibleType);

            spawnAnchorObject.transform.position = new Vector3(5f, 2f, 3f);
            dragonStartObject.transform.position = new Vector3(0f, 8f, 0f);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "xrOrigin", xrOrigin);
            SetField(sequence, "respawnManager", respawnManager);
            SetField(sequence, "playerHealth", playerHealth);
            SetField(sequence, "playerSpawnAnchor", spawnAnchorObject.transform);
            SetField(sequence, "dragonStartAnchor", dragonStartObject.transform);
            SetField(sequence, "movePlayerToStartOnPlay", false);
            SetField(sequence, "snapDragonToStartOnPlay", false);
            SetRequiredCrystals(sequence, crystalA, crystalB);

            ResolveRequiredCrystalsMethod.Invoke(sequence, null);
            ConfigureSequenceMethod.Invoke(sequence, null);

            Assert.That(crystalAObject.activeSelf, Is.False, "Crystals should stay hidden until the WP03 landing completes.");
            Assert.That(crystalBObject.activeSelf, Is.False, "Every required crystal should stay hidden until the crystal step begins.");
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalA), Is.False, "Objective collection should stay disabled before the WP03 stop.");
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalB), Is.False, "Every required objective should stay uncollectible during the ride.");
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.True, "The player should still be able to mount the creature for the initial ride.");
            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.True,
                "The creature should be under scripted sequence control during the linear ride.");
            Assert.That((Transform)RespawnWaypointField.GetValue(respawnManager), Is.EqualTo(spawnAnchorObject.transform),
                "Initial respawn should stay tied to the starting spawn anchor.");

            Transform checkpoint = (Transform)PlayerHealthType.GetField("respawnPoint", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(playerHealth);
            Assert.That(checkpoint, Is.Not.Null, "PlayerHealth should receive the same spawn checkpoint as the fall-respawn system.");
            AssertVectorApproximately(checkpoint.position, spawnAnchorObject.transform.position);
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
            CleanupDynamicRespawnPoint();
        }
    }

    [Test]
    public void BuildFlightPaths_UsesNumericWaypointOrder_And_StopsOnActualWp03()
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
            SetField(sequence, "crystalStopWaypointName", "WP03");

            BuildFlightPathsMethod.Invoke(sequence, null);

            IList firstLeg = (IList)FirstFlightWaypointsField.GetValue(sequence);
            IList secondLeg = (IList)SecondFlightWaypointsField.GetValue(sequence);

            Assert.That(firstLeg.Count, Is.EqualTo(3), "The first leg should contain WP01 -> WP02 -> WP03.");
            Assert.That(((Transform)firstLeg[0]).name, Is.EqualTo("WP01_Return"));
            Assert.That(((Transform)firstLeg[1]).name, Is.EqualTo("WP02_BigDive"));
            Assert.That(((Transform)firstLeg[2]).name, Is.EqualTo("WP03_ParadiseSweep"),
                "The first ride must stop on the actual WP03 waypoint, not a generated substitute.");

            Assert.That(secondLeg.Count, Is.EqualTo(2));
            Assert.That(((Transform)secondLeg[0]).name, Is.EqualTo("WP04_Paradise"));
            Assert.That(((Transform)secondLeg[1]).name, Is.EqualTo("WP10_Liftoff"));
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
    public void RideableCreature_ScriptedSequenceLock_DisablesJetpack_And_RestoresIt_WhenReleased()
    {
        AssertSharedTypes();
        Assert.That(AutoJetpackControllerType, Is.Not.Null, "AutoJetpackController type not found");
        Assert.That(CreatureSetScriptedSequenceActiveMethod, Is.Not.Null, "RideableCreature.SetScriptedSequenceActive not found");
        Assert.That(CreatureJetpackControllerField, Is.Not.Null, "RideableCreature.jetpackController field not found");
        Assert.That(CreatureInitialJetpackEnabledStateField, Is.Not.Null, "RideableCreature.initialJetpackEnabledState field not found");
        Assert.That(CreatureHasInitialJetpackEnabledStateField, Is.Not.Null, "RideableCreature.hasInitialJetpackEnabledState field not found");

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
            Assert.That(((Behaviour)jetpack).enabled, Is.False,
                "Activating the scripted ride lock should disable the jetpack controller.");

            CreatureSetScriptedSequenceActiveMethod.Invoke(dragon, new object[] { false, false });

            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.False);
            Assert.That(((Behaviour)jetpack).enabled, Is.True,
                "Releasing the scripted ride lock should restore the jetpack controller state.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
        }
    }

    [Test]
    public void HandleDragonMounted_ReappliesInitialWp03FlightPath()
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
        GameObject wrongWp04 = new GameObject("WP04");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);

            SetField(sequence, "dragon", dragon);
            AddWaypoints(FirstFlightWaypointsField, sequence, wp01.transform, wp02.transform, wp03.transform);
            SetStage(sequence, "WaitingForInitialMount");

            CreatureSetFlightPathMethod.Invoke(dragon, new object[] { new List<Transform> { wp01.transform, wp02.transform, wp03.transform, wrongWp04.transform }, true });

            HandleDragonMountedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("FlyingToCrystal"));
            Assert.That((int)CreatureTotalWaypointsProperty.GetValue(dragon), Is.EqualTo(3),
                "Mounting the scripted ride should force the creature back onto the WP01 -> WP02 -> WP03 leg.");
            Assert.That((Transform)CreatureCurrentWaypointProperty.GetValue(dragon), Is.EqualTo(wp01.transform),
                "The mounted ride should restart from the first waypoint of the crystal leg.");
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(wp01);
            UnityEngine.Object.DestroyImmediate(wp02);
            UnityEngine.Object.DestroyImmediate(wp03);
            UnityEngine.Object.DestroyImmediate(wrongWp04);
        }
    }

    [Test]
    public void BuildCrystalAnchors_UsesWp03StopWaypointPosition()
    {
        AssertSharedTypes();
        Assert.That(BuildCrystalAnchorsMethod, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject wp03 = new GameObject("WP03");
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);

            wp03.transform.position = new Vector3(12f, 20f, -6f);
            ground.transform.position = new Vector3(12f, -0.5f, -6f);
            ground.transform.localScale = new Vector3(20f, 1f, 20f);

            SetField(sequence, "crystalStopWaypoint", wp03.transform);

            BuildCrystalAnchorsMethod.Invoke(sequence, null);

            Transform landingAnchor = (Transform)GetPrivateField("crystalLandingAnchor").GetValue(sequence);
            Transform dismountAnchor = (Transform)GetPrivateField("crystalDismountAnchor").GetValue(sequence);

            Assert.That(landingAnchor, Is.Not.Null);
            Assert.That(dismountAnchor, Is.Not.Null);
            Assert.That(landingAnchor.position.x, Is.EqualTo(wp03.transform.position.x).Within(0.01f),
                "The crystal landing anchor should stay aligned with WP03 on the platform.");
            Assert.That(landingAnchor.position.z, Is.EqualTo(wp03.transform.position.z).Within(0.01f),
                "The crystal landing anchor should stay aligned with WP03 on the platform.");
            Assert.That(dismountAnchor.position.x, Is.EqualTo(wp03.transform.position.x).Within(0.01f));
            Assert.That(dismountAnchor.position.z, Is.EqualTo(wp03.transform.position.z).Within(0.01f));
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(wp03);
            UnityEngine.Object.DestroyImmediate(ground);
        }
    }

    [Test]
    public void TryTriggerCrystalLandingFromWaypoint_ForcesStopAtWp03()
    {
        AssertSharedTypes();
        Assert.That(TryTriggerCrystalLandingFromWaypointMethod, Is.Not.Null);
        Assert.That(CreatureIsParkingProperty, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject wp03 = new GameObject("WP03");
        GameObject dismountAnchorObject = new GameObject("CrystalDismountAnchor");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);

            dragonObject.transform.position = new Vector3(40f, 50f, 60f);
            wp03.transform.position = new Vector3(42f, 50f, 63f);
            dismountAnchorObject.transform.position = new Vector3(42f, 45f, 63f);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "crystalStopWaypoint", wp03.transform);
            SetField(sequence, "crystalDismountAnchor", dismountAnchorObject.transform);
            SetField(sequence, "crystalStopReachDistance", 6f);
            SetStage(sequence, "FlyingToCrystal");

            TryTriggerCrystalLandingFromWaypointMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("LandingAtCrystal"),
                "Reaching WP03 should directly trigger the landing phase even if the creature is still carrying the full serialized route.");
            Assert.That((bool)CreatureIsParkingProperty.GetValue(dragon), Is.True,
                "The rideable creature should switch into parking/landing mode as soon as WP03 is reached.");
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(wp03);
            UnityEngine.Object.DestroyImmediate(dismountAnchorObject);
        }
    }

    [Test]
    public void CrystalLanding_KeepsRemountLocked_Until_AllRequiredCrystalsAreCollected()
    {
        AssertSharedTypes();
        Assert.That(HandleLandingCompletedMethod, Is.Not.Null);
        Assert.That(HandleCrystalCollectedMethod, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject playerObject = new GameObject("Player");
        GameObject crystalAObject = new GameObject("CrystalA");
        GameObject crystalBObject = new GameObject("CrystalB");
        GameObject landingAnchorObject = new GameObject("CrystalLandingAnchor");
        GameObject dismountAnchorObject = new GameObject("CrystalDismountAnchor");
        GameObject exitWaypointA = new GameObject("WP04");
        GameObject exitWaypointB = new GameObject("WP05");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component xrOrigin = playerObject.AddComponent(XROriginType);
            Component respawnManager = playerObject.AddComponent(PlayerRespawnManagerType);
            Component playerHealth = playerObject.AddComponent(PlayerHealthType);
            Component crystalA = crystalAObject.AddComponent(CrystalCollectibleType);
            Component crystalB = crystalBObject.AddComponent(CrystalCollectibleType);

            dragonObject.transform.position = new Vector3(10f, 20f, 30f);
            dismountAnchorObject.transform.position = new Vector3(40f, 5f, -12f);
            dismountAnchorObject.transform.rotation = Quaternion.Euler(0f, 95f, 0f);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "xrOrigin", xrOrigin);
            SetField(sequence, "respawnManager", respawnManager);
            SetField(sequence, "playerHealth", playerHealth);
            SetField(sequence, "crystalLandingAnchor", landingAnchorObject.transform);
            SetField(sequence, "crystalDismountAnchor", dismountAnchorObject.transform);
            SetRequiredCrystals(sequence, crystalA, crystalB);
            ResolveRequiredCrystalsMethod.Invoke(sequence, null);

            AddWaypoints(SecondFlightWaypointsField, sequence, exitWaypointA.transform, exitWaypointB.transform);
            SetStage(sequence, "LandingAtCrystal");

            HandleLandingCompletedMethod.Invoke(sequence, new object[] { null });

            Assert.That(GetStageName(sequence), Is.EqualTo("WaitingForCrystalPickup"));
            Assert.That(crystalAObject.activeSelf, Is.True, "Crystals should become collectible only after the WP03 landing is finished.");
            Assert.That(crystalBObject.activeSelf, Is.True, "Every required crystal should unlock together on the platform.");
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalA), Is.True, "The objective should become collectible after the WP03 landing completes.");
            Assert.That((bool)CrystalCollectionEnabledProperty.GetValue(crystalB), Is.True, "Every required objective should become collectible together.");
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.False, "The creature must stay locked while crystals remain.");
            Assert.That((Transform)RespawnWaypointField.GetValue(respawnManager), Is.EqualTo(dismountAnchorObject.transform),
                "Falling off the WP03 platform should respawn the player on that platform.");

            CrystalCollectedField.SetValue(crystalA, true);
            HandleCrystalCollectedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("WaitingForCrystalPickup"),
                "Collecting only part of the crystal set must not unlock the remount.");
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.False,
                "The creature must remain blocked until all required crystals are collected.");

            CrystalCollectedField.SetValue(crystalB, true);
            HandleCrystalCollectedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("WaitingForCrystalRemount"),
                "Collecting the full crystal set should unlock the remount sequence.");
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.True,
                "The creature should become mountable again only after all required crystals are collected.");
            Assert.That((int)CreatureTotalWaypointsProperty.GetValue(dragon), Is.EqualTo(3),
                "After crystal completion, the creature should receive the remount start plus the remaining exit waypoints.");

            Transform crystalRespawnAnchor = (Transform)CrystalRespawnAnchorField.GetValue(sequence);
            Assert.That(crystalRespawnAnchor, Is.Not.Null, "Collecting the crystals should create a dedicated platform respawn anchor.");
            AssertVectorApproximately(crystalRespawnAnchor.position, dismountAnchorObject.transform.position,
                "Post-collection respawn should still land on the same crystal platform.");
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(crystalAObject);
            UnityEngine.Object.DestroyImmediate(crystalBObject);
            UnityEngine.Object.DestroyImmediate(landingAnchorObject);
            UnityEngine.Object.DestroyImmediate(dismountAnchorObject);
            UnityEngine.Object.DestroyImmediate(exitWaypointA);
            UnityEngine.Object.DestroyImmediate(exitWaypointB);
            CleanupDynamicRespawnPoint();
        }
    }

    [Test]
    public void RoverLanding_Dismount_CompletesHandoff_And_EnablesRover()
    {
        AssertSharedTypes();
        Assert.That(HandleDragonDismountedMethod, Is.Not.Null);
        Assert.That(RoverCanMountProperty, Is.Not.Null);

        GameObject sequenceObject = new GameObject("Sequence");
        GameObject dragonObject = new GameObject("Dragon");
        GameObject roverObject = new GameObject("Rover");
        GameObject playerObject = new GameObject("Player");
        GameObject roverAnchorObject = new GameObject("RoverDismountAnchor");

        try
        {
            Component sequence = sequenceObject.AddComponent(SequenceType);
            Component dragon = dragonObject.AddComponent(RideableCreatureType);
            Component rover = roverObject.AddComponent(RoverPhysicsControllerType);
            Component respawnManager = playerObject.AddComponent(PlayerRespawnManagerType);
            Component playerHealth = playerObject.AddComponent(PlayerHealthType);

            roverAnchorObject.transform.position = new Vector3(-8f, 1.5f, 22f);

            SetField(sequence, "dragon", dragon);
            SetField(sequence, "rover", rover);
            SetField(sequence, "respawnManager", respawnManager);
            SetField(sequence, "playerHealth", playerHealth);
            SetField(sequence, "roverDismountAnchor", roverAnchorObject.transform);
            SetStage(sequence, "LandingAtRover");

            HandleDragonDismountedMethod.Invoke(sequence, null);

            Assert.That(GetStageName(sequence), Is.EqualTo("Completed"),
                "Dismounting at the rover landing should finalize the creature sequence.");
            Assert.That((bool)CreatureCanMountField.GetValue(dragon), Is.False,
                "The creature should stay locked after the rover handoff.");
            Assert.That((bool)CreatureScriptedSequenceActiveProperty.GetValue(dragon), Is.False,
                "Completing the creature sequence should release scripted ride control.");
            Assert.That((bool)RoverCanMountProperty.GetValue(rover), Is.True,
                "The rover should become mountable when the creature sequence ends.");
            Assert.That((Transform)RespawnWaypointField.GetValue(respawnManager), Is.EqualTo(roverAnchorObject.transform),
                "The rover anchor should become the new fall-respawn checkpoint after the handoff.");

            Transform checkpoint = (Transform)PlayerHealthType.GetField("respawnPoint", BindingFlags.Instance | BindingFlags.Public)
                ?.GetValue(playerHealth);
            Assert.That(checkpoint, Is.Not.Null);
            AssertVectorApproximately(checkpoint.position, roverAnchorObject.transform.position);
        }
        finally
        {
            CleanupRuntimeAnchors();
            UnityEngine.Object.DestroyImmediate(sequenceObject);
            UnityEngine.Object.DestroyImmediate(dragonObject);
            UnityEngine.Object.DestroyImmediate(roverObject);
            UnityEngine.Object.DestroyImmediate(playerObject);
            UnityEngine.Object.DestroyImmediate(roverAnchorObject);
            CleanupDynamicRespawnPoint();
        }
    }

    private static void AssertSharedTypes()
    {
        Assert.That(SequenceType, Is.Not.Null, "FloatingIslandIntroSequence type not found");
        Assert.That(RideableCreatureType, Is.Not.Null, "RideableCreature type not found");
        Assert.That(CrystalCollectibleType, Is.Not.Null, "CrystalCollectible type not found");
        Assert.That(PlayerRespawnManagerType, Is.Not.Null, "PlayerRespawnManager type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(SequenceStageType, Is.Not.Null, "FloatingIslandIntroSequence.SequenceStage type not found");
        Assert.That(XROriginType, Is.Not.Null, "XROrigin type not found");
        Assert.That(StageField, Is.Not.Null, "FloatingIslandIntroSequence.stage field not found");
        Assert.That(RequiredCrystalsField, Is.Not.Null, "FloatingIslandIntroSequence.requiredCrystals field not found");
        Assert.That(RespawnWaypointField, Is.Not.Null, "PlayerRespawnManager.wp01Transform field not found");
        Assert.That(CrystalCollectedField, Is.Not.Null, "CrystalCollectible.isCollected field not found");
        Assert.That(CrystalCollectionEnabledProperty, Is.Not.Null, "CrystalCollectible.CollectionEnabled property not found");
        Assert.That(CreatureCanMountField, Is.Not.Null, "RideableCreature.canPlayerMount field not found");
        Assert.That(CreatureScriptedSequenceActiveProperty, Is.Not.Null, "RideableCreature.IsScriptedSequenceActive property not found");
        Assert.That(CreatureTotalWaypointsProperty, Is.Not.Null, "RideableCreature.TotalWaypoints property not found");
        Assert.That(CreatureIsParkingProperty, Is.Not.Null, "RideableCreature.IsParking property not found");
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
            "CrystalLandingAnchor_Runtime",
            "CrystalDismountAnchor_Runtime",
            "CrystalRespawnAnchor_Runtime",
            "CrystalApproachWaypoint_Runtime",
            "CrystalRemountStart_Runtime",
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
