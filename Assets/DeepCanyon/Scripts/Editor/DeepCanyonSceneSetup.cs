using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;
using UnityEngine.VFX;
using TMPro;
using UnityEngine.UI;

public class DeepCanyonSceneSetup : EditorWindow
{
    [MenuItem("DeepCanyon/Build Entire Scene")]
    public static void BuildScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "DeepCanyon";

        var systems = CreateSystems();
        var bathyscaphe = CreateBathyscaphe(systems);
        CreateCamera(bathyscaphe.transform);
        CreateCanyon();
        CreateCurrentZones();
        CreateCreatures();
        CreateCrystals();
        CreateLighting();
        var volume = CreatePostProcessing();
        CreateUI(systems, bathyscaphe);
        SetupDepthPostProcess(systems, volume);
        CreateAmbientVFX(bathyscaphe.transform);
        SetupRemainingComponents(systems, bathyscaphe);

        string dir = System.IO.Path.Combine(Application.dataPath, "DeepCanyon", "Scenes");
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        EditorSceneManager.SaveScene(scene, "Assets/DeepCanyon/Scenes/DeepCanyon.unity");
        Debug.Log("[DeepCanyon] Scene built and saved!");
    }

    // ===================== SYSTEMS =====================

    static GameObject CreateSystems()
    {
        var go = new GameObject("--- SYSTEMS ---");
        go.AddComponent<CurrentField>();
        go.AddComponent<FlockingManager>();
        go.AddComponent<GameManager>();
        return go;
    }

    // ===================== BATHYSCAPHE =====================

    static GameObject CreateBathyscaphe(GameObject systems)
    {
        var root = new GameObject("Bathyscaphe");
        root.transform.position = new Vector3(0f, -1.5f, 0f);
        root.tag = "Player";

        var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        body.name = "Hull";
        body.transform.SetParent(root.transform);
        body.transform.localPosition = Vector3.zero;
        body.transform.localScale = new Vector3(2.5f, 2f, 3f);
        SetMat(body, new Color(0.4f, 0.45f, 0.5f));

        var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        dome.name = "Viewport";
        dome.transform.SetParent(root.transform);
        dome.transform.localPosition = new Vector3(0f, 0.6f, 1.1f);
        dome.transform.localScale = Vector3.one * 1.2f;
        SetMat(dome, new Color(0.5f, 0.8f, 1f, 0.7f), true);

        var engine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        engine.name = "Engine";
        engine.transform.SetParent(root.transform);
        engine.transform.localPosition = new Vector3(0f, 0f, -1.8f);
        engine.transform.localScale = new Vector3(0.8f, 0.6f, 0.8f);
        engine.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        SetMat(engine, new Color(0.3f, 0.3f, 0.35f));

        CreateArm(root.transform, "LeftArm", new Vector3(-1.5f, -0.3f, 0.8f), 30f);
        CreateArm(root.transform, "RightArm", new Vector3(1.5f, -0.3f, 0.8f), -30f);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = 8f;
        rb.useGravity = false;
        rb.linearDamping = 0.8f;
        rb.angularDamping = 1.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        var col = root.AddComponent<SphereCollider>();
        col.radius = 1.5f;

        root.AddComponent<DepthTracker>();
        root.AddComponent<BuoyancySystem>();
        var hull = root.AddComponent<HullIntegrity>();
        root.AddComponent<CurrentForceApplier>();

        var input = root.AddComponent<PlayerInput>();
        var asset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/DeepCanyon/Input/BathyscapheControls.inputactions");
        if (asset != null)
        {
            input.actions = asset;
            input.defaultActionMap = "Bathyscaphe";
            input.notificationBehavior = PlayerNotifications.SendMessages;
        }

        root.AddComponent<BathyscapheController>();

        // Light beam
        var beamObj = new GameObject("LightBeamSpot");
        beamObj.transform.SetParent(root.transform);
        beamObj.transform.localPosition = new Vector3(0f, 0f, 1.5f);
        var spot = beamObj.AddComponent<Light>();
        spot.type = LightType.Spot;
        spot.spotAngle = 70f;
        spot.range = 50f;
        spot.color = new Color(0.5f, 0.9f, 1f);
        spot.intensity = 25f;
        spot.enabled = false;

        // Beam particles — bright, volumetric cone
        var beamVfxObj = new GameObject("LightBeamVFX");
        beamVfxObj.transform.SetParent(beamObj.transform, false);
        var beamParticles = beamVfxObj.AddComponent<ParticleSystem>();
        var beamMain = beamParticles.main;
        beamMain.startLifetime = 2.5f;
        beamMain.startSpeed = 8f;
        beamMain.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        beamMain.startColor = new Color(0.5f, 0.9f, 1f, 0.45f);
        beamMain.maxParticles = 400;
        beamMain.simulationSpace = ParticleSystemSimulationSpace.Local;
        var beamEmission = beamParticles.emission;
        beamEmission.rateOverTime = 120f;
        var beamShape = beamParticles.shape;
        beamShape.shapeType = ParticleSystemShapeType.Cone;
        beamShape.angle = 18f;
        beamShape.radius = 0.3f;
        beamShape.length = 0.5f;
        var beamColorLife = beamParticles.colorOverLifetime;
        beamColorLife.enabled = true;
        var beamGrad = new Gradient();
        beamGrad.SetKeys(
            new[] {
                new GradientColorKey(new Color(0.6f, 0.95f, 1f), 0f),
                new GradientColorKey(new Color(0.4f, 0.8f, 1f), 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.55f, 0.1f),
                new GradientAlphaKey(0.35f, 0.5f),
                new GradientAlphaKey(0f, 1f)
            });
        beamColorLife.color = beamGrad;
        var beamSizeLife = beamParticles.sizeOverLifetime;
        beamSizeLife.enabled = true;
        beamSizeLife.size = new ParticleSystem.MinMaxCurve(1f, new AnimationCurve(
            new Keyframe(0f, 0.3f), new Keyframe(0.5f, 1f), new Keyframe(1f, 1.5f)));
        ApplyParticleMaterial(beamParticles, new Color(0.6f, 0.95f, 1f, 0.5f));
        beamParticles.Stop();

        // Beam line renderer — clear directional indicator
        var beamLineObj = new GameObject("LightBeamLine");
        beamLineObj.transform.SetParent(beamObj.transform, false);
        var beamLine = beamLineObj.AddComponent<LineRenderer>();
        beamLine.positionCount = 2;
        beamLine.useWorldSpace = true;
        beamLine.startWidth = 0.35f;
        beamLine.endWidth = 0.06f;
        beamLine.numCapVertices = 6;
        beamLine.shadowCastingMode = ShadowCastingMode.Off;
        beamLine.receiveShadows = false;
        beamLine.enabled = false;
        beamLine.sharedMaterial = CreateTrailMaterial(new Color(0.5f, 0.9f, 1f, 0.7f));
        beamLine.startColor = new Color(0.6f, 0.95f, 1f, 0.8f);
        beamLine.endColor = new Color(0.5f, 0.9f, 1f, 0.05f);

        var beam = root.AddComponent<LightBeam>();
        SetField(beam, "spotLight", spot);
        SetField(beam, "flockingManager", systems.GetComponent<FlockingManager>());
        SetField(beam, "beamParticles", beamParticles);
        SetField(beam, "beamLine", beamLine);

        var buff = root.AddComponent<CreatureBuff>();
        SetField(buff, "flockingManager", systems.GetComponent<FlockingManager>());
        SetField(buff, "bathyscaphe", root.GetComponent<BathyscapheController>());
        SetField(buff, "hullIntegrity", hull);

        // Leak FX
        ParticleSystem[] leaks = new ParticleSystem[5];
        Vector3[] lpos = {
            new(0, 0.8f, 0.5f), new(-0.8f, 0.3f, 0),
            new(0.8f, 0.3f, 0), new(0, -0.5f, 0.3f), new(0, 0, -1f)
        };
        for (int i = 0; i < 5; i++)
        {
            var lk = new GameObject($"Leak_{i}");
            lk.transform.SetParent(root.transform);
            lk.transform.localPosition = lpos[i];
            var ps = lk.AddComponent<ParticleSystem>();
            var m = ps.main;
            m.startLifetime = 1.2f; m.startSpeed = 3f; m.startSize = 0.08f;
            m.startColor = new Color(0.6f, 0.8f, 1f, 0.5f);
            m.maxParticles = 60; m.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 30f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 12f; sh.radius = 0.04f;
            ApplyParticleMaterial(ps, new Color(0.7f, 0.9f, 1f, 0.6f));
            ps.Stop();
            leaks[i] = ps;
        }
        SetField(hull, "leakEffects", leaks);
        SetField(systems.GetComponent<GameManager>(), "hullIntegrity", hull);

        return root;
    }

    static GameObject CreateArm(Transform parent, string name, Vector3 pos, float rollZ)
    {
        var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        arm.name = name;
        arm.transform.SetParent(parent);
        arm.transform.localPosition = pos;
        arm.transform.localScale = new Vector3(0.15f, 1f, 0.15f);
        arm.transform.localRotation = Quaternion.Euler(0f, 0f, rollZ);
        SetMat(arm, new Color(0.55f, 0.5f, 0.25f));
        return arm;
    }

    // ===================== CAMERA =====================

    static void CreateCamera(Transform target)
    {
        var camObj = new GameObject("MainCamera");
        camObj.tag = "MainCamera";
        camObj.transform.position = target.position + new Vector3(0, 4.5f, -11f);
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.38f, 0.72f, 0.95f);
        cam.nearClipPlane = 0.3f;
        cam.farClipPlane = 400f;
        cam.fieldOfView = 72f;
        camObj.AddComponent<UniversalAdditionalCameraData>();
        camObj.AddComponent<AudioListener>();
        var depthFx = camObj.AddComponent<CameraDepthEffect>();
        SetField(depthFx, "target", target);
        SetField(depthFx, "depthTracker", target.GetComponent<DepthTracker>());
    }

    // ===================== CANYON =====================

    static void CreateCanyon()
    {
        var root = new GameObject("--- CANYON ---");

        BuildWalls(root.transform, 0f, -30f, 40f, 30f, 30f);
        BuildWalls(root.transform, -30f, -80f, 32f, 50f, 25f);
        BuildWalls(root.transform, -80f, -150f, 24f, 70f, 22f);
        BuildWalls(root.transform, -150f, -250f, 18f, 100f, 18f);

        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.name = "CanyonFloor";
        floor.transform.SetParent(root.transform);
        floor.transform.position = new Vector3(0, -255, 0);
        floor.transform.localScale = new Vector3(70, 5, 70);
        floor.isStatic = true;
        SetMat(floor, new Color(0.18f, 0.14f, 0.12f));

        BuildArch(root.transform, new Vector3(0, -55, 0), 14f);
        BuildArch(root.transform, new Vector3(5, -115, 8), 11f);
        BuildArch(root.transform, new Vector3(-3, -190, -4), 9f);

        for (int i = 0; i < 10; i++)
        {
            var ledge = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ledge.name = $"Ledge_{i}";
            ledge.transform.SetParent(root.transform);
            float y = -25f - i * 24f;
            float x = (i % 2 == 0 ? -1f : 1f) * Random.Range(6f, 14f);
            ledge.transform.position = new Vector3(x, y, Random.Range(-10f, 10f));
            ledge.transform.localScale = new Vector3(Random.Range(4f, 8f), 0.6f, Random.Range(3f, 7f));
            ledge.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360), Random.Range(-5, 5));
            ledge.isStatic = true;
            SetMat(ledge, new Color(0.22f + Random.value * 0.08f, 0.18f, 0.14f));
        }

        for (int i = 0; i < 16; i++)
        {
            var wl = new GameObject($"WallLight_{i}");
            wl.transform.SetParent(root.transform);
            float y = -15f - i * 16f;
            float x = (i % 2 == 0 ? -1f : 1f) * Random.Range(8f, 16f);
            wl.transform.position = new Vector3(x, y, Random.Range(-6f, 6f));
            var lt = wl.AddComponent<Light>();
            lt.type = LightType.Point;
            lt.range = 14f;
            lt.intensity = i < 4 ? 1.5f : 0.8f;
            lt.color = i < 4
                ? new Color(0.4f, 0.7f, 1f)
                : new Color(0.15f, 0.3f, 0.6f);
        }
    }

    static void BuildWalls(Transform p, float top, float bot, float w, float h, float d)
    {
        float cy = (top + bot) * 0.5f;
        float shade = Mathf.Lerp(0.3f, 0.1f, Mathf.Abs(cy) / 250f);
        Color c = new Color(shade, shade * 0.85f, shade * 0.75f);

        Wall(p, $"WL_{top}", new Vector3(-w / 2 - 2.5f, cy, 0), new Vector3(5, h, d), c);
        Wall(p, $"WR_{top}", new Vector3(w / 2 + 2.5f, cy, 0), new Vector3(5, h, d), c);
        Wall(p, $"WB_{top}", new Vector3(0, cy, -d / 2 - 2.5f), new Vector3(w + 10, h, 5), c * 0.85f);
        Wall(p, $"WF_{top}", new Vector3(0, cy, d / 2 + 2.5f), new Vector3(w + 10, h, 5), c * 0.85f);
    }

    static void Wall(Transform p, string n, Vector3 pos, Vector3 scale, Color c)
    {
        var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
        w.name = n; w.transform.SetParent(p); w.transform.position = pos;
        w.transform.localScale = scale; w.isStatic = true; SetMat(w, c);
    }

    static void BuildArch(Transform p, Vector3 pos, float sz)
    {
        var root = new GameObject("Arch");
        root.transform.SetParent(p); root.transform.position = pos;
        Color c = new Color(0.2f, 0.17f, 0.14f);

        var l = GameObject.CreatePrimitive(PrimitiveType.Cube);
        l.name = "PillarL"; l.transform.SetParent(root.transform);
        l.transform.localPosition = new Vector3(-sz / 2, 0, 0);
        l.transform.localScale = new Vector3(1.5f, sz * 1.2f, 2.5f); l.isStatic = true; SetMat(l, c);

        var r = GameObject.CreatePrimitive(PrimitiveType.Cube);
        r.name = "PillarR"; r.transform.SetParent(root.transform);
        r.transform.localPosition = new Vector3(sz / 2, 0, 0);
        r.transform.localScale = new Vector3(1.5f, sz * 1.2f, 2.5f); r.isStatic = true; SetMat(r, c);

        var t = GameObject.CreatePrimitive(PrimitiveType.Cube);
        t.name = "Top"; t.transform.SetParent(root.transform);
        t.transform.localPosition = new Vector3(0, sz * 0.6f, 0);
        t.transform.localScale = new Vector3(sz + 1.5f, 1.8f, 2.5f); t.isStatic = true; SetMat(t, c);
    }

    // ===================== CURRENT ZONES =====================

    static void CreateCurrentZones()
    {
        var root = new GameObject("--- CURRENTS ---");

        // Assist zones — green/cyan tint, help the player
        MakeZone(root.transform, "Current_Z2", new Vector3(0, -55, 0), new Vector3(40, 30, 40), Vector3.right, 3f, 1,
            new Color(0.2f, 1f, 0.6f));
        MakeZone(root.transform, "Current_Z3", new Vector3(4, -115, 4), new Vector3(40, 40, 40), new Vector3(1, -0.2f, 0.5f), 5f, 1,
            new Color(0.3f, 0.9f, 0.7f));
        MakeZone(root.transform, "Current_Jet", new Vector3(-4, -130, 0), new Vector3(30, 30, 30), Vector3.forward, 8f, 2,
            new Color(1f, 0.35f, 0.2f));
        MakeZone(root.transform, "Current_Deep", new Vector3(0, -200, 0), new Vector3(40, 80, 40), new Vector3(-0.5f, 0.15f, 1), 6f, 2,
            new Color(1f, 0.5f, 0.15f));
    }

    static void MakeZone(Transform p, string name, Vector3 pos, Vector3 size, Vector3 dir, float str, int role, Color zoneColor)
    {
        var go = new GameObject(name);
        go.transform.SetParent(p); go.transform.position = pos;
        var bc = go.AddComponent<BoxCollider>(); bc.isTrigger = true; bc.size = size;
        var cz = go.AddComponent<CurrentZone>();
        SetField(cz, "flowDirection", dir);
        SetField(cz, "strength", str);
        SetField(cz, "zoneRole", role);

        if (role == 1)
            SetField(cz, "roleBonusMultiplier", 1.55f);
        else if (role == 2)
        {
            SetField(cz, "roleBonusMultiplier", 1.7f);
            SetField(cz, "armorerMitigation", 0.4f);
        }

        // VFX Graph lanes
        var asset = AssetDatabase.LoadAssetAtPath<VisualEffectAsset>(
            "Assets/DeepCanyon/VFX/Currents/UnderwaterCurrentZone.vfx");

        CreateCurrentLane(go.transform, "VFX_Core", asset, Vector3.zero, new Vector3(1f, 0.7f, 1.1f), 1f);
        CreateCurrentLane(go.transform, "VFX_Upper", asset, new Vector3(0f, 0.28f, 0f), new Vector3(0.8f, 0.45f, 0.95f), 0.92f);
        CreateCurrentLane(go.transform, "VFX_Lower", asset, new Vector3(0f, -0.28f, 0f), new Vector3(0.8f, 0.45f, 0.95f), 1.08f);

        if (str >= 10f)
        {
            CreateCurrentLane(go.transform, "VFX_Jet", asset, new Vector3(0.22f, 0f, 0f), new Vector3(0.36f, 0.35f, 1.25f), 1.45f);
            CreateCurrentLane(go.transform, "VFX_JetMirror", asset, new Vector3(-0.22f, 0f, 0f), new Vector3(0.36f, 0.35f, 1.25f), 1.25f);
        }

        // Colored zone light — main visual indicator
        var lightObj = new GameObject("ZoneLight");
        lightObj.transform.SetParent(go.transform, false);
        var zoneLight = lightObj.AddComponent<Light>();
        zoneLight.type = LightType.Point;
        zoneLight.color = zoneColor;
        zoneLight.intensity = role == 2 ? 8f : 5f;
        zoneLight.range = Mathf.Max(size.x, size.z) * 0.8f;

        // Streaming particles showing flow direction — core visual
        var streamObj = new GameObject("FlowStream");
        streamObj.transform.SetParent(go.transform, false);
        streamObj.transform.localRotation = Quaternion.LookRotation(dir.normalized);
        var streamPS = streamObj.AddComponent<ParticleSystem>();
        var sm = streamPS.main;
        sm.startLifetime = 4f;
        sm.startSpeed = str * 0.4f;
        sm.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.45f);
        sm.startColor = new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.6f);
        sm.maxParticles = 200;
        sm.simulationSpace = ParticleSystemSimulationSpace.World;
        var sEmission = streamPS.emission;
        sEmission.rateOverTime = 50f;
        var sShape = streamPS.shape;
        sShape.shapeType = ParticleSystemShapeType.Box;
        sShape.scale = size * 0.6f;
        var sVel = streamPS.velocityOverLifetime;
        sVel.enabled = true;
        sVel.x = dir.normalized.x * str * 0.3f;
        sVel.y = dir.normalized.y * str * 0.3f;
        sVel.z = dir.normalized.z * str * 0.3f;
        var sColorLife = streamPS.colorOverLifetime;
        sColorLife.enabled = true;
        var sGrad = new Gradient();
        sGrad.SetKeys(
            new[] {
                new GradientColorKey(zoneColor, 0f),
                new GradientColorKey(zoneColor * 0.7f, 1f)
            },
            new[] {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(0.7f, 0.15f),
                new GradientAlphaKey(0.5f, 0.6f),
                new GradientAlphaKey(0f, 1f)
            });
        sColorLife.color = sGrad;
        ApplyParticleMaterial(streamPS, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.6f));

        // Boundary glow — semi-transparent cube showing zone edges
        var boundary = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boundary.name = "ZoneBoundary";
        boundary.transform.SetParent(go.transform, false);
        boundary.transform.localScale = size * 0.98f;
        Object.DestroyImmediate(boundary.GetComponent<BoxCollider>());
        SetMat(boundary, new Color(zoneColor.r, zoneColor.g, zoneColor.b, 0.04f), true);
        SetEmission(boundary, zoneColor, role == 2 ? 1.5f : 0.8f);
    }

    static void CreateCurrentLane(Transform parent, string name, VisualEffectAsset asset, Vector3 laneOffsetNormalized, Vector3 laneScaleMultiplier, float playRateMultiplier)
    {
        var lane = new GameObject(name);
        lane.transform.SetParent(parent);
        lane.transform.localPosition = Vector3.zero;
        lane.transform.localRotation = Quaternion.identity;

        var visualEffect = lane.AddComponent<VisualEffect>();
        visualEffect.visualEffectAsset = asset;

        var binder = lane.AddComponent<CurrentZoneVFXBinder>();
        SetField(binder, "laneOffsetNormalized", laneOffsetNormalized);
        SetField(binder, "laneScaleMultiplier", laneScaleMultiplier);
        SetField(binder, "playRateMultiplier", playRateMultiplier);
    }

    // ===================== CREATURES =====================

    static void CreateCreatures()
    {
        var root = new GameObject("--- CREATURES ---");
        CreatureType[] types = { CreatureType.Healer, CreatureType.Speeder, CreatureType.Armorer, CreatureType.Scout };
        Color[] cols = { new(0.2f,1,0.4f), new(1,0.9f,0.2f), new(0.3f,0.5f,1), new(0.8f,0.3f,1) };
        int id = 0;

        // Spawn clusters of same-type creatures near the player's path
        // Each zone has pairs of same type so buffs activate easily
        float[] zoneY = { -30f, -65f, -110f, -170f, -220f };
        int[][] zoneTypes = {
            new[] { 0, 0, 1, 1, 2 },    // zone 0: 2 healers, 2 speeders, 1 armorer
            new[] { 1, 1, 2, 2, 3 },    // zone 1: 2 speeders, 2 armorers, 1 scout
            new[] { 0, 0, 2, 2, 3, 3 }, // zone 2: 2 healers, 2 armorers, 2 scouts
            new[] { 0, 1, 2, 3, 0, 1, 2, 3 }, // zone 3: 2 of each
            new[] { 0, 0, 1, 1, 2, 2, 3, 3 }  // zone 4: 2 of each
        };

        for (int z = 0; z < zoneY.Length; z++)
        {
            for (int i = 0; i < zoneTypes[z].Length; i++)
            {
                int typeIdx = zoneTypes[z][i];
                var t = types[typeIdx];
                var c = cols[typeIdx];
                var cr = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                cr.name = $"Creature_{t}_{id}";
                cr.transform.SetParent(root.transform);
                float clusterX = (typeIdx % 2 == 0 ? -1f : 1f) * Random.Range(2f, 6f);
                float clusterZ = (typeIdx < 2 ? -1f : 1f) * Random.Range(2f, 6f);
                cr.transform.position = new Vector3(
                    clusterX + Random.Range(-2f, 2f),
                    zoneY[z] + Random.Range(-4f, 4f),
                    clusterZ + Random.Range(-2f, 2f));
                cr.transform.localScale = Vector3.one * 0.6f;
                SetMat(cr, c * 0.5f);
                SetEmission(cr, c, 5f);

                var agent = cr.AddComponent<FlockingAgent>();
                SetField(agent, "creatureType", t);

                var lg = new GameObject("Glow"); lg.transform.SetParent(cr.transform); lg.transform.localPosition = Vector3.zero;
                var lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.color = c; lt.intensity = 3f; lt.range = 12f;

                var trail = cr.AddComponent<TrailRenderer>();
                trail.time = 1.5f; trail.startWidth = 0.22f; trail.endWidth = 0;
                trail.startColor = c; trail.endColor = new Color(c.r, c.g, c.b, 0);
                trail.sharedMaterial = CreateTrailMaterial(c);
                id++;
            }
        }
    }

    // ===================== CRYSTALS =====================

    static void CreateCrystals()
    {
        var root = new GameObject("--- CRYSTALS ---");
        Color cc = new Color(0.3f, 1f, 0.8f);
        Vector3[] pos = {
            new(6,-8,4), new(-9,-22,-3),
            new(11,-42,6), new(-7,-58,-5), new(1,-72,9),
            new(9,-92,-4), new(-8,-108,7), new(4,-125,-8), new(-11,-142,3),
            new(6,-175,5), new(-5,-205,-6), new(8,-235,4),
            new(0,-248,0)
        };
        for (int i = 0; i < pos.Length; i++)
        {
            var cr = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cr.name = $"EchoCrystal_{i}";
            cr.transform.SetParent(root.transform);
            cr.transform.position = pos[i];
            cr.transform.localScale = new Vector3(0.4f, 1.4f, 0.4f);
            cr.transform.rotation = Quaternion.Euler(Random.Range(-12, 12), Random.Range(0, 360), Random.Range(-12, 12));
            SetMat(cr, cc); SetEmission(cr, cc, 6f);
            Object.DestroyImmediate(cr.GetComponent<BoxCollider>());
            cr.AddComponent<EchoCrystal>();

            var lg = new GameObject("Light"); lg.transform.SetParent(cr.transform); lg.transform.localPosition = Vector3.zero;
            var lt = lg.AddComponent<Light>(); lt.type = LightType.Point; lt.color = cc; lt.intensity = 6f; lt.range = 18f;

            var shimmer = new GameObject("Shimmer"); shimmer.transform.SetParent(cr.transform); shimmer.transform.localPosition = Vector3.zero;
            var ps = shimmer.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startLifetime = 2.5f; main.startSpeed = 0.6f; main.startSize = 0.07f;
            main.startColor = new Color(cc.r, cc.g, cc.b, 0.5f);
            main.maxParticles = 40; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var em = ps.emission; em.rateOverTime = 12f;
            var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Sphere; sh.radius = 1f;
            ApplyParticleMaterial(ps, new Color(cc.r, cc.g, cc.b, 0.5f));
        }
    }

    // ===================== LIGHTING =====================

    static void CreateLighting()
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.45f, 0.7f, 0.9f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.35f, 0.72f, 0.95f);
        RenderSettings.fogDensity = 0.0025f;

        var sun = new GameObject("Sun");
        sun.transform.rotation = Quaternion.Euler(35f, -40f, 0f);
        var sl = sun.AddComponent<Light>();
        sl.type = LightType.Directional;
        sl.color = new Color(0.9f, 0.97f, 1f);
        sl.intensity = 3.2f;
        sl.shadows = LightShadows.Soft;

        var fill = new GameObject("FillLight");
        fill.transform.rotation = Quaternion.Euler(-60f, 20f, 0f);
        var fl = fill.AddComponent<Light>();
        fl.type = LightType.Directional;
        fl.color = new Color(0.35f, 0.55f, 0.8f);
        fl.intensity = 1.1f;
        fl.shadows = LightShadows.None;

        var surface = GameObject.CreatePrimitive(PrimitiveType.Plane);
        surface.name = "WaterSurface";
        surface.transform.position = new Vector3(0f, 1.5f, 0f);
        surface.transform.localScale = new Vector3(12f, 1f, 12f);
        surface.isStatic = true;
        SetMat(surface, new Color(0.5f, 0.85f, 1f, 0.35f), true);
        var surfaceCollider = surface.GetComponent<Collider>();
        if (surfaceCollider != null)
            Object.DestroyImmediate(surfaceCollider);

        var rays = new GameObject("GodRays");
        rays.transform.position = new Vector3(0, 5, 0);
        rays.transform.rotation = Quaternion.Euler(90, 0, 0);
        var ps = rays.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startLifetime = 6f; main.startSpeed = 0.3f; main.startSize = 3f;
        main.startColor = new Color(0.5f, 0.7f, 1f, 0.08f);
        main.maxParticles = 30; main.simulationSpace = ParticleSystemSimulationSpace.World;
        var em = ps.emission; em.rateOverTime = 4f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone;
        sh.angle = 20f; sh.radius = 15f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var g = new Gradient();
        g.SetKeys(
            new[] { new GradientColorKey(new Color(0.6f, 0.8f, 1f), 0), new GradientColorKey(new Color(0.3f, 0.5f, 0.8f), 1) },
            new[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(0.1f, 0.3f), new GradientAlphaKey(0, 1) }
        );
        col.color = g;
        ApplyParticleMaterial(ps, new Color(0.7f, 0.9f, 1f, 0.1f));
    }

    // ===================== POST PROCESSING =====================

    static Volume CreatePostProcessing()
    {
        var go = new GameObject("PostProcessVolume");
        var vol = go.AddComponent<Volume>();
        vol.isGlobal = true;
        var prof = ScriptableObject.CreateInstance<VolumeProfile>();

        var ca = prof.Add<ColorAdjustments>();
        ca.postExposure.Override(1.6f);
        ca.colorFilter.Override(new Color(0.78f, 0.92f, 1f));
        ca.saturation.Override(20f);
        ca.contrast.Override(6f);

        var vig = prof.Add<Vignette>();
        vig.intensity.Override(0.03f);
        vig.color.Override(new Color(0.15f, 0.35f, 0.55f));

        var bl = prof.Add<Bloom>();
        bl.threshold.Override(0.7f);
        bl.intensity.Override(1.2f);

        var chr = prof.Add<ChromaticAberration>();
        chr.intensity.Override(0f);

        string dir = System.IO.Path.Combine(Application.dataPath, "DeepCanyon", "Settings");
        if (!System.IO.Directory.Exists(dir)) System.IO.Directory.CreateDirectory(dir);
        const string volumeAssetPath = "Assets/DeepCanyon/Settings/DeepCanyonVolume.asset";
        if (AssetDatabase.LoadAssetAtPath<VolumeProfile>(volumeAssetPath) != null)
            AssetDatabase.DeleteAsset(volumeAssetPath);
        AssetDatabase.CreateAsset(prof, volumeAssetPath);
        vol.profile = prof;
        return vol;
    }

    // ===================== UI =====================

    static void CreateUI(GameObject systems, GameObject bathyscaphe)
    {
        var canvasObj = new GameObject("GameCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        var scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasObj.AddComponent<GraphicRaycaster>();

        var depthPanel = MakePanel(canvasObj.transform, "DepthPanel", new Vector2(0,0.5f), new Vector2(0,0.5f), new Vector2(50,0), new Vector2(60,350));
        var depthBg = MakeImg(depthPanel.transform, "DepthBg", new Color(0,0,0,0.35f));
        Stretch(depthBg);
        var depthFill = MakeImg(depthPanel.transform, "DepthFill", new Color(0.3f,0.6f,1f,0.6f));
        Stretch(depthFill);
        depthFill.GetComponent<Image>().type = Image.Type.Filled;
        depthFill.GetComponent<Image>().fillMethod = Image.FillMethod.Vertical;
        depthFill.GetComponent<Image>().fillAmount = 0;
        var depthTxt = MakeTMP(depthPanel.transform, "DepthText", "0m", 16);
        var dtRT = depthTxt.GetComponent<RectTransform>();
        dtRT.anchorMin = new Vector2(0,0); dtRT.anchorMax = new Vector2(1,0);
        dtRT.anchoredPosition = new Vector2(0,-18); dtRT.sizeDelta = new Vector2(0,28);

        var hullPanel = MakePanel(canvasObj.transform, "HullPanel", new Vector2(0.5f,1), new Vector2(0.5f,1), new Vector2(0,-35), new Vector2(350,24));
        var hullBg = MakeImg(hullPanel.transform, "HullBg", new Color(0,0,0,0.4f));
        Stretch(hullBg);
        var hullFill = MakeImg(hullPanel.transform, "HullFill", Color.green);
        Stretch(hullFill);
        hullFill.GetComponent<Image>().type = Image.Type.Filled;
        hullFill.GetComponent<Image>().fillMethod = Image.FillMethod.Horizontal;
        var hullTxt = MakeTMP(hullPanel.transform, "HullText", "100 / 100", 14);
        Stretch(hullTxt);

        var crTxt = MakeTMP(canvasObj.transform, "CrystalText", "0 / 13", 26);
        var crRT = crTxt.GetComponent<RectTransform>();
        crRT.anchorMin = crRT.anchorMax = new Vector2(1,1);
        crRT.anchoredPosition = new Vector2(-80,-35); crRT.sizeDelta = new Vector2(150,40);
        crTxt.GetComponent<TextMeshProUGUI>().color = new Color(0.3f,1,0.8f);

        var prTxt = MakeTMP(canvasObj.transform, "PressureText", "1.0 ATM", 16);
        var prRT = prTxt.GetComponent<RectTransform>();
        prRT.anchorMin = prRT.anchorMax = Vector2.zero;
        prRT.anchoredPosition = new Vector2(80,35); prRT.sizeDelta = new Vector2(140,28);
        var prFrame = MakeImg(canvasObj.transform, "PressureFrame", Color.clear);
        Stretch(prFrame);

        var crosshairRoot = MakePanel(canvasObj.transform, "Crosshair",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(20, 20));
        var crosshairH = MakeImg(crosshairRoot.transform, "CrosshairH", new Color(0.9f, 0.98f, 1f, 0.9f));
        var crosshairHRT = crosshairH.GetComponent<RectTransform>();
        crosshairHRT.anchorMin = crosshairHRT.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairHRT.anchoredPosition = Vector2.zero;
        crosshairHRT.sizeDelta = new Vector2(16, 2);
        var crosshairV = MakeImg(crosshairRoot.transform, "CrosshairV", new Color(0.9f, 0.98f, 1f, 0.9f));
        var crosshairVRT = crosshairV.GetComponent<RectTransform>();
        crosshairVRT.anchorMin = crosshairVRT.anchorMax = new Vector2(0.5f, 0.5f);
        crosshairVRT.anchoredPosition = Vector2.zero;
        crosshairVRT.sizeDelta = new Vector2(2, 16);

        var bp = MakePanel(canvasObj.transform, "BuffPanel", new Vector2(1,1), new Vector2(1,1), new Vector2(-80,-75), new Vector2(160,28));
        var hi = MakeImg(bp.transform, "Healer", new Color(0.2f,1,0.4f,0.15f)); SetRT(hi, new Vector2(8,0), new Vector2(28,28));
        var si = MakeImg(bp.transform, "Speeder", new Color(1,0.9f,0.2f,0.15f)); SetRT(si, new Vector2(42,0), new Vector2(28,28));
        var ai = MakeImg(bp.transform, "Armorer", new Color(0.3f,0.5f,1,0.15f)); SetRT(ai, new Vector2(76,0), new Vector2(28,28));
        var sci = MakeImg(bp.transform, "Scout", new Color(0.8f,0.3f,1,0.15f)); SetRT(sci, new Vector2(110,0), new Vector2(28,28));

        var vp = MakePanel(canvasObj.transform, "VictoryPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(500,300));
        vp.AddComponent<Image>().color = new Color(0,0.15f,0.3f,0.92f);
        var vTxt = MakeTMP(vp.transform, "VictoryStats", "КАНЬОН ИССЛЕДОВАН!", 30);
        Stretch(vTxt); vTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        vp.SetActive(false);

        var gop = MakePanel(canvasObj.transform, "GameOverPanel", new Vector2(0.5f,0.5f), new Vector2(0.5f,0.5f), Vector2.zero, new Vector2(500,300));
        gop.AddComponent<Image>().color = new Color(0.25f,0,0,0.92f);
        var goTxt = MakeTMP(gop.transform, "GOText", "КОРПУС РАЗРУШЕН", 30);
        var goTR = goTxt.GetComponent<RectTransform>();
        goTR.anchorMin = new Vector2(0,0.5f); goTR.anchorMax = Vector2.one; goTR.sizeDelta = Vector2.zero;
        goTxt.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;

        var btnGO = new GameObject("RetryButton");
        btnGO.transform.SetParent(gop.transform, false);
        var bRT = btnGO.AddComponent<RectTransform>();
        bRT.anchorMin = new Vector2(0.3f,0.12f); bRT.anchorMax = new Vector2(0.7f,0.32f); bRT.sizeDelta = Vector2.zero;
        btnGO.AddComponent<Image>().color = new Color(0.2f,0.45f,0.7f);
        var btn = btnGO.AddComponent<Button>();
        var bLbl = MakeTMP(btnGO.transform, "Lbl", "ЗАНОВО", 20); Stretch(bLbl);
        bLbl.GetComponent<TextMeshProUGUI>().alignment = TextAlignmentOptions.Center;
        gop.SetActive(false);

        var es = new GameObject("EventSystem");
        es.AddComponent<UnityEngine.EventSystems.EventSystem>();
        es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

        var hud = canvasObj.AddComponent<HUDController>();
        SetField(hud, "depthTracker", bathyscaphe.GetComponent<DepthTracker>());
        SetField(hud, "hullIntegrity", bathyscaphe.GetComponent<HullIntegrity>());
        SetField(hud, "creatureBuff", bathyscaphe.GetComponent<CreatureBuff>());
        SetField(hud, "depthFill", depthFill.GetComponent<Image>());
        SetField(hud, "depthText", depthTxt.GetComponent<TextMeshProUGUI>());
        SetField(hud, "hullFill", hullFill.GetComponent<Image>());
        SetField(hud, "hullText", hullTxt.GetComponent<TextMeshProUGUI>());
        SetField(hud, "crystalText", crTxt.GetComponent<TextMeshProUGUI>());
        SetField(hud, "pressureText", prTxt.GetComponent<TextMeshProUGUI>());
        SetField(hud, "pressureFrame", prFrame.GetComponent<Image>());
        SetField(hud, "healerIcon", hi.GetComponent<Image>());
        SetField(hud, "speederIcon", si.GetComponent<Image>());
        SetField(hud, "armorerIcon", ai.GetComponent<Image>());
        SetField(hud, "scoutIcon", sci.GetComponent<Image>());
        SetField(hud, "victoryPanel", vp);
        SetField(hud, "victoryStatsText", vTxt.GetComponent<TextMeshProUGUI>());
        SetField(hud, "gameOverPanel", gop);
        SetField(hud, "retryButton", btn);
    }

    static void SetupDepthPostProcess(GameObject systems, Volume volume)
    {
        var dpp = systems.AddComponent<DepthPostProcess>();
        var bathy = GameObject.FindWithTag("Player");
        SetField(dpp, "globalVolume", volume);
        SetField(dpp, "depthTracker", bathy?.GetComponent<DepthTracker>());
    }

    static void CreateAmbientVFX(Transform target)
    {
        var go = new GameObject("UnderwaterAmbient");
        var amb = go.AddComponent<UnderwaterAmbient>();
        SetField(amb, "followTarget", target);

        var dust = new GameObject("Dust"); dust.transform.SetParent(go.transform);
        var dps = dust.AddComponent<ParticleSystem>();
        var dm = dps.main;
        dm.startLifetime = 5f; dm.startSpeed = 0.25f; dm.startSize = 0.05f;
        dm.startColor = new Color(0.8f,0.9f,1f,0.25f);
        dm.maxParticles = 150; dm.simulationSpace = ParticleSystemSimulationSpace.World;
        var de = dps.emission; de.rateOverTime = 12f;
        var ds = dps.shape; ds.shapeType = ParticleSystemShapeType.Box; ds.scale = new Vector3(25,25,25);
        ApplyParticleMaterial(dps, new Color(0.85f, 0.95f, 1f, 0.18f));
        SetField(amb, "dustParticles", dps);

        var bub = new GameObject("Bubbles"); bub.transform.SetParent(go.transform);
        var bps = bub.AddComponent<ParticleSystem>();
        var bm = bps.main;
        bm.startLifetime = 4f; bm.startSpeed = 1.5f;
        bm.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
        bm.startColor = new Color(0.7f,0.85f,1f,0.4f);
        bm.maxParticles = 60; bm.simulationSpace = ParticleSystemSimulationSpace.World;
        var be = bps.emission; be.rateOverTime = 8f;
        var bs = bps.shape; bs.shapeType = ParticleSystemShapeType.Sphere; bs.radius = 2.5f;
        var bv = bps.velocityOverLifetime; bv.enabled = true; bv.y = 2.5f;
        ApplyParticleMaterial(bps, new Color(0.8f, 0.92f, 1f, 0.28f));
        SetField(amb, "bubbleParticles", bps);
    }

    static void SetupRemainingComponents(GameObject systems, GameObject bathyscaphe)
    {
        var spawner = systems.AddComponent<CreatureSpawner>();
        SetField(spawner, "flockingManager", systems.GetComponent<FlockingManager>());

        var scout = systems.AddComponent<ScoutHighlight>();
        SetField(scout, "creatureBuff", bathyscaphe.GetComponent<CreatureBuff>());
        SetField(scout, "player", bathyscaphe.transform);

        var symbiosis = new GameObject("CreatureSymbiosisVFX");
        symbiosis.transform.SetParent(bathyscaphe.transform, false);
        symbiosis.transform.localPosition = Vector3.zero;

        var pulses = symbiosis.AddComponent<ParticleSystem>();
        var main = pulses.main;
        main.startLifetime = 1.1f;
        main.startSpeed = 0.8f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.startColor = new Color(0.8f, 0.95f, 1f, 0.35f);
        main.maxParticles = 36;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        var emission = pulses.emission;
        emission.rateOverTime = 10f;
        var shape = pulses.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.9f;
        ApplyParticleMaterial(pulses, new Color(0.8f, 0.95f, 1f, 0.22f));
        pulses.Stop();

        var symbiosisVfx = symbiosis.AddComponent<CreatureSymbiosisVFX>();
        SetField(symbiosisVfx, "flockingManager", systems.GetComponent<FlockingManager>());
        SetField(symbiosisVfx, "source", bathyscaphe.transform);
    }

    // ===================== HELPERS =====================

    static void SetMat(GameObject go, Color c, bool transparent = false)
    {
        var r = go.GetComponent<Renderer>(); if (!r) return;
        var m = CreateLitMaterial(c, transparent);
        if (transparent)
        {
            if (m.HasProperty("_Surface")) m.SetFloat("_Surface", 1);
            if (m.HasProperty("_Blend")) m.SetFloat("_Blend", 0);
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = 3000;
            if (m.HasProperty("_SrcBlend")) m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
            if (m.HasProperty("_DstBlend")) m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
            if (m.HasProperty("_ZWrite")) m.SetInt("_ZWrite", 0);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }
        r.sharedMaterial = m;
    }

    static Material CreateLitMaterial(Color color, bool transparent = false)
    {
        var shader = FindFirstAvailableShader(
            "Universal Render Pipeline/Lit",
            "Standard",
            "Diffuse");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    static void SetEmission(GameObject go, Color c, float intensity)
    {
        var r = go.GetComponent<Renderer>(); if (!r) return;
        if (r.sharedMaterial.HasProperty("_EmissionColor"))
        {
            r.sharedMaterial.SetColor("_EmissionColor", c * intensity);
            r.sharedMaterial.EnableKeyword("_EMISSION");
        }
    }

    static void ApplyParticleMaterial(ParticleSystem ps, Color color)
    {
        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        if (renderer == null) return;
        renderer.sharedMaterial = CreateParticleMaterial(color);
        renderer.trailMaterial = renderer.sharedMaterial;
    }

    static Material CreateParticleMaterial(Color color)
    {
        var shader = FindFirstAvailableShader(
            "Universal Render Pipeline/Particles/Unlit",
            "Particles/Standard Unlit",
            "Sprites/Default",
            "Unlit/Color");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        return material;
    }

    static Material CreateTrailMaterial(Color color)
    {
        var shader = FindFirstAvailableShader(
            "Universal Render Pipeline/Particles/Unlit",
            "Sprites/Default",
            "Unlit/Color");
        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
            material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color"))
            material.SetColor("_Color", color);
        return material;
    }

    static Shader FindFirstAvailableShader(params string[] shaderNames)
    {
        for (int i = 0; i < shaderNames.Length; i++)
        {
            var shader = Shader.Find(shaderNames[i]);
            if (shader != null)
                return shader;
        }
        return Shader.Find("Sprites/Default");
    }

    static GameObject MakePanel(Transform p, string n, Vector2 amin, Vector2 amax, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        var rt = go.AddComponent<RectTransform>(); rt.anchorMin = amin; rt.anchorMax = amax;
        rt.anchoredPosition = pos; rt.sizeDelta = size; return go;
    }

    static GameObject MakeImg(Transform p, string n, Color c)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>(); go.AddComponent<Image>().color = c; return go;
    }

    static GameObject MakeTMP(Transform p, string n, string text, int sz)
    {
        var go = new GameObject(n); go.transform.SetParent(p, false);
        go.AddComponent<RectTransform>();
        var t = go.AddComponent<TextMeshProUGUI>(); t.text = text; t.fontSize = sz;
        t.alignment = TextAlignmentOptions.Center; t.color = Color.white; return go;
    }

    static void Stretch(GameObject go)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one; rt.sizeDelta = Vector2.zero;
    }

    static void SetRT(GameObject go, Vector2 pos, Vector2 size)
    {
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0, 0.5f);
        rt.anchoredPosition = pos; rt.sizeDelta = size;
    }

    static void SetField(object target, string name, object value)
    {
        var f = target.GetType().GetField(name,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        if (f != null) f.SetValue(target, value);
        else Debug.LogWarning($"[DeepCanyon] Field '{name}' not found on {target.GetType().Name}");
    }
}
