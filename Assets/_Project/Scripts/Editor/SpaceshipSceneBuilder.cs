using System.IO;
using Unity.Mathematics;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.Splines;
using UnityEngine.UI;

namespace Spaceship.EditorTools
{
    /// <summary>
    /// Builds the greybox test level from the GDD: rail, ship with 5-slot movement and cockpit camera,
    /// two-player split touch input, weapons, shared weapon, status bar, placeholder targets, post-processing.
    /// Re-runnable: overwrites Level01 and the generated assets.
    /// </summary>
    public static class SpaceshipSceneBuilder
    {
        const string Root = "Assets/_Project";
        const string ScenePath = Root + "/Scenes/Level01.unity";

        [MenuItem("Spaceship/Build Greybox Level01")]
        public static void Build()
        {
            var mats = CreateMaterials();
            var weapons = CreateWeapons();
            var volumeProfile = CreateVolumeProfile();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Lighting & environment ---
            RenderSettings.skybox = null;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.12f, 0.14f, 0.22f);
            var sun = new GameObject("Directional Light").AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.88f);
            sun.intensity = 1.6f;
            sun.shadows = LightShadows.Soft;
            sun.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = volumeProfile;

            // --- Rail ---
            var railGo = new GameObject("Rail");
            var rail = railGo.AddComponent<SplineContainer>();
            rail.Spline = new Spline(new float3[]
            {
                new(0, 0, 0), new(0, 0, 150), new(40, 10, 300), new(0, 20, 450), new(-50, 0, 600), new(0, 0, 750),
            }, TangentMode.AutoSmooth);

            // --- Ship ---
            var shipRoot = new GameObject("PlayerShip");
            var follower = shipRoot.AddComponent<RailFollower>();
            Set(follower, "rail", rail);
            var status = shipRoot.AddComponent<ShipStatus>();

            var body = new GameObject("ShipBody");
            body.layer = 2; // Ignore Raycast: player shots never hit our own ship
            body.transform.SetParent(shipRoot.transform, false);
            var slots = body.AddComponent<ShipSlotController>();
            var rb = body.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity = false;
            var shipCol = body.AddComponent<BoxCollider>();
            shipCol.size = new Vector3(3f, 1.5f, 4f);

            var camGo = new GameObject("Cockpit Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(body.transform, false);
            camGo.transform.localPosition = new Vector3(0, 0.4f, 0);
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.01f, 0.01f, 0.035f);
            cam.fieldOfView = 70f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 2000f;
            camGo.AddComponent<AudioListener>();
            var camData = cam.GetUniversalAdditionalCameraData();
            camData.renderPostProcessing = true;

            // --- Systems ---
            var systems = new GameObject("GameSystems");
            var level = systems.AddComponent<LevelManager>();
            Set(level, "rail", follower);
            Set(level, "ship", status);

            var shared = systems.AddComponent<SharedWeaponController>();
            Set(shared, "ship", status);
            Set(shared, "viewCamera", cam);

            var left = CreatePlayer(systems.transform, PlayerSide.Left, cam, status, weapons, mats.leftLine);
            var right = CreatePlayer(systems.transform, PlayerSide.Right, cam, status, weapons, mats.rightLine);

            var input = systems.AddComponent<SplitTouchInput>();
            Set(input, "leftPlayer", left);
            Set(input, "rightPlayer", right);

            // --- Targets & hazards along the rail ---
            var targets = new GameObject("Encounters").transform;
            var rng = new System.Random(7);
            for (int i = 0; i < 30; i++)
            {
                float t = 0.06f + i * 0.03f;
                rail.Evaluate(t, out var p, out var tan, out var up);
                var frame = Quaternion.LookRotation((Vector3)tan, (Vector3)up);
                var offset = new Vector3((float)(rng.NextDouble() * 16 - 8), (float)(rng.NextDouble() * 10 - 5), 0);
                bool asteroid = i % 3 == 2;
                var go = GameObject.CreatePrimitive(asteroid ? PrimitiveType.Sphere : PrimitiveType.Cube);
                go.name = asteroid ? $"Asteroid_{i:00}" : $"EnemyPlaceholder_{i:00}";
                go.transform.SetParent(targets, false);
                go.transform.SetPositionAndRotation((Vector3)p + frame * offset, frame);
                go.transform.localScale = asteroid ? Vector3.one * (2f + (float)rng.NextDouble() * 2f) : new Vector3(2.5f, 1f, 2.5f);
                go.GetComponent<Renderer>().sharedMaterial = asteroid ? mats.asteroid : mats.enemy;
                var dmg = go.AddComponent<Damageable>();
                Set(dmg, "maxHealth", asteroid ? 40f : 20f);
                Set(dmg, "scoreValue", asteroid ? 50 : 100);
                if (asteroid)
                {
                    go.GetComponent<Collider>().isTrigger = true;
                    go.AddComponent<ShipHazard>();
                }
            }

            // --- UI ---
            BuildUI(status, slots, left, right, shared);

            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log($"[Spaceship] Built {ScenePath}");
        }

        static PlayerWeaponController CreatePlayer(Transform parent, PlayerSide side, Camera cam, ShipStatus status,
            WeaponDefinition[] weapons, Material lineMat)
        {
            var go = new GameObject($"Player_{side}");
            go.transform.SetParent(parent, false);
            var line = go.AddComponent<LineRenderer>();
            line.useWorldSpace = true;
            line.positionCount = 0;
            line.widthMultiplier = 0.015f;
            line.sharedMaterial = lineMat;
            line.shadowCastingMode = ShadowCastingMode.Off;

            var pwc = go.AddComponent<PlayerWeaponController>();
            Set(pwc, "side", (int)side);
            Set(pwc, "aimCamera", cam);
            Set(pwc, "ship", status);
            Set(pwc, "drawLine", line);
            var so = new SerializedObject(pwc);
            var arr = so.FindProperty("weapons");
            arr.arraySize = weapons.Length;
            for (int i = 0; i < weapons.Length; i++) arr.GetArrayElementAtIndex(i).objectReferenceValue = weapons[i];
            so.ApplyModifiedPropertiesWithoutUndo();
            return pwc;
        }

        // ---------------- UI ----------------

        static void BuildUI(ShipStatus status, ShipSlotController slots, PlayerWeaponController left,
            PlayerWeaponController right, SharedWeaponController shared)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();

            var canvasGo = new GameObject("HUD Canvas");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 1f; // touch walls are often ultra-wide: scale by height
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // Top status bar (GDD 2.5)
            var bar = Panel(root, "TopStatusBar", new Vector2(0.3f, 1), new Vector2(0.7f, 1), new Vector2(0, -70), new Vector2(0, -10), new Color(0, 0, 0, 0.45f));
            var hp = Fill(bar, "HealthFill", new Vector2(0.02f, 0.55f), new Vector2(0.98f, 0.9f), new Color(0.3f, 1f, 0.45f));
            var en = Fill(bar, "EnergyFill", new Vector2(0.02f, 0.1f), new Vector2(0.98f, 0.45f), new Color(0.25f, 0.7f, 1f));
            var statusBar = bar.gameObject.AddComponent<ShipStatusBar>();
            Set(statusBar, "ship", status);
            Set(statusBar, "healthFill", hp);
            Set(statusBar, "energyFill", en);

            // Per-player weapon buttons (GDD 2.3): left edge / right edge
            Color[] weaponColors = { new(1f, 0.25f, 0.3f), new(1f, 0.75f, 0.2f), new(0.7f, 0.35f, 1f) };
            string[] weaponNames = { "Laser", "MachineGun", "AreaDestruction" };
            foreach (var (pwc, ax) in new[] { (left, 0f), (right, 1f) })
            {
                var col = Panel(root, $"Weapons_{pwc.Side}", new Vector2(ax, 0.25f), new Vector2(ax, 0.75f),
                    new Vector2(ax == 0 ? 20 : -160, 0), new Vector2(ax == 0 ? 160 : -20, 0), Color.clear);
                for (int i = 0; i < 3; i++)
                {
                    var b = Button(col, $"{weaponNames[i]}Button", new Vector2(0, 1 - (i + 1) / 3f), new Vector2(1, 1 - i / 3f), weaponColors[i]);
                    UnityEventTools.AddIntPersistentListener(b.onClick, pwc.SelectWeapon, i);
                }
            }

            // Center: shared weapon (two confirm buttons) + 5-position movement pad (GDD 2.3, 2.4)
            var center = Panel(root, "CenterConsole", new Vector2(0.5f, 0), new Vector2(0.5f, 0), new Vector2(-170, 20), new Vector2(170, 380), new Color(0, 0, 0, 0.3f));
            var sl = Button(center, "SharedConfirm_Left", new Vector2(0.03f, 0.78f), new Vector2(0.48f, 0.97f), new Color(0.2f, 0.9f, 1f));
            UnityEventTools.AddPersistentListener(sl.onClick, shared.ConfirmLeft);
            var sr = Button(center, "SharedConfirm_Right", new Vector2(0.52f, 0.78f), new Vector2(0.97f, 0.97f), new Color(1f, 0.55f, 0.15f));
            UnityEventTools.AddPersistentListener(sr.onClick, shared.ConfirmRight);

            var pad = new Color(0.85f, 0.85f, 0.95f);
            UnityEventTools.AddPersistentListener(Button(center, "Move_Up", new(0.36f, 0.52f), new(0.64f, 0.74f), pad).onClick, slots.GoUp);
            UnityEventTools.AddPersistentListener(Button(center, "Move_Left", new(0.05f, 0.27f), new(0.33f, 0.49f), pad).onClick, slots.GoLeft);
            UnityEventTools.AddPersistentListener(Button(center, "Move_Center", new(0.36f, 0.27f), new(0.64f, 0.49f), new Color(0.6f, 1f, 0.7f)).onClick, slots.GoCenter);
            UnityEventTools.AddPersistentListener(Button(center, "Move_Right", new(0.67f, 0.27f), new(0.95f, 0.49f), pad).onClick, slots.GoRight);
            UnityEventTools.AddPersistentListener(Button(center, "Move_Down", new(0.36f, 0.02f), new(0.64f, 0.24f), pad).onClick, slots.GoDown);

            // Thin divider showing the two player halves
            Panel(root, "SplitDivider", new Vector2(0.5f, 0.35f), new Vector2(0.5f, 0.9f), new Vector2(-1, 0), new Vector2(1, 0), new Color(1, 1, 1, 0.15f))
                .GetComponent<Image>().raycastTarget = false;
        }

        static RectTransform Panel(Transform parent, string name, Vector2 aMin, Vector2 aMax, Vector2 oMin, Vector2 oMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            var rt = (RectTransform)go.transform;
            rt.SetParent(parent, false);
            rt.anchorMin = aMin; rt.anchorMax = aMax; rt.offsetMin = oMin; rt.offsetMax = oMax;
            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = color.a > 0f;
            return rt;
        }

        static Image Fill(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            var rt = Panel(parent, name, aMin, aMax, Vector2.zero, Vector2.zero, color);
            var img = rt.GetComponent<Image>();
            img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 1f;
            img.raycastTarget = false;
            return img;
        }

        static Button Button(RectTransform parent, string name, Vector2 aMin, Vector2 aMax, Color color)
        {
            var rt = Panel(parent, name, aMin, aMax, new Vector2(4, 4), new Vector2(-4, -4), color);
            rt.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            rt.GetComponent<Image>().type = Image.Type.Sliced;
            return rt.gameObject.AddComponent<Button>();
        }

        // ---------------- Assets ----------------

        struct Mats { public Material enemy, asteroid, leftLine, rightLine; }

        static Mats CreateMaterials()
        {
            var lit = GraphicsSettings.currentRenderPipeline.defaultMaterial.shader;
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            if (unlit == null) throw new System.Exception("URP Unlit shader not found");
            return new Mats
            {
                enemy = Mat("M_EnemyPlaceholder", lit, new Color(0.6f, 0.1f, 0.12f), new Color(2f, 0.2f, 0.2f)),
                asteroid = Mat("M_Asteroid", lit, new Color(0.35f, 0.33f, 0.31f), Color.black),
                leftLine = Mat("M_DrawLine_Left", unlit, new Color(0.2f, 0.9f, 1f), Color.black),
                rightLine = Mat("M_DrawLine_Right", unlit, new Color(1f, 0.55f, 0.15f), Color.black),
            };
        }

        static Material Mat(string name, Shader shader, Color baseColor, Color emission)
        {
            var path = $"{Root}/Materials/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null) { m = new Material(shader); AssetDatabase.CreateAsset(m, path); }
            m.shader = shader;
            m.SetColor("_BaseColor", baseColor);
            if (emission.maxColorComponent > 0f)
            {
                m.EnableKeyword("_EMISSION");
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
                m.SetColor("_EmissionColor", emission);
            }
            EditorUtility.SetDirty(m);
            return m;
        }

        static WeaponDefinition[] CreateWeapons() => new[]
        {
            Weapon("Weapon_Laser", WeaponType.Laser, "Laser", damage: 25f, cost: 3f, interval: 0.25f),
            Weapon("Weapon_MachineGun", WeaponType.MachineGun, "Machine Gun", damage: 4f, cost: 0.6f, interval: 0.08f),
            Weapon("Weapon_AreaDestruction", WeaponType.AreaDestruction, "Area Destruction", damage: 50f, cost: 20f, interval: 1f),
        };

        static WeaponDefinition Weapon(string file, WeaponType type, string display, float damage, float cost, float interval)
        {
            var path = $"{Root}/Data/Weapons/{file}.asset";
            var w = AssetDatabase.LoadAssetAtPath<WeaponDefinition>(path);
            if (w == null) { w = ScriptableObject.CreateInstance<WeaponDefinition>(); AssetDatabase.CreateAsset(w, path); }
            w.type = type; w.displayName = display; w.damage = damage; w.energyCost = cost; w.fireInterval = interval;
            EditorUtility.SetDirty(w);
            return w;
        }

        static VolumeProfile CreateVolumeProfile()
        {
            var path = $"{Root}/Data/VP_Space.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = profile.Add<Tonemapping>(true);
            tone.mode.Override(TonemappingMode.ACES);
            var bloom = profile.Add<Bloom>(true);
            bloom.intensity.Override(0.8f);
            bloom.threshold.Override(0.9f);
            bloom.scatter.Override(0.7f);
            var vig = profile.Add<Vignette>(true);
            vig.intensity.Override(0.25f);
            foreach (var c in profile.components) { c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy; AssetDatabase.AddObjectToAsset(c, profile); }
            EditorUtility.SetDirty(profile);
            return profile;
        }

        // ---------------- Serialized field helper ----------------

        static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new System.Exception($"{target.GetType().Name}.{field} not found");
            switch (value)
            {
                case Object o: p.objectReferenceValue = o; break;
                case float f: p.floatValue = f; break;
                case int i when p.propertyType == SerializedPropertyType.Enum: p.enumValueIndex = i; break;
                case int i: p.intValue = i; break;
                case bool b: p.boolValue = b; break;
                default: throw new System.Exception($"Unsupported value for {field}");
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
