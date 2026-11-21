using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class SetupSceneWeapons
{
    private const string SceneWeaponsKey = "SceneWeaponsSetup_V7";

    static SetupSceneWeapons()
    {
        EditorApplication.delayCall += () =>
        {
            if (!SessionState.GetBool(SceneWeaponsKey, false))
            {
                SessionState.SetBool(SceneWeaponsKey, true);
                RunSetup();
            }
        };

        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                string pendingJson = SessionState.GetString("WeaponSway_PendingEditorSave", "");
                if (!string.IsNullOrEmpty(pendingJson))
                {
                    SessionState.EraseString("WeaponSway_PendingEditorSave");
                    var sway = Object.FindFirstObjectByType<WeaponSwaySystem>();
                    if (sway != null)
                    {
                        sway.ImportFromJson(pendingJson);
                        EditorUtility.SetDirty(sway);
                        EditorSceneManager.MarkSceneDirty(sway.gameObject.scene);
                        Debug.Log("[SetupSceneWeapons] Play Mode'da kaydedilen WeaponSwaySystem ayarları sahneye kalıcı olarak aktarıldı.");
                    }
                }
            }
        };
    }

    [MenuItem("Tools/Setup Scene Weapons")]
    public static void RunSetup()
    {
        string scenePath = "Assets/Scenes/SampleScene.unity";
        Scene scene = EditorSceneManager.GetActiveScene();
        if (scene.path != scenePath)
        {
            scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        }

        GameObject character = GameObject.Find("Character");
        if (character == null)
        {
            Debug.LogError("[SetupSceneWeapons] Character GameObject not found in scene!");
            return;
        }

        Player1 playerComp = character.GetComponent<Player1>();
        if (playerComp == null)
        {
            Debug.LogError("[SetupSceneWeapons] Player1 component not found on Character!");
            return;
        }

        // 1. Sol El Kemik (hand_L.L) ve Sağ El Kemik (hand_L.R) referanslarını bul
        Transform handR = null;
        Transform handL = null;
        foreach (var t in character.GetComponentsInChildren<Transform>(true))
        {
            if (t.name.Equals("hand_L.R", System.StringComparison.OrdinalIgnoreCase)) handR = t;
            if (t.name.Equals("hand_L.L", System.StringComparison.OrdinalIgnoreCase)) handL = t;
        }

        // 2. Kamera altındaki eski placeholder küpleri devre dışı bırak
        Camera mainCam = Camera.main;
        Transform camTr = (mainCam != null) ? mainCam.transform : character.transform.Find("Camera");
        if (camTr != null)
        {
            for (int i = 0; i < camTr.childCount; i++)
            {
                Transform child = camTr.GetChild(i);
                if (child.GetComponent<MeshFilter>() != null)
                {
                    if (child.name == "Pistol" || child.name == "Shotgun" || child.name == "Rifle")
                    {
                        child.gameObject.SetActive(false);
                        Debug.Log($"[SetupSceneWeapons] Disabled placeholder cube: {child.name}");
                    }
                }
            }
        }

        // 3. TABANCA (Pistol_L) AYARLARI
        GameObject pistolObj = GameObject.Find("Pistol_L");
        if (pistolObj == null)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "Pistol_L" && go.scene == scene)
                {
                    pistolObj = go;
                    break;
                }
            }
        }

        if (pistolObj != null)
        {
            // Eski çakışan Gun_Controller Animator'ını kaldır
            var oldAnim = pistolObj.GetComponent<Animator>();
            if (oldAnim != null)
            {
                Object.DestroyImmediate(oldAnim);
                Debug.Log("[SetupSceneWeapons] Removed conflicting Animator from Pistol_L");
            }

            var socket = pistolObj.GetComponent<FPSWeaponSocket>();
            if (socket == null) socket = pistolObj.AddComponent<FPSWeaponSocket>();
            socket.targetBoneName = "hand_L.R";
            socket.positionOffset = new Vector3(0.002f, 0.0053f, 0.0007f);
            socket.rotationOffset = new Vector3(-798.2f, -218.6f, 8.8f);
            socket.scaleOffset = new Vector3(0.04f, 0.04f, 0.04f);

            var magReload = pistolObj.GetComponent<WeaponMagazineReload>();
            if (magReload == null) magReload = pistolObj.AddComponent<WeaponMagazineReload>();
            magReload.isPistol = true;
            magReload.reloadDuration = 2.08f;
            magReload.dropDepthMultiplier = 1.0f;
            magReload.dropSpreadMultiplier = 1.0f;
            magReload.magazineScaleMultiplier = new Vector3(0.88f, 0.96f, 0.90f);
            magReload.enableHalfwayIllusion = true;

            playerComp.Pistol = pistolObj;
            pistolObj.SetActive(true); // Tabanca başlangıçta aktif (ID = 1)
            Debug.Log("[SetupSceneWeapons] Pistol_L setup complete and assigned.");
        }

        // 4. POMPALI (ShotGun_C) AYARLARI - Kullanıcının sahnede ayarladığı asıl pompalı
        GameObject shotgunObj = null;
        foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
        {
            if ((go.name == "ShotGun_C" || go.name == "Shotgun_C") && go.scene == scene)
            {
                shotgunObj = go;
                break;
            }
        }

        if (shotgunObj != null)
        {
            var socket = shotgunObj.GetComponent<FPSWeaponSocket>();
            if (socket == null) socket = shotgunObj.AddComponent<FPSWeaponSocket>();
            socket.targetBoneName = "hand_L.R";

            shotgunObj.SetActive(false); // Başlangıçta gizli (ID = 2)
            playerComp.Shotgun = shotgunObj;
            Debug.Log("[SetupSceneWeapons] ShotGun_C başarıyla bağlandı ve pompalı olarak ayarlandı.");
        }

        // 5. TÜFEK / AK-47 (AK74) AYARLARI
        GameObject rifleObj = GameObject.Find("AK74");
        if (rifleObj == null)
        {
            foreach (var go in Resources.FindObjectsOfTypeAll<GameObject>())
            {
                if (go.name == "AK74" && go.scene == scene)
                {
                    rifleObj = go;
                    break;
                }
            }
        }

        if (rifleObj == null)
        {
            string akPrefabPath = "Assets/_AssetPacks/Low Poly Weapons VOL.1/Prefabs/AK74.prefab";
            var akPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(akPrefabPath);
            if (akPrefab != null)
            {
                rifleObj = (GameObject)PrefabUtility.InstantiatePrefab(akPrefab);
                rifleObj.name = "AK74";
                Debug.Log("[SetupSceneWeapons] Instantiated AK74 prefab into scene.");
            }
        }

        if (rifleObj != null)
        {
            var socket = rifleObj.GetComponent<FPSWeaponSocket>();
            if (socket == null) socket = rifleObj.AddComponent<FPSWeaponSocket>();
            socket.targetBoneName = "hand_L.R";
            // Ele tam oturan doğal açı ve konum (havada uçmasını engeller)
            socket.positionOffset = new Vector3(0.002f, 0.004f, 0.001f);
            socket.rotationOffset = new Vector3(290f, -222f, 25f);
            socket.scaleOffset = new Vector3(0.04f, 0.04f, 0.04f);

            var magReload = rifleObj.GetComponent<WeaponMagazineReload>();
            if (magReload == null) magReload = rifleObj.AddComponent<WeaponMagazineReload>();
            magReload.isPistol = false;
            magReload.reloadDuration = 1.4f;

            if (handR != null)
            {
                rifleObj.transform.SetParent(handR, false);
            }
            else if (camTr != null)
            {
                rifleObj.transform.SetParent(camTr, false);
            }

            rifleObj.SetActive(false); // Başlangıçta gizli
            playerComp.Rifle = rifleObj;
            Debug.Log("[SetupSceneWeapons] AK74 ele oturtuldu, şarjör zamanlamaları senkronize edildi.");
        }

        // 6. WEAPON VFX & IMPACT SISTEMI
        var vfxManager = character.GetComponent<WeaponVFXManager>();
        if (vfxManager == null) vfxManager = character.AddComponent<WeaponVFXManager>();
        vfxManager.AutoLoadAssets();
        playerComp.VFXManager = vfxManager;
        EditorUtility.SetDirty(vfxManager);
        Debug.Log("[SetupSceneWeapons] WeaponVFXManager Character üzerine eklendi ve prefabları bağlandı.");

        // 7. WEAPON SWAY SISTEMI
        var swaySystem = character.GetComponent<WeaponSwaySystem>();
        if (swaySystem == null) swaySystem = character.AddComponent<WeaponSwaySystem>();
        playerComp.SwaySystem = swaySystem;
        EditorUtility.SetDirty(swaySystem);
        Debug.Log("[SetupSceneWeapons] WeaponSwaySystem Character üzerine eklendi ve bağlandı.");

        EditorUtility.SetDirty(character);
        EditorUtility.SetDirty(playerComp);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[SetupSceneWeapons] Scene successfully saved with all weapon references and exclusivity!");
    }
}
