using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

// Generates the game scene from code, so no .unity file needs to be committed.
public static class BuildScript
{
    const string ScenePath = "Assets/Scenes/Main.unity";

    public static void BuildAndroid()
    {
        try
        {
            CreateScene();

            PlayerSettings.companyName = "MiniWarfare";
            PlayerSettings.productName = "Mini Warfare";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, "com.miniwarfare.game");
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel24;
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.LandscapeLeft;
            EditorUserBuildSettings.buildAppBundle = false;

            Directory.CreateDirectory("build/Android");
            var opts = new BuildPlayerOptions
            {
                scenes = new[] { ScenePath },
                locationPathName = "build/Android/MiniWarfare.apk",
                target = BuildTarget.Android,
                options = BuildOptions.None
            };
            var report = BuildPipeline.BuildPlayer(opts);
            if (report.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
                EditorApplication.Exit(1);
        }
        catch (System.Exception e)
        {
            Debug.LogError(e);
            EditorApplication.Exit(1);
        }
    }

    static Material Mat(string name, Color c)
    {
        var m = new Material(Shader.Find("Mobile/Diffuse")) { color = c };
        AssetDatabase.CreateAsset(m, "Assets/Materials/" + name + ".mat");
        return m;
    }

    static GameObject Prim(PrimitiveType t, string name, Vector3 pos, Vector3 scale, Material m, Transform parent)
    {
        var g = GameObject.CreatePrimitive(t);
        g.name = name;
        g.transform.SetParent(parent, false);
        g.transform.localPosition = pos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = m;
        return g;
    }

    static GameObject MakeBot(Team team, Vector3 pos, Material m)
    {
        var b = Prim(PrimitiveType.Capsule, team + "Bot", pos + Vector3.up, Vector3.one, m, null);
        var agent = b.AddComponent<NavMeshAgent>();
        agent.baseOffset = 1f;
        agent.speed = 4.5f;
        agent.radius = 0.4f;
        agent.enabled = false; // BotAI enables it after the NavMesh is built
        b.AddComponent<Health>().team = team;
        b.AddComponent<BotAI>();
        return b;
    }

    static void CreateScene()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Scenes")) AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Materials")) AssetDatabase.CreateFolder("Assets", "Materials");

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        Material ground = Mat("Ground", new Color(0.35f, 0.5f, 0.3f));
        Material cover = Mat("Cover", new Color(0.55f, 0.55f, 0.5f));
        Material blue = Mat("Blue", new Color(0.2f, 0.4f, 0.9f));
        Material red = Mat("Red", new Color(0.9f, 0.2f, 0.2f));
        Material dark = Mat("Gun", new Color(0.1f, 0.1f, 0.1f));

        // Environment (map 100x100)
        var env = new GameObject("Environment").transform;
        Prim(PrimitiveType.Plane, "Ground", Vector3.zero, new Vector3(10, 1, 10), ground, env);
        Prim(PrimitiveType.Cube, "WallN", new Vector3(0, 3, 50), new Vector3(100, 6, 1), cover, env);
        Prim(PrimitiveType.Cube, "WallS", new Vector3(0, 3, -50), new Vector3(100, 6, 1), cover, env);
        Prim(PrimitiveType.Cube, "WallE", new Vector3(50, 3, 0), new Vector3(1, 6, 100), cover, env);
        Prim(PrimitiveType.Cube, "WallW", new Vector3(-50, 3, 0), new Vector3(1, 6, 100), cover, env);

        var rng = new System.Random(42);
        System.Func<float, float, float> R = (a, b) => a + (float)rng.NextDouble() * (b - a);
        for (int i = 0; i < 45; i++)
        {
            float sx = R(2, 8), sy = R(1.2f, 4f), sz = R(2, 8);
            Prim(PrimitiveType.Cube, "Cover" + i, new Vector3(R(-42, 42), sy / 2f, R(-30, 30)),
                new Vector3(sx, sy, sz), cover, env);
        }

        // Spawn points
        var blueSpawn = new GameObject("BlueSpawn").transform; blueSpawn.position = new Vector3(0, 0, -42);
        var redSpawn = new GameObject("RedSpawn").transform; redSpawn.position = new Vector3(0, 0, 42);

        // Player
        var player = new GameObject("Player");
        player.transform.position = blueSpawn.position + Vector3.up * 0.1f;
        var cc = player.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.35f; cc.center = new Vector3(0, 0.9f, 0);
        var hp = player.AddComponent<Health>(); hp.team = Team.Blue;
        var weapon = player.AddComponent<Weapon>(); weapon.team = Team.Blue;
        var pc = player.AddComponent<PlayerController>();

        var cam = Camera.main;
        cam.transform.SetParent(player.transform, false);
        cam.transform.localPosition = new Vector3(0, 1.6f, 0);
        cam.transform.localRotation = Quaternion.identity;
        cam.farClipPlane = 150f;
        pc.cam = cam.transform;
        weapon.cam = cam.transform;

        var gun = Prim(PrimitiveType.Cube, "Gun", new Vector3(0.25f, -0.25f, 0.55f),
            new Vector3(0.08f, 0.1f, 0.5f), dark, cam.transform);
        Object.DestroyImmediate(gun.GetComponent<Collider>());

        // Bots: you + 4 allies vs 5 enemies
        for (int i = 0; i < 4; i++) MakeBot(Team.Blue, blueSpawn.position + new Vector3(-6 + i * 4, 0, 4), blue);
        for (int i = 0; i < 5; i++) MakeBot(Team.Red, redSpawn.position + new Vector3(-8 + i * 4, 0, -4), red);

        // Game manager
        var gm = new GameObject("GameManager").AddComponent<GameManager>();
        gm.environment = env;
        gm.blueSpawn = blueSpawn;
        gm.redSpawn = redSpawn;
        gm.player = hp;
        gm.playerWeapon = weapon;

        EditorSceneManager.SaveScene(scene, ScenePath);
        EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        AssetDatabase.SaveAssets();
    }
}
