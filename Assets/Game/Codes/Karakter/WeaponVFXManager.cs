using System.Collections;
using UnityEngine;

/// <summary>
/// Silah VFX ve Mermi Vuruş Yönetim Sistemi (WeaponVFXManager).
/// DUSK ve Turbo Overkill tarzı tok, abartılı ve doyurucu retro-FPS combat hissiyatı sunar:
/// 1. Pompalı (Shotgun): Odayı aydınlatan devasa 4-Plane flaş, yoğun kıvılcım ve yüksek menzilli dinamik ışık patlaması.
/// 2. Tabanca (Pistol): Keskin, parlak ve seri namlu alevi ve anlık ışık.
/// 3. Mermi Vuruşları & Delikler: Duvara veya zemine çarptığında anlık alev patlaması, kıvılcım ve delik sticker'ı (decal).
/// 4. Canlı Arayüz (F10): Oyundan çıkmadan tüm parlaklık, boyut ve efekt ayarlarını test edebileceğin pratik menü.
/// </summary>
public class WeaponVFXManager : MonoBehaviour
{
    public static WeaponVFXManager Instance { get; private set; }

    [System.Serializable]
    public class MuzzleFlashConfig
    {
        public string weaponName = "Weapon";
        public GameObject muzzlePrefab;
        [Range(0.2f, 5.0f)] public float scaleMultiplier = 1.0f;
        [Range(0.5f, 25.0f)] public float lightIntensity = 5.0f;
        [Range(2.0f, 40.0f)] public float lightRange = 12.0f;
        public Color lightColor = new Color(1f, 0.7f, 0.2f);
        public float lightDuration = 0.05f;
    }

    [Header("=== SİLAH NAMLU ALEVİ (MUZZLE FLASH) KONFİGÜRASYONLARI ===")]
    [Tooltip("Tabanca (Pistol) Efekt Ayarları")]
    public MuzzleFlashConfig pistolConfig = new MuzzleFlashConfig
    {
        weaponName = "Pistol",
        scaleMultiplier = 1.3f,
        lightIntensity = 4.5f,
        lightRange = 9.0f,
        lightColor = new Color(1f, 0.75f, 0.25f),
        lightDuration = 0.045f
    };

    [Tooltip("Pompalı (Shotgun - DUSK Tarzı Devasa Patlama) Efekt Ayarları")]
    public MuzzleFlashConfig shotgunConfig = new MuzzleFlashConfig
    {
        weaponName = "Shotgun",
        scaleMultiplier = 2.4f,
        lightIntensity = 12.0f,
        lightRange = 20.0f,
        lightColor = new Color(1f, 0.65f, 0.15f),
        lightDuration = 0.08f
    };

    [Tooltip("Tüfek (AK-74) Efekt Ayarları")]
    public MuzzleFlashConfig rifleConfig = new MuzzleFlashConfig
    {
        weaponName = "Rifle",
        scaleMultiplier = 1.5f,
        lightIntensity = 5.5f,
        lightRange = 13.0f,
        lightColor = new Color(1f, 0.70f, 0.20f),
        lightDuration = 0.05f
    };

    [Header("=== MERMİ ÇARPMALARI & DELİK STICKERLARI (IMPACT & DECALS) ===")]
    [Tooltip("Beton/Taş zemin vuruş prefabı (Toz, kıvılcım ve mermi deliği içerir)")]
    public GameObject impactConcretePrefab;

    [Tooltip("Metal zemin vuruş prefabı (Ağır kıvılcım ve metal deliği içerir)")]
    public GameObject impactMetalPrefab;

    [Tooltip("Ahşap zemin vuruş prefabı (Kıymık ve ahşap deliği içerir)")]
    public GameObject impactWoodPrefab;

    [Tooltip("Toprak/Zemin vuruş prefabı")]
    public GameObject impactDirtPrefab;

    [Tooltip("Çarptığı zeminde anlık alev patlaması çıksın mı? (DUSK / Turbo Overkill tarzı)")]
    public bool enableImpactFlames = true;

    [Tooltip("Zemindeki anlık alev patlama prefabı (WFX_Explosion Small)")]
    public GameObject impactFlamePrefab;

    [Range(0.2f, 3.0f)]
    public float impactScale = 1.0f;

    [Tooltip("Mermi delikleri duvarda kaç saniye kalsın?")]
    [Range(5f, 60f)]
    public float decalLifetime = 20f;

    [Header("=== CANLI ARAYÜZ (IN-GAME TWEAK MENU) ===")]
    [Tooltip("Oyun içi ayar menüsünü açma/kapatma tuşu")]
    public KeyCode menuToggleKey = KeyCode.F10;
    public bool enableInGameMenu = true;
    private bool _showMenu = false;
    public static bool IsMenuOpen => Instance != null && Instance._showMenu;
    private Rect _windowRect = new Rect(20, 20, 360, 520);

    // Dinamik Işık Havuzu (Garbage Collector ve FPS drop oluşturmaz)
    private Light _flashLight;
    private Coroutine _flashLightRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        SetupFlashLight();
        AutoLoadAssets();
    }

    void SetupFlashLight()
    {
        if (_flashLight == null)
        {
            GameObject lightObj = new GameObject("MuzzleFlash_DynamicLight");
            lightObj.transform.SetParent(transform, false);
            _flashLight = lightObj.AddComponent<Light>();
            _flashLight.type = LightType.Point;
            _flashLight.shadows = LightShadows.None; // Performans için
            _flashLight.enabled = false;
        }
    }

    /// <summary>
    /// Silah ateşlendiğinde namlu alevini ve anlık dinamik ışığı patlatır.
    /// </summary>
    public void TriggerMuzzleFlash(int gunID, bool isLeftHand)
    {
        MuzzleFlashConfig cfg = GetConfig(gunID);
        if (cfg == null) return;

        Transform muzzleTr = GetMuzzleTransform(gunID, isLeftHand);
        Vector3 spawnPos = muzzleTr != null ? muzzleTr.position : (Camera.main != null ? Camera.main.transform.position + Camera.main.transform.forward * 0.5f : transform.position);
        Quaternion spawnRot = muzzleTr != null ? muzzleTr.rotation : (Camera.main != null ? Camera.main.transform.rotation : transform.rotation);

        // 1. Partikül Efektini Doğur
        if (cfg.muzzlePrefab != null)
        {
            GameObject flashObj = Instantiate(cfg.muzzlePrefab, spawnPos, spawnRot);
            flashObj.transform.localScale = Vector3.one * cfg.scaleMultiplier;
            // Silahla birlikte hafif hareket etmesi için opsiyonel parenting
            if (muzzleTr != null)
            {
                flashObj.transform.SetParent(muzzleTr, true);
            }
            Destroy(flashObj, 0.4f);
        }

        // 2. Dinamik Işık Patlaması (Odayı anlık aydınlatma)
        if (_flashLight != null)
        {
            _flashLight.transform.position = spawnPos;
            if (_flashLightRoutine != null) StopCoroutine(_flashLightRoutine);
            _flashLightRoutine = StartCoroutine(FlashLightRoutine(cfg.lightIntensity, cfg.lightRange, cfg.lightColor, cfg.lightDuration));
        }
    }

    private IEnumerator FlashLightRoutine(float intensity, float range, Color col, float duration)
    {
        _flashLight.color = col;
        _flashLight.range = range;
        _flashLight.intensity = intensity;
        _flashLight.enabled = true;

        float half = duration * 0.5f;
        yield return new WaitForSeconds(half);

        float elapsed = 0f;
        while (elapsed < half)
        {
            elapsed += Time.deltaTime;
            _flashLight.intensity = Mathf.Lerp(intensity, 0f, elapsed / half);
            yield return null;
        }

        _flashLight.enabled = false;
        _flashLightRoutine = null;
    }

    /// <summary>
    /// Merminin çarptığı zemine göre kıvılcım, alev ve delik sticker'ı (decal) yerleştirir.
    /// </summary>
    public void TriggerImpact(Vector3 hitPoint, Vector3 hitNormal, Collider hitCollider, int gunType)
    {
        GameObject prefab = impactConcretePrefab;

        // Yüzey tipini isim veya tag'den otomatik tespit et
        if (hitCollider != null)
        {
            string n = (hitCollider.name + " " + (hitCollider.sharedMaterial != null ? hitCollider.sharedMaterial.name : "")).ToLower();
            if (n.Contains("metal") || n.Contains("iron") || n.Contains("steel"))
            {
                prefab = impactMetalPrefab != null ? impactMetalPrefab : impactConcretePrefab;
            }
            else if (n.Contains("wood") || n.Contains("plank") || n.Contains("door"))
            {
                prefab = impactWoodPrefab != null ? impactWoodPrefab : impactConcretePrefab;
            }
            else if (n.Contains("dirt") || n.Contains("terrain") || n.Contains("ground") || n.Contains("sand"))
            {
                prefab = impactDirtPrefab != null ? impactDirtPrefab : impactConcretePrefab;
            }
        }

        Quaternion hitRot = Quaternion.LookRotation(hitNormal);
        Vector3 spawnPos = hitPoint + (hitNormal * 0.003f); // Z-fighting'i önlemek için minik pay

        // 1. Standart Vuruş ve Mermi Deliği (WarFX Decal)
        if (prefab != null)
        {
            GameObject impactObj = Instantiate(prefab, spawnPos, hitRot);
            impactObj.transform.localScale = Vector3.one * (gunType == 2 ? impactScale * 1.3f : impactScale);

            // Hareketli nesnelere (ör: kapı, kutu) yapışması için parent yap
            if (hitCollider != null && hitCollider.transform != null)
            {
                impactObj.transform.SetParent(hitCollider.transform, true);
            }

            // Süre sonunda temizle
            Destroy(impactObj, decalLifetime);
        }

        // 2. Anlık Çarpma Alevi (Kullanıcının "çarptığı zeminde alev çıksın" talebi)
        if (enableImpactFlames && impactFlamePrefab != null)
        {
            GameObject flameObj = Instantiate(impactFlamePrefab, spawnPos, hitRot);
            flameObj.transform.localScale = Vector3.one * (gunType == 2 ? 0.35f : 0.20f);
            Destroy(flameObj, 0.3f);
        }
    }

    private Transform GetMuzzleTransform(int gunID, bool isLeftHand)
    {
        Player1 player = Player1.Instance != null ? Player1.Instance : FindFirstObjectByType<Player1>();
        if (player == null) return null;

        GameObject weaponObj = null;
        if (isLeftHand)
        {
            if (gunID == 1) weaponObj = player.Dual_Pistol;
            else if (gunID == 2) weaponObj = player.Dual_Shotgun;
            else if (gunID == 3) weaponObj = player.Dual_Rifle;
        }
        else
        {
            if (gunID == 1) weaponObj = player.Pistol;
            else if (gunID == 2) weaponObj = player.Shotgun;
            else if (gunID == 3) weaponObj = player.Rifle;
        }

        if (weaponObj != null && weaponObj.activeInHierarchy)
        {
            foreach (Transform t in weaponObj.GetComponentsInChildren<Transform>(true))
            {
                string name = t.name.ToLower();
                if (name.Contains("barrel") || name.Contains("muzzle") || name.Contains("fire_point"))
                {
                    return t;
                }
            }
            return weaponObj.transform;
        }

        return player.Fire_Point != null ? player.Fire_Point : (Camera.main != null ? Camera.main.transform : transform);
    }

    public MuzzleFlashConfig GetConfig(int gunID)
    {
        if (gunID == 1) return pistolConfig;
        if (gunID == 2) return shotgunConfig;
        if (gunID == 3) return rifleConfig;
        return pistolConfig;
    }

    // =========================================================================
    // CANLI OYUN İÇİ AYAR MENÜSÜ (F10 ARAYÜZÜ)
    // =========================================================================
    void Update()
    {
        if (enableInGameMenu && Input.GetKeyDown(menuToggleKey))
        {
            _showMenu = !_showMenu;
            if (_showMenu)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else
            {
                if (!WeaponSwaySystem.IsMenuOpen)
                {
                    Cursor.lockState = CursorLockMode.Locked;
                    Cursor.visible = false;
                }
            }
        }
    }

    void OnGUI()
    {
        if (!_showMenu || !enableInGameMenu) return;

        GUI.backgroundColor = new Color(0.12f, 0.12f, 0.16f, 0.95f);
        _windowRect = GUI.Window(999, _windowRect, DrawTweakWindow, "🔥 VFX KONTROL MERKEZİ (DUSK / TURBO OVERKILL)");
    }

    void DrawTweakWindow(int windowID)
    {
        GUILayout.Space(6);
        GUILayout.Label("<b>--- POMPALI (SHOTGUN) AYARLARI ---</b>");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Flaş Boyutu: {shotgunConfig.scaleMultiplier:F1}x", GUILayout.Width(140));
        shotgunConfig.scaleMultiplier = GUILayout.HorizontalSlider(shotgunConfig.scaleMultiplier, 0.5f, 4.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Işık Parlaklığı: {shotgunConfig.lightIntensity:F1}", GUILayout.Width(140));
        shotgunConfig.lightIntensity = GUILayout.HorizontalSlider(shotgunConfig.lightIntensity, 1.0f, 25.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Işık Menzili: {shotgunConfig.lightRange:F0}m", GUILayout.Width(140));
        shotgunConfig.lightRange = GUILayout.HorizontalSlider(shotgunConfig.lightRange, 5.0f, 35.0f);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("<b>--- TABANCA (PISTOL) AYARLARI ---</b>");
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Flaş Boyutu: {pistolConfig.scaleMultiplier:F1}x", GUILayout.Width(140));
        pistolConfig.scaleMultiplier = GUILayout.HorizontalSlider(pistolConfig.scaleMultiplier, 0.5f, 3.0f);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Işık Parlaklığı: {pistolConfig.lightIntensity:F1}", GUILayout.Width(140));
        pistolConfig.lightIntensity = GUILayout.HorizontalSlider(pistolConfig.lightIntensity, 1.0f, 15.0f);
        GUILayout.EndHorizontal();

        GUILayout.Space(10);
        GUILayout.Label("<b>--- MERMİ VURUŞU & ZEMİN DELİKLERİ ---</b>");
        enableImpactFlames = GUILayout.Toggle(enableImpactFlames, " Çarptığı Yerde Anlık Alev Patlaması Çıksın");
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Vuruş Efekt Boyutu: {impactScale:F1}x", GUILayout.Width(140));
        impactScale = GUILayout.HorizontalSlider(impactScale, 0.5f, 2.5f);
        GUILayout.EndHorizontal();

        GUILayout.Space(14);
        GUILayout.BeginHorizontal();
        GUI.color = new Color(1f, 0.4f, 0.2f);
        if (GUILayout.Button("🔥 ATEŞ ET (TEST ET)"))
        {
            Player1 player = Player1.Instance != null ? Player1.Instance : FindFirstObjectByType<Player1>();
            int currentGun = player != null ? player.Right_Gun_ID : 2;
            TriggerMuzzleFlash(currentGun, false);
        }
        GUI.color = Color.white;

        if (GUILayout.Button("KAPAT [F10]"))
        {
            _showMenu = false;
            if (!WeaponSwaySystem.IsMenuOpen)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 25));
    }

    /// <summary>
    /// WarFX klasöründeki hazır prefablari otomatik olarak hafızaya çeker.
    /// </summary>
    public void AutoLoadAssets()
    {
#if UNITY_EDITOR
        string basePath = "Assets/JMO Assets/WarFX/_Effects/";
        if (pistolConfig.muzzlePrefab == null)
            pistolConfig.muzzlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "MuzzleFlashes/FPS/WFX_MF FPS RIFLE1.prefab");

        if (shotgunConfig.muzzlePrefab == null)
            shotgunConfig.muzzlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "MuzzleFlashes/4Planes/WFX_MF 4P RIFLE3.prefab");

        if (rifleConfig.muzzlePrefab == null)
            rifleConfig.muzzlePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "MuzzleFlashes/FPS/WFX_MF FPS RIFLE2.prefab");

        if (impactConcretePrefab == null)
            impactConcretePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Concrete + Hole Lit.prefab");

        if (impactMetalPrefab == null)
            impactMetalPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Metal + Hole Lit.prefab");

        if (impactWoodPrefab == null)
            impactWoodPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Bullet Impacts/+ Bullet Hole/Lit/WFX_BImpact Wood + Hole Lit.prefab");

        if (impactDirtPrefab == null)
            impactDirtPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Bullet Impacts/+ Bullet Hole/WFX_BImpact Dirt + Hole.prefab");

        if (impactFlamePrefab == null)
            impactFlamePrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(basePath + "Explosions/WFX_Explosion Small.prefab");
#endif
    }

    void Reset()
    {
        AutoLoadAssets();
    }
}
