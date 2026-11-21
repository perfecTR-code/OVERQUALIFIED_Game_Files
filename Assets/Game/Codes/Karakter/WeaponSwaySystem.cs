using System;
using UnityEngine;

/// <summary>
/// Tek bir silah tipine ozel prosedurel sallanma (Weapon Sway) profili.
/// Kamera donus eylemsizligi, Bodycam serbest namlu kaymasi, strafe yatisi,
/// ziplama/inme agirligi ve organik nefes salinimi parametrelerini icerir.
/// </summary>
[Serializable]
public class WeaponSwayProfile
{
    public string weaponName = "Weapon";

    [Header("=== GENEL SIDDET ===")]
    [Tooltip("Bu silahin tum sallanma efektlerini carpan ana guc katsayisi")]
    [Range(0f, 2.5f)]
    public float overallIntensity = 1.0f;

    [Header("=== KAMERA DONUS SAVRULMASI (POZISYON - METRE) ===")]
    [Tooltip("Yatay kamera donusunde silahin yana kayma hassasiyeti")]
    [Range(0f, 0.12f)]
    public float lookPosX = 0.028f;

    [Tooltip("Yatay kaymanin ust siniri (Metre)")]
    [Range(0.01f, 0.2f)]
    public float maxLookPosX = 0.07f;

    [Tooltip("Dikey kamera donusunde silahin asagi/yukari kayma hassasiyeti")]
    [Range(0f, 0.12f)]
    public float lookPosY = 0.022f;

    [Tooltip("Dikey kaymanin ust siniri (Metre)")]
    [Range(0.01f, 0.2f)]
    public float maxLookPosY = 0.055f;

    [Tooltip("Hizli saga/sola donuste silahin hafifce geriye (omza) cekilme orani")]
    [Range(0f, 1.0f)]
    public float turnPullbackZ = 0.28f;

    [Header("=== KAMERA DONUS SAVRULMASI (NAMLU ACI & BILEK YATISI - DERECE) ===")]
    [Tooltip("Yatay donuste namlunun geriden gelme / savrulma acisi (Yaw)")]
    [Range(0f, 18f)]
    public float lookRotYaw = 5.5f;

    [Tooltip("Maksimum yatay namlu savrulma acisi (Derece)")]
    [Range(1f, 25f)]
    public float maxLookRotYaw = 13f;

    [Tooltip("Yukari/asagi bakarken namlunun geriden gelme egimi (Pitch)")]
    [Range(0f, 15f)]
    public float lookRotPitch = 4.2f;

    [Tooltip("Maksimum dikey namlu savrulma acisi (Derece)")]
    [Range(1f, 22f)]
    public float maxLookRotPitch = 10f;

    [Tooltip("Saga/sola donuste bilegin ve silahin kendi ekseninde yatisi (Roll tilt)")]
    [Range(0f, 18f)]
    public float lookRotRoll = 5.8f;

    [Tooltip("Maksimum bilek yatisi acisi (Derece)")]
    [Range(1f, 25f)]
    public float maxLookRotRoll = 13f;

    [Header("=== BODYCAM SERBEST NAMLU KAYMASI (FREE-AIM DRIFT) ===")]
    [Tooltip("Kamerayi cevirdiginde silahin bir sure kameradan bagimsiz eksende asili kalip sonradan merkeze donme orani")]
    [Range(0f, 2.0f)]
    public float bodycamDriftAmount = 0.78f;

    [Tooltip("Bodycam serbest kaymasinin cikabilecegi maksimum aci (Derece)")]
    [Range(0f, 15f)]
    public float bodycamMaxAngle = 6.2f;

    [Tooltip("Bodycam kaymasinin kameranin merkezine tekrar odaklanma hizi")]
    [Range(1f, 15f)]
    public float bodycamRecenterSpeed = 4.8f;

    [Header("=== YAY FIZIGI & AGIRLIK (SPRING-DAMPER) ===")]
    [Tooltip("Yay sertligi / tepki hizi (Dusuk = agir silah hissi, Yuksek = seri CoD hissi)")]
    [Range(4f, 30f)]
    public float springFrequency = 13.5f;

    [Tooltip("Yay sonumleme orani (<1 = hafif yaylanma/overshoot, 1 = kritik sonumleme)")]
    [Range(0.35f, 1.4f)]
    public float springDamping = 0.66f;

    [Header("=== HAREKET & YANA YATIS (STRAFE / WALK INERTIA) ===")]
    [Tooltip("A/D ile yan yururken silahin ters yone kaymasi (Metre)")]
    [Range(0f, 0.06f)]
    public float strafePosX = 0.018f;

    [Tooltip("A/D ile yan yururken silahin hafifce yana yatisi (Roll - Derece)")]
    [Range(0f, 10f)]
    public float strafeTiltRoll = 3.6f;

    [Tooltip("A/D ile yan yururken namlunun hafifce aci almasi (Yaw - Derece)")]
    [Range(0f, 5f)]
    public float strafeYawOffset = 1.6f;

    [Tooltip("Ileri/geri (W/S) giderken silahin derinlik kaymasi (Metre)")]
    [Range(0f, 0.05f)]
    public float forwardPosZ = 0.014f;

    [Tooltip("Ileri/geri (W/S) giderken namlu egimi (Pitch - Derece)")]
    [Range(0f, 5f)]
    public float forwardPitch = 1.1f;

    [Header("=== ZIPLAMA, NEFES VE YURUYUS SALINIMI ===")]
    [Tooltip("Ziplarken silahin yukari/asagi suzulmesi ve yere inince agirlik cokmesi")]
    [Range(0f, 2.5f)]
    public float jumpSwayAmount = 1.10f;

    [Tooltip("Dururken nefes alma konumsal salinimi (Metre)")]
    [Range(0f, 0.015f)]
    public float breathPosAmount = 0.0035f;

    [Tooltip("Dururken nefes alma acisal salinimi (Derece)")]
    [Range(0f, 3f)]
    public float breathRotAmount = 0.60f;

    [Tooltip("Nefes alma hizi")]
    [Range(0.3f, 3.5f)]
    public float breathSpeed = 1.15f;

    [Tooltip("Yururken animasyona ek prosedurel 8-cizme salinimi")]
    [Range(0f, 2.0f)]
    public float walkBobAmount = 0.48f;

    [Tooltip("Reload veya Equip sirasinda sarjor animasyonunu bozmamak icin sway azaltma carpani")]
    [Range(0f, 1f)]
    public float actionSwayMultiplier = 0.40f;

    public WeaponSwayProfile Clone()
    {
        return (WeaponSwayProfile)MemberwiseClone();
    }

    public static WeaponSwayProfile CreateIdealPistol()
    {
        return new WeaponSwayProfile
        {
            weaponName = "Tabanca (Pistol)",
            overallIntensity = 1.0f,
            lookPosX = 0.022f,
            maxLookPosX = 0.055f,
            lookPosY = 0.018f,
            maxLookPosY = 0.045f,
            turnPullbackZ = 0.22f,
            lookRotYaw = 4.2f,
            maxLookRotYaw = 10f,
            lookRotPitch = 3.2f,
            maxLookRotPitch = 8f,
            lookRotRoll = 4.8f,
            maxLookRotRoll = 11f,
            bodycamDriftAmount = 0.55f,
            bodycamMaxAngle = 4.5f,
            bodycamRecenterSpeed = 6.5f,
            springFrequency = 17.5f,
            springDamping = 0.74f,
            strafePosX = 0.014f,
            strafeTiltRoll = 2.6f,
            strafeYawOffset = 1.2f,
            forwardPosZ = 0.010f,
            forwardPitch = 0.8f,
            jumpSwayAmount = 0.85f,
            breathPosAmount = 0.0028f,
            breathRotAmount = 0.45f,
            breathSpeed = 1.35f,
            walkBobAmount = 0.35f,
            actionSwayMultiplier = 0.40f
        };
    }

    public static WeaponSwayProfile CreateIdealShotgun()
    {
        return new WeaponSwayProfile
        {
            weaponName = "Pompali (Shotgun)",
            overallIntensity = 1.15f,
            lookPosX = 0.038f,
            maxLookPosX = 0.090f,
            lookPosY = 0.030f,
            maxLookPosY = 0.075f,
            turnPullbackZ = 0.35f,
            lookRotYaw = 7.2f,
            maxLookRotYaw = 16f,
            lookRotPitch = 5.4f,
            maxLookRotPitch = 12.5f,
            lookRotRoll = 7.5f,
            maxLookRotRoll = 16f,
            bodycamDriftAmount = 1.05f,
            bodycamMaxAngle = 8.5f,
            bodycamRecenterSpeed = 3.8f,
            springFrequency = 10.5f,
            springDamping = 0.58f,
            strafePosX = 0.024f,
            strafeTiltRoll = 4.8f,
            strafeYawOffset = 2.2f,
            forwardPosZ = 0.018f,
            forwardPitch = 1.6f,
            jumpSwayAmount = 1.45f,
            breathPosAmount = 0.0045f,
            breathRotAmount = 0.85f,
            breathSpeed = 0.95f,
            walkBobAmount = 0.65f,
            actionSwayMultiplier = 0.35f
        };
    }

    public static WeaponSwayProfile CreateIdealRifle()
    {
        return new WeaponSwayProfile
        {
            weaponName = "Tufek (AK-74)",
            overallIntensity = 1.0f,
            lookPosX = 0.028f,
            maxLookPosX = 0.070f,
            lookPosY = 0.022f,
            maxLookPosY = 0.055f,
            turnPullbackZ = 0.28f,
            lookRotYaw = 5.5f,
            maxLookRotYaw = 13f,
            lookRotPitch = 4.2f,
            maxLookRotPitch = 10f,
            lookRotRoll = 5.8f,
            maxLookRotRoll = 13f,
            bodycamDriftAmount = 0.78f,
            bodycamMaxAngle = 6.2f,
            bodycamRecenterSpeed = 4.8f,
            springFrequency = 13.5f,
            springDamping = 0.66f,
            strafePosX = 0.018f,
            strafeTiltRoll = 3.6f,
            strafeYawOffset = 1.6f,
            forwardPosZ = 0.014f,
            forwardPitch = 1.1f,
            jumpSwayAmount = 1.10f,
            breathPosAmount = 0.0035f,
            breathRotAmount = 0.60f,
            breathSpeed = 1.15f,
            walkBobAmount = 0.48f,
            actionSwayMultiplier = 0.40f
        };
    }

    public static WeaponSwayProfile CreateIdealForGun(int gunId)
    {
        if (gunId == 2) return CreateIdealShotgun();
        if (gunId == 3) return CreateIdealRifle();
        return CreateIdealPistol();
    }

    public static WeaponSwayProfile CreateHeavyBodycamForGun(int gunId)
    {
        WeaponSwayProfile p = CreateIdealForGun(gunId);
        p.overallIntensity = Mathf.Clamp(p.overallIntensity * 1.25f, 0f, 2.5f);
        p.lookPosX = Mathf.Clamp(p.lookPosX * 1.35f, 0f, 0.12f);
        p.lookPosY = Mathf.Clamp(p.lookPosY * 1.35f, 0f, 0.12f);
        p.lookRotYaw = Mathf.Clamp(p.lookRotYaw * 1.45f, 0f, 18f);
        p.lookRotPitch = Mathf.Clamp(p.lookRotPitch * 1.35f, 0f, 15f);
        p.lookRotRoll = Mathf.Clamp(p.lookRotRoll * 1.45f, 0f, 18f);
        p.maxLookRotYaw = Mathf.Clamp(p.maxLookRotYaw * 1.35f, 1f, 25f);
        p.maxLookRotPitch = Mathf.Clamp(p.maxLookRotPitch * 1.35f, 1f, 22f);
        p.maxLookRotRoll = Mathf.Clamp(p.maxLookRotRoll * 1.35f, 1f, 25f);
        p.bodycamDriftAmount = Mathf.Clamp(p.bodycamDriftAmount * 1.65f, 0f, 2.0f);
        p.bodycamMaxAngle = Mathf.Clamp(p.bodycamMaxAngle * 1.45f, 0f, 15f);
        p.bodycamRecenterSpeed = Mathf.Clamp(p.bodycamRecenterSpeed * 0.65f, 1f, 15f);
        p.springFrequency = Mathf.Clamp(p.springFrequency * 0.72f, 4f, 30f);
        p.springDamping = Mathf.Clamp(p.springDamping * 0.84f, 0.35f, 1.4f);
        p.walkBobAmount = Mathf.Clamp(p.walkBobAmount * 1.35f, 0f, 2.0f);
        return p;
    }

    public static WeaponSwayProfile CreateSnappyCoDForGun(int gunId)
    {
        WeaponSwayProfile p = CreateIdealForGun(gunId);
        p.overallIntensity = Mathf.Clamp(p.overallIntensity * 0.90f, 0f, 2.5f);
        p.lookPosX = Mathf.Clamp(p.lookPosX * 0.82f, 0f, 0.12f);
        p.lookPosY = Mathf.Clamp(p.lookPosY * 0.82f, 0f, 0.12f);
        p.lookRotYaw = Mathf.Clamp(p.lookRotYaw * 0.78f, 0f, 18f);
        p.lookRotPitch = Mathf.Clamp(p.lookRotPitch * 0.78f, 0f, 15f);
        p.lookRotRoll = Mathf.Clamp(p.lookRotRoll * 0.85f, 0f, 18f);
        p.bodycamDriftAmount = Mathf.Clamp(p.bodycamDriftAmount * 0.35f, 0f, 2.0f);
        p.bodycamRecenterSpeed = Mathf.Clamp(p.bodycamRecenterSpeed * 1.6f, 1f, 15f);
        p.springFrequency = Mathf.Clamp(p.springFrequency * 1.35f, 4f, 30f);
        p.springDamping = Mathf.Clamp(p.springDamping * 1.15f, 0.35f, 1.4f);
        return p;
    }
}

/// <summary>
/// Call of Duty + Bodycam Tarzi Prosedurel Silah Sallanma Sistemi (WeaponSwaySystem).
/// Animasyon klipinden tamamen bagimsiz olarak 2. derece yay fizigi, Bodycam serbest namlu kaymasi,
/// bilek yatisi (Roll), strafe eylemsizligi, ziplama/inme agirligi ve organik nefes salinimi uygular.
/// F9 tusuyla acilan oyun ici canli arayuzden ve Inspector'dan her silah ayri ayri ayarlanabilir.
/// </summary>
public class WeaponSwaySystem : MonoBehaviour
{
    private const string PrefsKey = "WeaponSwaySystem_ConfigV1";
    private const string EditorPendingSaveKey = "WeaponSway_PendingEditorSave";
    private const float MaxSubStep = 0.008f;

    public static WeaponSwaySystem Instance { get; private set; }
    public static bool IsMenuOpen => Instance != null && Instance._showMenu;

    [Header("=== GENEL SWAY KONTROLU ===")]
    [Tooltip("Prosedurel silah sallanmasini ac/kapat")]
    public bool enableSway = true;

    [Tooltip("Silahin kamera onundeki donme ekseni (Kamera lokal uzayinda gogus/onkol merkezi)")]
    public Vector3 pivotLocalOffset = new Vector3(0f, -0.15f, 0.12f);

    [Tooltip("Kapali = Gercekci eylemsizlik (silah donusun gerisinden gelir) | Acik = Ters/kirbac hissi")]
    public bool invertLookSway = false;

    [Tooltip("Opsiyonel kol kok objesi (Bos birakilirsa HandWithGloves otomatik bulunur)")]
    public Transform armsRootOverride;

    [Header("=== SILAH BAZLI PROFILLER ===")]
    public WeaponSwayProfile tabancaProfili = WeaponSwayProfile.CreateIdealPistol();
    public WeaponSwayProfile pompaliProfili = WeaponSwayProfile.CreateIdealShotgun();
    public WeaponSwayProfile tufekProfili = WeaponSwayProfile.CreateIdealRifle();

    [Header("=== CANLI AYAR MENUSU (IN-GAME UI - F9) ===")]
    [Tooltip("Oyun ici sway ayar menusunu acma/kapatma tusu")]
    public KeyCode menuToggleKey = KeyCode.F9;
    public bool enableInGameMenu = true;
    public bool loadSavedSettingsOnStart = true;

    [Serializable]
    private class SwaySaveData
    {
        public bool enableSway;
        public bool invertLookSway;
        public WeaponSwayProfile pistol;
        public WeaponSwayProfile shotgun;
        public WeaponSwayProfile rifle;
    }

    // Pivot ve bilesen referanslari
    private Transform _armsRoot;
    private Transform _swayPivot;
    private Vector3 _basePivotLocalPos;
    private FPSCamera _fpsCamera;
    private Player1 _player;
    private Rigidbody _rb;

    // Fallback look takibi (FPSCamera yoksa)
    private float _prevCamPitch;
    private float _prevBodyYaw;
    private bool _hasPrevLookAngles;

    // Yay fizigi durum degiskenleri
    private Vector3 _currentPos = Vector3.zero;
    private Vector3 _posVel = Vector3.zero;
    private Vector3 _currentRot = Vector3.zero;
    private Vector3 _rotVel = Vector3.zero;

    private Vector2 _smoothLookVel = Vector2.zero;
    private Vector2 _bodycamDrift = Vector2.zero;
    private float _prevVelY = 0f;
    private float _walkBobTimer = 0f;
    private float _actionBlend = 1f;
    private float _seed = 0f;

    // UI durum degiskenleri
    private bool _showMenu = false;
    private int _selectedTabGunId = 1;
    private int _lastSyncedPlayerGunId = -1;
    private Rect _windowRect = new Rect(400, 20, 410, 620);
    private Vector2 _scrollPos = Vector2.zero;
    private float _saveFeedbackTimer = 0f;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;

        _seed = UnityEngine.Random.value * 500f;
        EnsureProfilesInitialized();

        if (loadSavedSettingsOnStart && PlayerPrefs.HasKey(PrefsKey))
        {
            ImportFromJson(PlayerPrefs.GetString(PrefsKey));
        }
    }

    void Start()
    {
        CacheReferences();
        EnsureSwayPivot();
        if (_player != null)
        {
            _selectedTabGunId = Mathf.Clamp(_player.Right_Gun_ID, 1, 3);
            _lastSyncedPlayerGunId = _selectedTabGunId;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void EnsureProfilesInitialized()
    {
        if (tabancaProfili == null) tabancaProfili = WeaponSwayProfile.CreateIdealPistol();
        if (pompaliProfili == null) pompaliProfili = WeaponSwayProfile.CreateIdealShotgun();
        if (tufekProfili == null) tufekProfili = WeaponSwayProfile.CreateIdealRifle();
    }

    private void CacheReferences()
    {
        if (_player == null) _player = GetComponent<Player1>() ?? FindFirstObjectByType<Player1>();
        if (_rb == null) _rb = GetComponent<Rigidbody>() ?? GetComponentInParent<Rigidbody>();
        if (_fpsCamera == null) _fpsCamera = GetComponentInChildren<FPSCamera>(true) ?? FindFirstObjectByType<FPSCamera>();
    }

    /// <summary>
    /// HandWithGloves objesi local origin'i karakterin ayaklarinda (y = -2) tuttugu icin
    /// dogrudan kol objesini dondurmek silahi ekrandan firlatir. Bunun yerine Kamera altinda
    /// onkol/gogus hizasinda temiz bir WeaponSwayPivot olusturulur ve kol objesi bunun altina alinir.
    /// </summary>
    private void EnsureSwayPivot()
    {
        if (_swayPivot != null && _armsRoot != null) return;

        if (_armsRoot == null)
        {
            if (armsRootOverride != null) _armsRoot = armsRootOverride;
            else if (WeaponAnimationSystem.Instance != null && WeaponAnimationSystem.Instance.targetAnimator != null)
                _armsRoot = WeaponAnimationSystem.Instance.targetAnimator.transform;
            else if (_player != null && _player.Player_Arms_Animator != null)
                _armsRoot = _player.Player_Arms_Animator.transform;
            else
            {
                Animator anim = GetComponentInChildren<Animator>(true);
                if (anim != null) _armsRoot = anim.transform;
                else
                {
                    GameObject armsGo = GameObject.Find("HandWithGloves");
                    if (armsGo != null) _armsRoot = armsGo.transform;
                }
            }
        }

        if (_armsRoot == null) return;

        Transform parent = _armsRoot.parent;
        if (parent != null && parent.name == "WeaponSwayPivot")
        {
            _swayPivot = parent;
            _basePivotLocalPos = _swayPivot.localPosition;
            return;
        }

        Transform camParent = parent != null ? parent : (_fpsCamera != null ? _fpsCamera.transform : null);
        if (camParent == null) return;

        GameObject pivotGo = new GameObject("WeaponSwayPivot");
        _swayPivot = pivotGo.transform;
        _swayPivot.SetParent(camParent, false);
        _swayPivot.localPosition = pivotLocalOffset;
        _swayPivot.localRotation = Quaternion.identity;
        _swayPivot.localScale = Vector3.one;

        _armsRoot.SetParent(_swayPivot, true);
        _basePivotLocalPos = pivotLocalOffset;
    }

    void Update()
    {
        if (_saveFeedbackTimer > 0f)
        {
            _saveFeedbackTimer = Mathf.Max(0f, _saveFeedbackTimer - Time.unscaledDeltaTime);
        }

        // Oyuncu 1-2-3 tuslariyla silah degistirdiginde menudeki sekmeyi senkronize et
        if (_player != null && _player.Right_Gun_ID != _lastSyncedPlayerGunId)
        {
            _lastSyncedPlayerGunId = _player.Right_Gun_ID;
            _selectedTabGunId = Mathf.Clamp(_lastSyncedPlayerGunId, 1, 3);
        }

        if (enableInGameMenu && Input.GetKeyDown(menuToggleKey))
        {
            SetMenuVisible(!_showMenu);
        }
    }

    private void SetMenuVisible(bool visible)
    {
        _showMenu = visible;
        if (_showMenu)
        {
            if (_player != null) _selectedTabGunId = Mathf.Clamp(_player.Right_Gun_ID, 1, 3);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else if (!WeaponVFXManager.IsMenuOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    void LateUpdate()
    {
        if (_swayPivot == null || _armsRoot == null)
        {
            CacheReferences();
            EnsureSwayPivot();
            if (_swayPivot == null) return;
        }

        float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 0.033f);
        int activeGunId = _player != null ? _player.Right_Gun_ID : 1;
        WeaponSwayProfile profile = GetProfile(activeGunId);

        if (!enableSway || profile == null)
        {
            StepSpring(ref _currentPos, ref _posVel, Vector3.zero, 16f, 0.9f, dt);
            StepSpring(ref _currentRot, ref _rotVel, Vector3.zero, 16f, 0.9f, dt);
            ApplyPivotTransform();
            return;
        }

        bool menuBlocking = Cursor.lockState != CursorLockMode.Locked || IsMenuOpen || WeaponVFXManager.IsMenuOpen;
        Vector2 lookDelta = menuBlocking ? Vector2.zero : ReadLookDelta();

        Vector3 targetPos = Vector3.zero;
        Vector3 targetRot = Vector3.zero;

        EvaluateLookInertia(lookDelta, dt, profile, ref targetPos, ref targetRot);
        EvaluateBodycamDrift(lookDelta, dt, profile, ref targetPos, ref targetRot);
        EvaluateMovementTilt(menuBlocking, dt, profile, ref targetPos, ref targetRot);
        EvaluateJumpAndLanding(dt, profile, ref targetPos, ref targetRot);
        EvaluateBreathingAndWalkBob(dt, profile, ref targetPos, ref targetRot);

        // Reload / Equip aksiyonlarinda sarjor animasyonunu korumak icin yumusak azaltma
        bool actionActive = WeaponAnimationSystem.Instance != null && WeaponAnimationSystem.Instance.IsActionPlaying();
        float targetActionBlend = actionActive ? profile.actionSwayMultiplier : 1f;
        _actionBlend = Mathf.Lerp(_actionBlend, targetActionBlend, 1f - Mathf.Exp(-14f * dt));

        float scale = profile.overallIntensity * _actionBlend;
        targetPos *= scale;
        targetRot *= scale;

        StepSpring(ref _currentPos, ref _posVel, targetPos, profile.springFrequency, profile.springDamping, dt);
        StepSpring(ref _currentRot, ref _rotVel, targetRot, profile.springFrequency, profile.springDamping, dt);
        ApplyPivotTransform();
    }

    private Vector2 ReadLookDelta()
    {
        if (_fpsCamera != null)
        {
            return _fpsCamera.LastLookDelta;
        }

        Transform camTr = Camera.main != null ? Camera.main.transform : transform;
        float pitch = camTr.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        float yaw = (_player != null ? _player.transform.eulerAngles.y : camTr.eulerAngles.y);

        if (!_hasPrevLookAngles)
        {
            _prevCamPitch = pitch;
            _prevBodyYaw = yaw;
            _hasPrevLookAngles = true;
            return Vector2.zero;
        }

        float deltaYaw = Mathf.DeltaAngle(_prevBodyYaw, yaw);
        float deltaPitch = _prevCamPitch - pitch;
        _prevCamPitch = pitch;
        _prevBodyYaw = yaw;
        return new Vector2(deltaYaw, deltaPitch);
    }

    private void EvaluateLookInertia(Vector2 lookDelta, float dt, WeaponSwayProfile p, ref Vector3 targetPos, ref Vector3 targetRot)
    {
        Vector2 rawNormVel = (lookDelta / dt) / 150f;
        float velSmooth = 1f - Mathf.Exp(-28f * dt);
        _smoothLookVel = Vector2.Lerp(_smoothLookVel, rawNormVel, velSmooth);

        float sign = invertLookSway ? -1f : 1f;
        float lookPosX = SoftClamp(-_smoothLookVel.x * p.lookPosX * sign, p.maxLookPosX);
        float lookPosY = SoftClamp(-_smoothLookVel.y * p.lookPosY * sign, p.maxLookPosY);
        float lookPosZ = -Mathf.Abs(lookPosX) * p.turnPullbackZ;

        targetPos.x += lookPosX;
        targetPos.y += lookPosY;
        targetPos.z += lookPosZ;

        targetRot.y += SoftClamp(-_smoothLookVel.x * p.lookRotYaw * sign, p.maxLookRotYaw);
        targetRot.x += SoftClamp(_smoothLookVel.y * p.lookRotPitch * sign, p.maxLookRotPitch);
        targetRot.z += SoftClamp(_smoothLookVel.x * p.lookRotRoll * sign, p.maxLookRotRoll);
    }

    private void EvaluateBodycamDrift(Vector2 lookDelta, float dt, WeaponSwayProfile p, ref Vector3 targetPos, ref Vector3 targetRot)
    {
        if (p.bodycamDriftAmount <= 0.001f || p.bodycamMaxAngle <= 0.001f)
        {
            _bodycamDrift = Vector2.Lerp(_bodycamDrift, Vector2.zero, 1f - Mathf.Exp(-10f * dt));
            return;
        }

        float sign = invertLookSway ? -1f : 1f;
        _bodycamDrift.x = SoftClamp(_bodycamDrift.x - lookDelta.x * 0.12f * p.bodycamDriftAmount * sign, p.bodycamMaxAngle);
        _bodycamDrift.y = SoftClamp(_bodycamDrift.y + lookDelta.y * 0.09f * p.bodycamDriftAmount * sign, p.bodycamMaxAngle * 0.75f);

        float recenter = 1f - Mathf.Exp(-p.bodycamRecenterSpeed * dt);
        _bodycamDrift = Vector2.Lerp(_bodycamDrift, Vector2.zero, recenter);

        targetRot.y += _bodycamDrift.x;
        targetRot.x += _bodycamDrift.y;
        targetRot.z -= _bodycamDrift.x * 0.45f;

        targetPos.x += _bodycamDrift.x * 0.0032f;
        targetPos.y -= _bodycamDrift.y * 0.0025f;
    }

    private void EvaluateMovementTilt(bool menuBlocking, float dt, WeaponSwayProfile p, ref Vector3 targetPos, ref Vector3 targetRot)
    {
        float h = menuBlocking ? 0f : Input.GetAxisRaw("Horizontal");
        float v = menuBlocking ? 0f : Input.GetAxisRaw("Vertical");

        targetPos.x += -h * p.strafePosX;
        targetPos.z += -v * p.forwardPosZ;

        targetRot.x += v * p.forwardPitch;
        targetRot.y += h * p.strafeYawOffset;
        targetRot.z += -h * p.strafeTiltRoll;
    }

    private void EvaluateJumpAndLanding(float dt, WeaponSwayProfile p, ref Vector3 targetPos, ref Vector3 targetRot)
    {
        if (_rb == null || p.jumpSwayAmount <= 0.001f)
            return;

        float velY = _rb.linearVelocity.y;
        targetPos.y += SoftClamp(-velY * 0.0035f * p.jumpSwayAmount, 0.045f);
        targetRot.x += SoftClamp(velY * 0.55f * p.jumpSwayAmount, 6.5f);

        // Yere inis darbesini tespit et ve yay hizina ani agirlik sok
        if (_prevVelY < -2.5f && velY > -0.6f)
        {
            float impact = Mathf.Clamp(-_prevVelY / 10f, 0.25f, 1.5f) * p.jumpSwayAmount * p.overallIntensity * _actionBlend;
            _posVel.y -= impact * 0.45f;
            _rotVel.x += impact * 42f;
            _rotVel.z += (UnityEngine.Random.value > 0.5f ? 1f : -1f) * impact * 22f;
        }

        _prevVelY = velY;
    }

    private void EvaluateBreathingAndWalkBob(float dt, WeaponSwayProfile p, ref Vector3 targetPos, ref Vector3 targetRot)
    {
        float t = Time.time * p.breathSpeed + _seed;
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && _rb != null
            && new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z).sqrMagnitude > 4f;
        float breathAtten = isSprinting ? 0.35f : 1f;

        float noiseX = (Mathf.PerlinNoise(_seed + 1.7f, t * 0.65f) - 0.5f) * 2f;
        float noiseY = (Mathf.PerlinNoise(_seed + 5.3f, t * 0.72f) - 0.5f) * 2f;

        float breathSin = Mathf.Sin(t * 1.4f);
        float breathCos = Mathf.Cos(t * 0.85f);

        targetPos.x += (breathCos * 0.55f + noiseX * 0.45f) * p.breathPosAmount * breathAtten;
        targetPos.y += (breathSin * 0.65f + noiseY * 0.35f) * p.breathPosAmount * breathAtten;

        targetRot.x += (breathSin * 0.6f + noiseY * 0.4f) * p.breathRotAmount * breathAtten;
        targetRot.y += (breathCos * 0.5f + noiseX * 0.5f) * p.breathRotAmount * 0.75f * breathAtten;
        targetRot.z += -breathCos * p.breathRotAmount * 0.4f * breathAtten;

        // Yuruyus / depar sirasinda eklenti 8-cizme sallanmasi
        if (p.walkBobAmount > 0.001f && _rb != null)
        {
            Vector2 horizVel = new Vector2(_rb.linearVelocity.x, _rb.linearVelocity.z);
            float speedMag = horizVel.magnitude;
            bool grounded = Mathf.Abs(_rb.linearVelocity.y) < 1.2f;

            if (speedMag > 0.6f && grounded)
            {
                float speedNorm = Mathf.Clamp01(speedMag / 16f);
                float bobRate = isSprinting ? 13.5f : 9.0f;
                _walkBobTimer += dt * bobRate;

                float sinBob = Mathf.Sin(_walkBobTimer);
                float cosBob = Mathf.Cos(_walkBobTimer * 2f);

                targetPos.x += sinBob * 0.0085f * p.walkBobAmount * speedNorm;
                targetPos.y += (cosBob - 1f) * 0.0045f * p.walkBobAmount * speedNorm;

                targetRot.z += -sinBob * 1.35f * p.walkBobAmount * speedNorm;
                targetRot.x += cosBob * 0.65f * p.walkBobAmount * speedNorm;
            }
            else
            {
                _walkBobTimer = Mathf.MoveTowards(_walkBobTimer, 0f, dt * 4f);
            }
        }
    }

    private static float SoftClamp(float val, float max)
    {
        if (max <= 0.0001f) return 0f;
        return max * (float)Math.Tanh(val / max);
    }

    private static void StepSpring(ref Vector3 value, ref Vector3 velocity, Vector3 target, float freq, float dampingRatio, float dt)
    {
        float omega = Mathf.Clamp(freq, 2f, 35f);
        float zeta = Mathf.Clamp(dampingRatio, 0.25f, 2.0f);

        int steps = Mathf.Clamp(Mathf.CeilToInt(dt / MaxSubStep), 1, 6);
        float subDt = dt / steps;
        float k = omega * omega;
        float c = 2f * zeta * omega;

        for (int i = 0; i < steps; i++)
        {
            Vector3 accel = (target - value) * k - velocity * c;
            velocity += accel * subDt;
            value += velocity * subDt;
        }
    }

    private void ApplyPivotTransform()
    {
        if (_swayPivot == null) return;
        _swayPivot.localPosition = _basePivotLocalPos + _currentPos;
        _swayPivot.localRotation = Quaternion.Euler(_currentRot);
    }

    public WeaponSwayProfile GetProfile(int gunId)
    {
        if (gunId == 2) return pompaliProfili;
        if (gunId == 3) return tufekProfili;
        return tabancaProfili;
    }

    public void SetProfile(int gunId, WeaponSwayProfile profile)
    {
        if (profile == null) return;
        if (gunId == 2) pompaliProfili = profile;
        else if (gunId == 3) tufekProfili = profile;
        else tabancaProfili = profile;
    }

    // =========================================================================
    // KAYDETME VE YUKLEME (JSON + PLAY MODE -> EDITOR AKTARIMI)
    // =========================================================================

    public string ExportToJson()
    {
        EnsureProfilesInitialized();
        SwaySaveData data = new SwaySaveData
        {
            enableSway = enableSway,
            invertLookSway = invertLookSway,
            pistol = tabancaProfili.Clone(),
            shotgun = pompaliProfili.Clone(),
            rifle = tufekProfili.Clone()
        };
        return JsonUtility.ToJson(data);
    }

    public void ImportFromJson(string json)
    {
        if (string.IsNullOrEmpty(json)) return;
        SwaySaveData data = JsonUtility.FromJson<SwaySaveData>(json);
        if (data == null) return;

        enableSway = data.enableSway;
        invertLookSway = data.invertLookSway;
        if (data.pistol != null) tabancaProfili = data.pistol;
        if (data.shotgun != null) pompaliProfili = data.shotgun;
        if (data.rifle != null) tufekProfili = data.rifle;
    }

    public void SaveSettings()
    {
        string json = ExportToJson();
        PlayerPrefs.SetString(PrefsKey, json);
        PlayerPrefs.Save();

#if UNITY_EDITOR
        UnityEditor.SessionState.SetString(EditorPendingSaveKey, json);
#endif
        _saveFeedbackTimer = 2.0f;
    }

    public void ResetAllToIdeal()
    {
        enableSway = true;
        invertLookSway = false;
        tabancaProfili = WeaponSwayProfile.CreateIdealPistol();
        pompaliProfili = WeaponSwayProfile.CreateIdealShotgun();
        tufekProfili = WeaponSwayProfile.CreateIdealRifle();

        PlayerPrefs.DeleteKey(PrefsKey);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.SessionState.SetString(EditorPendingSaveKey, ExportToJson());
#endif
        _saveFeedbackTimer = 2.0f;
    }

    // =========================================================================
    // CANLI OYUN ICI AYAR MENUSU (F9 ARAYUZU)
    // =========================================================================

    void OnGUI()
    {
        if (!_showMenu || !enableInGameMenu) return;

        float maxH = Mathf.Max(320f, Screen.height - 40f);
        float maxW = Mathf.Max(340f, Screen.width - 40f);
        _windowRect.width = Mathf.Min(420f, maxW);
        _windowRect.height = Mathf.Min(620f, maxH);
        _windowRect.x = Mathf.Clamp(_windowRect.x, 10f, Mathf.Max(10f, Screen.width - _windowRect.width - 10f));
        _windowRect.y = Mathf.Clamp(_windowRect.y, 10f, Mathf.Max(10f, Screen.height - _windowRect.height - 10f));

        GUI.backgroundColor = new Color(0.11f, 0.13f, 0.18f, 0.96f);
        _windowRect = GUI.Window(998, _windowRect, DrawSwayWindow, "🎯 SILAH SALLANMA (SWAY) AYAR MERKEZI [F9]");
    }

    private void DrawSwayWindow(int windowID)
    {
        GUILayout.Space(4);

        // Ust genel kontroller
        GUILayout.BeginHorizontal();
        enableSway = GUILayout.Toggle(enableSway, " Sallanma Aktif");
        invertLookSway = GUILayout.Toggle(invertLookSway, " Ters Yon (Lead)");
        GUILayout.EndHorizontal();

        GUILayout.Space(6);

        // Silah sekmeleri
        GUILayout.BeginHorizontal();
        DrawTabButton(1, "1: Tabanca");
        DrawTabButton(2, "2: Pompali");
        DrawTabButton(3, "3: Tufek");
        GUILayout.EndHorizontal();

        GUILayout.Space(6);
        DrawPresetButtons(_selectedTabGunId);
        GUILayout.Space(6);

        WeaponSwayProfile activeProfile = GetProfile(_selectedTabGunId);
        if (activeProfile != null)
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));

            GUILayout.Label($"<b>--- {activeProfile.weaponName.ToUpperInvariant()} PROFILI ---</b>");
            DrawSlider("Genel Siddet", ref activeProfile.overallIntensity, 0f, 2.5f, "F2", "x");

            GUILayout.Space(6);
            GUILayout.Label("<b>Kamera Donus Savrulmasi</b>");
            DrawSlider("Yatay Kayma (Pos X)", ref activeProfile.lookPosX, 0f, 0.10f, "F3");
            DrawSlider("Dikey Kayma (Pos Y)", ref activeProfile.lookPosY, 0f, 0.10f, "F3");
            DrawSlider("Namlu Yatay (Yaw)", ref activeProfile.lookRotYaw, 0f, 16f, "F1", "°");
            DrawSlider("Namlu Dikey (Pitch)", ref activeProfile.lookRotPitch, 0f, 14f, "F1", "°");
            DrawSlider("Bilek Yatisi (Roll)", ref activeProfile.lookRotRoll, 0f, 16f, "F1", "°");

            GUILayout.Space(6);
            GUILayout.Label("<b>Bodycam Serbest Namlu & Yay Fizigi</b>");
            DrawSlider("Bodycam Serbest Kayma", ref activeProfile.bodycamDriftAmount, 0f, 2.0f, "F2");
            DrawSlider("Bodycam Toparlanma", ref activeProfile.bodycamRecenterSpeed, 1f, 15f, "F1");
            DrawSlider("Yay Sertligi / Hiz", ref activeProfile.springFrequency, 4f, 28f, "F1");
            DrawSlider("Yay Sonumleme", ref activeProfile.springDamping, 0.35f, 1.35f, "F2");

            GUILayout.Space(6);
            GUILayout.Label("<b>Hareket, Ziplama ve Nefes</b>");
            DrawSlider("A/D Yana Yatis (Roll)", ref activeProfile.strafeTiltRoll, 0f, 10f, "F1", "°");
            DrawSlider("A/D Kayma (Pos X)", ref activeProfile.strafePosX, 0f, 0.05f, "F3");
            DrawSlider("Ziplama / Inme Agirligi", ref activeProfile.jumpSwayAmount, 0f, 2.5f, "F2");

            float prevBreathRot = activeProfile.breathRotAmount;
            DrawSlider("Nefes Salinimi", ref activeProfile.breathRotAmount, 0f, 2.5f, "F2", "°");
            if (!Mathf.Approximately(prevBreathRot, activeProfile.breathRotAmount))
            {
                activeProfile.breathPosAmount = activeProfile.breathRotAmount * 0.0058f;
            }

            DrawSlider("Yuruyus Prosedurel Bob", ref activeProfile.walkBobAmount, 0f, 2.0f, "F2");

            GUILayout.EndScrollView();
        }

        if (_saveFeedbackTimer > 0f)
        {
            GUI.color = new Color(0.45f, 1f, 0.55f);
            GUILayout.Label("<b>✓ Ayarlar kaydedildi! (Play modundan ciksan da korunur)</b>");
            GUI.color = Color.white;
        }

        GUILayout.Space(6);
        GUILayout.BeginHorizontal();
        GUI.color = new Color(0.35f, 0.85f, 0.45f);
        if (GUILayout.Button("💾 KAYDET"))
        {
            SaveSettings();
        }

        GUI.color = new Color(0.95f, 0.75f, 0.30f);
        if (GUILayout.Button("🔄 SIFIRLA"))
        {
            ResetAllToIdeal();
        }

        GUI.color = Color.white;
        if (GUILayout.Button("KAPAT [F9]"))
        {
            SetMenuVisible(false);
        }
        GUILayout.EndHorizontal();

        GUI.DragWindow(new Rect(0, 0, 10000, 24));
    }

    private void DrawTabButton(int gunId, string label)
    {
        bool selected = _selectedTabGunId == gunId;
        GUI.color = selected ? new Color(0.35f, 0.78f, 1f) : Color.white;
        if (GUILayout.Button(selected ? $"[{label}]" : label))
        {
            _selectedTabGunId = gunId;
        }
        GUI.color = Color.white;
    }

    private void DrawPresetButtons(int gunId)
    {
        GUILayout.BeginHorizontal();
        GUI.color = new Color(0.55f, 0.92f, 0.70f);
        if (GUILayout.Button("🎮 Ideal (Onerilen)"))
        {
            SetProfile(gunId, WeaponSwayProfile.CreateIdealForGun(gunId));
        }

        GUI.color = new Color(1f, 0.65f, 0.40f);
        if (GUILayout.Button("📹 Agir Bodycam"))
        {
            SetProfile(gunId, WeaponSwayProfile.CreateHeavyBodycamForGun(gunId));
        }

        GUI.color = new Color(0.55f, 0.75f, 1f);
        if (GUILayout.Button("⚡ Siki CoD"))
        {
            SetProfile(gunId, WeaponSwayProfile.CreateSnappyCoDForGun(gunId));
        }
        GUI.color = Color.white;
        GUILayout.EndHorizontal();
    }

    private static void DrawSlider(string label, ref float val, float min, float max, string format = "F2", string suffix = "")
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label($"{label}: {val.ToString(format)}{suffix}", GUILayout.Width(195));
        val = GUILayout.HorizontalSlider(val, min, max);
        GUILayout.EndHorizontal();
    }
}
