using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class WaypointEnvironmentTransitionTests
{
    private static readonly Type TransitionType =
        Type.GetType("WaypointEnvironmentTransition, Assembly-CSharp");

    private static readonly MethodInfo EvaluateBlendWindowMethod =
        TransitionType?.GetMethod("EvaluateBlendWindow", BindingFlags.Public | BindingFlags.Static);

    [Test]
    public void EvaluateBlendWindow_BeforeDangerApproach_ReturnsZero()
    {
        Assert.That(EvaluateBlendWindowMethod, Is.Not.Null, "WaypointEnvironmentTransition.EvaluateBlendWindow not found");

        float blend = InvokeEvaluateBlendWindow(
            5,
            new Vector3(100f, 40f, 190f),
            new Vector3(101f, 32f, 153f),
            new Vector3(102f, 7f, 112f),
            new Vector3(-4f, 38f, 161f),
            new Vector3(115f, 38f, 231f));

        Assert.That(blend, Is.EqualTo(0f).Within(0.0001f));
    }

    [Test]
    public void EvaluateBlendWindow_UsesLateWp06ToWp09Runway()
    {
        float wp07Blend = InvokeEvaluateBlendWindow(
            7,
            new Vector3(60f, 20f, 135f),
            new Vector3(101f, 32f, 153f),
            new Vector3(102f, 7f, 112f),
            new Vector3(-4f, 38f, 161f),
            new Vector3(115f, 38f, 231f));

        float wp08Blend = InvokeEvaluateBlendWindow(
            8,
            new Vector3(70f, 38f, 205f),
            new Vector3(101f, 32f, 153f),
            new Vector3(102f, 7f, 112f),
            new Vector3(-4f, 38f, 161f),
            new Vector3(115f, 38f, 231f));

        Assert.That(wp07Blend, Is.GreaterThan(0.05f));
        Assert.That(wp08Blend, Is.GreaterThan(wp07Blend));
        Assert.That(wp08Blend, Is.LessThan(1f));
    }

    [Test]
    public void EvaluateBlendWindow_ReachesFullRedByWp09()
    {
        float blend = InvokeEvaluateBlendWindow(
            9,
            new Vector3(115f, 38f, 231f),
            new Vector3(101f, 32f, 153f),
            new Vector3(102f, 7f, 112f),
            new Vector3(-4f, 38f, 161f),
            new Vector3(115f, 38f, 231f));

        Assert.That(blend, Is.EqualTo(1f).Within(0.0001f));
    }

    private static float InvokeEvaluateBlendWindow(
        int waypointNumber,
        Vector3 currentPosition,
        Vector3 blendStartPosition,
        Vector3 waypoint07Position,
        Vector3 waypoint08Position,
        Vector3 waypoint09Position)
    {
        Assert.That(EvaluateBlendWindowMethod, Is.Not.Null, "WaypointEnvironmentTransition.EvaluateBlendWindow not found");

        object result = EvaluateBlendWindowMethod.Invoke(null, new object[]
        {
            waypointNumber,
            currentPosition,
            blendStartPosition,
            waypoint07Position,
            waypoint08Position,
            waypoint09Position
        });

        return result is float blend ? blend : 0f;
    }
}
