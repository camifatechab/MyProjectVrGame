using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RoverDriverTests
{
    private static readonly Type RoverDriverType = Type.GetType("RoverDriver, Assembly-CSharp");
    private static readonly Type AutoJetpackControllerType = Type.GetType("AutoJetpackController, Assembly-CSharp");
    private static readonly Type FlightSmokeTrailType = Type.GetType("FlightSmokeTrail, Assembly-CSharp");

    [Test]
    public void Mount_DisablesJetpack_And_Dismount_ReenablesIt()
    {
        Assert.That(RoverDriverType, Is.Not.Null, "RoverDriver type not found");
        Assert.That(AutoJetpackControllerType, Is.Not.Null, "AutoJetpackController type not found");
        Assert.That(FlightSmokeTrailType, Is.Not.Null, "FlightSmokeTrail type not found");

        GameObject rover = new GameObject("Rover");
        GameObject seat = new GameObject("Seat");
        GameObject player = new GameObject("Player");

        try
        {
            seat.transform.SetParent(rover.transform, false);

            rover.AddComponent<BoxCollider>();
            Component driver = rover.AddComponent(RoverDriverType);
            RoverDriverType.GetField("seatAnchor", BindingFlags.Instance | BindingFlags.Public)?.SetValue(driver, seat.transform);

            Component jetpack = player.AddComponent(AutoJetpackControllerType);
            Component smokeTrail = player.AddComponent(FlightSmokeTrailType);
            FlightSmokeTrailType.GetField("jetpackController", BindingFlags.Instance | BindingFlags.Public)?.SetValue(smokeTrail, jetpack);

            MethodInfo startMethod = RoverDriverType.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo mountMethod = RoverDriverType.GetMethod("Mount", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo dismountMethod = RoverDriverType.GetMethod("Dismount", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo isMountedProperty = RoverDriverType.GetProperty("IsMounted", BindingFlags.Instance | BindingFlags.Public);

            Assert.That(startMethod, Is.Not.Null, "RoverDriver.Start not found");
            Assert.That(mountMethod, Is.Not.Null, "RoverDriver.Mount not found");
            Assert.That(dismountMethod, Is.Not.Null, "RoverDriver.Dismount not found");
            Assert.That(isMountedProperty, Is.Not.Null, "RoverDriver.IsMounted not found");

            startMethod.Invoke(driver, null);
            mountMethod.Invoke(driver, null);

            Assert.That((bool)isMountedProperty.GetValue(driver), Is.True, "Rover should enter mounted state.");
            Assert.That(((Behaviour)jetpack).enabled, Is.False, "Mounting the rover should disable AutoJetpackController.");

            dismountMethod.Invoke(driver, null);

            Assert.That((bool)isMountedProperty.GetValue(driver), Is.False, "Rover should leave mounted state after dismount.");
            Assert.That(((Behaviour)jetpack).enabled, Is.True, "Dismounting the rover should re-enable AutoJetpackController.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rover);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }
}

public class RoverPhysicsControllerTests
{
    private static readonly Type RoverPhysicsControllerType = Type.GetType("RoverPhysicsController, Assembly-CSharp");
    private static readonly Type AutoJetpackControllerType = Type.GetType("AutoJetpackController, Assembly-CSharp");
    private static readonly Type FlightSmokeTrailType = Type.GetType("FlightSmokeTrail, Assembly-CSharp");
    private static readonly Type JetpackAudioManagerType = Type.GetType("JetpackAudioManager, Assembly-CSharp");
    private static readonly Type RoverRadioControllerType = Type.GetType("RoverRadioController, Assembly-CSharp");
    private static readonly Type RoverRoadFailSafeType = Type.GetType("RoverRoadFailSafe, Assembly-CSharp");
    private static readonly Type RoverEdgeRecoveryAssistType = Type.GetType("RoverEdgeRecoveryAssist, Assembly-CSharp");
    private static readonly Type LaserShooterType = Type.GetType("LaserShooter, Assembly-CSharp");

    [Test]
    public void SetJetpackMountedState_DisablesJetpack_And_LocksFlight()
    {
        Assert.That(RoverPhysicsControllerType, Is.Not.Null, "RoverPhysicsController type not found");
        Assert.That(AutoJetpackControllerType, Is.Not.Null, "AutoJetpackController type not found");
        Assert.That(FlightSmokeTrailType, Is.Not.Null, "FlightSmokeTrail type not found");

        GameObject rover = new GameObject("RoverPhysics");
        GameObject player = new GameObject("Player");

        try
        {
            Component controller = rover.AddComponent(RoverPhysicsControllerType);
            Component jetpack = player.AddComponent(AutoJetpackControllerType);
            Component smokeTrail = player.AddComponent(FlightSmokeTrailType);

            FlightSmokeTrailType.GetField("jetpackController", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(smokeTrail, jetpack);

            RoverPhysicsControllerType.GetField("jetpack", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, jetpack);
            RoverPhysicsControllerType.GetField("flightSmokeTrail", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(controller, smokeTrail);

            FieldInfo isFlyingField = AutoJetpackControllerType.GetField("isFlying", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo externalLockField = AutoJetpackControllerType.GetField("externalFlightLock", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo cooldownField = AutoJetpackControllerType.GetField("postDismountCooldown", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo setMountedStateMethod = RoverPhysicsControllerType.GetMethod("SetJetpackMountedState", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(isFlyingField, Is.Not.Null, "AutoJetpackController.isFlying not found");
            Assert.That(externalLockField, Is.Not.Null, "AutoJetpackController.externalFlightLock not found");
            Assert.That(cooldownField, Is.Not.Null, "AutoJetpackController.postDismountCooldown not found");
            Assert.That(setMountedStateMethod, Is.Not.Null, "RoverPhysicsController.SetJetpackMountedState not found");

            isFlyingField.SetValue(jetpack, true);

            setMountedStateMethod.Invoke(controller, new object[] { true });

            Assert.That((bool)externalLockField.GetValue(jetpack), Is.True, "Mounting should externally lock the jetpack.");
            Assert.That((bool)isFlyingField.GetValue(jetpack), Is.False, "Mounting should force the jetpack out of flying state.");
            Assert.That(((Behaviour)jetpack).enabled, Is.False, "Mounting should disable AutoJetpackController.");

            setMountedStateMethod.Invoke(controller, new object[] { false });

            Assert.That((bool)externalLockField.GetValue(jetpack), Is.False, "Dismounting should clear the external flight lock.");
            Assert.That(((Behaviour)jetpack).enabled, Is.True, "Dismounting should re-enable AutoJetpackController.");
            Assert.That((float)cooldownField.GetValue(jetpack), Is.EqualTo(1f).Within(0.001f), "Dismounting should restore the post-dismount jetpack cooldown.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rover);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SetExternalFlightLock_StopsJetpackAudioImmediately()
    {
        Assert.That(AutoJetpackControllerType, Is.Not.Null, "AutoJetpackController type not found");
        Assert.That(JetpackAudioManagerType, Is.Not.Null, "JetpackAudioManager type not found");

        GameObject player = new GameObject("Player");

        try
        {
            Component jetpack = player.AddComponent(AutoJetpackControllerType);
            Component audioManager = player.AddComponent(JetpackAudioManagerType);

            AutoJetpackControllerType.GetField("audioManager", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(jetpack, audioManager);
            AutoJetpackControllerType.GetField("jetpackAudioSource", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(jetpack, player.AddComponent<AudioSource>());
            AutoJetpackControllerType.GetField("isFlying", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(jetpack, true);

            MethodInfo startThrustMethod = JetpackAudioManagerType.GetMethod("StartThrust", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo setExternalFlightLockMethod = AutoJetpackControllerType.GetMethod("SetExternalFlightLock", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo isThrustingProperty = JetpackAudioManagerType.GetProperty("IsThrusting", BindingFlags.Instance | BindingFlags.Public);

            Assert.That(startThrustMethod, Is.Not.Null, "JetpackAudioManager.StartThrust not found");
            Assert.That(setExternalFlightLockMethod, Is.Not.Null, "AutoJetpackController.SetExternalFlightLock not found");
            Assert.That(isThrustingProperty, Is.Not.Null, "JetpackAudioManager.IsThrusting not found");

            startThrustMethod.Invoke(audioManager, null);
            Assert.That((bool)isThrustingProperty.GetValue(audioManager), Is.True, "Jetpack audio manager should report active thrust before the flight lock is applied.");

            setExternalFlightLockMethod.Invoke(jetpack, new object[] { true });

            Assert.That((bool)isThrustingProperty.GetValue(audioManager), Is.False,
                "Applying the rover flight lock should stop active jetpack audio immediately.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void RadioTick_Dismount_PowersRadioOff()
    {
        Assert.That(RoverRadioControllerType, Is.Not.Null, "RoverRadioController type not found");

        GameObject rover = new GameObject("RoverRadio");
        AudioClip trackClip = AudioClip.Create("Track", 4410, 1, 44100, false);

        try
        {
            Component radioController = rover.AddComponent(RoverRadioControllerType);
            Type radioTrackType = RoverRadioControllerType.GetNestedType("RadioTrack", BindingFlags.Public);
            MethodInfo awakeMethod = RoverRadioControllerType.GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo tickMethod = RoverRadioControllerType.GetMethod("Tick", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo toggleMethod = RoverRadioControllerType.GetMethod("ToggleFromUi", BindingFlags.Instance | BindingFlags.Public);
            PropertyInfo isOnProperty = RoverRadioControllerType.GetProperty("IsOn", BindingFlags.Instance | BindingFlags.Public);
            FieldInfo tracksField = RoverRadioControllerType.GetField("tracks", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(radioTrackType, Is.Not.Null, "RoverRadioController.RadioTrack type not found");
            Assert.That(awakeMethod, Is.Not.Null, "RoverRadioController.Awake not found");
            Assert.That(tickMethod, Is.Not.Null, "RoverRadioController.Tick not found");
            Assert.That(toggleMethod, Is.Not.Null, "RoverRadioController.ToggleFromUi not found");
            Assert.That(isOnProperty, Is.Not.Null, "RoverRadioController.IsOn not found");
            Assert.That(tracksField, Is.Not.Null, "RoverRadioController.tracks not found");

            Array trackArray = Array.CreateInstance(radioTrackType, 1);
            object track = Activator.CreateInstance(radioTrackType);
            radioTrackType.GetField("displayName", BindingFlags.Instance | BindingFlags.Public)?.SetValue(track, "Track 1");
            radioTrackType.GetField("clip", BindingFlags.Instance | BindingFlags.Public)?.SetValue(track, trackClip);
            trackArray.SetValue(track, 0);
            tracksField.SetValue(radioController, trackArray);

            awakeMethod.Invoke(radioController, null);
            tickMethod.Invoke(radioController, new object[] { true });
            toggleMethod.Invoke(radioController, null);

            Assert.That((bool)isOnProperty.GetValue(radioController), Is.True, "Mounted radio toggle should power the radio on.");

            tickMethod.Invoke(radioController, new object[] { false });

            Assert.That((bool)isOnProperty.GetValue(radioController), Is.False, "Leaving the rover should power the radio off.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rover);
            UnityEngine.Object.DestroyImmediate(trackClip);
        }
    }

    [Test]
    public void MountedLaserFilter_IgnoresRoverCollider_ButAcceptsTargetCollider()
    {
        Assert.That(LaserShooterType, Is.Not.Null, "LaserShooter type not found");
        Assert.That(RoverPhysicsControllerType, Is.Not.Null, "RoverPhysicsController type not found");

        GameObject laserObject = new GameObject("Right Controller");
        GameObject rover = new GameObject("Rover");
        GameObject target = GameObject.CreatePrimitive(PrimitiveType.Cube);
        GameObject laserOrigin = new GameObject("LaserOrigin");

        try
        {
            laserOrigin.transform.SetParent(laserObject.transform, false);
            laserOrigin.transform.localPosition = Vector3.zero;
            laserOrigin.transform.localRotation = Quaternion.identity;

            Component laserShooter = laserObject.AddComponent(LaserShooterType);
            Component roverController = rover.AddComponent(RoverPhysicsControllerType);

            rover.transform.position = new Vector3(0f, 0f, 2f);
            BoxCollider roverCollider = rover.AddComponent<BoxCollider>();
            roverCollider.size = new Vector3(1.5f, 1.5f, 1.5f);

            target.transform.position = new Vector3(0f, 0f, 5f);

            RoverPhysicsControllerType.GetField("isMounted", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(roverController, true);

            LaserShooterType.GetField("roverController", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(laserShooter, roverController);
            LaserShooterType.GetField("laserOrigin", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(laserShooter, laserOrigin.transform);
            LaserShooterType.GetField("laserRange", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.SetValue(laserShooter, 20f);

            MethodInfo shouldIgnoreHitMethod = LaserShooterType.GetMethod("ShouldIgnoreHit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(shouldIgnoreHitMethod, Is.Not.Null, "LaserShooter.ShouldIgnoreHit not found");

            bool roverIgnored = (bool)shouldIgnoreHitMethod.Invoke(laserShooter, new object[] { roverCollider });
            bool targetIgnored = (bool)shouldIgnoreHitMethod.Invoke(laserShooter, new object[] { target.GetComponent<Collider>() });

            Assert.That(roverIgnored, Is.True, "Mounted rover laser should ignore the rover body collider.");
            Assert.That(targetIgnored, Is.False, "Mounted rover laser should still accept the dragon/target collider ahead.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(laserObject);
            UnityEngine.Object.DestroyImmediate(rover);
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void UnsupportedOffRoadState_ResetsWithoutWaitingForLongFall()
    {
        Assert.That(RoverRoadFailSafeType, Is.Not.Null, "RoverRoadFailSafe type not found");

        GameObject rover = new GameObject("RoverFailSafe");

        try
        {
            Rigidbody rb = rover.AddComponent<Rigidbody>();
            Component failSafe = rover.AddComponent(RoverRoadFailSafeType);

            rover.transform.position = Vector3.zero;

            RoverRoadFailSafeType.GetField("rb", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, rb);
            RoverRoadFailSafeType.GetField("offRoadResetDelay", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 0.1f);
            RoverRoadFailSafeType.GetField("unsupportedResetDelay", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 0.1f);

            FieldInfo lastSafePositionField = RoverRoadFailSafeType.GetField("lastSafePosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lastSafeRotationField = RoverRoadFailSafeType.GetField("lastSafeRotation", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo hasSafePositionField = RoverRoadFailSafeType.GetField("hasSafePosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo offRoadTimerField = RoverRoadFailSafeType.GetField("offRoadTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo fixedUpdateMethod = RoverRoadFailSafeType.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(lastSafePositionField, Is.Not.Null, "RoverRoadFailSafe.lastSafePosition not found");
            Assert.That(lastSafeRotationField, Is.Not.Null, "RoverRoadFailSafe.lastSafeRotation not found");
            Assert.That(hasSafePositionField, Is.Not.Null, "RoverRoadFailSafe.hasSafePosition not found");
            Assert.That(offRoadTimerField, Is.Not.Null, "RoverRoadFailSafe.offRoadTimer not found");
            Assert.That(fixedUpdateMethod, Is.Not.Null, "RoverRoadFailSafe.FixedUpdate not found");

            Vector3 safePosition = new Vector3(10f, 2f, 0f);
            rover.transform.position = new Vector3(6f, 2f, 0f);
            lastSafePositionField.SetValue(failSafe, safePosition);
            lastSafeRotationField.SetValue(failSafe, Quaternion.identity);
            hasSafePositionField.SetValue(failSafe, true);
            offRoadTimerField.SetValue(failSafe, 0.09f);

            fixedUpdateMethod.Invoke(failSafe, null);

            Assert.That(Vector3.Distance(rb.position, safePosition), Is.LessThan(0.001f),
                "Unsupported rover states should snap back to the last checkpoint without waiting for a long vertical fall.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rover);
        }
    }

    [Test]
    public void BriefUnsupportedStateNearSafeRoad_DoesNotKeepResetTimerRunning()
    {
        Assert.That(RoverRoadFailSafeType, Is.Not.Null, "RoverRoadFailSafe type not found");

        GameObject rover = new GameObject("RoverFailSafeStable");

        try
        {
            Rigidbody rb = rover.AddComponent<Rigidbody>();
            Component failSafe = rover.AddComponent(RoverRoadFailSafeType);

            rover.transform.position = new Vector3(10.4f, 2f, 0f);

            RoverRoadFailSafeType.GetField("rb", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, rb);
            RoverRoadFailSafeType.GetField("offRoadResetDelay", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 0.1f);
            RoverRoadFailSafeType.GetField("unsupportedResetDelay", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 0.1f);
            RoverRoadFailSafeType.GetField("unsupportedHorizontalResetDistance", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 1.75f);

            FieldInfo lastSafePositionField = RoverRoadFailSafeType.GetField("lastSafePosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo lastSafeRotationField = RoverRoadFailSafeType.GetField("lastSafeRotation", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo hasSafePositionField = RoverRoadFailSafeType.GetField("hasSafePosition", BindingFlags.Instance | BindingFlags.NonPublic);
            FieldInfo offRoadTimerField = RoverRoadFailSafeType.GetField("offRoadTimer", BindingFlags.Instance | BindingFlags.NonPublic);
            MethodInfo fixedUpdateMethod = RoverRoadFailSafeType.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);

            Assert.That(lastSafePositionField, Is.Not.Null, "RoverRoadFailSafe.lastSafePosition not found");
            Assert.That(lastSafeRotationField, Is.Not.Null, "RoverRoadFailSafe.lastSafeRotation not found");
            Assert.That(hasSafePositionField, Is.Not.Null, "RoverRoadFailSafe.hasSafePosition not found");
            Assert.That(offRoadTimerField, Is.Not.Null, "RoverRoadFailSafe.offRoadTimer not found");
            Assert.That(fixedUpdateMethod, Is.Not.Null, "RoverRoadFailSafe.FixedUpdate not found");

            Vector3 safePosition = new Vector3(10f, 2f, 0f);
            lastSafePositionField.SetValue(failSafe, safePosition);
            lastSafeRotationField.SetValue(failSafe, Quaternion.identity);
            hasSafePositionField.SetValue(failSafe, true);
            offRoadTimerField.SetValue(failSafe, 0.09f);
            rb.linearVelocity = Vector3.zero;

            fixedUpdateMethod.Invoke(failSafe, null);

            float offRoadTimer = (float)offRoadTimerField.GetValue(failSafe);
            Assert.That(offRoadTimer, Is.EqualTo(0f).Within(0.0001f),
                "Brief unsupported moments near the last safe road point should clear the timer instead of repeatedly re-arming rover resets.");
            Assert.That(Vector3.Distance(rb.position, safePosition), Is.GreaterThan(0.2f),
                "Stable rover states near the checkpoint should not be teleported back to the last safe road point.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(rover);
        }
    }

    [Test]
    public void OffsetRoadUnderRoverFootprint_StillCountsAsOnRoad()
    {
        Assert.That(RoverRoadFailSafeType, Is.Not.Null, "RoverRoadFailSafe type not found");
        Assert.That(RoverPhysicsControllerType, Is.Not.Null, "RoverPhysicsController type not found");

        GameObject rover = new GameObject("RoverFootprint");
        GameObject road = GameObject.CreatePrimitive(PrimitiveType.Cube);

        try
        {
            Rigidbody rb = rover.AddComponent<Rigidbody>();
            Component controller = rover.AddComponent(RoverPhysicsControllerType);
            BoxCollider bodyCollider = rover.AddComponent<BoxCollider>();
            bodyCollider.size = new Vector3(2.4f, 0.6f, 1.8f);

            Component failSafe = rover.AddComponent(RoverRoadFailSafeType);

            rover.transform.position = Vector3.zero;

            road.name = "Road_Test";
            road.transform.position = new Vector3(1f, -1f, 0f);
            road.transform.localScale = new Vector3(0.4f, 0.2f, 1.8f);

            RoverRoadFailSafeType.GetField("rb", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, rb);
            RoverRoadFailSafeType.GetField("controller", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, controller);
            RoverRoadFailSafeType.GetField("safeProbeRadius", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 0.2f);
            RoverRoadFailSafeType.GetField("safeProbeDistance", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(failSafe, 4f);

            RoverPhysicsControllerType.GetField("bodyCollider", BindingFlags.Instance | BindingFlags.Public)
                ?.SetValue(controller, bodyCollider);

            MethodInfo tryGetSafeHitMethod = RoverRoadFailSafeType.GetMethod("TryGetSafeHit", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(tryGetSafeHitMethod, Is.Not.Null, "RoverRoadFailSafe.TryGetSafeHit not found");

            Physics.SyncTransforms();

            object[] parameters = { null };
            bool foundSafeRoad = (bool)tryGetSafeHitMethod.Invoke(failSafe, parameters);
            RaycastHit hit = (RaycastHit)parameters[0];

            Assert.That(foundSafeRoad, Is.True,
                "Road directly under the rover footprint should count as on-road even when the rover center is not directly above the road mesh.");
            Assert.That(hit.collider, Is.Not.Null);
            Assert.That(hit.collider.name, Does.StartWith("Road_"));
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(road);
            UnityEngine.Object.DestroyImmediate(rover);
        }
    }

    [Test]
    public void EdgeRecoveryDirection_RemovesBackwardComponentFromGuideNormal()
    {
        Assert.That(RoverEdgeRecoveryAssistType, Is.Not.Null, "RoverEdgeRecoveryAssist type not found");

        MethodInfo resolveDirectionMethod = RoverEdgeRecoveryAssistType.GetMethod(
            "ResolveRecoveryDirection",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(resolveDirectionMethod, Is.Not.Null, "RoverEdgeRecoveryAssist.ResolveRecoveryDirection not found");

        Vector3 forward = new Vector3(0.44f, 0f, 0.9f).normalized;
        Vector3 curvedGuideNormal = new Vector3(-0.253896862f, 0.958468139f, -0.129904777f).normalized;

        Vector3 resolved = (Vector3)resolveDirectionMethod.Invoke(null, new object[] { curvedGuideNormal, forward });

        Assert.That(resolved.sqrMagnitude, Is.GreaterThan(0.0001f));
        Assert.That(Vector3.Dot(resolved, forward), Is.EqualTo(0f).Within(0.0001f),
            "Guide recovery should be lateral-only so curved guide contacts do not oppose forward driving.");
    }

    [Test]
    public void PassablePrefixes_AlwaysIncludeRequiredBarrierHelpers()
    {
        Assert.That(RoverRoadFailSafeType, Is.Not.Null, "RoverRoadFailSafe type not found");

        MethodInfo ensurePrefixesMethod = RoverRoadFailSafeType.GetMethod(
            "EnsureRequiredPrefixes",
            BindingFlags.Static | BindingFlags.NonPublic);

        Assert.That(ensurePrefixesMethod, Is.Not.Null, "RoverRoadFailSafe.EnsureRequiredPrefixes not found");

        string[] merged = (string[])ensurePrefixesMethod.Invoke(null, new object[] { new[] { "Guide_", "Shelf_" }, new[] { "Guide_", "Shelf_", "Edge_" } });

        CollectionAssert.Contains(merged, "Guide_");
        CollectionAssert.Contains(merged, "Shelf_");
        CollectionAssert.Contains(merged, "Edge_");
    }
}
