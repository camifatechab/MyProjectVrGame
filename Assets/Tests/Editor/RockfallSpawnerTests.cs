using System;
using System.Collections;
using System.Reflection;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public class RockfallSpawnerTests
{
    private static readonly Type RockfallSpawnerType = Type.GetType("RockfallSpawner, Assembly-CSharp");

    private static readonly MethodInfo CreatePoolMethod =
        RockfallSpawnerType?.GetMethod("CreatePool", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly MethodInfo DropRockMethod =
        RockfallSpawnerType?.GetMethod("DropRock", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo PoolField =
        RockfallSpawnerType?.GetField("pool", BindingFlags.Instance | BindingFlags.NonPublic);

    private static readonly FieldInfo RockPrefabField =
        RockfallSpawnerType?.GetField("rockPrefab", BindingFlags.Instance | BindingFlags.Public);

    private static readonly FieldInfo PoolSizeField =
        RockfallSpawnerType?.GetField("poolSize", BindingFlags.Instance | BindingFlags.Public);

    [Test]
    public void DropRock_AddsRequiredPhysicsComponentsToRockPrefab()
    {
        Assert.That(RockfallSpawnerType, Is.Not.Null);
        Assert.That(CreatePoolMethod, Is.Not.Null);
        Assert.That(DropRockMethod, Is.Not.Null);
        Assert.That(PoolField, Is.Not.Null);
        Assert.That(RockPrefabField, Is.Not.Null);
        Assert.That(PoolSizeField, Is.Not.Null);

        GameObject rockPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/LowPoly Environment Pack/Prefabs/Rock_5.prefab");

        Assert.That(rockPrefab, Is.Not.Null);
        Assert.That(rockPrefab.GetComponent<Rigidbody>(), Is.Null);

        GameObject spawnerObject = new GameObject("RockfallSpawnerTest");
        GameObject spawnedRock = null;

        try
        {
            Component spawner = spawnerObject.AddComponent(RockfallSpawnerType);
            RockPrefabField.SetValue(spawner, rockPrefab);
            PoolSizeField.SetValue(spawner, 1);

            CreatePoolMethod.Invoke(spawner, null);
            Assert.DoesNotThrow(() => DropRockMethod.Invoke(spawner, null));

            spawnedRock = ((GameObject[])PoolField.GetValue(spawner))[0];

            Assert.That(spawnedRock, Is.Not.Null);
            Assert.That(spawnedRock.activeSelf, Is.True);
            Assert.That(spawnedRock.TryGetComponent(out Rigidbody rigidbody), Is.True);

            MeshCollider meshCollider = spawnedRock.GetComponent<MeshCollider>();
            Assert.That(meshCollider, Is.Not.Null);
            Assert.That(meshCollider.convex, Is.True);
        }
        finally
        {
            if (spawnedRock != null)
                UnityEngine.Object.DestroyImmediate(spawnedRock);

            UnityEngine.Object.DestroyImmediate(spawnerObject);
        }
    }
}

public class BatteryPickupTests
{
    private static readonly Type BatteryPickupType = Type.GetType("BatteryPickup, Assembly-CSharp");

    private static readonly MethodInfo RespawnRoutineMethod =
        BatteryPickupType?.GetMethod("RespawnRoutine", BindingFlags.Instance | BindingFlags.NonPublic);

    [Test]
    public void RespawnRoutine_DisablesAndReenablesAllPrefabRenderers()
    {
        Assert.That(BatteryPickupType, Is.Not.Null, "BatteryPickup type not found");
        Assert.That(RespawnRoutineMethod, Is.Not.Null, "BatteryPickup.RespawnRoutine not found");

        GameObject batteryInstance = new GameObject("BatteryPickupTest");
        batteryInstance.AddComponent<CapsuleCollider>();

        GameObject visualChild = new GameObject("Visual");
        visualChild.transform.SetParent(batteryInstance.transform);
        visualChild.AddComponent<ParticleSystem>();

        try
        {
            Component batteryPickup = batteryInstance.AddComponent(BatteryPickupType);
            IEnumerator routine = (IEnumerator)RespawnRoutineMethod.Invoke(batteryPickup, null);
            Renderer[] renderers = batteryInstance.GetComponentsInChildren<Renderer>(true);
            Collider collider = batteryInstance.GetComponent<Collider>();

            Assert.That(collider, Is.Not.Null, "Battery instance has no collider");
            Assert.That(renderers.Length, Is.GreaterThan(0), "Battery instance has no renderers");
            Assert.That(batteryInstance.GetComponentInChildren<MeshRenderer>(true), Is.Null, "Test setup unexpectedly has a MeshRenderer");
            Assert.That(renderers[0].enabled, Is.True, "Battery renderer should start enabled");

            Assert.DoesNotThrow(() => routine.MoveNext());
            Assert.That(collider.enabled, Is.False);
            Assert.That(Array.TrueForAll(renderers, renderer => !renderer.enabled), Is.True);

            Assert.DoesNotThrow(() => routine.MoveNext());
            Assert.That(collider.enabled, Is.True);
            Assert.That(Array.TrueForAll(renderers, renderer => renderer.enabled), Is.True);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(batteryInstance);
        }
    }
}
