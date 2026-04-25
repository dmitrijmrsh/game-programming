using NUnit.Framework;
using UnityEngine;
using UnityEngine.VFX;

public class CurrentZoneVisualsEditModeTests
{
    GameObject root;

    [TearDown]
    public void TearDown()
    {
        if (root != null)
        {
            Object.DestroyImmediate(root);
        }

        var sceneSystems = Object.FindAnyObjectByType<CurrentField>();
        if (sceneSystems != null)
        {
            Object.DestroyImmediate(sceneSystems.gameObject);
        }

        var builtZone = GameObject.Find("Current_Z2");
        if (builtZone != null)
        {
            Object.DestroyImmediate(builtZone.transform.root.gameObject);
        }
    }

    [Test]
    public void CurrentZone_DepthFactor_GrowsWithDepth()
    {
        var zone = BuildZone(new Vector3(0f, -200f, 0f), Vector3.right, 12f);

        Assert.That(zone.GetDepthFactor(), Is.GreaterThan(0.75f));
    }

    [Test]
    public void CurrentZone_VisualDirection_MatchesZoneDirection()
    {
        var zone = BuildZone(Vector3.zero, new Vector3(1f, 0f, 0.5f), 8f);

        AssertVectorEqual(zone.GetVisualDirection(), new Vector3(1f, 0f, 0.5f).normalized);
    }

    [Test]
    public void Binder_MapsZoneData_ToRuntimeProfile()
    {
        var zone = BuildZone(new Vector3(0f, -180f, 0f), Vector3.forward, 14f);
        var field = Object.FindAnyObjectByType<CurrentField>();
        SetPrivateField(field, "baseStrength", 0f);

        var profile = CurrentZoneVFXBinder.BuildProfile(zone, 0f);

        AssertVectorEqual(profile.FlowDirection, Vector3.forward);
        Assert.That(profile.FlowStrength, Is.GreaterThan(0f));
        Assert.That(profile.DepthFactor, Is.GreaterThan(0.7f));
    }

    [Test]
    public void Binder_PrefersRuntimeCurrentDirection_WhenFieldIsAvailable()
    {
        var zone = BuildZone(new Vector3(20f, -180f, -35f), Vector3.right, 0.5f);
        var field = Object.FindAnyObjectByType<CurrentField>();
        SetPrivateField(field, "baseStrength", 9f);
        SetPrivateField(field, "frequency", 0.07f);
        SetPrivateField(field, "drift", 0f);

        Vector3 sample = field.SampleCurrent(zone.GetWorldCenter(), 0f).normalized;
        var profile = CurrentZoneVFXBinder.BuildProfile(zone, 0f);

        AssertVectorEqual(profile.FlowDirection, sample, 0.02f);
    }

    [Test]
    public void BuildScene_CurrentZone_HasVisualEffectBinder()
    {
        DeepCanyonSceneSetup.BuildScene();

        var zone = GameObject.Find("Current_Z2");

        Assert.That(zone, Is.Not.Null);
        Assert.That(zone.GetComponentInChildren<CurrentZoneVFXBinder>(), Is.Not.Null);
    }

    [Test]
    public void BuildScene_CurrentZone_HasVisualEffectAssetAssigned()
    {
        DeepCanyonSceneSetup.BuildScene();

        var zone = GameObject.Find("Current_Z2");
        var visualEffect = zone.GetComponentInChildren<VisualEffect>();

        Assert.That(zone, Is.Not.Null);
        Assert.That(visualEffect, Is.Not.Null);
        Assert.That(visualEffect.visualEffectAsset, Is.Not.Null);
    }

    [Test]
    public void BuildScene_CurrentZone_VfxChild_DoesNotCreateNestedCurrentZone()
    {
        DeepCanyonSceneSetup.BuildScene();

        var zone = GameObject.Find("Current_Z2");
        var vfxChildren = zone.GetComponentsInChildren<VisualEffect>(true);

        Assert.That(zone, Is.Not.Null);
        Assert.That(vfxChildren.Length, Is.GreaterThan(0));

        for (int i = 0; i < vfxChildren.Length; i++)
        {
            Assert.That(vfxChildren[i].GetComponent<CurrentZone>(), Is.Null);
        }
    }

    [Test]
    public void CurrentZone_AssistZone_GetsStronger_WithSpeederEscort()
    {
        var zone = BuildZone(Vector3.zero, Vector3.forward, 10f);
        SetPrivateField(zone, "zoneRole", 1);
        SetPrivateField(zone, "roleBonusMultiplier", 1.5f);

        float baseForce = zone.GetForceAt(Vector3.zero).magnitude;
        float boostedForce = zone.GetForceAt(Vector3.zero, hasSpeederBuff: true, hasArmorerBuff: false).magnitude;

        Assert.That(boostedForce, Is.GreaterThan(baseForce));
    }

    [Test]
    public void CurrentZone_HazardZone_IsReduced_ByArmorerEscort()
    {
        var zone = BuildZone(Vector3.zero, Vector3.right, 12f);
        SetPrivateField(zone, "zoneRole", 2);
        SetPrivateField(zone, "roleBonusMultiplier", 1.6f);
        SetPrivateField(zone, "armorerMitigation", 0.45f);

        float dangerousForce = zone.GetForceAt(Vector3.zero, hasSpeederBuff: false, hasArmorerBuff: false).magnitude;
        float mitigatedForce = zone.GetForceAt(Vector3.zero, hasSpeederBuff: false, hasArmorerBuff: true).magnitude;

        Assert.That(mitigatedForce, Is.LessThan(dangerousForce));
    }

    [Test]
    public void CurrentField_SampleCurrent_IsStrongInsideZoneCenter()
    {
        var zone = BuildZone(new Vector3(0f, -130f, 0f), Vector3.forward, 18f);
        SetPrivateField(zone, "zoneRole", 2);

        var field = Object.FindAnyObjectByType<CurrentField>();
        SetPrivateField(field, "baseStrength", 0f);

        float sampled = field.SampleCurrent(zone.GetWorldCenter(), 0f).magnitude;

        Assert.That(sampled, Is.GreaterThan(10f));
    }

    static CurrentZone BuildZone(Vector3 position, Vector3 direction, float strength)
    {
        var systems = new GameObject("CurrentSystems");
        var field = systems.AddComponent<CurrentField>();
        InvokePrivateMethod(field, "Awake");

        var go = new GameObject("TestZone");
        go.transform.position = position;
        var collider = go.AddComponent<BoxCollider>();
        collider.size = new Vector3(10f, 12f, 8f);
        var zone = go.AddComponent<CurrentZone>();

        InvokePrivateMethod(zone, "Awake");
        InvokePrivateMethod(zone, "OnEnable");
        SetPrivateField(zone, "flowDirection", direction);
        SetPrivateField(zone, "strength", strength);

        return zone;
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

    static void AssertVectorEqual(Vector3 actual, Vector3 expected, float tolerance = 0.001f)
    {
        Assert.That(actual.x, Is.EqualTo(expected.x).Within(tolerance));
        Assert.That(actual.y, Is.EqualTo(expected.y).Within(tolerance));
        Assert.That(actual.z, Is.EqualTo(expected.z).Within(tolerance));
    }
}
