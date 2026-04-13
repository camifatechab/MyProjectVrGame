using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class DragonAttackTests
{
    private static readonly Type DragonAttackType = Type.GetType("DragonAttack, Assembly-CSharp");
    private static readonly Type FlyingCreaturePatrolType = Type.GetType("FlyingCreaturePatrol, Assembly-CSharp");
    private static readonly Type PlayerHealthType = Type.GetType("PlayerHealth, Assembly-CSharp");

    private static readonly MethodInfo PatrolStartMethod =
        FlyingCreaturePatrolType?.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo PatrolUpdateMethod =
        FlyingCreaturePatrolType?.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly PropertyInfo PatrolCurrentWaypointIndexProperty =
        FlyingCreaturePatrolType?.GetProperty("CurrentWaypointIndex", BindingFlags.Instance | BindingFlags.Public);

    private static readonly MethodInfo DragonOnEnableMethod =
        DragonAttackType?.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonOnDisableMethod =
        DragonAttackType?.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonStartMethod =
        DragonAttackType?.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonUpdateMethod =
        DragonAttackType?.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo ProjectileOnTriggerEnterMethod =
        Type.GetType("DragonProjectile, Assembly-CSharp")?.GetMethod("OnTriggerEnter", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonModeField =
        DragonAttackType?.GetField("mode", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonModeTimerField =
        DragonAttackType?.GetField("modeTimer", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonPlayerInsideZoneField =
        DragonAttackType?.GetField("playerInsideEngagementZone", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonPlayerWithinAwarenessZoneField =
        DragonAttackType?.GetField("playerWithinAwarenessZone", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonGraceExpiredField =
        DragonAttackType?.GetField("s_graceExpired", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonAlertUntilField =
        DragonAttackType?.GetField("s_alertUntil", BindingFlags.Static | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonSmoothedPlayerSpeedField =
        DragonAttackType?.GetField("smoothedPlayerSpeed", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonLastPlayerPositionField =
        DragonAttackType?.GetField("lastPlayerPosition", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonHasPlayerPositionSampleField =
        DragonAttackType?.GetField("hasPlayerPositionSample", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo GetCurrentAttackRangeMethod =
        DragonAttackType?.GetMethod("GetCurrentAttackRange", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo GetCurrentMinDistanceMethod =
        DragonAttackType?.GetMethod("GetCurrentMinDistance", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo PatrolWaypointsField =
        FlyingCreaturePatrolType?.GetField("waypoints", BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo PatrolWaypointPositionsField =
        FlyingCreaturePatrolType?.GetField("waypointPositions", BindingFlags.Instance | BindingFlags.NonPublic);

    [Test]
    public void TargetInsideWaypointZone_StaysOnWaypointPatrolUntilAttackWindowOpens()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonModeField, Is.Not.Null, "DragonAttack.mode not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(DragonPlayerInsideZoneField, Is.Not.Null, "DragonAttack.playerInsideEngagementZone not found");
        Assert.That(GetCurrentAttackRangeMethod, Is.Not.Null, "DragonAttack.GetCurrentAttackRange not found");
        Assert.That(GetCurrentMinDistanceMethod, Is.Not.Null, "DragonAttack.GetCurrentMinDistance not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(10f, 0f, 10f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-20f, 0f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"WP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);
            DragonUpdateMethod.Invoke(dragonAttack, null);

            bool playerInsideZone = (bool)DragonPlayerInsideZoneField.GetValue(dragonAttack);
            string currentMode = DragonModeField.GetValue(dragonAttack).ToString();
            float modeTimer = (float)DragonModeTimerField.GetValue(dragonAttack);
            float attackRange = (float)GetCurrentAttackRangeMethod.Invoke(dragonAttack, null);
            float minDistance = (float)GetCurrentMinDistanceMethod.Invoke(dragonAttack, null);

            Assert.That(playerInsideZone, Is.True, "Player should be recognized inside the engagement zone built from patrol waypoints.");
            Assert.That(patrol.enabled, Is.False, "Once the player is inside the combat footprint, target dragons should switch off passive patrol and shadow the player while preparing the next attack run.");
            Assert.That(currentMode, Is.EqualTo("SkyPatrol"), "Target dragons can remain in their alert sky-patrol mode while they set up the next attack run.");
            Assert.That(modeTimer, Is.GreaterThan(0f), "Target dragons should keep a short attack timer while stalking the player so the first dive still has a readable telegraph.");
            Assert.That(attackRange, Is.GreaterThan(minDistance), "Aggressive attack range must exceed the minimum follow distance so dragons can actually fire.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TargetInsideWaypointZone_ClampsLongPatrolWaitWhenCombatStarts()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(DragonGraceExpiredField, Is.Not.Null, "DragonAttack.s_graceExpired not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(10f, 0f, 10f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-20f, 0f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"EngageWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);

            DragonGraceExpiredField.SetValue(null, true);
            DragonModeTimerField.SetValue(dragonAttack, 14f);

            DragonUpdateMethod.Invoke(dragonAttack, null);

            float modeTimer = (float)DragonModeTimerField.GetValue(dragonAttack);
            Assert.That(modeTimer, Is.LessThan(5f),
                "When the player enters the combat footprint, target dragons should clamp any long patrol delay so the fight starts quickly.");
            Assert.That(modeTimer, Is.GreaterThan(0f),
                "Targets should still keep a brief readable beat before attacking instead of jumping instantly with no telegraph.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PlayerNearWaypointZone_TriggersProactiveHuntBeforeCrossingCombatBoundary()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonPlayerInsideZoneField, Is.Not.Null, "DragonAttack.playerInsideEngagementZone not found");
        Assert.That(DragonPlayerWithinAwarenessZoneField, Is.Not.Null, "DragonAttack.playerWithinAwarenessZone not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(45f, 0f, 0f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-20f, 0f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"AwareWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);
            DragonAlertUntilField?.SetValue(null, 0f);
            DragonModeTimerField.SetValue(dragonAttack, 9f);
            DragonSmoothedPlayerSpeedField.SetValue(dragonAttack, 24f);
            DragonLastPlayerPositionField.SetValue(dragonAttack, player.transform.position - new Vector3(20f, 0f, 0f));
            DragonHasPlayerPositionSampleField.SetValue(dragonAttack, true);

            DragonUpdateMethod.Invoke(dragonAttack, null);

            bool playerInsideZone = (bool)DragonPlayerInsideZoneField.GetValue(dragonAttack);
            bool playerWithinAwarenessZone = (bool)DragonPlayerWithinAwarenessZoneField.GetValue(dragonAttack);
            float modeTimer = (float)DragonModeTimerField.GetValue(dragonAttack);

            Assert.That(playerInsideZone, Is.False, "This setup should keep the player outside the strict combat footprint.");
            Assert.That(playerWithinAwarenessZone || modeTimer < 5f, Is.True,
                "High rover speed should still trigger an early hunting response before the player fully crosses the combat footprint, whether that happens through the awareness halo or direct proactive detection.");
            Assert.That(patrol.enabled, Is.False, "Fast movement near the target area should pull dragons out of passive waypoint patrol and into a stalking state.");
            Assert.That(modeTimer, Is.LessThan(5f), "Fast rover speed should shorten long idle waits once the player nears the fight space.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void SlowPlayerOutsideCombatBoundary_StaysControlledUntilCloser()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonPlayerInsideZoneField, Is.Not.Null, "DragonAttack.playerInsideEngagementZone not found");
        Assert.That(DragonPlayerWithinAwarenessZoneField, Is.Not.Null, "DragonAttack.playerWithinAwarenessZone not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(60f, 0f, 0f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-20f, 0f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"SlowWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);
            DragonAlertUntilField?.SetValue(null, 0f);
            DragonModeTimerField.SetValue(dragonAttack, 9f);
            DragonSmoothedPlayerSpeedField.SetValue(dragonAttack, 0f);
            DragonLastPlayerPositionField.SetValue(dragonAttack, player.transform.position);
            DragonHasPlayerPositionSampleField.SetValue(dragonAttack, true);

            DragonUpdateMethod.Invoke(dragonAttack, null);

            bool playerInsideZone = (bool)DragonPlayerInsideZoneField.GetValue(dragonAttack);
            bool playerWithinAwarenessZone = (bool)DragonPlayerWithinAwarenessZoneField.GetValue(dragonAttack);
            string currentMode = DragonModeField.GetValue(dragonAttack).ToString();

            Assert.That(playerInsideZone, Is.False, "This setup should keep the player outside the strict combat footprint.");
            Assert.That(playerWithinAwarenessZone, Is.False, "At low speed, dragons should not overreact to a player who is still comfortably outside the combat boundary.");
            Assert.That(currentMode, Is.EqualTo("SkyPatrol"), "Slow movement outside the fight space should leave dragons in their normal patrol state instead of prematurely switching into combat.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void FastMovingPlayer_BypassesGraceDelayAndForcesEarlyCombatAlert()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(DragonGraceExpiredField, Is.Not.Null, "DragonAttack.s_graceExpired not found");
        Assert.That(DragonSmoothedPlayerSpeedField, Is.Not.Null, "DragonAttack.smoothedPlayerSpeed not found");
        Assert.That(DragonLastPlayerPositionField, Is.Not.Null, "DragonAttack.lastPlayerPosition not found");
        Assert.That(DragonHasPlayerPositionSampleField, Is.Not.Null, "DragonAttack.hasPlayerPositionSample not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(10f, 0f, 10f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 0f, -20f),
                new Vector3(20f, 0f, -20f),
                new Vector3(20f, 0f, 20f),
                new Vector3(-20f, 0f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"SpeedWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);

            DragonGraceExpiredField.SetValue(null, false);
            DragonModeTimerField.SetValue(dragonAttack, 8f);
            DragonSmoothedPlayerSpeedField.SetValue(dragonAttack, 18f);
            DragonLastPlayerPositionField.SetValue(dragonAttack, player.transform.position);
            DragonHasPlayerPositionSampleField.SetValue(dragonAttack, true);

            DragonUpdateMethod.Invoke(dragonAttack, null);

            float modeTimer = (float)DragonModeTimerField.GetValue(dragonAttack);
            Assert.That(modeTimer, Is.LessThan(5f),
                "A fast-moving player should trigger an early combat response even before the normal grace timer expires.");
            Assert.That(patrol.enabled, Is.False,
                "High player speed should force target dragons out of passive patrol and into combat shadowing immediately.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TargetWithoutAuthoredWaypoints_GeneratesFallbackPatrolRouteNearItsSpawn()
    {
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PatrolStartMethod, Is.Not.Null, "FlyingCreaturePatrol.Start not found");
        Assert.That(PatrolWaypointPositionsField, Is.Not.Null, "FlyingCreaturePatrol.waypointPositions not found");

        GameObject target = new GameObject("Target_E");
        Behaviour patrol = null;

        try
        {
            target.transform.position = new Vector3(315f, -309f, 725f);
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            PatrolStartMethod.Invoke(patrol, null);

            IList waypointPositions = PatrolWaypointPositionsField.GetValue(patrol) as IList;
            Assert.That(waypointPositions, Is.Not.Null);
            Assert.That(waypointPositions.Count, Is.GreaterThanOrEqualTo(4),
                "Targets without authored Target_*_WP* objects still need a fallback patrol route so they do not freeze in place.");

            float farthestDistance = 0f;
            foreach (object entry in waypointPositions)
            {
                Vector3 waypoint = (Vector3)entry;
                farthestDistance = Mathf.Max(farthestDistance, Vector3.Distance(target.transform.position, waypoint));
                Assert.That(Mathf.Abs(waypoint.y - target.transform.position.y), Is.LessThan(20f),
                    "Fallback target patrol waypoints should stay near the dragon's authored flying band instead of using the generic world-space height defaults.");
            }

            Assert.That(farthestDistance, Is.GreaterThan(10f),
                "Fallback patrol generation should create a real local loop, not collapse all waypoints back onto the dragon's spawn position.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(target);
        }
    }

    [Test]
    public void TargetAttackRun_TransitionsToRetreatInsteadOfHovering()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonModeField, Is.Not.Null, "DragonAttack.mode not found");
        Assert.That(DragonModeTimerField, Is.Not.Null, "DragonAttack.modeTimer not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(10f, 0f, 10f);

            target.transform.position = new Vector3(12f, 2f, 14f);
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, 20f, -20f),
                new Vector3(40f, 25f, -10f),
                new Vector3(35f, 28f, 40f),
                new Vector3(-15f, 24f, 35f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"RetreatWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);

            object attackMode = Enum.Parse(DragonModeField.FieldType, "Attack");
            DragonModeField.SetValue(dragonAttack, attackMode);
            DragonModeTimerField.SetValue(dragonAttack, 0f);

            DragonUpdateMethod.Invoke(dragonAttack, null);

            string currentMode = DragonModeField.GetValue(dragonAttack).ToString();
            Assert.That(currentMode, Is.EqualTo("Retreat"),
                "Target dragons should leave the player after an attack pass instead of hovering in front of the player indefinitely.");
            Assert.That(patrol.enabled, Is.False, "The retreat leg should remain under DragonAttack control until the dragon reaches its patrol space again.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TargetWithWaypointZone_PreservesScenePlacementOnStart()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];
        Vector3 initialPosition = new Vector3(240f, -300f, 640f);

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(100f, 30f, 190f);

            target.transform.position = initialPosition;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(220f, -320f, 620f),
                new Vector3(260f, -310f, 660f),
                new Vector3(250f, -295f, 700f),
                new Vector3(210f, -305f, 675f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"WP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);

            Assert.That(Vector3.Distance(target.transform.position, initialPosition), Is.LessThan(0.001f),
                "Target dragons with an authored waypoint engagement zone should keep their scene placement instead of snapping to a player orbit on Start.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TargetOutsideWaypointZone_SnapsToNearestWaypointOnStart()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];
        Vector3 initialPosition = new Vector3(285f, 5f, 274f);

        Vector3[] waypointPositions =
        {
            new Vector3(40f, -332f, 500f),
            new Vector3(125f, -300f, 460f),
            new Vector3(205f, -315f, 540f),
            new Vector3(95f, -288f, 620f)
        };

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(100f, 30f, 190f);

            target.transform.position = initialPosition;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"WP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);

            float nearestWaypointDistance = float.MaxValue;
            foreach (Vector3 waypointPosition in waypointPositions)
                nearestWaypointDistance = Mathf.Min(nearestWaypointDistance, Vector3.Distance(target.transform.position, waypointPosition));

            Assert.That(nearestWaypointDistance, Is.LessThan(0.001f),
                "Target dragons that start far outside their authored waypoint volume should snap into that patrol zone instead of spending runtime descending from an unrelated prefab root position.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void TargetAboveWaypointZone_StillCountsAsInsideAggressionFootprint()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonPlayerInsideZoneField, Is.Not.Null, "DragonAttack.playerInsideEngagementZone not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject player = new GameObject("Player");
        GameObject cameraChild = new GameObject("PlayerCamera");
        GameObject target = new GameObject("Target_A");
        Behaviour patrol = null;
        Component dragonAttack = null;
        Transform[] waypoints = new Transform[4];

        try
        {
            player.AddComponent(PlayerHealthType);
            cameraChild.transform.SetParent(player.transform, false);
            cameraChild.AddComponent<Camera>();
            player.transform.position = new Vector3(10f, 30f, 10f);

            target.transform.position = Vector3.zero;
            target.AddComponent<SphereCollider>();

            patrol = target.AddComponent(FlyingCreaturePatrolType) as Behaviour;
            dragonAttack = target.AddComponent(DragonAttackType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(-20f, -25f, -20f),
                new Vector3(20f, -20f, -20f),
                new Vector3(20f, -18f, 20f),
                new Vector3(-20f, -22f, 20f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"WP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            DragonOnEnableMethod.Invoke(dragonAttack, null);
            DragonStartMethod.Invoke(dragonAttack, null);
            DragonUpdateMethod.Invoke(dragonAttack, null);

            bool playerInsideZone = (bool)DragonPlayerInsideZoneField.GetValue(dragonAttack);
            Assert.That(playerInsideZone, Is.True,
                "Players above the target waypoint volume but inside its XZ footprint should still trigger target aggression.");
        }
        finally
        {
            if (dragonAttack != null && DragonOnDisableMethod != null)
                DragonOnDisableMethod.Invoke(dragonAttack, null);

            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void DragonProjectileHit_PlayerCharacterController_DamagesPlayerHealth()
    {
        Type dragonProjectileType = Type.GetType("DragonProjectile, Assembly-CSharp");

        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(dragonProjectileType, Is.Not.Null, "DragonProjectile type not found");
        Assert.That(ProjectileOnTriggerEnterMethod, Is.Not.Null, "DragonProjectile.OnTriggerEnter not found");

        GameObject player = new GameObject("Player");
        GameObject projectile = new GameObject("Projectile");

        try
        {
            var health = player.AddComponent(PlayerHealthType);
            PlayerHealthType.GetField("maxHealth", BindingFlags.Instance | BindingFlags.Public)?.SetValue(health, 100f);
            PlayerHealthType.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public)?.SetValue(health, 100f);
            CharacterController controller = player.AddComponent<CharacterController>();
            controller.radius = 0.4f;
            controller.height = 1.7f;

            projectile.AddComponent<SphereCollider>().isTrigger = true;
            projectile.AddComponent<Rigidbody>().useGravity = false;
            Component dragonProjectile = projectile.AddComponent(dragonProjectileType);

            ProjectileOnTriggerEnterMethod.Invoke(dragonProjectile, new object[] { controller });

            float currentHealth = (float)PlayerHealthType.GetField("currentHealth", BindingFlags.Instance | BindingFlags.Public).GetValue(health);
            Assert.That(currentHealth, Is.EqualTo(80f).Within(0.001f),
                "DragonProjectile should damage PlayerHealth when the projectile overlaps the player's CharacterController collider.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(projectile);
            UnityEngine.Object.DestroyImmediate(player);
        }
    }

    [Test]
    public void PatrolStartingOnWaypoint_AdvancesToNextWaypointInsteadOfHovering()
    {
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PatrolStartMethod, Is.Not.Null, "FlyingCreaturePatrol.Start not found");
        Assert.That(PatrolUpdateMethod, Is.Not.Null, "FlyingCreaturePatrol.Update not found");
        Assert.That(PatrolCurrentWaypointIndexProperty, Is.Not.Null, "FlyingCreaturePatrol.CurrentWaypointIndex not found");
        Assert.That(PatrolWaypointsField, Is.Not.Null, "FlyingCreaturePatrol.waypoints not found");

        GameObject creature = new GameObject("Target_A");
        Transform[] waypoints = new Transform[4];

        try
        {
            creature.transform.position = new Vector3(10f, 5f, 10f);
            creature.AddComponent<SphereCollider>();
            Component patrol = creature.AddComponent(FlyingCreaturePatrolType);

            IList patrolWaypoints = PatrolWaypointsField.GetValue(patrol) as IList;
            Assert.That(patrolWaypoints, Is.Not.Null);

            Vector3[] waypointPositions =
            {
                new Vector3(10f, 5f, 10f),
                new Vector3(25f, 5f, 10f),
                new Vector3(25f, 5f, 25f),
                new Vector3(10f, 5f, 25f)
            };

            for (int i = 0; i < waypointPositions.Length; i++)
            {
                GameObject waypoint = new GameObject($"PatrolWP_{i}");
                waypoint.transform.position = waypointPositions[i];
                waypoints[i] = waypoint.transform;
                patrolWaypoints.Add(waypoints[i]);
            }

            PatrolStartMethod.Invoke(patrol, null);
            PatrolUpdateMethod.Invoke(patrol, null);

            int currentWaypointIndex = (int)PatrolCurrentWaypointIndexProperty.GetValue(patrol);
            Assert.That(currentWaypointIndex, Is.EqualTo(1),
                "Creatures that spawn directly on a patrol waypoint should advance to the next waypoint instead of hovering in place and only bobbing vertically.");
        }
        finally
        {
            foreach (Transform waypoint in waypoints)
            {
                if (waypoint != null)
                    UnityEngine.Object.DestroyImmediate(waypoint.gameObject);
            }

            UnityEngine.Object.DestroyImmediate(creature);
        }
    }
}
