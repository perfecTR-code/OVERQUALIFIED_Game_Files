using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Temiz, modüler ve yüksek performanslı FPS Ses Sistemi (Pure C# Audio System)
/// Karakter, silahlar, şarjör mekaniği, ayak sesleri ve 3D mermi vuruşlarını yönetir.
/// GUI veya ekran menüleri İÇERMEZ; tüm ayarlar standart Unity Inspector üzerinden yönetilir.
/// </summary>
[DisallowMultipleComponent]
public class WeaponAudioSystem : MonoBehaviour
{
    public static WeaponAudioSystem Instance { get; private set; }

    [Header("=== TABANCA (PISTOL) SESLERİ ===")]
    [Tooltip("Tabanca ateş sesi (Tok, çevik 9mm sesi)")]
    public AudioClip pistolFireClip;
    [Tooltip("Tabanca sürgü çekme / kayma sesi")]
    public AudioClip pistolSlideClip;
    [Tooltip("Boş tetik düşürme sesi (Mermi bittiğinde)")]
    public AudioClip pistolDryFireClip;
    [Range(0f, 1f)] public float pistolVolume = 0.90f;

    [Header("=== POMPALI (SHOTGUN) SESLERİ ===")]
    [Tooltip("Pompalı ateş sesi (Ağır 12-gauge patlama)")]
    public AudioClip shotgunFireClip;
    [Tooltip("Pompalama mekanizma sesi (Pump forward/backward)")]
    public AudioClip shotgunPumpClip;
    [Tooltip("Fişek yuvaya itme / doldurma sesi (Shell insert)")]
    public AudioClip shotgunInsertClip;
    [Tooltip("Pompalı boş tetik sesi")]
    public AudioClip shotgunDryFireClip;
    [Range(0f, 1f)] public float shotgunVolume = 1.0f;

    [Header("=== TÜFEK (RIFLE / AK-74) SESLERİ ===")]
    [Tooltip("Tüfek otomatik ateş sesi (Ritmik, tok ve sert)")]
    public AudioClip rifleFireClip;
    [Tooltip("Tüfek boş tetik sesi")]
    public AudioClip rifleDryFireClip;
    [Range(0f, 1f)] public float rifleVolume = 0.85f;

    [Header("=== TARET (TURRET) SESLERİ ===")]
    [Tooltip("Taret kurulum / açılma servo sesi")]
    public AudioClip turretDeployClip;
    [Tooltip("Taret otomatik seri ateş sesi")]
    public AudioClip turretFireClip;
    [Tooltip("Taret motor / namlu dönüş sesi")]
    public AudioClip turretMotorClip;
    [Range(0f, 1f)] public float turretVolume = 0.95f;

    [Header("=== ŞARJÖR VE MEKANİZMA (RELOAD & HANDLING) ===")]
    [Tooltip("Şarjör çıkarma / yuvadan düşme sesi")]
    public AudioClip magOutClip;
    [Tooltip("Şarjör takma / yuvaya oturma sesi")]
    public AudioClip magInClip;
    [Tooltip("Kurma kolu / sürgü çekme sesi")]
    public AudioClip boltCockClip;
    [Tooltip("Silah çekme / kuşanma foley sesi")]
    public AudioClip weaponEquipClip;
    [Tooltip("Silah sallantısı / kıyafet hışırtısı")]
    public AudioClip weaponRustleClip;
    [Range(0f, 1f)] public float handlingVolume = 0.80f;

    [Header("=== AYAK SESLERİ (FOOTSTEPS) ===")]
    public AudioClip footstepConcreteClip;
    public AudioClip footstepMetalClip;
    public AudioClip footstepWoodClip;
    public AudioClip footstepDirtClip;
    public AudioClip landClip;
    [Range(0f, 1f)] public float footstepVolume = 0.70f;
    [Tooltip("Yürüyüş adım aralığı (saniye)")]
    public float walkStepInterval = 0.44f;
    [Tooltip("Hızlı koşu adım aralığı (saniye)")]
    public float sprintStepInterval = 0.28f;

    [Header("=== MERMİ VURUŞ SESLERİ (3D BULLET IMPACTS) ===")]
    public AudioClip impactConcreteClip;
    public AudioClip impactMetalClip;
    public AudioClip impactFleshClip;
    public AudioClip impactWoodClip;
    [Range(0f, 1f)] public float impactVolume = 0.85f;

    [Header("=== ORGANİK ÇEŞİTLİLİK (PITCH RANDOMIZATION) ===")]
    [Range(0f, 0.15f)] public float firePitchVariation = 0.035f;
    [Range(0f, 0.20f)] public float footstepPitchVariation = 0.06f;

    // Ses Çalma Kaynakları (AudioSources)
    private AudioSource _weaponAudioSource;
    private AudioSource _handlingAudioSource;
    private AudioSource _footstepAudioSource;

    // Durum Değişkenleri
    private Rigidbody _playerRb;
    private float _nextFootstepTime = 0f;
    private bool _wasGrounded = true;
    private float _lastDryFireTime = 0f;

    // 3D Vuruş Sesleri için Küçük Havuz (Audio Pooling)
    private const int IMPACT_POOL_SIZE = 6;
    private AudioSource[] _impactSources;
    private int _currentImpactIndex = 0;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        SetupAudioSources();
        _playerRb = GetComponent<Rigidbody>();
        if (_playerRb == null) _playerRb = GetComponentInParent<Rigidbody>();

#if UNITY_EDITOR
        AutoBindDefaultClipsIfEmpty();
#endif
    }

    void Reset()
    {
#if UNITY_EDITOR
        AutoBindDefaultClipsIfEmpty();
#endif
    }

    void SetupAudioSources()
    {
        // 1. Silah Ateş Kaynağı (2D, Yüksek Öncelik, Net ve Tok)
        _weaponAudioSource = gameObject.AddComponent<AudioSource>();
        _weaponAudioSource.spatialBlend = 0f; // 2D (Karakterin kulaklarında dolgun)
        _weaponAudioSource.playOnAwake = false;
        _weaponAudioSource.priority = 0; // En yüksek öncelik

        // 2. Mekanizma / Şarjör Kaynağı
        _handlingAudioSource = gameObject.AddComponent<AudioSource>();
        _handlingAudioSource.spatialBlend = 0f;
        _handlingAudioSource.playOnAwake = false;
        _handlingAudioSource.priority = 32;

        // 3. Ayak Sesi Kaynağı
        _footstepAudioSource = gameObject.AddComponent<AudioSource>();
        _footstepAudioSource.spatialBlend = 0.15f;
        _footstepAudioSource.playOnAwake = false;
        _footstepAudioSource.priority = 64;

        // 4. 3D Vuruş Kaynakları Havuzu
        GameObject poolRoot = new GameObject("_ImpactAudioPool");
        poolRoot.transform.SetParent(transform);
        _impactSources = new AudioSource[IMPACT_POOL_SIZE];
        for (int i = 0; i < IMPACT_POOL_SIZE; i++)
        {
            var src = poolRoot.AddComponent<AudioSource>();
            src.spatialBlend = 1.0f; // Tam 3D
            src.rolloffMode = AudioRolloffMode.Logarithmic;
            src.minDistance = 2f;
            src.maxDistance = 45f;
            src.playOnAwake = false;
            _impactSources[i] = src;
        }
    }

    void Update()
    {
        HandleFootsteps();
    }

    // ==========================================
    // SİLAH ATEŞLEME (FIRE)
    // ==========================================

    /// <summary>
    /// Silah ateş sesini çalar.
    /// gunID: 1 = Pistol, 2 = Shotgun, 3 = Rifle, 4 = Turret
    /// </summary>
    public void PlayFire(int gunID, bool isLeftHand = false)
    {
        AudioClip clipToPlay = null;
        float vol = 1.0f;

        switch (gunID)
        {
            case 1: // Pistol
                clipToPlay = pistolFireClip;
                vol = pistolVolume;
                break;
            case 2: // Shotgun
                clipToPlay = shotgunFireClip;
                vol = shotgunVolume;
                break;
            case 3: // Rifle
                clipToPlay = rifleFireClip;
                vol = rifleVolume;
                break;
            case 4: // Turret
                clipToPlay = turretFireClip != null ? turretFireClip : rifleFireClip;
                vol = turretVolume;
                break;
        }

        if (clipToPlay != null && _weaponAudioSource != null)
        {
            // İki el ayrı veya seri atışlarda hafif pitch ve pan farkı (Genişlik hissi)
            float pitchOffset = Random.Range(-firePitchVariation, firePitchVariation);
            if (isLeftHand) pitchOffset += 0.015f;

            _weaponAudioSource.pitch = 1.0f + pitchOffset;
            _weaponAudioSource.panStereo = isLeftHand ? -0.15f : 0.15f;
            _weaponAudioSource.PlayOneShot(clipToPlay, vol);
        }
    }

    // ==========================================
    // BOŞ TETİK (DRY FIRE)
    // ==========================================

    public void PlayDryFire(int gunID)
    {
        // Tetik basılı tutulduğunda aşırı sarmayı önlemek için kısa cooldown (0.2s)
        if (Time.time < _lastDryFireTime + 0.2f) return;
        _lastDryFireTime = Time.time;

        AudioClip clip = pistolDryFireClip;
        if (gunID == 2 && shotgunDryFireClip != null) clip = shotgunDryFireClip;
        else if (gunID == 3 && rifleDryFireClip != null) clip = rifleDryFireClip;

        if (clip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.97f, 1.03f);
            _handlingAudioSource.panStereo = 0f;
            _handlingAudioSource.PlayOneShot(clip, handlingVolume);
        }
    }

    // ==========================================
    // ŞARJÖR VE MEKANİZMA (RELOAD / COCK / PUMP)
    // ==========================================

    public void PlayMagOut(int gunID)
    {
        if (magOutClip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.98f, 1.02f);
            _handlingAudioSource.PlayOneShot(magOutClip, handlingVolume);
        }
    }

    public void PlayMagIn(int gunID)
    {
        if (magInClip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.98f, 1.02f);
            _handlingAudioSource.PlayOneShot(magInClip, handlingVolume);
        }
    }

    public void PlayBoltCock(int gunID)
    {
        AudioClip clip = boltCockClip != null ? boltCockClip : pistolSlideClip;
        if (clip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.97f, 1.03f);
            _handlingAudioSource.PlayOneShot(clip, handlingVolume);
        }
    }

    public void PlayShotgunPump()
    {
        AudioClip clip = shotgunPumpClip != null ? shotgunPumpClip : boltCockClip;
        if (clip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.96f, 1.02f);
            _handlingAudioSource.PlayOneShot(clip, handlingVolume);
        }
    }

    public void PlayShotgunInsert()
    {
        AudioClip clip = shotgunInsertClip != null ? shotgunInsertClip : magInClip;
        if (clip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(1.05f, 1.15f);
            _handlingAudioSource.PlayOneShot(clip, handlingVolume * 0.9f);
        }
    }

    public void PlayEquip(int gunID)
    {
        if (weaponEquipClip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = Random.Range(0.97f, 1.03f);
            _handlingAudioSource.PlayOneShot(weaponEquipClip, handlingVolume * 0.75f);
        }

        if (weaponRustleClip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.PlayOneShot(weaponRustleClip, handlingVolume * 0.5f);
        }
    }

    public void PlayTurretDeploy()
    {
        if (turretDeployClip != null && _handlingAudioSource != null)
        {
            _handlingAudioSource.pitch = 1.0f;
            _handlingAudioSource.PlayOneShot(turretDeployClip, turretVolume);
        }
    }

    // ==========================================
    // AYAK SESLERİ VE ZIPLAMA (FOOTSTEPS & LANDING)
    // ==========================================

    void HandleFootsteps()
    {
        // Karakterin yatay hızını hesapla
        Vector3 vel = Vector3.zero;
        if (_playerRb != null)
        {
#if UNITY_6000_0_OR_NEWER
            vel = _playerRb.linearVelocity;
#else
            vel = _playerRb.velocity;
#endif
        }

        float horizontalSpeed = new Vector2(vel.x, vel.z).magnitude;

        // Yer kontrolü (Raycast)
        RaycastHit hit;
        bool isGrounded = Physics.Raycast(transform.position + Vector3.up * 0.1f, Vector3.down, out hit, 1.3f);

        // Zıplamadan sonra yere iniş (Landing) kontrolü
        if (isGrounded && !_wasGrounded)
        {
            PlayLand();
        }
        _wasGrounded = isGrounded;

        if (!isGrounded || horizontalSpeed < 0.8f) return;

        // Koşma kontrolü
        bool isSprinting = Input.GetKey(KeyCode.LeftShift) && horizontalSpeed > 11f;
        float interval = isSprinting ? sprintStepInterval : walkStepInterval;

        if (Time.time >= _nextFootstepTime)
        {
            _nextFootstepTime = Time.time + interval;
            PlayFootstep(hit.collider, isSprinting);
        }
    }

    void PlayFootstep(Collider groundCol, bool isSprinting)
    {
        AudioClip clip = footstepConcreteClip;

        if (groundCol != null)
        {
            string matName = "";
            if (groundCol.sharedMaterial != null) matName = groundCol.sharedMaterial.name.ToLower();
            else
            {
                var rend = groundCol.GetComponent<Renderer>();
                if (rend != null && rend.sharedMaterial != null) matName = rend.sharedMaterial.name.ToLower();
            }

            if (matName.Contains("metal") || matName.Contains("iron") || matName.Contains("steel") || matName.Contains("grate"))
            {
                clip = footstepMetalClip != null ? footstepMetalClip : footstepConcreteClip;
            }
            else if (matName.Contains("wood") || matName.Contains("plank") || matName.Contains("parquet"))
            {
                clip = footstepWoodClip != null ? footstepWoodClip : footstepConcreteClip;
            }
            else if (matName.Contains("dirt") || matName.Contains("grass") || matName.Contains("gravel") || matName.Contains("mud"))
            {
                clip = footstepDirtClip != null ? footstepDirtClip : footstepConcreteClip;
            }
        }

        if (clip != null && _footstepAudioSource != null)
        {
            _footstepAudioSource.pitch = Random.Range(1.0f - footstepPitchVariation, 1.0f + footstepPitchVariation);
            float vol = footstepVolume * (isSprinting ? 1.15f : 0.85f);
            _footstepAudioSource.PlayOneShot(clip, vol);
        }
    }

    public void PlayLand()
    {
        AudioClip clip = landClip != null ? landClip : footstepConcreteClip;
        if (clip != null && _footstepAudioSource != null)
        {
            _footstepAudioSource.pitch = Random.Range(0.92f, 0.98f);
            _footstepAudioSource.PlayOneShot(clip, footstepVolume * 1.2f);
        }
    }

    // ==========================================
    // 3D MERMİ VURUŞ SESLERİ (BULLET IMPACTS)
    // ==========================================

    public void PlayImpact(Vector3 hitPoint, Vector3 normal, Collider hitCollider)
    {
        AudioClip clip = impactConcreteClip;

        if (hitCollider != null)
        {
            if (hitCollider.CompareTag("Enemy") || hitCollider.name.ToLower().Contains("enemy") || hitCollider.name.ToLower().Contains("zombie"))
            {
                clip = impactFleshClip != null ? impactFleshClip : impactConcreteClip;
            }
            else
            {
                string matName = "";
                if (hitCollider.sharedMaterial != null) matName = hitCollider.sharedMaterial.name.ToLower();
                else
                {
                    var rend = hitCollider.GetComponent<Renderer>();
                    if (rend != null && rend.sharedMaterial != null) matName = rend.sharedMaterial.name.ToLower();
                }

                if (matName.Contains("metal") || matName.Contains("iron") || matName.Contains("steel"))
                {
                    clip = impactMetalClip != null ? impactMetalClip : impactConcreteClip;
                }
                else if (matName.Contains("wood") || matName.Contains("plank"))
                {
                    clip = impactWoodClip != null ? impactWoodClip : impactConcreteClip;
                }
                else if (matName.Contains("flesh") || matName.Contains("body") || matName.Contains("meat"))
                {
                    clip = impactFleshClip != null ? impactFleshClip : impactConcreteClip;
                }
            }
        }

        if (clip != null && _impactSources != null && _impactSources.Length > 0)
        {
            var src = _impactSources[_currentImpactIndex];
            _currentImpactIndex = (_currentImpactIndex + 1) % IMPACT_POOL_SIZE;

            src.transform.position = hitPoint + normal * 0.05f;
            src.pitch = Random.Range(0.95f, 1.08f);
            src.PlayOneShot(clip, impactVolume);
        }
    }

#if UNITY_EDITOR
    /// <summary>
    /// Editörde klip slotları boşsa projedeki Assets/Game/Audio/ seslerini otomatik bağlar.
    /// </summary>
    public void AutoBindDefaultClipsIfEmpty()
    {
        if (pistolFireClip == null) pistolFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Pistol/Pistol_Fire_Suppressed.ogg");
        if (pistolSlideClip == null) pistolSlideClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Weapon_Racking_Slide.ogg");
        if (pistolDryFireClip == null) pistolDryFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Dry_Fire_Empty_Chamber.ogg");

        if (shotgunFireClip == null) shotgunFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Shotgun/Shotgun_Fire_Blast.ogg");
        if (shotgunPumpClip == null) shotgunPumpClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Shotgun/Shotgun_Pump_Mechanism.wav");
        if (shotgunInsertClip == null) shotgunInsertClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Mag_In.ogg");
        if (shotgunDryFireClip == null) shotgunDryFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Dry_Fire_Empty_Chamber.ogg");

        if (rifleFireClip == null) rifleFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Rifle/Rifle_Fire_Assault.ogg");
        if (rifleDryFireClip == null) rifleDryFireClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Dry_Fire_Empty_Chamber.ogg");

        if (turretDeployClip == null) turretDeployClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Turret/Turret_Deploy_Motor.wav");

        if (magOutClip == null) magOutClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Mag_Out.ogg");
        if (magInClip == null) magInClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Mag_In.ogg");
        if (boltCockClip == null) boltCockClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Weapon_Racking_Slide.ogg");
        if (weaponEquipClip == null) weaponEquipClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Weapon_Draw_Equip.ogg");
        if (weaponRustleClip == null) weaponRustleClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Weapons/Common/Weapon_Cloth_Rustle.wav");

        if (footstepConcreteClip == null) footstepConcreteClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Footsteps/Footstep_Concrete.ogg");
        if (footstepMetalClip == null) footstepMetalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Footsteps/Footstep_Metal.ogg");
        if (footstepWoodClip == null) footstepWoodClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Footsteps/Footstep_Wood.ogg");
        if (footstepDirtClip == null) footstepDirtClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Footsteps/Footstep_Dirt.ogg");

        if (impactConcreteClip == null) impactConcreteClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Impacts/Impact_Concrete.ogg");
        if (impactMetalClip == null) impactMetalClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Impacts/Impact_Metal.wav");
        if (impactFleshClip == null) impactFleshClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Impacts/Impact_Flesh.ogg");
        if (impactWoodClip == null) impactWoodClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Game/Audio/Impacts/Impact_Wood.ogg");
    }
#endif
}
