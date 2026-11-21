using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

[InitializeOnLoad]
public static class RebuildAllWeaponAnimations
{
    private const string SessionKey = "RebuildTriggered_V15";

    static RebuildAllWeaponAnimations()
    {
        if (!SessionState.GetBool(SessionKey, false))
        {
            SessionState.SetBool(SessionKey, true);
            EditorApplication.delayCall += () =>
            {
                Run(true);
                SetupCodeDrivenAnimationSystem();
            };
        }
    }

    [MenuItem("Tools/Setup Code-Driven Animation System")]
    public static void SetupCodeDrivenAnimationSystem()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);

        Player1 player = UnityEngine.Object.FindFirstObjectByType<Player1>();
        if (player == null)
        {
            Debug.LogError("Player1 not found in SampleScene!");
            return;
        }

        var animSystem = player.GetComponent<WeaponAnimationSystem>();
        if (animSystem == null)
        {
            animSystem = player.gameObject.AddComponent<WeaponAnimationSystem>();
        }

        animSystem.AutoLoadClipsIfNull();
        animSystem.FindTargetAnimator();
        player.Weapon_Anim_System = animSystem;

        // Mecanim controller'ı sıfırla ki çakışma olmasın
        if (animSystem.targetAnimator != null)
        {
            animSystem.targetAnimator.runtimeAnimatorController = null;
            UnityEditor.EditorUtility.SetDirty(animSystem.targetAnimator);
        }

        // Shotgun referansını ShotGun_C yap
        GameObject shotgunC = null;
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "ShotGun_C" || t.name == "Shotgun_C")
            {
                shotgunC = t.gameObject;
                break;
            }
        }
        if (shotgunC != null)
        {
            player.Shotgun = shotgunC;
        }

        // Sahnedeki fazlalık ShotGun_F objelerini kapat
        foreach (var t in UnityEngine.Object.FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (t.name == "ShotGun_F" || t.name == "Shotgun_F")
            {
                t.gameObject.SetActive(false);
            }
        }

        UnityEditor.EditorUtility.SetDirty(player);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupCodeDrivenAnimationSystem] Kod tabanlı animasyon sistemi başarıyla kuruldu ve SampleScene kaydedildi!");
    }

    [MenuItem("Tools/Rebuild All Weapon Animations")]
    public static void MenuItemRun()
    {
        Run(true);
    }

    [MenuItem("Tools/Build Weapon State Machine")]
    public static void MenuItemBuildStateMachine()
    {
        BuildStateMachine();
    }

    public static void Run(bool refreshAssets = false)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== RebuildAllWeaponAnimations Started ===");

        if (refreshAssets)
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        var tasks = new (string fbxPath, string animPath, string stateName, bool loop, bool reverse)[]
        {
            // Pistol
            ("Assets/Game/Animations/Double_Hand_Pistol_Idle.fbx", "Assets/Game/Animations/Pistol_Idle_Fixed.anim", "Pistol_Idle", true, false),
            ("Assets/Game/Animations/Double_Hand_Pistol_Fire.fbx", "Assets/Game/Animations/Pistol_Fire_Fixed.anim", "Pistol_Fire", false, false),
            ("Assets/Game/Animations/Double_Hand_Pistol_Reload.fbx", "Assets/Game/Animations/Pistol_Reload_Fixed.anim", "Pistol_Reload", false, false),
            ("Assets/Game/Animations/Double_Hand_Pistol_Equip.fbx", "Assets/Game/Animations/Pistol_Equip_Fixed.anim", "Pistol_Equip", false, true),
            ("Assets/Game/Animations/Double_Hand_Pistol_Run.fbx", "Assets/Game/Animations/Pistol_Run_Fixed.anim", "Pistol_Run", true, false),
            ("Assets/Game/Animations/Double_Hand_Pistol_Sprint.fbx", "Assets/Game/Animations/Pistol_Sprint_Fixed.anim", "Pistol_Sprint", true, false),

            // Shotgun (Base states)
            ("Assets/Game/Animations/Double_Hand_Shotgun_Idle.fbx", "Assets/Game/Animations/Shotgun_Idle_Fixed.anim", "Shotgun_Idle", true, false),
            ("Assets/Game/Animations/Double_Hand_Shotgun_Fire.fbx", "Assets/Game/Animations/Shotgun_Fire_Fixed.anim", "Shotgun_Fire", false, false),
            ("Assets/Game/Animations/Double_Hand_Shotgun_Equip.fbx", "Assets/Game/Animations/Shotgun_Equip_Fixed.anim", "Shotgun_Equip", false, true),
            ("Assets/Game/Animations/Double_Hand_Shotgun_Run.fbx", "Assets/Game/Animations/Shotgun_Run_Fixed.anim", "Shotgun_Run", true, false),

            // Shotgun (Multi-Stage Single-Shell Reload)
            ("Assets/Game/Animations/Double_Hand_Shotgun_Preparation_Before.fbx", "Assets/Game/Animations/Shotgun_Preparation_Before_Fixed.anim", "Shotgun_Reload_Start", false, false),
            ("Assets/Game/Animations/Double_Hand_Shotgun_Reload_Insert.fbx", "Assets/Game/Animations/Shotgun_Reload_Insert_Fixed.anim", "Shotgun_Reload_Insert", false, false),
            ("Assets/Game/Animations/Double_Hand_Shotgun_Preparation_After.fbx", "Assets/Game/Animations/Shotgun_Preparation_After_Fixed.anim", "Shotgun_Reload_End", false, false),

            // Rifle
            ("Assets/Game/Animations/Double_Hand_Rifle_Idle.fbx", "Assets/Game/Animations/Rifle_Idle_Fixed.anim", "Rifle_Idle", true, false),
            ("Assets/Game/Animations/Double_Hand_Rifle_Fire.fbx", "Assets/Game/Animations/Rifle_Fire_Fixed.anim", "Rifle_Fire", false, false),
            ("Assets/Game/Animations/Double_Hand_Rifle_Reload.fbx", "Assets/Game/Animations/Rifle_Reload_Fixed.anim", "Rifle_Reload", false, false),
            ("Assets/Game/Animations/Double_Hand_Rifle_Equip.fbx", "Assets/Game/Animations/Rifle_Equip_Fixed.anim", "Rifle_Equip", false, false),
            ("Assets/Game/Animations/Double_Hand_Rifle_Run.fbx", "Assets/Game/Animations/Rifle_Run_Fixed.anim", "Rifle_Run", true, false),
        };

        var createdClips = new Dictionary<string, AnimationClip>();

        foreach (var t in tasks)
        {
            AssetDatabase.ImportAsset(t.fbxPath, ImportAssetOptions.ForceSynchronousImport);
            var assets = AssetDatabase.LoadAllAssetsAtPath(t.fbxPath);
            AnimationClip srcClip = null;
            foreach (var a in assets)
            {
                if (a is AnimationClip ac && !ac.name.StartsWith("__preview__"))
                {
                    srcClip = ac;
                    break;
                }
            }

            if (srcClip == null)
            {
                sb.AppendLine($"[WARN] Could not find AnimationClip in {t.fbxPath} (assets count: {assets.Length})");
                continue;
            }

            AnimationClip fixedClip = new AnimationClip();
            fixedClip.name = Path.GetFileNameWithoutExtension(t.animPath);
            fixedClip.frameRate = srcClip.frameRate;
            fixedClip.legacy = false;

            var bindings = AnimationUtility.GetCurveBindings(srcClip);
            int count = 0;
            float clipLen = srcClip.length;

            foreach (var cb in bindings)
            {
                if (cb.propertyName.Contains("Scale"))
                    continue;

                if (string.IsNullOrEmpty(cb.path) || cb.path == "Armature" || cb.path.Contains("camera") || cb.path.Contains("IK"))
                    continue;

                // Position curves: ignore root motion on pose_controller, but preserve shoulder and body positions
                if (cb.propertyName.Contains("Position"))
                {
                    if (cb.path == "pose_controller" || cb.path.EndsWith("pose_controller"))
                        continue;
                }

                string newPath = cb.path.StartsWith("Armature/") ? cb.path : "Armature/" + cb.path;
                var curve = AnimationUtility.GetEditorCurve(srcClip, cb);
                if (curve != null && curve.keys.Length > 0)
                {
                    if (t.reverse && clipLen > 0.001f)
                    {
                        var keys = curve.keys;
                        var newKeys = new Keyframe[keys.Length];
                        for (int k = 0; k < keys.Length; k++)
                        {
                            var orig = keys[k];
                            newKeys[k] = new Keyframe(
                                clipLen - orig.time,
                                orig.value,
                                -orig.outTangent,
                                -orig.inTangent
                            );
                        }
                        Array.Sort(newKeys, (k1, k2) => k1.time.CompareTo(k2.time));
                        curve = new AnimationCurve(newKeys);
                    }

                    EditorCurveBinding newBinding = cb;
                    newBinding.path = newPath;
                    AnimationUtility.SetEditorCurve(fixedClip, newBinding, curve);
                    count++;
                }
            }

            if (t.loop)
            {
                var settings = AnimationUtility.GetAnimationClipSettings(fixedClip);
                settings.loopTime = true;
                AnimationUtility.SetAnimationClipSettings(fixedClip, settings);
            }

            AssetDatabase.CreateAsset(fixedClip, t.animPath);
            createdClips[t.stateName] = fixedClip;
            sb.AppendLine($"Created {t.animPath} with {count} rotation curves (len: {srcClip.length:F2}s, loop: {t.loop}, rev: {t.reverse})");
        }

        AssetDatabase.SaveAssets();

        // Rebuild and wire state machine in Player_Animator.controller
        BuildStateMachine();
        sb.AppendLine("BuildStateMachine() completed.");

        File.WriteAllText("rebuild_all_result.txt", sb.ToString());
        Debug.Log(sb.ToString());
    }

    private class WeaponStates
    {
        public string Name;
        public int WeaponType;
        public AnimatorState Idle;
        public AnimatorState Walk;
        public AnimatorState Run;
        public AnimatorState Fire;
        public AnimatorState Reload;
        public AnimatorState Equip;
        // Shotgun specific
        public AnimatorState ReloadStart;
        public AnimatorState ReloadInsert;
        public AnimatorState ReloadEnd;
    }

    public static void BuildStateMachine()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("=== Weapon State Machine Build Started ===");

        string controllerPath = "Assets/Game/Animations/Player_Animator.controller";
        string metaPath = controllerPath + ".meta";
        string originalMeta = File.Exists(metaPath) ? File.ReadAllText(metaPath) : null;

        // Recreate pristine controller to purge all 13,000 lines of orphaned sub-asset transitions
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        if (controller == null)
        {
            Debug.LogError($"[RebuildAllWeaponAnimations] Could not create controller at {controllerPath}");
            return;
        }

        // 1. Ensure Animator Parameters
        EnsureParameter(controller, "Speed", AnimatorControllerParameterType.Float);
        EnsureParameter(controller, "WeaponType", AnimatorControllerParameterType.Int);
        EnsureParameter(controller, "Fire", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Reload", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Equip", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Fire_Left", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Reload_Insert", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "Reload_End", AnimatorControllerParameterType.Trigger);

        if (controller.layers.Length == 0)
        {
            controller.AddLayer("Base Layer");
        }

        var rootSM = controller.layers[0].stateMachine;

        // Clean out ALL old states and transitions completely to avoid duplicates
        var existingStates = rootSM.states;
        for (int i = existingStates.Length - 1; i >= 0; i--)
        {
            rootSM.RemoveState(existingStates[i].state);
        }

        var existingAnyTrans = rootSM.anyStateTransitions;
        for (int i = existingAnyTrans.Length - 1; i >= 0; i--)
        {
            rootSM.RemoveAnyStateTransition(existingAnyTrans[i]);
        }

        var existingEntryTrans = rootSM.entryTransitions;
        for (int i = existingEntryTrans.Length - 1; i >= 0; i--)
        {
            rootSM.RemoveEntryTransition(existingEntryTrans[i]);
        }

        var weaponConfigs = new (string name, int weaponType, Vector3 basePos)[]
        {
            ("Pistol", 1, new Vector3(300, 100, 0)),
            ("Shotgun", 2, new Vector3(300, 520, 0)),
            ("Rifle", 3, new Vector3(300, 960, 0))
        };

        var weaponMap = new Dictionary<string, WeaponStates>();

        foreach (var cfg in weaponConfigs)
        {
            var ws = new WeaponStates
            {
                Name = cfg.name,
                WeaponType = cfg.weaponType,
                Equip = rootSM.AddState($"{cfg.name}_Equip", cfg.basePos + new Vector3(-240, 0, 0)),
                Idle = rootSM.AddState($"{cfg.name}_Idle", cfg.basePos + new Vector3(0, 0, 0)),
                Walk = rootSM.AddState($"{cfg.name}_Walk", cfg.basePos + new Vector3(240, -80, 0)),
                Run = rootSM.AddState($"{cfg.name}_Run", cfg.basePos + new Vector3(240, 80, 0)),
                Fire = rootSM.AddState($"{cfg.name}_Fire", cfg.basePos + new Vector3(500, -80, 0))
            };

            string idlePath = $"Assets/Game/Animations/{cfg.name}_Idle_Fixed.anim";
            string runPath = $"Assets/Game/Animations/{cfg.name}_Run_Fixed.anim";
            string firePath = $"Assets/Game/Animations/{cfg.name}_Fire_Fixed.anim";
            string equipPath = $"Assets/Game/Animations/{cfg.name}_Equip_Fixed.anim";

            ws.Idle.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(idlePath);
            ws.Idle.speed = 1.0f;

            ws.Walk.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(runPath);
            ws.Walk.speed = 0.7f; // Walk tempo

            ws.Run.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(runPath);
            ws.Run.speed = 1.15f; // Run tempo

            ws.Fire.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(firePath);
            ws.Fire.speed = (cfg.name == "Pistol") ? 0.55f : 1.0f;

            ws.Equip.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(equipPath);
            ws.Equip.speed = 1.0f;

            if (cfg.name == "Shotgun")
            {
                // Multi-stage Reload states for Shotgun
                ws.ReloadStart = rootSM.AddState("Shotgun_Reload_Start", cfg.basePos + new Vector3(500, 40, 0));
                ws.ReloadInsert = rootSM.AddState("Shotgun_Reload_Insert", cfg.basePos + new Vector3(740, 40, 0));
                ws.ReloadEnd = rootSM.AddState("Shotgun_Reload_End", cfg.basePos + new Vector3(980, 40, 0));

                ws.ReloadStart.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Preparation_Before_Fixed.anim");
                ws.ReloadStart.speed = 0.5f;

                ws.ReloadInsert.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Reload_Insert_Fixed.anim");
                ws.ReloadInsert.speed = 0.45f;

                ws.ReloadEnd.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Preparation_After_Fixed.anim");
                ws.ReloadEnd.speed = 0.55f;

                sb.AppendLine("Created Shotgun states (Idle, Walk, Run, Fire, Equip, Reload_Start, Reload_Insert, Reload_End)");
            }
            else
            {
                // Box Magazine Reload for Pistol & Rifle
                string reloadPath = $"Assets/Game/Animations/{cfg.name}_Reload_Fixed.anim";
                ws.Reload = rootSM.AddState($"{cfg.name}_Reload", cfg.basePos + new Vector3(500, 80, 0));
                ws.Reload.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(reloadPath);
                ws.Reload.speed = (cfg.name == "Pistol") ? 0.50f : 0.55f;

                sb.AppendLine($"Created {cfg.name} states (Idle, Walk, Run, Fire, Reload, Equip)");
            }

            weaponMap[cfg.name] = ws;
        }

        // Default state: Pistol_Idle
        if (weaponMap.TryGetValue("Pistol", out var pistolGroup))
        {
            rootSM.defaultState = pistolGroup.Idle;
        }

        // 3. Setup Transitions
        foreach (var kvp in weaponMap)
        {
            var ws = kvp.Value;

            // --- A. MOVEMENT TRANSITIONS (Idle <-> Walk <-> Run) ---
            // Snappy 0.08s transitions with DestinationThenSource so WASD immediately interrupts Idle
            AddTransition(ws.Idle, ws.Walk, 0.08f, false,
                (AnimatorConditionMode.Greater, 0.1f, "Speed"),
                (AnimatorConditionMode.Less, 0.75f, "Speed"));

            AddTransition(ws.Walk, ws.Idle, 0.08f, false,
                (AnimatorConditionMode.Less, 0.1f, "Speed"));

            AddTransition(ws.Idle, ws.Run, 0.08f, false,
                (AnimatorConditionMode.Greater, 0.75f, "Speed"));

            AddTransition(ws.Run, ws.Idle, 0.08f, false,
                (AnimatorConditionMode.Less, 0.1f, "Speed"));

            AddTransition(ws.Walk, ws.Run, 0.08f, false,
                (AnimatorConditionMode.Greater, 0.75f, "Speed"));

            AddTransition(ws.Run, ws.Walk, 0.08f, false,
                (AnimatorConditionMode.Less, 0.75f, "Speed"),
                (AnimatorConditionMode.Greater, 0.1f, "Speed"));

            // --- B. FIRE TRANSITIONS ---
            AddTransition(ws.Idle, ws.Fire, 0.04f, false,
                (AnimatorConditionMode.If, 0, "Fire"));

            AddTransition(ws.Walk, ws.Fire, 0.04f, false,
                (AnimatorConditionMode.If, 0, "Fire"));

            AddTransition(ws.Run, ws.Fire, 0.04f, false,
                (AnimatorConditionMode.If, 0, "Fire"));

            // Rapid shooting support (self loop)
            var selfFire = AddTransition(ws.Fire, ws.Fire, 0.02f, false,
                (AnimatorConditionMode.If, 0, "Fire"));
            selfFire.canTransitionToSelf = true;

            // Fire Exit Transitions back to movement
            float fireExit = (ws.Name == "Shotgun") ? 0.35f : 0.25f;
            AddExitTransition(ws.Fire, ws.Idle, fireExit, 0.08f,
                (AnimatorConditionMode.Less, 0.1f, "Speed"));

            AddExitTransition(ws.Fire, ws.Walk, fireExit, 0.08f,
                (AnimatorConditionMode.Greater, 0.1f, "Speed"),
                (AnimatorConditionMode.Less, 0.75f, "Speed"));

            AddExitTransition(ws.Fire, ws.Run, fireExit, 0.08f,
                (AnimatorConditionMode.Greater, 0.75f, "Speed"));

            // --- C. RELOAD TRANSITIONS ---
            if (ws.Name == "Shotgun")
            {
                // 1. Enter Reload Start (Preparation Before)
                AddTransition(ws.Idle, ws.ReloadStart, 0.04f, false, (AnimatorConditionMode.If, 0, "Reload"));
                AddTransition(ws.Walk, ws.ReloadStart, 0.04f, false, (AnimatorConditionMode.If, 0, "Reload"));
                AddTransition(ws.Run, ws.ReloadStart, 0.04f, false, (AnimatorConditionMode.If, 0, "Reload"));

                // 2. Reload Start -> Reload Insert (automatically on finish)
                AddExitTransition(ws.ReloadStart, ws.ReloadInsert, 0.90f, 0.05f);

                // 3. Reload Insert -> Reload Insert (loop on each shell insertion)
                var insertLoop = AddTransition(ws.ReloadInsert, ws.ReloadInsert, 0.02f, false,
                    (AnimatorConditionMode.If, 0, "Reload_Insert"));
                insertLoop.canTransitionToSelf = true;

                // 4. Reload Insert -> Reload End (Preparation After / Pump)
                // When Reload_End is triggered (magazine full or cancelled)
                AddTransition(ws.ReloadInsert, ws.ReloadEnd, 0.04f, false,
                    (AnimatorConditionMode.If, 0, "Reload_End"));

                // When user clicks Fire during insert (interrupt immediately with pump)
                AddTransition(ws.ReloadInsert, ws.ReloadEnd, 0.04f, false,
                    (AnimatorConditionMode.If, 0, "Fire"));

                // Fallback exit time if no trigger arrives
                AddExitTransition(ws.ReloadInsert, ws.ReloadEnd, 0.98f, 0.05f);

                // 5. Reload End (Pump) -> Movement States on completion
                AddExitTransition(ws.ReloadEnd, ws.Idle, 0.85f, 0.08f,
                    (AnimatorConditionMode.Less, 0.1f, "Speed"));

                AddExitTransition(ws.ReloadEnd, ws.Walk, 0.85f, 0.08f,
                    (AnimatorConditionMode.Greater, 0.1f, "Speed"),
                    (AnimatorConditionMode.Less, 0.75f, "Speed"));

                AddExitTransition(ws.ReloadEnd, ws.Run, 0.85f, 0.08f,
                    (AnimatorConditionMode.Greater, 0.75f, "Speed"));

                // Ready to fire as pump action nears completion (~0.75 exit)
                AddExitTransition(ws.ReloadEnd, ws.Fire, 0.75f, 0.04f,
                    (AnimatorConditionMode.If, 0, "Fire"));
            }
            else
            {
                // Box Magazine Reload for Pistol / Rifle
                AddTransition(ws.Idle, ws.Reload, 0.04f, false,
                    (AnimatorConditionMode.If, 0, "Reload"));

                AddTransition(ws.Walk, ws.Reload, 0.04f, false,
                    (AnimatorConditionMode.If, 0, "Reload"));

                AddTransition(ws.Run, ws.Reload, 0.04f, false,
                    (AnimatorConditionMode.If, 0, "Reload"));

                AddExitTransition(ws.Reload, ws.Idle, 0.90f, 0.08f,
                    (AnimatorConditionMode.Less, 0.1f, "Speed"));

                AddExitTransition(ws.Reload, ws.Walk, 0.90f, 0.08f,
                    (AnimatorConditionMode.Greater, 0.1f, "Speed"),
                    (AnimatorConditionMode.Less, 0.75f, "Speed"));

                AddExitTransition(ws.Reload, ws.Run, 0.90f, 0.08f,
                    (AnimatorConditionMode.Greater, 0.75f, "Speed"));
            }

            // --- D. EQUIP TRANSITIONS ---
            var anyToEquip = rootSM.AddAnyStateTransition(ws.Equip);
            anyToEquip.hasExitTime = false;
            anyToEquip.duration = 0.04f;
            anyToEquip.hasFixedDuration = true;
            anyToEquip.canTransitionToSelf = false;
            anyToEquip.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
            anyToEquip.AddCondition(AnimatorConditionMode.If, 0, "Equip");
            anyToEquip.AddCondition(AnimatorConditionMode.Equals, ws.WeaponType, "WeaponType");

            AddExitTransition(ws.Equip, ws.Idle, 0.85f, 0.08f,
                (AnimatorConditionMode.Less, 0.1f, "Speed"));

            AddExitTransition(ws.Equip, ws.Walk, 0.85f, 0.08f,
                (AnimatorConditionMode.Greater, 0.1f, "Speed"),
                (AnimatorConditionMode.Less, 0.75f, "Speed"));

            AddExitTransition(ws.Equip, ws.Run, 0.85f, 0.08f,
                (AnimatorConditionMode.Greater, 0.75f, "Speed"));
        }

        // --- E. CROSS-WEAPON SWITCHING ---
        foreach (var fromKvp in weaponMap)
        {
            var fromWs = fromKvp.Value;
            foreach (var toKvp in weaponMap)
            {
                if (fromKvp.Key == toKvp.Key) continue;
                var toWs = toKvp.Value;

                AddTransition(fromWs.Idle, toWs.Idle, 0.08f, false,
                    (AnimatorConditionMode.Equals, toWs.WeaponType, "WeaponType"));
                AddTransition(fromWs.Walk, toWs.Idle, 0.08f, false,
                    (AnimatorConditionMode.Equals, toWs.WeaponType, "WeaponType"));
                AddTransition(fromWs.Run, toWs.Idle, 0.08f, false,
                    (AnimatorConditionMode.Equals, toWs.WeaponType, "WeaponType"));
            }
        }

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();

        if (!string.IsNullOrEmpty(originalMeta))
        {
            File.WriteAllText(metaPath, originalMeta);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        sb.AppendLine("=== Weapon State Machine Build Completed Successfully ===");
        Debug.Log(sb.ToString());
        File.WriteAllText("state_machine_build_result.txt", sb.ToString());
    }

    private static AnimatorStateTransition AddTransition(AnimatorState from, AnimatorState to, float duration, bool hasExitTime,
        params (AnimatorConditionMode mode, float threshold, string param)[] conditions)
    {
        var tr = from.AddTransition(to);
        tr.hasExitTime = hasExitTime;
        tr.duration = duration;
        tr.hasFixedDuration = true;
        tr.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
        tr.orderedInterruption = false;
        tr.canTransitionToSelf = false;
        foreach (var c in conditions)
        {
            tr.AddCondition(c.mode, c.threshold, c.param);
        }
        return tr;
    }

    private static AnimatorStateTransition AddExitTransition(AnimatorState from, AnimatorState to, float exitTime, float duration,
        params (AnimatorConditionMode mode, float threshold, string param)[] conditions)
    {
        var tr = from.AddTransition(to);
        tr.hasExitTime = true;
        tr.exitTime = exitTime;
        tr.duration = duration;
        tr.hasFixedDuration = true;
        tr.interruptionSource = TransitionInterruptionSource.DestinationThenSource;
        tr.orderedInterruption = false;
        tr.canTransitionToSelf = false;
        foreach (var c in conditions)
        {
            tr.AddCondition(c.mode, c.threshold, c.param);
        }
        return tr;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        foreach (var p in controller.parameters)
        {
            if (p.name == name) return;
        }
        controller.AddParameter(name, type);
    }
}
