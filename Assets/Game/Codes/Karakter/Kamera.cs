using UnityEngine;

/// <summary>
/// FPS Kamera Bakış Kontrolcüsü (First Person Shooter Camera Look)
/// Karakterin baş ve gövde dönüşünü mouse girişine göre pürüzsüzce yönetir.
/// Dikey bakış (Pitch) kamerada, yatay bakış (Yaw) karakter gövdesinde uygulanır.
/// Procedural kamera efektleri (Sarsıntı, Geri Tepme, Head Bob) CameraShake bileşeninde LateUpdate'te uygulanır.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Camera))]
public class FPSCamera : MonoBehaviour
{
    [Header("Fare Hassasiyeti & Kontroller")]
    [Tooltip("Farenin dönüş hızı")]
    public float fareHassasiyeti = 100f;

    [Tooltip("Dikey bakış eksenini ters çevir (Invert Y)")]
    public bool fareTersY = false;

    [Tooltip("Mikro-titremeleri ve ani FPS düşüşlerinde farenin sağa/sola fırlamasını önlemek için yumuşatma")]
    public bool fareYumusatma = true;

    [Tooltip("Yumuşatma katsayısı (yüksek değer daha hızlı tepki, 20-25 arası espor standardı akıcılık sağlar)")]
    [Range(5f, 35f)]
    public float yumusatmaHizi = 24f;

    [Header("Bakış Limitleri")]
    [Tooltip("Aşağı bakma limiti (derece)")]
    public float minDikeyAci = -90f;

    [Tooltip("Yukarı bakma limiti (derece)")]
    public float maxDikeyAci = 90f;

    [Header("Hedefler & Referanslar")]
    [Tooltip("Karakterin kendisi / gövdesi (Capsule/Player Transform)")]
    public Transform oyuncuGovdesi;

    [Header("İmleç Yönetimi")]
    [Tooltip("Başlangıçta fare imlecini pencereye kilitle ve gizle")]
    public bool otomatikImlecKilitle = true;

    [Header("Görüş Açısı (Field of View / FOV)")]
    [Tooltip("Kamera görüş açısı (Düşük değer ellerin istenmeyen kısımlarını gizler, varsayılan 60)")]
    [Range(45f, 90f)]
    public float fieldOfView = 60f;

    private Camera cam;

    // Kameranın dikey açısını (Pitch) tutan değişken
    private float xRotation = 0f;

    // Fare yumuşatma tamponları
    private Vector2 smoothMouseInput = Vector2.zero;

    public float CurrentPitch => xRotation;

    private void OnValidate()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }
        CameraShake cs = GetComponent<CameraShake>();
        if (cs != null)
        {
            cs.baseFOV = fieldOfView;
        }
    }

    private void Awake()
    {
        cam = GetComponent<Camera>();
        if (cam != null)
        {
            cam.fieldOfView = fieldOfView;
        }

        // 1. CameraShake bileşeni yoksa otomatik ekle
        if (GetComponent<CameraShake>() == null)
        {
            gameObject.AddComponent<CameraShake>();
        }

        // 2. Oyuncu gövdesi referansı atanmamışsa akıllı otomatik algılama
        if (oyuncuGovdesi == null)
        {
            if (transform.parent != null)
            {
                oyuncuGovdesi = transform.parent;
            }
            else
            {
                var player = Object.FindFirstObjectByType<Player1>();
                if (player != null)
                {
                    oyuncuGovdesi = player.transform;
                }
            }
        }
    }

    private void Start()
    {
        if (otomatikImlecKilitle)
        {
            LockCursor();
        }

        // Mevcut dikey açıyı oku ve sınırla
        xRotation = transform.localEulerAngles.x;
        if (xRotation > 180f) xRotation -= 360f;
        xRotation = Mathf.Clamp(xRotation, minDikeyAci, maxDikeyAci);
    }

    private void Update()
    {
        HandleCursorLockInput();
        HandleLookInput();
    }

    /// <summary>
    /// ESC ile imleç serbest bırakma, tıklama ile yeniden kilitleme
    /// </summary>
    private void HandleCursorLockInput()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            UnlockCursor();
        }
        else if (Input.GetMouseButtonDown(0) && Cursor.lockState != CursorLockMode.Locked)
        {
            LockCursor();
        }
    }

    /// <summary>
    /// Fare girişlerini oku, sınırla ve kamera/gövde rotasyonlarını uygula
    /// </summary>
    private void HandleLookInput()
    {
        // İmleç kilitli değilse kamera dönmesin (UI veya menü etkileşimi için)
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float rawMouseX = Input.GetAxisRaw("Mouse X");
        float rawMouseY = Input.GetAxisRaw("Mouse Y");

        Vector2 targetInput = new Vector2(rawMouseX, rawMouseY);

        // Frame drop (ani FPS düşüşü) veya donmalarda deltaTime'ın fırlayıp
        // fareyi aniden 180 derece sağa savurmasını önlemek için deltaTime sınırlandırılır (max 30fps/0.033s eşdeğeri)
        float dt = Mathf.Clamp(Time.deltaTime, 0.0001f, 0.033f);

        if (fareYumusatma)
        {
            float lerpFactor = 1f - Mathf.Exp(-yumusatmaHizi * dt);
            smoothMouseInput = Vector2.Lerp(smoothMouseInput, targetInput, lerpFactor);
        }
        else
        {
            smoothMouseInput = targetInput;
        }

        float fareX = smoothMouseInput.x * fareHassasiyeti * dt;
        float fareY = smoothMouseInput.y * fareHassasiyeti * dt * (fareTersY ? -1f : 1f);

        // Tek bir karede fizik/lag sıçraması olsa dahi kameranın fırlamasını engelleyen sert güvenlik limiti
        fareX = Mathf.Clamp(fareX, -22f, 22f);
        fareY = Mathf.Clamp(fareY, -22f, 22f);

        // 1. Dikey Bakış (Pitch): Sadece Kamera döner
        xRotation -= fareY;
        xRotation = Mathf.Clamp(xRotation, minDikeyAci, maxDikeyAci);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);

        // 2. Yatay Bakış (Yaw): Karakterin GÖVDESİ döner
        if (oyuncuGovdesi != null)
        {
            oyuncuGovdesi.Rotate(Vector3.up * fareX);
        }
    }

    /// <summary>
    /// Nişangahın yukarı şahlanması (Kalıcı recoil tırmanışı için yardımcı)
    /// </summary>
    public void AddPitchClimb(float climbAmount)
    {
        xRotation -= climbAmount;
        xRotation = Mathf.Clamp(xRotation, minDikeyAci, maxDikeyAci);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
    }

    public void LockCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void UnlockCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}
