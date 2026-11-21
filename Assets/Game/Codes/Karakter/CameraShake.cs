using UnityEngine;

/// <summary>
/// Call of Duty Tarzı Geri Tepme ve Sarsıntı Şablonu (Gun Recoil & Camera Shake Profile)
/// Her silah için açısal (Pitch/Yaw/Roll), konumsal (X/Y/Z), yay dinamikleri (Snappiness/Return),
/// titreşim (Trauma) ve FOV patlama şoku parametrelerini Inspector'dan canlı ayarlamayı sağlar.
/// </summary>
[System.Serializable]
public class GunRecoilProfile
{
    [Header("Açısal Geri Tepme (Rotational Recoil - Derece)")]
    [Tooltip("Yukarı namlu şahlanması (Pitch: Pozitif = Yukarı şahlanma)")]
    public float pitchKick = 2.4f;

    [Tooltip("Yatay namlu sapması aralığı (Yaw: X = En çok sola sapma, Y = En çok sağa sapma)")]
    public Vector2 yawKickRange = new Vector2(-0.5f, 0.5f);

    [Tooltip("Bileğin/namlunun yana burkulma açısı (Roll tilt: Pozitif = Sağa, Negatif = Sola yatış)")]
    public float rollKick = 0.8f;

    [Header("Konumsal Geri Tepme (Positional Recoil - Metre)")]
    [Tooltip("Omuza vuran geriye tepme mesafesi (Z ekseni: Pozitif = Geriye itiş)")]
    public float backwardKickZ = 0.03f;

    [Tooltip("Dikey sekme mesafesi (Y ekseni: Pozitif = Yukarı sıçrama)")]
    public float verticalKickY = 0.004f;

    [Tooltip("Yanal kayma mesafesi (X ekseni: Sağa/Sola hafif deplasman)")]
    public float horizontalKickX = 0.002f;

    [Header("Call of Duty Yay Dinamikleri (Spring Physics)")]
    [Tooltip("Geri tepmenin tepe noktasına fırlama hızı (Snappiness: Yüksek değer = Çok sert, ani ve tok vuruş)")]
    [Range(10f, 80f)]
    public float snappiness = 42f;

    [Tooltip("Silahın tekrar nişan noktasına toparlanma hızı (Return Speed: Yüksek değer = Hızlı toparlanma)")]
    [Range(5f, 50f)]
    public float returnSpeed = 22f;

    [Header("Titreşim & Hava Basıncı (Trauma & FOV Concussion)")]
    [Tooltip("Namlu patlamasının Perlin gürültüsü titreşimi (0..1)")]
    [Range(0f, 1f)]
    public float traumaAmount = 0.2f;

    [Tooltip("Patlama anındaki anlık FOV hava şoku (Negatif = Namlu patlamasıyla ani zoom şoku)")]
    public float concussiveFOVKick = -0.5f;

    public GunRecoilProfile() { }

    public GunRecoilProfile(float pitch, Vector2 yaw, float roll, float kickZ, float kickY, float trauma, float fovKick, float snap, float ret)
    {
        pitchKick = pitch;
        yawKickRange = yaw;
        rollKick = roll;
        backwardKickZ = kickZ;
        verticalKickY = kickY;
        horizontalKickX = 0.002f;
        traumaAmount = trauma;
        concussiveFOVKick = fovKick;
        snappiness = snap;
        returnSpeed = ret;
    }
}

/// <summary>
/// FPS Kamera Efektleri ve Sarsıntı Yöneticisi (Camera Shake & Screen FX)
/// Perlin noise trauma sarsıntısı, Call of Duty tarzı yaylı geri tepme (Snappy kick & smooth return),
/// yürüyüş/koşma kafa sallantısı (Lissajous Head Bobbing), dinamik FOV ve hasar görsel efektlerini
/// LateUpdate'te pürüzsüzce kameraya katmanlar halinde uygular.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    [Header("=== CALL OF DUTY SİLAH GERİ TEPME ŞABLONLARI (PRESETS) ===")]
    [Tooltip("Tabanca (Pistol) için geri tepme, açı ve yay ayarları")]
    public GunRecoilProfile tabancaProfili = new GunRecoilProfile(
        pitch: 2.4f,
        yaw: new Vector2(-0.4f, 0.4f),
        roll: 0.9f,
        kickZ: 0.028f,
        kickY: 0.003f,
        trauma: 0.18f,
        fovKick: -0.4f,
        snap: 45f,
        ret: 26f
    );

    [Tooltip("Pompalı (Shotgun) için tok, sert, omuza vuran ağır geri tepme ve hava şoku")]
    public GunRecoilProfile pompaliProfili = new GunRecoilProfile(
        pitch: 7.2f,
        yaw: new Vector2(-1.5f, 1.5f),
        roll: 2.2f,
        kickZ: 0.095f,
        kickY: 0.014f,
        trauma: 0.65f,
        fovKick: -2.2f,
        snap: 48f,
        ret: 18f
    );

    [Tooltip("Otomatik Tüfek / AK-47 (Rifle) için ritmik, seri tırmanış ve yaylanma ayarları")]
    public GunRecoilProfile tufekProfili = new GunRecoilProfile(
        pitch: 1.55f,
        yaw: new Vector2(-0.65f, 0.65f),
        roll: 0.5f,
        kickZ: 0.022f,
        kickY: 0.002f,
        trauma: 0.15f,
        fovKick: -0.3f,
        snap: 40f,
        ret: 22f
    );

    [Tooltip("Hasar alındığında yönlü / sarsıcı flinch darbesi ayarları")]
    public GunRecoilProfile hasarProfili = new GunRecoilProfile(
        pitch: 4.0f,
        yaw: new Vector2(-3.0f, 3.0f),
        roll: 4.0f,
        kickZ: 0.040f,
        kickY: 0.010f,
        trauma: 0.45f,
        fovKick: 0.0f,
        snap: 38f,
        ret: 16f
    );

    [Header("=== GENEL TİTREŞİM AYARLARI (PERLIN NOISE TRAUMA) ===")]
    [Tooltip("Titreşimin sönümlenme hızı")]
    [Range(1f, 10f)]
    public float traumaDecay = 3.5f;

    [Tooltip("Perlin gürültüsünün frekansı (Titreşim hızı)")]
    public float traumaFrequency = 24f;

    [Tooltip("Trauma non-linear üstel gücü (2 = karesi, COD tarzı organik his)")]
    public float traumaPower = 2f;

    [Tooltip("Maksimum açısal sapma limiti (Derece: Pitch, Yaw, Roll)")]
    public Vector3 maxAngularOffset = new Vector3(8f, 5f, 4f);

    [Tooltip("Maksimum konumsal sapma limiti (Metre: X, Y, Z)")]
    public Vector3 maxPositionalOffset = new Vector3(0.035f, 0.035f, 0.06f);

    [Header("=== KAFA SALLANTISI (HEAD BOBBING) ===")]
    [Tooltip("Yürürken ve koşarken kafa sallantısını aktif et")]
    public bool enableHeadBob = true;

    [Tooltip("Yürüyüş adım frekansı")]
    public float walkBobSpeed = 9.5f;

    [Tooltip("Koşma adım frekansı")]
    public float sprintBobSpeed = 14f;

    [Tooltip("Yürüyüş sallantı genliği (X: yatay salınım, Y: dikey çöküş)")]
    public Vector3 walkBobAmount = new Vector3(0.012f, 0.016f, 0f);

    [Tooltip("Koşma sallantı genliği (X: yatay salınım, Y: dikey çöküş)")]
    public Vector3 sprintBobAmount = new Vector3(0.022f, 0.035f, 0f);

    [Tooltip("Yürürken sağ/sol adıma göre kafa yatış açısı (Roll tilt)")]
    public float walkTiltRoll = 0.45f;

    [Tooltip("Koşarken sağ/sol adıma göre kafa yatış açısı (Roll tilt)")]
    public float sprintTiltRoll = 1.1f;

    [Tooltip("Kafa sallantısının dururken sönümlenme hızı")]
    public float bobDamping = 12f;

    [Header("=== DİNAMİK FOV & EKRAN EFEKTLERİ ===")]
    [Tooltip("Koşarken ve ateş ederken dinamik FOV değişimini aktif et")]
    public bool enableDynamicFOV = true;

    [Tooltip("Temel görüş açısı (Base FOV - 60 derece ellerin arkasındaki boşluğu gizler)")]
    [Range(45f, 90f)]
    public float baseFOV = 60f;

    [Tooltip("Koşarken FOV genişleme miktarı (derece)")]
    public float sprintFOVOffset = 6.0f;

    [Tooltip("FOV geçiş yumuşatma hızı")]
    public float fovTransitionSpeed = 10f;

    [Tooltip("Hasar alındığında ekran kırmızı parlamasını aktif et")]
    public bool enableDamageFlash = true;

    [Tooltip("Hasar ekran parlaması rengi")]
    public Color damageFlashColor = new Color(0.85f, 0.04f, 0.04f, 0.45f);

    [Tooltip("Hasar parlamasının sönümlenme hızı")]
    public float flashFadeSpeed = 3.2f;

    [Tooltip("Can kritik seviyeye indiğinde kenarlarda nabız gibi atan vinyet")]
    public bool enableLowHealthVignette = true;

    [Tooltip("Düşük can eşiği (0..1)")]
    [Range(0.1f, 0.5f)]
    public float lowHealthThreshold = 0.30f;

    [Tooltip("Düşük can vinyet rengi")]
    public Color lowHealthColor = new Color(0.75f, 0.02f, 0.02f, 0.38f);

    [Tooltip("Düşük can nabız atış hızı")]
    public float heartbeatSpeed = 5.0f;

    // --- Dahili Durum Değişkenleri ---
    private float trauma = 0f;

    // Yaylı açısal geri tepme (Pitch, Yaw, Roll)
    private Vector3 currentRecoilRot = Vector3.zero;
    private Vector3 targetRecoilRot = Vector3.zero;
    private float activeSnappiness = 42f;
    private float activeReturnSpeed = 22f;

    // Yaylı konumsal geri tepme (X, Y, Z)
    private Vector3 currentRecoilPos = Vector3.zero;
    private Vector3 targetRecoilPos = Vector3.zero;

    // Kafa sallantısı durumu
    private float bobTimer = 0f;
    private Vector3 currentBobPos = Vector3.zero;
    private Vector3 currentBobRot = Vector3.zero;

    // Dinamik FOV
    private Camera targetCamera;
    private float targetConcussiveFOV = 0f;
    private float currentConcussiveFOV = 0f;

    // Screen FX & Vinyet
    private float currentFlashAlpha = 0f;
    private Texture2D vignetteTexture;

    // Karakter referansları
    private Player1 cachedPlayer;
    private Rigidbody cachedRigidbody;

    private Vector3 initialLocalPos;
    private float seed;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if (Instance != this)
        {
            Destroy(this);
            return;
        }

        targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            FPSCamera fpsCam = GetComponent<FPSCamera>();
            if (fpsCam != null)
            {
                baseFOV = fpsCam.fieldOfView;
            }
            targetCamera.fieldOfView = baseFOV;
        }

        initialLocalPos = transform.localPosition;
        seed = Random.value * 1000f;

        cachedPlayer = GetComponentInParent<Player1>();
        cachedRigidbody = GetComponentInParent<Rigidbody>();

        GenerateVignetteTexture();
    }

    private void OnValidate()
    {
        if (targetCamera == null) targetCamera = GetComponent<Camera>();
        if (targetCamera != null)
        {
            targetCamera.fieldOfView = baseFOV;
        }
        FPSCamera fpsCam = GetComponent<FPSCamera>();
        if (fpsCam != null)
        {
            fpsCam.fieldOfView = baseFOV;
        }
    }

    private void OnDestroy()
    {
        if (vignetteTexture != null)
        {
            Destroy(vignetteTexture);
        }
    }

    /// <summary>
    /// Harici asset bağımlılığı olmadan kodla oluşturulan sinematik vinyet dokusu
    /// </summary>
    private void GenerateVignetteTexture()
    {
        const int size = 64;
        vignetteTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        vignetteTexture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float u = (x / (float)(size - 1)) * 2f - 1f;
                float v = (y / (float)(size - 1)) * 2f - 1f;
                float dist = Mathf.Clamp01(Mathf.Sqrt(u * u + v * v));
                float alpha = Mathf.SmoothStep(0.25f, 1.0f, dist);
                vignetteTexture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        vignetteTexture.Apply();
    }

    private void LateUpdate()
    {
        float dt = Time.deltaTime;

        // 1. Trauma sönümlenmesi
        if (trauma > 0f)
        {
            trauma = Mathf.Clamp01(trauma - traumaDecay * dt);
        }

        // 2. Yaylı geri tepme toparlanması (Call of Duty damped spring physics)
        targetRecoilRot = Vector3.MoveTowards(targetRecoilRot, Vector3.zero, activeReturnSpeed * dt);
        float rotLerp = 1f - Mathf.Exp(-activeSnappiness * dt);
        currentRecoilRot = Vector3.Lerp(currentRecoilRot, targetRecoilRot, rotLerp);

        targetRecoilPos = Vector3.MoveTowards(targetRecoilPos, Vector3.zero, (activeReturnSpeed * 0.85f) * dt);
        float posLerp = 1f - Mathf.Exp(-(activeSnappiness * 0.9f) * dt);
        currentRecoilPos = Vector3.Lerp(currentRecoilPos, targetRecoilPos, posLerp);

        // 3. Head Bobbing hesaplaması
        UpdateHeadBob(dt);

        // 4. Perlin Noise Titreşimi
        Vector3 shakeRotOffset = Vector3.zero;
        Vector3 shakePosOffset = Vector3.zero;

        float shakeFactor = Mathf.Pow(trauma, traumaPower);
        if (shakeFactor > 0.0001f)
        {
            float time = Time.time * traumaFrequency;

            shakePosOffset.x = maxPositionalOffset.x * shakeFactor * (Mathf.PerlinNoise(seed + 1.2f, time) * 2f - 1f);
            shakePosOffset.y = maxPositionalOffset.y * shakeFactor * (Mathf.PerlinNoise(seed + 2.4f, time) * 2f - 1f);
            shakePosOffset.z = maxPositionalOffset.z * shakeFactor * (Mathf.PerlinNoise(seed + 3.6f, time) * 2f - 1f);

            shakeRotOffset.x = maxAngularOffset.x * shakeFactor * (Mathf.PerlinNoise(seed + 4.8f, time) * 2f - 1f);
            shakeRotOffset.y = maxAngularOffset.y * shakeFactor * (Mathf.PerlinNoise(seed + 6.0f, time) * 2f - 1f);
            shakeRotOffset.z = maxAngularOffset.z * shakeFactor * (Mathf.PerlinNoise(seed + 7.2f, time) * 2f - 1f);
        }

        // 5. Kameraya yerel rotasyon ve pozisyonu birleştirip uygula
        Vector3 totalPosOffset = currentBobPos + currentRecoilPos + shakePosOffset;
        Vector3 totalRotOffset = currentBobRot + currentRecoilRot + shakeRotOffset;

        transform.localPosition = initialLocalPos + totalPosOffset;
        transform.localRotation = transform.localRotation * Quaternion.Euler(totalRotOffset);

        // 6. Dinamik FOV güncellemesi
        UpdateDynamicFOV(dt);

        // 7. Hasar parlaması sönümlenmesi
        if (currentFlashAlpha > 0f)
        {
            currentFlashAlpha = Mathf.MoveTowards(currentFlashAlpha, 0f, flashFadeSpeed * dt);
        }
    }

    /// <summary>
    /// Lissajous eğrisi ile organik adımlama kafa sallantısı
    /// </summary>
    private void UpdateHeadBob(float dt)
    {
        if (!enableHeadBob)
        {
            currentBobPos = Vector3.Lerp(currentBobPos, Vector3.zero, 1f - Mathf.Exp(-bobDamping * dt));
            currentBobRot = Vector3.Lerp(currentBobRot, Vector3.zero, 1f - Mathf.Exp(-bobDamping * dt));
            return;
        }

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool hasInput = (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f);

        bool isMoving = hasInput;
        if (cachedRigidbody != null)
        {
            Vector3 horizVel = new Vector3(cachedRigidbody.linearVelocity.x, 0f, cachedRigidbody.linearVelocity.z);
            if (horizVel.sqrMagnitude > 0.04f)
                isMoving = true;
        }

        bool isSprinting = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        Vector3 targetBobPos = Vector3.zero;
        Vector3 targetBobRot = Vector3.zero;

        if (isMoving)
        {
            float bobSpeed = isSprinting ? sprintBobSpeed : walkBobSpeed;
            Vector3 bobAmp = isSprinting ? sprintBobAmount : walkBobAmount;
            float tiltAmp = isSprinting ? sprintTiltRoll : walkTiltRoll;

            bobTimer += dt * bobSpeed;

            targetBobPos.x = Mathf.Sin(bobTimer * 0.5f) * bobAmp.x;
            targetBobPos.y = (Mathf.Cos(bobTimer) - 1f) * 0.5f * bobAmp.y;
            targetBobRot.z = -Mathf.Sin(bobTimer * 0.5f) * tiltAmp;
        }
        else
        {
            bobTimer = Mathf.MoveTowards(bobTimer, 0f, dt * 5f);
        }

        float blend = 1f - Mathf.Exp(-bobDamping * dt);
        currentBobPos = Vector3.Lerp(currentBobPos, targetBobPos, blend);
        currentBobRot = Vector3.Lerp(currentBobRot, targetBobRot, blend);
    }

    /// <summary>
    /// Koşma ve ateş etme anlarında dinamik FOV değişimi
    /// </summary>
    private void UpdateDynamicFOV(float dt)
    {
        if (!enableDynamicFOV || targetCamera == null)
            return;

        targetConcussiveFOV = Mathf.MoveTowards(targetConcussiveFOV, 0f, 15f * dt);
        currentConcussiveFOV = Mathf.Lerp(currentConcussiveFOV, targetConcussiveFOV, 1f - Mathf.Exp(-30f * dt));

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        bool isMoving = (Mathf.Abs(h) > 0.05f || Mathf.Abs(v) > 0.05f);
        bool isSprinting = isMoving && (Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift));

        FPSCamera fpsCam = GetComponent<FPSCamera>();
        float effectiveBaseFOV = (fpsCam != null) ? fpsCam.fieldOfView : baseFOV;

        float targetFOV = effectiveBaseFOV + (isSprinting ? sprintFOVOffset : 0f) + currentConcussiveFOV;
        targetCamera.fieldOfView = Mathf.Lerp(targetCamera.fieldOfView, targetFOV, 1f - Mathf.Exp(-fovTransitionSpeed * dt));
    }

    /// <summary>
    /// Hasar anında ekran flaşı ve düşük can vinyetini çizer
    /// </summary>
    private void OnGUI()
    {
        if (Event.current.type != EventType.Repaint)
            return;

        Rect screenRect = new Rect(0, 0, Screen.width, Screen.height);

        // 1. Düşük Can Nabız Vinyeti
        if (enableLowHealthVignette && vignetteTexture != null && cachedPlayer != null && cachedPlayer.Max_Health > 0f)
        {
            float healthRatio = cachedPlayer.Current_Health / cachedPlayer.Max_Health;
            if (healthRatio <= lowHealthThreshold && healthRatio > 0f)
            {
                float dangerFactor = 1f - (healthRatio / lowHealthThreshold);
                float pulse = 0.55f + 0.45f * Mathf.Sin(Time.time * heartbeatSpeed);
                float vignetteAlpha = dangerFactor * pulse * lowHealthColor.a;

                Color c = lowHealthColor;
                c.a = vignetteAlpha;
                GUI.color = c;
                GUI.DrawTexture(screenRect, vignetteTexture, ScaleMode.StretchToFill);
            }
        }

        // 2. Hasar Ekran Flaşı
        if (enableDamageFlash && currentFlashAlpha > 0.005f)
        {
            Color c = damageFlashColor;
            c.a = currentFlashAlpha * damageFlashColor.a;
            GUI.color = c;

            if (vignetteTexture != null)
            {
                GUI.DrawTexture(screenRect, vignetteTexture, ScaleMode.StretchToFill);
            }
            else
            {
                GUI.DrawTexture(screenRect, Texture2D.whiteTexture, ScaleMode.StretchToFill);
            }
        }
    }

    // =========================================================================
    // MODÜLER PROFİL UYGULAMA VE SİLAH METOTLARI
    // =========================================================================

    /// <summary>
    /// Bir GunRecoilProfile şablonundaki tüm değerleri yay dinamikleriyle kameraya uygular
    /// </summary>
    public void ApplyRecoilProfile(GunRecoilProfile profile, bool isLeftHand = false, float intensityMultiplier = 1f)
    {
        if (profile == null) return;

        // Yay hızlarını profilden çek
        activeSnappiness = profile.snappiness;
        activeReturnSpeed = profile.returnSpeed;

        // Titreşim (Trauma)
        AddTrauma(profile.traumaAmount * intensityMultiplier);

        // Açısal sapmalar (Pitch, Yaw, Roll)
        float pitch = -profile.pitchKick * intensityMultiplier;
        float yaw = Random.Range(profile.yawKickRange.x, profile.yawKickRange.y) * intensityMultiplier;
        float roll = (isLeftHand ? profile.rollKick : -profile.rollKick) * intensityMultiplier;

        targetRecoilRot += new Vector3(pitch, yaw, roll);

        // Konumsal sapmalar (X, Y, Z omuza çarpma)
        float kickZ = profile.backwardKickZ * intensityMultiplier;
        float kickY = profile.verticalKickY * intensityMultiplier;
        float kickX = Random.Range(-profile.horizontalKickX, profile.horizontalKickX) * intensityMultiplier;

        targetRecoilPos += new Vector3(kickX, kickY, kickZ);

        // FOV hava basıncı şoku
        targetConcussiveFOV += profile.concussiveFOVKick * intensityMultiplier;
    }

    /// <summary>
    /// Genel sarsıntı ekleme metodu (0..1)
    /// </summary>
    public void AddTrauma(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    /// <summary>
    /// Tabanca (Pistol): Inspector'daki tabancaProfili değerlerini uygular
    /// </summary>
    public void ShakePistol(bool isLeftHand = false)
    {
        ApplyRecoilProfile(tabancaProfili, isLeftHand);
    }

    /// <summary>
    /// Pompalı (Shotgun): Inspector'daki pompaliProfili değerlerini uygular
    /// </summary>
    public void ShakeShotgun(bool isLeftHand = false)
    {
        ApplyRecoilProfile(pompaliProfili, isLeftHand);
    }

    /// <summary>
    /// Otomatik Tüfek / AK-47: Inspector'daki tufekProfili değerlerini uygular
    /// </summary>
    public void ShakeRifle(bool isLeftHand = false)
    {
        ApplyRecoilProfile(tufekProfili, isLeftHand);
    }

    /// <summary>
    /// Hasar alındığında: Inspector'daki hasarProfili değerlerini ve yönlü flinch uygular
    /// </summary>
    public void ShakeDamage(float severity = 1f, Vector3 hitDirection = default)
    {
        severity = Mathf.Clamp(severity, 0.25f, 3f);
        ApplyRecoilProfile(hasarProfili, false, severity);

        // Eğer hasar yönü verilmişse yönlü flinch ekle
        if (hitDirection != default && hitDirection != Vector3.zero)
        {
            Vector3 localHit = transform.InverseTransformDirection(hitDirection.normalized);
            targetRecoilRot += new Vector3(-localHit.z * 3.5f, -localHit.x * 2.5f, localHit.x * 4.5f) * severity;
        }

        // Ekran flaşı tetikle
        if (enableDamageFlash)
        {
            currentFlashAlpha = Mathf.Clamp01(currentFlashAlpha + 0.65f * Mathf.Clamp01(severity));
        }
    }

    /// <summary>
    /// Geriye dönük uyumluluk için eski ShakeCustom metodu
    /// </summary>
    public void ShakeCustom(float traumaAmount, float pitchKick = 0f, float kickZ = 0f)
    {
        AddTrauma(traumaAmount);
        targetRecoilRot += new Vector3(-pitchKick, 0f, 0f);
        targetRecoilPos += new Vector3(0f, 0f, kickZ);
    }
}

