using NUnit.Framework;
using UnityEngine;

public class CreatureSymbiosisEditModeTests
{
    GameObject root;
    GameObject systems;
    FlockingManager flockingManager;
    BathyscapheController bathyscaphe;
    HullIntegrity hullIntegrity;
    CreatureBuff creatureBuff;

    [SetUp]
    public void SetUp()
    {
        systems = new GameObject("Systems");
        flockingManager = systems.AddComponent<FlockingManager>();

        root = new GameObject("Bathyscaphe");
        root.AddComponent<Rigidbody>();
        root.AddComponent<DepthTracker>();
        bathyscaphe = root.AddComponent<BathyscapheController>();
        hullIntegrity = root.AddComponent<HullIntegrity>();
        creatureBuff = root.AddComponent<CreatureBuff>();

        InvokePrivateMethod(hullIntegrity, "Awake");
        SetPrivateField(creatureBuff, "flockingManager", flockingManager);
        SetPrivateField(creatureBuff, "bathyscaphe", bathyscaphe);
        SetPrivateField(creatureBuff, "hullIntegrity", hullIntegrity);
    }

    [TearDown]
    public void TearDown()
    {
        if (root != null)
            Object.DestroyImmediate(root);
        if (systems != null)
            Object.DestroyImmediate(systems);
    }

    [Test]
    public void CreatureBuff_GainsSpeederBuff_WhenEnoughFollowersPresent()
    {
        CreateFollower(CreatureType.Speeder, true, flockingManager);
        CreateFollower(CreatureType.Speeder, true, flockingManager);

        InvokePrivateMethod(creatureBuff, "Update");

        Assert.That(creatureBuff.SpeederActive, Is.True);
    }

    [Test]
    public void CreatureBuff_KeepsBuff_ForGraceDuration_AfterFollowersLeave()
    {
        CreateFollower(CreatureType.Armorer, true, flockingManager);
        CreateFollower(CreatureType.Armorer, true, flockingManager);
        InvokePrivateMethod(creatureBuff, "Update");
        Assert.That(creatureBuff.ArmorerActive, Is.True);

        foreach (var agent in Object.FindObjectsByType<FlockingAgent>())
            agent.IsFollowing = false;

        InvokePrivateMethod(creatureBuff, "TickBuffTimers", 1f);

        Assert.That(creatureBuff.ArmorerActive, Is.True);
    }

    [Test]
    public void CreatureBuff_ExpiresBuff_AfterGraceDuration()
    {
        CreateFollower(CreatureType.Healer, true, flockingManager);
        CreateFollower(CreatureType.Healer, true, flockingManager);
        InvokePrivateMethod(creatureBuff, "Update");
        Assert.That(creatureBuff.HealerActive, Is.True);

        foreach (var agent in Object.FindObjectsByType<FlockingAgent>())
            agent.IsFollowing = false;

        InvokePrivateMethod(creatureBuff, "TickBuffTimers", 10f);

        Assert.That(creatureBuff.HealerActive, Is.False);
    }

    [Test]
    public void CreatureSymbiosisVfx_CreatesLinks_OnlyForFollowingAgents()
    {
        var vfxRoot = new GameObject("SymbiosisVfx");
        var symbiosisVfx = vfxRoot.AddComponent<CreatureSymbiosisVFX>();
        SetPrivateField(symbiosisVfx, "flockingManager", flockingManager);
        SetPrivateField(symbiosisVfx, "source", root.transform);

        CreateFollower(CreatureType.Speeder, true, flockingManager);
        CreateFollower(CreatureType.Armorer, true, flockingManager);
        CreateFollower(CreatureType.Healer, false, flockingManager);

        InvokePrivateMethod(symbiosisVfx, "Update");

        Assert.That(symbiosisVfx.ActiveLinkCount, Is.EqualTo(2));

        Object.DestroyImmediate(vfxRoot);
    }

    [Test]
    public void FlockingAgent_PullsBackTowardHabitat_WhenItDriftsTooFar()
    {
        var agent = CreateFollower(CreatureType.Scout, false, flockingManager);
        InvokePrivateMethod(agent, "Awake");
        SetPrivateField(agent, "habitatRadius", 10f);
        SetPrivateField(agent, "returnToHabitatForce", 5f);

        agent.transform.position = new Vector3(30f, 0f, 0f);

        InvokePrivateMethod(agent, "ApplyHabitatConstraint", 1f);

        Assert.That(agent.Velocity.x, Is.LessThan(0f));
    }

    [Test]
    public void LightBeam_UsesWideReadableCaptureCone()
    {
        var beamRoot = new GameObject("BeamRoot");
        var spot = new GameObject("Spot").AddComponent<Light>();
        spot.transform.SetParent(beamRoot.transform, false);

        var particles = new GameObject("BeamParticles").AddComponent<ParticleSystem>();
        particles.transform.SetParent(beamRoot.transform, false);

        var beam = beamRoot.AddComponent<LightBeam>();
        SetPrivateField(beam, "spotLight", spot);
        SetPrivateField(beam, "beamParticles", particles);
        SetPrivateField(beam, "flockingManager", flockingManager);

        InvokePrivateMethod(beam, "SetBeamActive", true);

        Assert.That(spot.spotAngle, Is.GreaterThanOrEqualTo(50f));
        Assert.That(spot.range, Is.GreaterThanOrEqualTo(40f));

        Object.DestroyImmediate(beamRoot);
    }

    static FlockingAgent CreateFollower(CreatureType type, bool following, FlockingManager manager)
    {
        var go = new GameObject(type.ToString());
        var agent = go.AddComponent<FlockingAgent>();
        SetPrivateField(agent, "creatureType", type);
        agent.IsFollowing = following;
        manager.Register(agent);
        return agent;
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
            argTypes[i] = args[i].GetType();

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
