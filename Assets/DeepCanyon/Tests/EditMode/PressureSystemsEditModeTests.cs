using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class PressureSystemsEditModeTests
{
    GameObject root;
    DepthTracker depthTracker;
    BuoyancySystem buoyancySystem;
    HullIntegrity hullIntegrity;
    Rigidbody rb;

    [SetUp]
    public void SetUp()
    {
        root = new GameObject("BathyscapheTestRoot");
        rb = root.AddComponent<Rigidbody>();
        rb.useGravity = false;
        depthTracker = root.AddComponent<DepthTracker>();
        buoyancySystem = root.AddComponent<BuoyancySystem>();
        hullIntegrity = root.AddComponent<HullIntegrity>();

        InvokePrivateMethod(depthTracker, "Update");
        InvokePrivateMethod(buoyancySystem, "Awake");
        InvokePrivateMethod(hullIntegrity, "Awake");
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }
    }

    [UnityTest]
    public IEnumerator DepthTracker_ReportsZeroPressure_AtSurface()
    {
        root.transform.position = Vector3.zero;
        InvokePrivateMethod(depthTracker, "Update");

        yield return null;

        Assert.That(depthTracker.CurrentDepth, Is.EqualTo(0f).Within(0.001f));
        Assert.That(depthTracker.NormalizedPressure, Is.EqualTo(0f).Within(0.001f));
        Assert.That(depthTracker.PressureCoefficient, Is.EqualTo(1f).Within(0.001f));
    }

    [UnityTest]
    public IEnumerator DepthTracker_IncreasesPressure_WithDepth()
    {
        root.transform.position = new Vector3(0f, -125f, 0f);
        InvokePrivateMethod(depthTracker, "Update");

        yield return null;

        Assert.That(depthTracker.CurrentDepth, Is.EqualTo(-125f).Within(0.001f));
        Assert.That(depthTracker.NormalizedPressure, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(depthTracker.PressureCoefficient, Is.GreaterThan(1.9f));
    }

    [UnityTest]
    public IEnumerator BuoyancySystem_DoesNotAccelerateUpward_AboveSurface()
    {
        root.transform.position = new Vector3(0f, 10f, 0f);
        rb.linearVelocity = new Vector3(0f, 8f, 0f);
        InvokePrivateMethod(depthTracker, "Update");
        InvokePrivateMethod(buoyancySystem, "FixedUpdate");

        yield return null;

        Assert.That(rb.linearVelocity.y, Is.LessThan(8f));
    }

    [UnityTest]
    public IEnumerator HullIntegrity_DamagesHull_WhenBelowSafeDepth()
    {
        root.transform.position = new Vector3(0f, -120f, 0f);
        float before = hullIntegrity.CurrentHull;
        InvokePrivateMethod(depthTracker, "Update");
        InvokePrivateMethod(hullIntegrity, "ApplyPressureStep", 1f);

        yield return null;

        Assert.That(hullIntegrity.CurrentHull, Is.LessThan(before));
    }

    [UnityTest]
    public IEnumerator HullIntegrity_BoostsLeakEmission_WhenHullIsCriticalAndPressureIsHigh()
    {
        var leaks = new ParticleSystem[4];
        for (int i = 0; i < leaks.Length; i++)
        {
            leaks[i] = new GameObject($"Leak_{i}").AddComponent<ParticleSystem>();
            leaks[i].transform.SetParent(root.transform);
            var emission = leaks[i].emission;
            emission.rateOverTime = 10f;
        }

        SetPrivateField(hullIntegrity, "leakEffects", leaks);
        SetPrivateField(hullIntegrity, "currentHull", hullIntegrity.MaxHull * 0.2f);
        root.transform.position = new Vector3(0f, -220f, 0f);
        InvokePrivateMethod(depthTracker, "Update");
        InvokePrivateMethod(hullIntegrity, "Update");

        yield return null;

        Assert.That(leaks[0].isPlaying, Is.True);
        Assert.That(leaks[0].emission.rateOverTime.constant, Is.GreaterThan(10f));
    }

    static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.That(field, Is.Not.Null, $"Field '{fieldName}' was not found on {target.GetType().Name}.");
        field.SetValue(target, value);
    }

    static void InvokePrivateMethod(object target, string methodName)
    {
        var method = target.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found on {target.GetType().Name}.");
        method.Invoke(target, null);
    }

    static void InvokePrivateMethod(object target, string methodName, params object[] args)
    {
        var argTypes = new System.Type[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            argTypes[i] = args[i].GetType();
        }

        var method = target.GetType().GetMethod(
            methodName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null,
            argTypes,
            null);

        Assert.That(method, Is.Not.Null, $"Method '{methodName}' was not found on {target.GetType().Name}.");
        method.Invoke(target, args);
    }
}
