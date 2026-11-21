using System.Collections;
using UnityEngine;

/// <summary>
/// Silah Şarjör Yenileme (Magazine Reload) Sistemi.
/// Tabanca (Pistol_L) veya Tüfek (AK74) objelerine eklenir.
/// 
/// Tabanca için pörtleme (dışa taşma), geri sıçrama ve açı sapması tamamen çözülmüştür:
/// 1. Sol el tabancanın altına uzanır (Frame 0-14, kilitli).
/// 2. Şarjör mekanik olarak yuvadan aşağı ve sola çekilir (Frame 14-20).
/// 3. Şarjör ekran/kamera altına doğru düşerek görüş alanından çıkar (Frame 20-36).
/// 4. Kamera altında yeni şarjör ele alınır ve yuvaya doğru yükselir (Frame 36-44).
/// 5. Yuvaya giriş: X=0, Z=0 ve Rot=0 hizasında, tek bir pürüzsüz yukarı hareketle yuvaya kayar (Frame 44-50).
///    Asla geri çıkmaz, kabza duvarlarını delmez, sürgüden yukarı taşmaz.
/// 6. Şarjör yuvaya tam oturup kilitlenir (Frame 50-72).
/// </summary>
public class WeaponMagazineReload : MonoBehaviour
{
    [System.Serializable]
    public struct KeyframePoint
    {
        public float time;
        public Vector3 posOffset;
        public Vector3 rotOffset;

        public KeyframePoint(float t, Vector3 pos, Vector3 rot)
        {
            time = t;
            posOffset = pos;
            rotOffset = rot;
        }
    }

    [Header("Şarjör Referansı")]
    [Tooltip("Silahın şarjör objesi (Örn: Pistol_L_Mag). Boş bırakılırsa hiyerarşide otomatik aranır.")]
    public Transform magazine;

    [Tooltip("Bu silah bir tabanca mı? (Boşsa isimden otomatik algılanır)")]
    public bool isPistol = true;

    [Header("Mekanik Hareket Ayarları")]
    [Tooltip("Şarjörün toplam animasyon süresi (saniye). Karakter kodundan animasyon klibi süresiyle beslenir.")]
    public float reloadDuration = 2.08f;

    [Tooltip("Şarjörün aşağı düşme derinlik çarpanı (1.0 = Orijinal Blender derinliği)")]
    [Range(0.2f, 2.0f)]
    public float dropDepthMultiplier = 1.0f;

    [Tooltip("Şarjörün sola açılma çarpanı (1.0 = Orijinal Blender sola açılması)")]
    [Range(0.2f, 2.0f)]
    public float dropSpreadMultiplier = 1.0f;

    [Header("Şarjör Boyut ve Pörtlemeyi Önleme Ayarları")]
    [Tooltip("Şarjörün genişlik (X), yükseklik (Y) ve derinlik (Z) ölçek çarpanı. Kabza dışına taşmasını %100 engeller.")]
    public Vector3 magazineScaleMultiplier = new Vector3(0.88f, 0.96f, 0.90f);

    [Tooltip("Şarjör yuvaya girerken üst kısmının sürgüden/namludan taşmasını engelleyen illüzyon modu")]
    public bool enableHalfwayIllusion = true;

    [Tooltip("Şarjörün yuvaya girişinde tepe noktasının aşamayacağı tavan (0 = tam kilit noktası)")]
    public float maxInsertionY = 0f;

    [Header("Geriye Dönük Uyumluluk (Opsiyonel)")]
    [System.Obsolete("Artık kullanılmıyor.")]
    public string handBoneName = "hand_L.L";

    [System.Obsolete("Artık kullanılmıyor.")]
    public Transform handSocket;

    [System.Obsolete("Artık kullanılmıyor.")]
    public float slideOutDistance = 0.05f;

    [System.Obsolete("Artık kullanılmıyor.")]
    public float handAttachNormalized = 0.20f;

    [System.Obsolete("Artık kullanılmıyor.")]
    public float insertStartNormalized = 0.60f;

    [System.Obsolete("Artık kullanılmıyor.")]
    public float lockInNormalized = 0.77f;

    [System.Obsolete("Artık kullanılmıyor.")]
    public Vector3 handLocalPositionOffset = Vector3.zero;

    [System.Obsolete("Artık kullanılmıyor.")]
    public Vector3 handLocalRotationOffset = Vector3.zero;

    [Header("Ses Efektleri (İsteğe Bağlı)")]
    public AudioClip magOutSound;
    public AudioClip magInSound;
    public AudioSource audioSource;

    // Orijinal durum kayıtları (Silah lokalinde kalması için)
    private Transform _originParent;
    private Vector3 _originLocalPos;
    private Quaternion _originLocalRot;
    private Vector3 _originLocalScale;
    private bool _initialized = false;
    private bool _isReloading = false;
    private Coroutine _reloadRoutine;

    // Blender'dan çıkarılan çıkış ve aşağı düşüş eğrisi (Frame 0-20)
    private static readonly KeyframePoint[] PistolEjectTrack = new KeyframePoint[]
    {
        new KeyframePoint(0.0000f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 0
        new KeyframePoint(0.0139f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 1
        new KeyframePoint(0.0278f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 2
        new KeyframePoint(0.0417f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 3
        new KeyframePoint(0.0556f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 4
        new KeyframePoint(0.0694f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 5
        new KeyframePoint(0.0833f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 6
        new KeyframePoint(0.0972f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 7
        new KeyframePoint(0.1111f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 8
        new KeyframePoint(0.1250f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 9
        new KeyframePoint(0.1389f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 10
        new KeyframePoint(0.1528f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 11
        new KeyframePoint(0.1667f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 12
        new KeyframePoint(0.1806f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 13
        new KeyframePoint(0.1944f, new Vector3(0.00000f, 0.00000f, 0.00000f), new Vector3(0.00f, 0.00f, 0.00f)), // Frame 14: Şarjör serbest kalır
        new KeyframePoint(0.2083f, new Vector3(-0.00802f, -0.03309f, -0.00628f), new Vector3(-2.97f, 3.89f, 1.71f)), // Frame 15
        new KeyframePoint(0.2222f, new Vector3(-0.01222f, -0.10953f, -0.03004f), new Vector3(-4.09f, 3.02f, 1.75f)), // Frame 16
        new KeyframePoint(0.2361f, new Vector3(-0.02070f, -0.20786f, -0.05895f), new Vector3(-5.52f, 1.79f, 1.92f)), // Frame 17
        new KeyframePoint(0.2500f, new Vector3(-0.03310f, -0.30432f, -0.08732f), new Vector3(-7.01f, 0.40f, 2.36f)), // Frame 18
        new KeyframePoint(0.2639f, new Vector3(-0.04689f, -0.37624f, -0.11072f), new Vector3(-8.37f, -0.99f, 3.17f)), // Frame 19
        new KeyframePoint(0.2778f, new Vector3(-0.05327f, -0.40495f, -0.12046f), new Vector3(-9.43f, -2.25f, 4.35f)), // Frame 20: Görüş alanından çıkar
    };

    void Awake()
    {
        InitializeMagazine();
    }

    void OnEnable()
    {
        InitializeMagazine();
    }

    void OnDisable()
    {
        CancelReload();
    }

    /// <summary>
    /// Şarjörün orijinal durumunu hafızaya alır ve ölçek toleransını ayarlar.
    /// </summary>
    public void InitializeMagazine()
    {
        if (magazine == null)
        {
            foreach (Transform child in GetComponentsInChildren<Transform>(true))
            {
                if (child != transform && child.name.ToLower().Contains("mag"))
                {
                    magazine = child;
                    break;
                }
            }
        }

        if (magazine != null && !_initialized)
        {
            _originParent = magazine.parent;
            _originLocalPos = magazine.localPosition;
            _originLocalRot = magazine.localRotation;
            _originLocalScale = magazine.localScale;
            _initialized = true;

            // Otomatik tabanca algılama
            string n = (gameObject.name + " " + magazine.name).ToLower();
            if (n.Contains("pistol") || n.Contains("tabanca"))
            {
                isPistol = true;
            }

            // Başlangıçta şarjörü kabza içine pörtlemeyecek toleransla oturt
            ApplyScaleMultiplier();
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }
    }

    private void ApplyScaleMultiplier()
    {
        if (magazine != null)
        {
            magazine.localScale = Vector3.Scale(_originLocalScale, magazineScaleMultiplier);
        }
    }

    public void StartReload()
    {
        StartReload(reloadDuration);
    }

    public void StartReload(float duration)
    {
        if (magazine == null) InitializeMagazine();
        if (magazine == null) return;

        if (_reloadRoutine != null)
        {
            StopCoroutine(_reloadRoutine);
        }

        _reloadRoutine = StartCoroutine(ReloadSequence(duration > 0.05f ? duration : reloadDuration));
    }

    /// <summary>
    /// Şarjör değiştirme sekansını anında iptal eder ve şarjörü yuvaya kilitler.
    /// </summary>
    public void CancelReload()
    {
        if (_reloadRoutine != null)
        {
            StopCoroutine(_reloadRoutine);
            _reloadRoutine = null;
        }

        if (_initialized && magazine != null)
        {
            if (_originParent != null && magazine.parent != _originParent)
            {
                magazine.SetParent(_originParent, false);
            }

            magazine.localPosition = _originLocalPos;
            magazine.localRotation = _originLocalRot;
            ApplyScaleMultiplier();
        }

        _isReloading = false;
    }

    IEnumerator ReloadSequence(float duration)
    {
        _isReloading = true;

        if (_originParent != null && magazine.parent != _originParent)
        {
            magazine.SetParent(_originParent, false);
        }

        magazine.localPosition = _originLocalPos;
        magazine.localRotation = _originLocalRot;
        ApplyScaleMultiplier();

        float elapsed = 0f;
        bool playedOutSound = false;
        bool playedInSound = false;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (isPistol)
            {
                // Tabanca: Pörtlemesiz, pürüzsüz koreografi
                EvaluatePistolTrack(t, out Vector3 posOffset, out Quaternion rotOffset);

                magazine.localPosition = _originLocalPos + posOffset;
                magazine.localRotation = _originLocalRot * rotOffset;

                // Dinamik İllüzyon: Yuvaya girerken üst kısmın sürgüyü delmesini engeller
                Vector3 currentScale = Vector3.Scale(_originLocalScale, magazineScaleMultiplier);
                if (enableHalfwayIllusion && t >= 0.6100f && t < 0.6944f)
                {
                    float insertProgress = Mathf.InverseLerp(0.6100f, 0.6944f, t);
                    // Şarjörün tepe noktasını yuvaya girerken zarifçe sönümle
                    float heightFactor = Mathf.Lerp(1.0f, 0.90f, Mathf.Sin(insertProgress * Mathf.PI));
                    currentScale.y *= heightFactor;
                }
                magazine.localScale = currentScale;

                // Ses senkronizasyonu
                if (!playedOutSound && t >= 0.1944f) // Frame 14: Şarjör yuvasından ayrılır
                {
                    playedOutSound = true;
                    if (magOutSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(magOutSound);
                    }
                    else
                    {
                        WeaponAudioSystem.Instance?.PlayMagOut(1);
                    }
                }

                if (!playedInSound && t >= 0.6944f) // Frame 50: Şarjör yuvaya tam oturur
                {
                    playedInSound = true;
                    if (magInSound != null && audioSource != null)
                    {
                        audioSource.PlayOneShot(magInSound);
                    }
                    else
                    {
                        WeaponAudioSystem.Instance?.PlayMagIn(1);
                    }
                }
            }
            else
            {
                // Tüfek (AK-74 vb.): Standart mekanik şarjör akışı
                EvaluateRifleTrack(t, out Vector3 posOffset, out Quaternion rotOffset);
                magazine.localPosition = _originLocalPos + posOffset;
                magazine.localRotation = _originLocalRot * rotOffset;
                ApplyScaleMultiplier();

                if (!playedOutSound && t >= 0.20f)
                {
                    playedOutSound = true;
                    if (magOutSound != null && audioSource != null) audioSource.PlayOneShot(magOutSound);
                    else WeaponAudioSystem.Instance?.PlayMagOut(3);
                }

                if (!playedInSound && t >= 0.78f)
                {
                    playedInSound = true;
                    if (magInSound != null && audioSource != null) audioSource.PlayOneShot(magInSound);
                    else WeaponAudioSystem.Instance?.PlayMagIn(3);
                }
            }

            yield return null;
        }

        // Sekans bitti: Yuvaya kusursuz kilitlenmiş orijinal konum
        magazine.localPosition = _originLocalPos;
        magazine.localRotation = _originLocalRot;
        ApplyScaleMultiplier();

        _isReloading = false;
        _reloadRoutine = null;
    }

    private void EvaluatePistolTrack(float t, out Vector3 posOffset, out Quaternion rotOffset)
    {
        // 1. AŞAMA: Kilitli başlangıç veya kilitli bitiş (Frame 0-14 ve 50-72)
        if (t <= 0.1944f || t >= 0.6944f)
        {
            posOffset = Vector3.zero;
            rotOffset = Quaternion.identity;
            return;
        }

        // 2. AŞAMA: Şarjörün yuvadan çıkıp aşağı-sola düşüşü (Frame 14-20)
        if (t < 0.2778f)
        {
            float norm = Mathf.InverseLerp(0.1944f, 0.2778f, t);
            float frameExact = norm * 6f; // 6 alt kare
            int idxA = Mathf.Clamp(14 + Mathf.FloorToInt(frameExact), 0, PistolEjectTrack.Length - 1);
            int idxB = Mathf.Clamp(idxA + 1, 0, PistolEjectTrack.Length - 1);
            float frac = frameExact - Mathf.Floor(frameExact);

            Vector3 pA = PistolEjectTrack[idxA].posOffset;
            Vector3 pB = PistolEjectTrack[idxB].posOffset;
            Vector3 rA = PistolEjectTrack[idxA].rotOffset;
            Vector3 rB = PistolEjectTrack[idxB].rotOffset;

            pA.x *= dropSpreadMultiplier;
            pB.x *= dropSpreadMultiplier;
            pA.y *= dropDepthMultiplier;
            pB.y *= dropDepthMultiplier;

            posOffset = Vector3.Lerp(pA, pB, frac);
            rotOffset = Quaternion.Euler(Vector3.Lerp(rA, rB, frac));
            return;
        }

        // 3. AŞAMA: Ekranın altında (görüş alanı dışı) kalış ve el değişimi (Frame 20-36)
        if (t < 0.5000f)
        {
            float u = Mathf.InverseLerp(0.2778f, 0.5000f, t);
            // Ekranın altında derinde tutulur
            float y = Mathf.Lerp(-0.405f * dropDepthMultiplier, -0.220f * dropDepthMultiplier, u);
            float x = Mathf.Lerp(-0.053f * dropSpreadMultiplier, -0.020f * dropSpreadMultiplier, u);
            float z = Mathf.Lerp(-0.120f, -0.040f, u);

            posOffset = new Vector3(x, y, z);
            rotOffset = Quaternion.Euler(-8f * (1f - u), 0f, 3f * (1f - u));
            return;
        }

        // 4. AŞAMA: Yeni şarjörün alttan yuva ağzına doğru yükselişi (Frame 36-44)
        if (t < 0.6100f)
        {
            float u = Mathf.InverseLerp(0.5000f, 0.6100f, t);
            float s = Mathf.SmoothStep(0f, 1f, u);

            // Yuvanın hemen ağzına (Y = -0.075m) kadar pürüzsüz yükselir ve açısını sıfırlar
            float y = Mathf.Lerp(-0.220f * dropDepthMultiplier, -0.075f, s);
            float x = Mathf.Lerp(-0.020f * dropSpreadMultiplier, 0f, s);
            float z = Mathf.Lerp(-0.040f, 0f, s);

            posOffset = new Vector3(x, y, z);
            rotOffset = Quaternion.Euler(Mathf.Lerp(-4f, 0f, s), 0f, Mathf.Lerp(2f, 0f, s));
            return;
        }

        // 5. AŞAMA: Yuvaya doğrudan giriş (Frame 44-50)
        // DİKKAT: Burada X=0, Z=0 ve Rot=0 kesinlikle korunur!
        // Asla geri sekmez, kabzayı delip sağa sola pörtlemez, tek bir akıcı hat üzerinde yukarı kayar.
        {
            float u = Mathf.InverseLerp(0.6100f, 0.6944f, t);
            float s = Mathf.SmoothStep(0f, 1f, u);

            float y = Mathf.Lerp(-0.075f, 0f, s);
            y = Mathf.Min(y, maxInsertionY); // Tavanı asla aşamaz

            posOffset = new Vector3(0f, y, 0f);
            rotOffset = Quaternion.identity; // Sıfır rotasyon: Kabza duvarlarını delmeyi imkansız kılar
        }
    }

    private void EvaluateRifleTrack(float t, out Vector3 posOffset, out Quaternion rotOffset)
    {
        if (t <= 0.15f || t >= 0.85f)
        {
            posOffset = Vector3.zero;
            rotOffset = Quaternion.identity;
            return;
        }

        if (t < 0.35f)
        {
            float factor = Mathf.InverseLerp(0.15f, 0.35f, t);
            posOffset = new Vector3(0f, -0.25f * factor, -0.05f * factor);
            rotOffset = Quaternion.Euler(-15f * factor, 0f, 0f);
        }
        else if (t < 0.65f)
        {
            posOffset = new Vector3(0.05f, -0.35f, -0.08f);
            rotOffset = Quaternion.Euler(-20f, 0f, 0f);
        }
        else
        {
            float factor = Mathf.InverseLerp(0.65f, 0.85f, t);
            posOffset = Vector3.Lerp(new Vector3(0f, -0.20f, -0.03f), Vector3.zero, factor);
            rotOffset = Quaternion.Slerp(Quaternion.Euler(-12f, 0f, 0f), Quaternion.identity, factor);
        }
    }

    public bool IsReloading => _isReloading;
}
