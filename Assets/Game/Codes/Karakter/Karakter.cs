using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Player1 : MonoBehaviour
{
    public static Player1 Instance { get; private set; }

    [Header("Fizik Ayarları")]
    public float pushForce = 500f;

    [Header("Anahtarlar ve UI")]
    public bool RedKey = false;
    public Image Health_Image;

    // SİLAH GÖRSELLERİ (ICONS)
    public GameObject Left_Pistol_Image;
    public GameObject Right_Pistol_Image;
    public GameObject Left_Rifle_Image;
    public GameObject Right_Rifle_Image;
    public GameObject Left_Shotgun_Image;
    public GameObject Right_Shotgun_Image;

    // MERMİ YAZILARI (TEXT MESH PRO)
    public TextMeshProUGUI Left_Shotgun_Current;
    public TextMeshProUGUI Left_Shotgun_Total;
    public TextMeshProUGUI Left_Rifle_Current;
    public TextMeshProUGUI Left_Rifle_Total;
    public TextMeshProUGUI Left_Pistol_Current;
    public TextMeshProUGUI Left_Pistol_Total;
    public TextMeshProUGUI Right_Shotgun_Current;
    public TextMeshProUGUI Right_Shotgun_Total;
    public TextMeshProUGUI Right_Rifle_Current;
    public TextMeshProUGUI Right_Rifle_Total;
    public TextMeshProUGUI Right_Pistol_Current;
    public TextMeshProUGUI Right_Pistol_Total;

    [Header("Sağlık Ayarları")]
    public float Current_Health = 100f;
    public float Max_Health = 100f;
    public float Enemy_Damage_Amount = 10f;
    private bool Is_Dead = false;

    [Header("Mühimmat ve Silah Ayarları")]
    public bool Dual_Weapon_Mode = false;

    // --- ÖNEMLİ: Buraya Main Camera'yı sürükle ---
    public Transform Fire_Point;

    public float Spread_Width;
    public float Spread_Height;
    public int Pellet_Count;

    // MODEL REFERANSLARI (SAĞ EL / ANA SİLAHLAR)
    public GameObject Pistol;
    public GameObject Rifle;
    public GameObject Shotgun;

    // MODEL REFERANSLARI (SOL EL / DUAL SİLAHLAR)
    // NOT: Bu objelerin içinde sadece SOL silah olmalı!
    public GameObject Dual_Pistol;
    public GameObject Dual_Rifle;
    public GameObject Dual_Shotgun;

    // SİLAH ID'LERİ (1:Pistol, 2:Shotgun, 3:Rifle)
    public int Right_Gun_ID = 1;
    public int Left_Gun_ID = 1;

    public GameObject Bullet;
    public Transform Bullet_Coordinate;
    public float Bullet_Speed = 40f;

    // CEPTEKİ TOPLAM MERMİ
    public int Total_Pistol_Ammo = 30;
    public int Total_Rifle_Ammo = 30;
    public int Total_Shotgun_Ammo = 30;

    // ŞARJÖRDEKİ MERMİLER (SOL)
    public int Left_Current_Bullet_Pistol = 7;
    public int Left_Current_Bullet_Shotgun = 6;
    public int Left_Current_Bullet_Rifle = 15;

    // ŞARJÖRDEKİ MERMİLER (SAĞ)
    public int Right_Current_Bullet_Pistol = 7;
    public int Right_Current_Bullet_Shotgun = 6;
    public int Right_Current_Bullet_Rifle = 15;

    // KAPASİTE SINIRLARI
    public int Max_Bullet_Rifle_Magazine = 30;
    public int Max_Bullet_Pistol_Magazine = 7;
    public int Max_Bullet_Shotgun_Magazine = 6;

    // COOLDOWN SÜRELERİ
    public float Fire_Cooldown_Rifle = 0.1f;
    public float Fire_Cooldown_Shotgun = 1.5f;
    public float Fire_Cooldown_Pistol = 0.25f; // Kullanıcı talebi: Tabanca animasyonu yarı hıza çekildi (0.25s)

    [Header("Reload Süreleri (Saniye)")]
    [Tooltip("Tabanca (Pistol) şarjör doldurma süresi (saniye). Animasyon, şarjör hareketi ve mermi dolumu bu süreye kilitlenir.")]
    [Range(0.5f, 5.0f)]
    public float Pistol_Reload_Duration = 2.08f;

    [Tooltip("Tüfek (Rifle / AK-74) şarjör doldurma süresi (saniye).")]
    [Range(0.5f, 5.0f)]
    public float Rifle_Reload_Duration = 1.40f;

    // ZAMANLAYICILAR
    private float Next_Fire_Time_Right = 0f;
    private float Next_Fire_Time_Left = 0f;
    private float Next_Reload_Time = 0f;
    private Coroutine shotgunReloadCoroutine = null;
    private bool isShotgunReloading = false;
    private Coroutine activeReloadCoroutine = null;
    private bool isMagazineReloading = false;

    public bool IsReloading => isMagazineReloading || isShotgunReloading;

    [Header("Koşu ve Ateş Dinamikleri (Sprint Mechanics)")]
    [Tooltip("Hızlı koşarken ateş edildikten sonra tekrar koşuya geçebilmek için toparlanma süresi (saniye)")]
    public float Sprint_Recovery_Cooldown = 0.35f;
    private bool _sprintInterruptedByFire = false;
    private float _nextSprintAllowedTime = 0f;

    /// <summary>
    /// Karakterin şu an hızlı koşu (sprint) yapmasına izin var mı?
    /// Ateş edildiğinde veya toparlanma süresinde false döner.
    /// </summary>
    public bool IsSprintAllowed
    {
        get
        {
            if (_sprintInterruptedByFire) return false;
            if (Time.time < _nextSprintAllowedTime) return false;
            return true;
        }
    }

    /// <summary>
    /// Koşuyu anında hem fiziksel hareket hızı hem animasyon olarak keser.
    /// </summary>
    public void InterruptSprint(float cooldown = -1f)
    {
        _sprintInterruptedByFire = true;
        _nextSprintAllowedTime = Time.time + (cooldown >= 0f ? cooldown : Sprint_Recovery_Cooldown);

        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.SetMovementState(PlayerMovementState.Walk);
        }
    }

    [Header("Animasyon Ayarlari")]
    [Tooltip("Pure C# Kod Tabanlı Animasyon Sistemi (Playables API)")]
    public WeaponAnimationSystem Weapon_Anim_System;
    public Animator Player_Arms_Animator;

    [Tooltip("Tabanca sürgüsüne (slide) PistolSlide scripti eklendikten sonra buraya sürükle")]
    public PistolSlide Pistol_Slide_Script;

    [Header("Ses Sistemi (Audio Playback)")]
    public WeaponAudioSystem AudioSystem;

    [Header("VFX Sistemi (Muzzle Flash & Impacts)")]
    public WeaponVFXManager VFXManager;

    [Header("Silah Sallanma Sistemi (Procedural Sway)")]
    public WeaponSwaySystem SwaySystem;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Karakter hiyerarşisindeki silah/model collider'larının kapsülle çarpışıp drop/hitch yapmasını önle
        var myCollider = GetComponent<Collider>();
        foreach (var col in GetComponentsInChildren<Collider>(true))
        {
            if (col != myCollider) col.enabled = false;
        }

        var rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.interpolation = RigidbodyInterpolation.Interpolate;
        }

        AutoBindWeapons();

        if (Weapon_Anim_System == null)
        {
            Weapon_Anim_System = GetComponent<WeaponAnimationSystem>();
        }
        if (Weapon_Anim_System == null)
        {
            Weapon_Anim_System = GetComponentInChildren<WeaponAnimationSystem>(true);
        }
        if (Weapon_Anim_System == null)
        {
            Weapon_Anim_System = WeaponAnimationSystem.Instance;
        }
        if (Weapon_Anim_System == null)
        {
            Weapon_Anim_System = gameObject.AddComponent<WeaponAnimationSystem>();
        }

        if (AudioSystem == null)
        {
            AudioSystem = GetComponent<WeaponAudioSystem>();
        }
        if (AudioSystem == null)
        {
            AudioSystem = GetComponentInChildren<WeaponAudioSystem>(true);
        }
        if (AudioSystem == null)
        {
            AudioSystem = WeaponAudioSystem.Instance;
        }
        if (AudioSystem == null)
        {
            AudioSystem = gameObject.AddComponent<WeaponAudioSystem>();
        }

        if (VFXManager == null)
        {
            VFXManager = GetComponent<WeaponVFXManager>();
        }
        if (VFXManager == null)
        {
            VFXManager = GetComponentInChildren<WeaponVFXManager>(true);
        }
        if (VFXManager == null)
        {
            VFXManager = WeaponVFXManager.Instance;
        }
        if (VFXManager == null)
        {
            VFXManager = gameObject.AddComponent<WeaponVFXManager>();
        }

        if (SwaySystem == null)
        {
            SwaySystem = GetComponent<WeaponSwaySystem>();
        }
        if (SwaySystem == null)
        {
            SwaySystem = GetComponentInChildren<WeaponSwaySystem>(true);
        }
        if (SwaySystem == null)
        {
            SwaySystem = WeaponSwaySystem.Instance;
        }
        if (SwaySystem == null)
        {
            SwaySystem = gameObject.AddComponent<WeaponSwaySystem>();
        }

        if (Player_Arms_Animator == null)
        {
            Player_Arms_Animator = GetComponentInChildren<Animator>(true);
        }

        if (Pistol_Slide_Script == null && Pistol != null)
        {
            Pistol_Slide_Script = Pistol.GetComponentInChildren<PistolSlide>(true);
        }
        if (Pistol_Slide_Script == null)
        {
            Pistol_Slide_Script = GetComponentInChildren<PistolSlide>(true);
        }
        Image_Amount_Measurer(Health_Image, Current_Health, Max_Health);
        UpdateAllAmmoUI();
        RefreshWeaponState();

        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.SetWeapon(Right_Gun_ID, playEquip: false);
        }
    }

    void AutoBindWeapons()
    {
        // 1. Placeholder küpleri kapat
        Transform camTr = Camera.main != null ? Camera.main.transform : transform.Find("Camera");
        if (camTr != null)
        {
            for (int i = 0; i < camTr.childCount; i++)
            {
                Transform c = camTr.GetChild(i);
                if (c.GetComponent<MeshFilter>() != null && (c.name == "Pistol" || c.name == "Shotgun" || c.name == "Rifle"))
                {
                    c.gameObject.SetActive(false);
                }
            }
        }

        // 2. Eksik veya küp referansları gerçek silah modelleriyle bağla (hand_L.R altındaki gerçek silahlar)
        Transform searchRoot = camTr != null ? camTr : transform;
        Transform[] allTransforms = searchRoot.GetComponentsInChildren<Transform>(true);

        // Tabanca (Pistol_L)
        if (Pistol == null || (Pistol.GetComponent<MeshFilter>() != null && Pistol.name == "Pistol"))
        {
            foreach (var t in allTransforms)
            {
                if (t.name == "Pistol_L") { Pistol = t.gameObject; break; }
            }
        }

        // Pompalı (ShotGun_C - kullanıcının istediği asıl shotgun)
        if (Shotgun == null || (Shotgun.GetComponent<MeshFilter>() != null && Shotgun.name == "Shotgun") || Shotgun.name.Contains("ShotGun_F"))
        {
            foreach (var t in allTransforms)
            {
                if (t.name == "ShotGun_C" || t.name == "Shotgun_C") { Shotgun = t.gameObject; break; }
            }
        }

        // Tüfek (AK74 - kullanıcının istediği asıl rifle)
        if (Rifle == null || (Rifle.GetComponent<MeshFilter>() != null && Rifle.name == "Rifle"))
        {
            foreach (var t in allTransforms)
            {
                if (t.name == "AK74") { Rifle = t.gameObject; break; }
            }
        }

        // Fazlalık ShotGun_F objesi varsa kesinlikle kapat
        foreach (var t in allTransforms)
        {
            if (t.name == "ShotGun_F" || t.name == "Shotgun_F")
            {
                t.gameObject.SetActive(false);
            }
        }
    }

    void Update()
    {
        // 1. RELOAD (R TUŞU)
        if (Input.GetKeyDown(KeyCode.R))
        {
            HandleReload();
        }

        // 2. MOD DEĞİŞTİRME (X TUŞU)
        if (Input.GetKeyDown(KeyCode.X))
        {
            Dual_Weapon_Mode = !Dual_Weapon_Mode;
            RefreshWeaponState();
        }

        // 3. SİLAH SEÇİMİ (1-2-3)
        if (Input.GetKeyDown(KeyCode.Alpha1)) ChangeGunInput(1);
        if (Input.GetKeyDown(KeyCode.Alpha2)) ChangeGunInput(2);
        if (Input.GetKeyDown(KeyCode.Alpha3)) ChangeGunInput(3);

        // 5. HAREKET ANIMASYONU (WASD & Shift)
        HandleMovementAnimation();

        // 4. ATEŞ ETME MANTIĞI
        HandleShooting();
    }

    void HandleMovementAnimation()
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f);

        // Kullanıcı ateşe basıp koşuyu kestiğinde, Shift tuşuna YENİDEN basana kadar hızlı koşuya dönemez!
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (Time.time >= _nextSprintAllowedTime)
            {
                _sprintInterruptedByFire = false;
            }
        }

        // Shift basılı mı ve toparlanma süresi doldu mu?
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        bool canSprint = isMoving && shiftHeld && IsSprintAllowed;

        PlayerMovementState moveState = PlayerMovementState.Idle;
        if (isMoving)
        {
            moveState = canSprint ? PlayerMovementState.Run : PlayerMovementState.Walk;
        }

        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.SetMovementState(moveState);
        }
    }

    // --- INPUT VE SİLAH DEĞİŞİMİ ---
    void ChangeGunInput(int selectedID)
    {
        CancelAllReloads(instantAnimStop: false);

        if (Dual_Weapon_Mode) Left_Gun_ID = selectedID; // Dual modda SOL değişir
        else Right_Gun_ID = selectedID; // Tekli modda SAĞ değişir

        RefreshWeaponState();
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.SetWeapon(Right_Gun_ID, playEquip: true);
        }
        if (AudioSystem != null)
        {
            AudioSystem.PlayEquip(selectedID);
        }
    }

    // --- ATEŞ ETME KONTROLCÜSÜ ---
    void HandleShooting()
    {
        if (Cursor.lockState != CursorLockMode.Locked || WeaponSwaySystem.IsMenuOpen || WeaponVFXManager.IsMenuOpen)
            return;

        bool fire1Pressed = Input.GetButtonDown("Fire1");
        bool fire1Held = Input.GetButton("Fire1");
        bool fire2Pressed = Dual_Weapon_Mode && Input.GetButtonDown("Fire2");
        bool fire2Held = Dual_Weapon_Mode && Input.GetButton("Fire2");

        // Herhangi bir silah (Tabanca, Pompalı, Tüfek) reload halindeyken ateş edilirse
        // reload işlemini, kol/şarjör animasyonunu ve tüm reload seslerini anında iptal et
        if (IsReloading)
        {
            bool wantsToFire = fire1Pressed || fire2Pressed
                || (!Dual_Weapon_Mode && Right_Gun_ID == 3 && fire1Held)
                || (Dual_Weapon_Mode && ((Left_Gun_ID == 3 && fire1Held) || (Right_Gun_ID == 3 && fire2Held)));

            if (wantsToFire)
            {
                bool hasAmmo = HasAmmoForFireInput(fire1Pressed || fire1Held, fire2Pressed || fire2Held);
                CancelAllReloads(instantAnimStop: hasAmmo);
            }
            else
            {
                return;
            }
        }

        // SOL TIK (FIRE 1)
        // Dual modda Sol Eli, Tekli modda Sağ Eli (Ana silahı) ateşler
        if (fire1Held || fire1Pressed)
        {
            if (Dual_Weapon_Mode) TryFireWeapon(Left_Gun_ID, true); // Sol El
            else TryFireWeapon(Right_Gun_ID, false); // Sağ El
        }

        // SAĞ TIK (FIRE 2) - Sadece Dual Mod
        if (Dual_Weapon_Mode)
        {
            if (fire2Held || fire2Pressed)
            {
                TryFireWeapon(Right_Gun_ID, false); // Sağ El
            }
        }
    }

    void TryFireWeapon(int gunID, bool isLeftHand)
    {
        bool isAutomatic = (gunID == 3); // Sadece Rifle otomatiktir

        // Hangi tuşa basıldığını belirle
        string btnName = isLeftHand ? "Fire1" : (Dual_Weapon_Mode ? "Fire2" : "Fire1");
        if (!Dual_Weapon_Mode && !isLeftHand) btnName = "Fire1"; // Tekli modda sağ el Fire1 ile çalışır

        // Tuşa basıldı mı? (Otomatikse GetButton, değilse GetButtonDown)
        bool trigger = isAutomatic ? Input.GetButton(btnName) : Input.GetButtonDown(btnName);

        if (!trigger) return;

        // Ateşe basıldığı an (ateş gerçekleşsin veya tetiklensin) koşu hem fiziksel hız hem animasyon olarak derhal kesilir!
        InterruptSprint();

        // Zamanlayıcı Kontrolü
        float nextFireTime = isLeftHand ? Next_Fire_Time_Left : Next_Fire_Time_Right;
        if (Time.time < nextFireTime) return;

        // Ateşle
        if (gunID == 1) Fire_Pistol(isLeftHand);
        else if (gunID == 2) Fire_Shotgun(isLeftHand);
        else if (gunID == 3) Fire_Rifle(isLeftHand);
    }

    // --- FİZİKSEL ATEŞLEME ---
    void Fire_Pistol(bool isLeftHand)
    {
        if (isLeftHand)
        {
            if (Left_Current_Bullet_Pistol <= 0)
            {
                AudioSystem?.PlayDryFire(1);
                return;
            }
            Left_Current_Bullet_Pistol--;
        }
        else
        {
            if (Right_Current_Bullet_Pistol <= 0)
            {
                AudioSystem?.PlayDryFire(1);
                return;
            }
            Right_Current_Bullet_Pistol--;
        }

        AudioSystem?.PlayFire(1, isLeftHand);
        (VFXManager ?? WeaponVFXManager.Instance)?.TriggerMuzzleFlash(1, isLeftHand);
        PerformRaycast(Fire_Point.forward, 1);
        CameraShake.Instance?.ShakePistol(isLeftHand);
        SetCooldown(Fire_Cooldown_Pistol, isLeftHand);
        UpdateAllAmmoUI();
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.PlayFire();
        }
        // Sürgü animasyonunu tetikle
        if (isLeftHand && Dual_Pistol != null)
        {
            var leftSlide = Dual_Pistol.GetComponentInChildren<PistolSlide>(true);
            if (leftSlide != null) leftSlide.Fire();
        }
        else
        {
            if (Pistol_Slide_Script != null)
                Pistol_Slide_Script.Fire();
            else if (Pistol != null)
            {
                Pistol_Slide_Script = Pistol.GetComponentInChildren<PistolSlide>(true);
                if (Pistol_Slide_Script != null) Pistol_Slide_Script.Fire();
            }
        }
    }

    void Fire_Rifle(bool isLeftHand)
    {
        if (isLeftHand)
        {
            if (Left_Current_Bullet_Rifle <= 0)
            {
                AudioSystem?.PlayDryFire(3);
                return;
            }
            Left_Current_Bullet_Rifle--;
        }
        else
        {
            if (Right_Current_Bullet_Rifle <= 0)
            {
                AudioSystem?.PlayDryFire(3);
                return;
            }
            Right_Current_Bullet_Rifle--;
        }

        AudioSystem?.PlayFire(3, isLeftHand);
        (VFXManager ?? WeaponVFXManager.Instance)?.TriggerMuzzleFlash(3, isLeftHand);
        Vector3 spreadDir = GetSpreadDirection(0.02f, 0.02f);
        PerformRaycast(spreadDir, 3);
        CameraShake.Instance?.ShakeRifle(isLeftHand);
        SetCooldown(Fire_Cooldown_Rifle, isLeftHand);
        UpdateAllAmmoUI();
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.PlayFire();
        }
    }

    void Fire_Shotgun(bool isLeftHand)
    {
        if (isLeftHand)
        {
            if (Left_Current_Bullet_Shotgun <= 0)
            {
                AudioSystem?.PlayDryFire(2);
                return;
            }
            Left_Current_Bullet_Shotgun--;
        }
        else
        {
            if (Right_Current_Bullet_Shotgun <= 0)
            {
                AudioSystem?.PlayDryFire(2);
                return;
            }
            Right_Current_Bullet_Shotgun--;
        }

        AudioSystem?.PlayFire(2, isLeftHand);
        (VFXManager ?? WeaponVFXManager.Instance)?.TriggerMuzzleFlash(2, isLeftHand);
        for (int i = 0; i < Pellet_Count; i++)
        {
            Vector3 spreadDir = GetSpreadDirection(Spread_Width, Spread_Height);
            PerformRaycast(spreadDir, 2);
        }
        CameraShake.Instance?.ShakeShotgun(isLeftHand);
        SetCooldown(Fire_Cooldown_Shotgun, isLeftHand);
        UpdateAllAmmoUI();
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.PlayFire();
        }
    }

    void PerformRaycast(Vector3 dir, int gunType)
    {
        RaycastHit hit;
        if (Physics.Raycast(Fire_Point.position, dir.normalized, out hit, 100f))
        {
            Rigidbody hitRb = hit.collider.GetComponent<Rigidbody>();
            if (hitRb != null) hitRb.AddForceAtPosition(dir.normalized * pushForce, hit.point);

            Color c = (gunType == 2) ? Color.red : Color.yellow;
            Debug.DrawLine(Fire_Point.position, hit.point, c, 0.1f);

            AudioSystem?.PlayImpact(hit.point, hit.normal, hit.collider);
            (VFXManager ?? WeaponVFXManager.Instance)?.TriggerImpact(hit.point, hit.normal, hit.collider, gunType);
        }
    }

    Vector3 GetSpreadDirection(float w, float h)
    {
        return Fire_Point.forward + (Fire_Point.right * Random.Range(-w, w)) + (Fire_Point.up * Random.Range(-h, h));
    }

    void SetCooldown(float duration, bool isLeftHand)
    {
        if (isLeftHand) Next_Fire_Time_Left = Time.time + duration;
        else Next_Fire_Time_Right = Time.time + duration;
    }

    // --- SİLAH RELOAD KONTROLÜ (MERMİ DOLUYSA YAPILAMAZ) ---
    bool CanReloadGun(int gunID, bool isLeftHand)
    {
        if (gunID == 1) // Pistol
        {
            int current = isLeftHand ? Left_Current_Bullet_Pistol : Right_Current_Bullet_Pistol;
            return current < Max_Bullet_Pistol_Magazine && Total_Pistol_Ammo > 0;
        }
        else if (gunID == 2) // Shotgun
        {
            int current = isLeftHand ? Left_Current_Bullet_Shotgun : Right_Current_Bullet_Shotgun;
            return current < Max_Bullet_Shotgun_Magazine && Total_Shotgun_Ammo > 0;
        }
        else if (gunID == 3) // Rifle
        {
            int current = isLeftHand ? Left_Current_Bullet_Rifle : Right_Current_Bullet_Rifle;
            return current < Max_Bullet_Rifle_Magazine && Total_Rifle_Ammo > 0;
        }
        return false;
    }

    int GetCurrentMagAmmo(int gunID, bool isLeftHand)
    {
        if (gunID == 1) return isLeftHand ? Left_Current_Bullet_Pistol : Right_Current_Bullet_Pistol;
        if (gunID == 2) return isLeftHand ? Left_Current_Bullet_Shotgun : Right_Current_Bullet_Shotgun;
        if (gunID == 3) return isLeftHand ? Left_Current_Bullet_Rifle : Right_Current_Bullet_Rifle;
        return 0;
    }

    bool HasAmmoForFireInput(bool fire1, bool fire2)
    {
        if (Dual_Weapon_Mode)
        {
            if (fire1 && GetCurrentMagAmmo(Left_Gun_ID, true) > 0) return true;
            if (fire2 && GetCurrentMagAmmo(Right_Gun_ID, false) > 0) return true;
            return false;
        }
        return fire1 && GetCurrentMagAmmo(Right_Gun_ID, false) > 0;
    }

    /// <summary>
    /// Tabanca, Pompalı veya Tüfek için aktif tüm reload işlemlerini, kol animasyonunu,
    /// şarjör hareketini ve reload seslerini anında iptal eder.
    /// </summary>
    public void CancelAllReloads(bool instantAnimStop = true)
    {
        if (shotgunReloadCoroutine != null)
        {
            StopCoroutine(shotgunReloadCoroutine);
            shotgunReloadCoroutine = null;
        }
        isShotgunReloading = false;

        if (activeReloadCoroutine != null)
        {
            StopCoroutine(activeReloadCoroutine);
            activeReloadCoroutine = null;
        }
        isMagazineReloading = false;

        Next_Reload_Time = Time.time;

        Weapon_Anim_System?.InterruptAction(smooth: !instantAnimStop);

        Pistol?.GetComponentInChildren<WeaponMagazineReload>(true)?.CancelReload();
        Rifle?.GetComponentInChildren<WeaponMagazineReload>(true)?.CancelReload();
        Dual_Pistol?.GetComponentInChildren<WeaponMagazineReload>(true)?.CancelReload();
        Dual_Rifle?.GetComponentInChildren<WeaponMagazineReload>(true)?.CancelReload();

        AudioSystem?.StopReloadAudio();

        Next_Fire_Time_Right = Mathf.Min(Next_Fire_Time_Right, Time.time);
        Next_Fire_Time_Left = Mathf.Min(Next_Fire_Time_Left, Time.time);
    }

    // --- RELOAD SİSTEMİ (DOLUYKEN ENGELLEMELİ & ZAMANLI) ---
    void HandleReload()
    {
        // 1. Zaten reload yapılıyorsa tekrar basılmasını engelle
        if (IsReloading || Time.time < Next_Reload_Time) return;

        // 2. Mermi silahın içinde tam doluyken veya yedek mermi yokken reload YAPMA (Her silah tipi için geçerli)
        bool canReloadRight = CanReloadGun(Right_Gun_ID, false);
        bool canReloadLeft = Dual_Weapon_Mode && CanReloadGun(Left_Gun_ID, true);

        // Her iki eldeki silah da doluysa hiçbir şey yapma ve animasyona girme!
        if (!canReloadRight && !canReloadLeft)
        {
            return;
        }

        // Pompalı için çok aşamalı tek tek fişek doldurma mantığı (Tekli Mod)
        if (!Dual_Weapon_Mode && Right_Gun_ID == 2)
        {
            if (shotgunReloadCoroutine != null) StopCoroutine(shotgunReloadCoroutine);
            shotgunReloadCoroutine = StartCoroutine(ShotgunReloadRoutine());
            return;
        }

        float reloadDuration;
        if (!Dual_Weapon_Mode && Right_Gun_ID == 1)
        {
            reloadDuration = Pistol_Reload_Duration;
        }
        else if (!Dual_Weapon_Mode && Right_Gun_ID == 3)
        {
            reloadDuration = Rifle_Reload_Duration;
        }
        else
        {
            reloadDuration = Right_Gun_ID == 1 ? Pistol_Reload_Duration : (Right_Gun_ID == 3 ? Rifle_Reload_Duration : 1.5f);
        }

        if (activeReloadCoroutine != null) StopCoroutine(activeReloadCoroutine);
        activeReloadCoroutine = StartCoroutine(MagazineReloadRoutine(canReloadRight, canReloadLeft, Right_Gun_ID, Left_Gun_ID, reloadDuration));
    }

    IEnumerator MagazineReloadRoutine(bool reloadRight, bool reloadLeft, int rightGunId, int leftGunId, float duration)
    {
        isMagazineReloading = true;

        Weapon_Anim_System?.PlayReloadWithDuration(duration);

        if (reloadRight) TriggerMagazineReload(rightGunId, false, duration);
        if (reloadLeft) TriggerMagazineReload(leftGunId, true, duration);

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (reloadLeft)
        {
            if (leftGunId == 1) ReloadSingleMagazine(ref Left_Current_Bullet_Pistol, ref Total_Pistol_Ammo, Max_Bullet_Pistol_Magazine);
            else if (leftGunId == 2) ReloadSingleMagazine(ref Left_Current_Bullet_Shotgun, ref Total_Shotgun_Ammo, Max_Bullet_Shotgun_Magazine);
            else if (leftGunId == 3) ReloadSingleMagazine(ref Left_Current_Bullet_Rifle, ref Total_Rifle_Ammo, Max_Bullet_Rifle_Magazine);
        }

        if (reloadRight)
        {
            if (rightGunId == 1) ReloadSingleMagazine(ref Right_Current_Bullet_Pistol, ref Total_Pistol_Ammo, Max_Bullet_Pistol_Magazine);
            else if (rightGunId == 2) ReloadSingleMagazine(ref Right_Current_Bullet_Shotgun, ref Total_Shotgun_Ammo, Max_Bullet_Shotgun_Magazine);
            else if (rightGunId == 3) ReloadSingleMagazine(ref Right_Current_Bullet_Rifle, ref Total_Rifle_Ammo, Max_Bullet_Rifle_Magazine);
        }

        UpdateAllAmmoUI();
        isMagazineReloading = false;
        activeReloadCoroutine = null;
        Next_Reload_Time = Time.time + 0.05f;
    }

    // --- ÇOK AŞAMALI TEK TEK FİŞEK DOLDURMA (SHOTGUN) ---
    IEnumerator ShotgunReloadRoutine()
    {
        isShotgunReloading = true;

        // 1. Doldurma hazırlığı (Preparation Before)
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.PlayShotgunPrepBefore();
        }

        float prepBeforeTime = Weapon_Anim_System != null ? Weapon_Anim_System.GetShotgunPrepBeforeDuration() : 0.13f;
        yield return new WaitForSeconds(prepBeforeTime);

        // 2. Fişekleri teker teker doldur
        while (Right_Current_Bullet_Shotgun < Max_Bullet_Shotgun_Magazine && Total_Shotgun_Ammo > 0)
        {
            if (Weapon_Anim_System != null)
            {
                Weapon_Anim_System.PlayShotgunReloadInsert();
            }

            float insertTime = Weapon_Anim_System != null ? Weapon_Anim_System.GetShotgunInsertDuration() : 0.17f;

            // Fişeğin namlu tüpüne oturduğu an (~%65 süre)
            yield return new WaitForSeconds(insertTime * 0.65f);

            AudioSystem?.PlayShotgunInsert();
            Right_Current_Bullet_Shotgun++;
            Total_Shotgun_Ammo--;
            UpdateAllAmmoUI();

            // Kalan yerleştirme animasyonu süresi
            float remaining = insertTime * 0.35f;
            if (remaining > 0f)
            {
                yield return new WaitForSeconds(remaining);
            }
        }

        // 3. Pompalama ve hazırlık (Preparation After / Pump)
        if (Weapon_Anim_System != null)
        {
            Weapon_Anim_System.PlayShotgunPrepAfter();
        }
        AudioSystem?.PlayShotgunPump();

        float prepAfterTime = Weapon_Anim_System != null ? Weapon_Anim_System.GetShotgunPrepAfterDuration() : 0.31f;
        yield return new WaitForSeconds(prepAfterTime);

        isShotgunReloading = false;
        shotgunReloadCoroutine = null;
    }

    void InterruptShotgunReload()
    {
        if (!isShotgunReloading) return;
        CancelAllReloads(instantAnimStop: true);
    }

    void TriggerMagazineReload(int gunID, bool isLeftHand, float duration)
    {
        // Pompalı için kutu şarjör yok
        if (gunID == 2) return;

        GameObject weaponObj = null;
        if (isLeftHand)
        {
            if (gunID == 1) weaponObj = Dual_Pistol;
            else if (gunID == 3) weaponObj = Dual_Rifle;
        }
        else
        {
            if (gunID == 1) weaponObj = Pistol;
            else if (gunID == 3) weaponObj = Rifle;
        }

        if (weaponObj != null)
        {
            var magReload = weaponObj.GetComponentInChildren<WeaponMagazineReload>(true);
            if (magReload == null)
            {
                magReload = weaponObj.AddComponent<WeaponMagazineReload>();
            }
            magReload.StartReload(duration);
        }
    }

    bool ReloadSingleMagazine(ref int current, ref int total, int max)
    {
        if (current >= max || total <= 0) return false;

        int needed = max - current;

        if (total >= needed)
        {
            total -= needed;
            current += needed;
        }
        else
        {
            current += total;
            total = 0;
        }

        return true;
    }

    // --- GÖRSEL VE UI GÜNCELLEME ---
    void RefreshWeaponState()
    {
        CancelAllReloads(instantAnimStop: false);

        // 1. SİLAH MODELLERİNİ AÇ/KAPA (SADECE SEÇİLEN SİLAH GÖZÜKÜR, DİĞERLERİ ANINDA KAPANIR)
        Transform searchRoot = Camera.main != null ? Camera.main.transform : transform;
        if (Pistol == null || Shotgun == null || Rifle == null || Shotgun.name.Contains("ShotGun_F"))
        {
            Transform[] all = searchRoot.GetComponentsInChildren<Transform>(true);
            foreach (var t in all)
            {
                if (Pistol == null && t.name == "Pistol_L") Pistol = t.gameObject;
                if ((Shotgun == null || Shotgun.name.Contains("ShotGun_F")) && (t.name == "ShotGun_C" || t.name == "Shotgun_C")) Shotgun = t.gameObject;
                if (Rifle == null && t.name == "AK74") Rifle = t.gameObject;
            }
        }

        if (Pistol != null) Pistol.SetActive(Right_Gun_ID == 1);
        if (Shotgun != null) Shotgun.SetActive(Right_Gun_ID == 2);
        if (Rifle != null) Rifle.SetActive(Right_Gun_ID == 3);

        // Fazlalık ShotGun_F objesi varsa kesinlikle kapalı tut
        if (searchRoot != null)
        {
            foreach (var t in searchRoot.GetComponentsInChildren<Transform>(true))
            {
                if (t.name == "ShotGun_F" || t.name == "Shotgun_F")
                {
                    t.gameObject.SetActive(false);
                }
            }
        }

        if (Dual_Pistol != null) Dual_Pistol.SetActive(Dual_Weapon_Mode && Left_Gun_ID == 1);
        if (Dual_Shotgun != null) Dual_Shotgun.SetActive(Dual_Weapon_Mode && Left_Gun_ID == 2);
        if (Dual_Rifle != null) Dual_Rifle.SetActive(Dual_Weapon_Mode && Left_Gun_ID == 3);

        // 2. UI İKONLARINI AÇ/KAPA
        // Önce hepsini kapat
        Left_Pistol_Image.SetActive(false); Right_Pistol_Image.SetActive(false);
        Left_Rifle_Image.SetActive(false); Right_Rifle_Image.SetActive(false);
        Left_Shotgun_Image.SetActive(false); Right_Shotgun_Image.SetActive(false);

        // Sağ Tarafı Aç
        if (Right_Gun_ID == 1) Right_Pistol_Image.SetActive(true);
        else if (Right_Gun_ID == 2) Right_Shotgun_Image.SetActive(true);
        else if (Right_Gun_ID == 3) Right_Rifle_Image.SetActive(true);

        // Sol Tarafı Aç (Dual ise)
        if (Dual_Weapon_Mode)
        {
            if (Left_Gun_ID == 1) Left_Pistol_Image.SetActive(true);
            else if (Left_Gun_ID == 2) Left_Shotgun_Image.SetActive(true);
            else if (Left_Gun_ID == 3) Left_Rifle_Image.SetActive(true);
        }

        UpdateAllAmmoUI();
    }

    void UpdateAllAmmoUI()
    {
        // Null check yaparak tüm UI'ı güncelle
        if (Left_Pistol_Current) Left_Pistol_Current.text = Left_Current_Bullet_Pistol.ToString();
        if (Left_Pistol_Total) Left_Pistol_Total.text = Total_Pistol_Ammo.ToString();

        if (Right_Pistol_Current) Right_Pistol_Current.text = Right_Current_Bullet_Pistol.ToString();
        if (Right_Pistol_Total) Right_Pistol_Total.text = Total_Pistol_Ammo.ToString();

        if (Left_Rifle_Current) Left_Rifle_Current.text = Left_Current_Bullet_Rifle.ToString();
        if (Left_Rifle_Total) Left_Rifle_Total.text = Total_Rifle_Ammo.ToString();

        if (Right_Rifle_Current) Right_Rifle_Current.text = Right_Current_Bullet_Rifle.ToString();
        if (Right_Rifle_Total) Right_Rifle_Total.text = Total_Rifle_Ammo.ToString();

        if (Left_Shotgun_Current) Left_Shotgun_Current.text = Left_Current_Bullet_Shotgun.ToString();
        if (Left_Shotgun_Total) Left_Shotgun_Total.text = Total_Shotgun_Ammo.ToString();

        if (Right_Shotgun_Current) Right_Shotgun_Current.text = Right_Current_Bullet_Shotgun.ToString();
        if (Right_Shotgun_Total) Right_Shotgun_Total.text = Total_Shotgun_Ammo.ToString();
    }

    private void Image_Amount_Measurer(Image imageUI, float currentVal, float maxVal)
    {
        if (imageUI != null) imageUI.fillAmount = currentVal / maxVal;
    }

    public void TakeDamage(float amount)
    {
        if (Is_Dead) return;
        Current_Health -= amount;
        if (Current_Health <= 0f)
        {
            Current_Health = 0f;
            Is_Dead = true;
        }
        Image_Amount_Measurer(Health_Image, Current_Health, Max_Health);
        CameraShake.Instance?.ShakeDamage(amount / 20f);
    }
}