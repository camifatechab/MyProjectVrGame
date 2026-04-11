using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class FlyingCreaturePatrolTests
{
    private static readonly Type FlyingCreaturePatrolType = Type.GetType("FlyingCreaturePatrol, Assembly-CSharp");

    private static readonly MethodInfo RaiseAboveTerrainMethod =
        FlyingCreaturePatrolType?.GetMethod("RaiseAboveTerrain", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo OnValidateMethod =
        FlyingCreaturePatrolType?.GetMethod("OnValidate", BindingFlags.Instance | BindingFlags.NonPublic);

    [Test]
    public void RaiseAboveTerrain_UsesColliderRadiusForTerrainClearance()
    {
        Assert.That(FlyingCreaturePatrolType, Is.Not.Null, "FlyingCreaturePatrol type not found");
        Assert.That(RaiseAboveTerrainMethod, Is.Not.Null, "FlyingCreaturePatrol.RaiseAboveTerrain not found");
        Assert.That(OnValidateMethod, Is.Not.Null, "FlyingCreaturePatrol.OnValidate not found");

        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        GameObject dragon = new GameObject("Target_A");

        try
        {
            ground.transform.position = Vector3.zero;

            CapsuleCollider capsuleCollider = dragon.AddComponent<CapsuleCollider>();
            capsuleCollider.radius = 5f;
            capsuleCollider.height = 10f;

            Component patrol = dragon.AddComponent(FlyingCreaturePatrolType);
            OnValidateMethod.Invoke(patrol, null);

            Vector3 raisedPoint = (Vector3)RaiseAboveTerrainMethod.Invoke(
                patrol,
                new object[] { new Vector3(0f, 1f, 0f) });

            float minimumExpectedHeight = capsuleCollider.radius + 0.05f;

            Assert.That(
                raisedPoint.y + 0.001f,
                Is.GreaterThanOrEqualTo(minimumExpectedHeight),
                "Generated patrol points should clear the terrain by at least the dragon collider radius.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(dragon);
            UnityEngine.Object.DestroyImmediate(ground);
        }
    }
}
