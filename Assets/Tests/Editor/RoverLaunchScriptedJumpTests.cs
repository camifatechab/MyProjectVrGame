using System;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public class RoverLaunchScriptedJumpTests
{
    private static readonly Type LaunchTriggerType = Type.GetType("RoverLaunchTrigger, Assembly-CSharp");
    private static readonly Type RoverPhysicsControllerType = Type.GetType("RoverPhysicsController, Assembly-CSharp");
    private static readonly Type RoverAirborneStabilizerType = Type.GetType("RoverAirborneStabilizer, Assembly-CSharp");

    [Test]
    public void BallisticSolver_ReturnsExpectedVelocityForKnownOriginTargetPitch()
    {
        Assert.That(LaunchTriggerType, Is.Not.Null, "RoverLaunchTrigger type not found");

        MethodInfo solver = LaunchTriggerType.GetMethod(
            "TrySolveBallisticVelocity",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(solver, Is.Not.Null, "RoverLaunchTrigger.TrySolveBallisticVelocity not found");

        Vector3 origin = Vector3.zero;
        Vector3 target = new Vector3(20f, 2f, 0f);
        float pitchDegrees = 20f;
        float gravity = 9.81f;

        object[] args = { origin, target, pitchDegrees, gravity, Vector3.zero };
        bool solved = (bool)solver.Invoke(null, args);
        Vector3 launchVelocity = (Vector3)args[4];

        Assert.That(solved, Is.True, "Expected ballistic solve to succeed for known geometry.");

        float pitchRadians = pitchDegrees * Mathf.Deg2Rad;
        float cos = Mathf.Cos(pitchRadians);
        float horizontalDistance = 20f;
        float denominator = 2f * cos * cos * (horizontalDistance * Mathf.Tan(pitchRadians) - 2f);
        float expectedSpeed = Mathf.Sqrt(gravity * horizontalDistance * horizontalDistance / denominator);
        Vector3 expectedVelocity = new Vector3(
            expectedSpeed * cos,
            expectedSpeed * Mathf.Sin(pitchRadians),
            0f);

        Assert.That(Vector3.Distance(launchVelocity, expectedVelocity), Is.LessThan(0.001f));

        float flightTime = horizontalDistance / launchVelocity.x;
        float reachedHeight = launchVelocity.y * flightTime - 0.5f * gravity * flightTime * flightTime;
        Assert.That(reachedHeight, Is.EqualTo(target.y).Within(0.01f));
    }

    [Test]
    public void BallisticSolver_ReturnsFalseForUnreachableTargetAtPitch()
    {
        Assert.That(LaunchTriggerType, Is.Not.Null, "RoverLaunchTrigger type not found");

        MethodInfo solver = LaunchTriggerType.GetMethod(
            "TrySolveBallisticVelocity",
            BindingFlags.Public | BindingFlags.Static);

        Assert.That(solver, Is.Not.Null, "RoverLaunchTrigger.TrySolveBallisticVelocity not found");

        object[] args = { Vector3.zero, new Vector3(5f, 50f, 0f), 15f, 9.81f, Vector3.zero };
        bool solved = (bool)solver.Invoke(null, args);
        Vector3 launchVelocity = (Vector3)args[4];

        Assert.That(solved, Is.False, "Expected solve to fail for unreachable geometry at fixed pitch.");
        Assert.That(launchVelocity, Is.EqualTo(Vector3.zero));
        Assert.That(
            !float.IsNaN(launchVelocity.x) &&
            !float.IsNaN(launchVelocity.y) &&
            !float.IsNaN(launchVelocity.z) &&
            !float.IsInfinity(launchVelocity.x) &&
            !float.IsInfinity(launchVelocity.y) &&
            !float.IsInfinity(launchVelocity.z),
            Is.True,
            "Fallback output must remain finite.");
    }

    [Test]
    public void ScriptedLaunchMode_DisablesForces_AndRestoresAfterEndOrTimeout()
    {
        Assert.That(RoverPhysicsControllerType, Is.Not.Null, "RoverPhysicsController type not found");
        Assert.That(RoverAirborneStabilizerType, Is.Not.Null, "RoverAirborneStabilizer type not found");

        GameObject rover = new GameObject("ScriptedLaunchRover");
        GameObject fl = new GameObject("WheelFL");
        GameObject fr = new GameObject("WheelFR");
        GameObject rl = new GameObject("WheelRL");
        GameObject rr = new GameObject("WheelRR");

        try
        {
            rover.AddComponent<Rigidbody>();
            Component controller = rover.AddComponent(RoverPhysicsControllerType);
            Component stabilizer = rover.AddComponent(RoverAirborneStabilizerType);
            ((Behaviour)stabilizer).enabled = true;

            WheelCollider wheelFL = fl.AddComponent<WheelCollider>();
            WheelCollider wheelFR = fr.AddComponent<WheelCollider>();
            WheelCollider wheelRL = rl.AddComponent<WheelCollider>();
            WheelCollider wheelRR = rr.AddComponent<WheelCollider>();

            RoverPhysicsControllerType.GetField("wheelFL", BindingFlags.Instance | BindingFlags.Public)?.SetValue(controller, wheelFL);
            RoverPhysicsControllerType.GetField("wheelFR", BindingFlags.Instance | BindingFlags.Public)?.SetValue(controller, wheelFR);
            RoverPhysicsControllerType.GetField("wheelRL", BindingFlags.Instance | BindingFlags.Public)?.SetValue(controller, wheelRL);
            RoverPhysicsControllerType.GetField("wheelRR", BindingFlags.Instance | BindingFlags.Public)?.SetValue(controller, wheelRR);

            MethodInfo begin = RoverPhysicsControllerType.GetMethod("BeginScriptedLaunch", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo end = RoverPhysicsControllerType.GetMethod("EndScriptedLaunch", BindingFlags.Instance | BindingFlags.Public);
            MethodInfo fixedUpdate = RoverPhysicsControllerType.GetMethod("FixedUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo activeProperty = RoverPhysicsControllerType.GetProperty("IsScriptedLaunchActive", BindingFlags.Instance | BindingFlags.Public);

            Assert.That(begin, Is.Not.Null);
            Assert.That(end, Is.Not.Null);
            Assert.That(fixedUpdate, Is.Not.Null);
            Assert.That(activeProperty, Is.Not.Null);

            wheelFL.motorTorque = 10f;
            wheelFR.motorTorque = 10f;
            wheelRL.motorTorque = 10f;
            wheelRR.motorTorque = 10f;
            wheelFL.brakeTorque = 40f;
            wheelFR.brakeTorque = 40f;
            wheelRL.brakeTorque = 40f;
            wheelRR.brakeTorque = 40f;

            begin.Invoke(controller, new object[] { 0.5f });
            Assert.That((bool)activeProperty.GetValue(controller), Is.True);
            Assert.That(((Behaviour)stabilizer).enabled, Is.False, "Scripted launch should disable RoverAirborneStabilizer.");

            fixedUpdate.Invoke(controller, null);

            Assert.That(wheelFL.motorTorque, Is.EqualTo(0f));
            Assert.That(wheelFR.motorTorque, Is.EqualTo(0f));
            Assert.That(wheelRL.motorTorque, Is.EqualTo(0f));
            Assert.That(wheelRR.motorTorque, Is.EqualTo(0f));
            Assert.That(wheelFL.brakeTorque, Is.EqualTo(0f));
            Assert.That(wheelFR.brakeTorque, Is.EqualTo(0f));
            Assert.That(wheelRL.brakeTorque, Is.EqualTo(0f));
            Assert.That(wheelRR.brakeTorque, Is.EqualTo(0f));

            end.Invoke(controller, null);
            Assert.That((bool)activeProperty.GetValue(controller), Is.False);
            Assert.That(((Behaviour)stabilizer).enabled, Is.True, "Scripted launch end should restore RoverAirborneStabilizer.");

            begin.Invoke(controller, new object[] { 0.001f });
            fixedUpdate.Invoke(controller, null);
            Assert.That((bool)activeProperty.GetValue(controller), Is.False, "Scripted launch should exit on timeout.");
            Assert.That(((Behaviour)stabilizer).enabled, Is.True, "Timeout exit should restore RoverAirborneStabilizer.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(fl);
            UnityEngine.Object.DestroyImmediate(fr);
            UnityEngine.Object.DestroyImmediate(rl);
            UnityEngine.Object.DestroyImmediate(rr);
            UnityEngine.Object.DestroyImmediate(rover);
        }
    }
}
