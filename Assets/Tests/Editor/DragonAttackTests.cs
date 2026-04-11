using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class DragonAttackTests
{
    private static readonly Type DragonAttackType = Type.GetType("DragonAttack, Assembly-CSharp");
    private static readonly Type FlyingCreaturePatrolType = Type.GetType("FlyingCreaturePatrol, Assembly-CSharp");
    private static readonly Type PlayerHealthType = Type.GetType("PlayerHealth, Assembly-CSharp");

    private static readonly MethodInfo DragonOnEnableMethod =
        DragonAttackType?.GetMethod("OnEnable", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonOnDisableMethod =
        DragonAttackType?.GetMethod("OnDisable", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonStartMethod =
        DragonAttackType?.GetMethod("Start", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DragonUpdateMethod =
        DragonAttackType?.GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonModeField =
        DragonAttackType?.GetField("mode", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo DragonPlayerInsideZoneField =
        DragonAttackType?.GetField("playerInsideEngagementZone", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo GetCurrentAttackRangeMethod =
        DragonAttackType?.GetMethod("GetCurrentAttackRange", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo GetCurrentMinDistanceMethod =
        DragonAttackType?.GetMethod("GetCurrentMinDistance", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo PatrolWaypointsField =
        FlyingCreaturePatrolType?.GetField("waypoints", BindingFlags.Instance | BindingFlags.Public);

    [Test]
    public void TargetInsideWaypointZone_DisablesPatrolAndEntersAttackCycle()
    {
        Assert.That(DragonAttackType, Is.Not.Null, "DragonAttack type not found");
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(PlayerHealthType, Is.Not.Null, "PlayerHealth type not found");
        Assert.That(DragonOnEnableMethod, Is.Not.Null, "DragonAttack.OnEnable not found");
        Assert.That(DragonStartMethod, Is.Not.Null, "DragonAttack.Start not found");
        Assert.That(DragonUpdateMethod, Is.Not.Null, "DragonAttack.Update not found");
        Assert.That(DragonModeField, Is.Not.Null, "DragonAttack.mode not found");
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
            float attackRange = (float)GetCurrentAttackRangeMethod.Invoke(dragonAttack, null);
            float minDistance = (float)GetCurrentMinDistanceMethod.Invoke(dragonAttack, null);

            Assert.That(playerInsideZone, Is.True, "Player should be recognized inside the engagement zone built from patrol waypoints.");
            Assert.That(patrol.enabled, Is.False, "Target patrol should stop once the player enters the engagement zone.");
            Assert.That(currentMode, Is.Not.EqualTo("SkyPatrol"), "Target should immediately leave passive sky patrol when the player enters the zone.");
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
}
