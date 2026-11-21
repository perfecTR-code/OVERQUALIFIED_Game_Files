using System.Collections;
using UnityEngine;

/// <summary>
/// Tabanca Sürgüsü (Slide / BarrelGuard) Animasyonu
/// Bu script silahın üst sürgü objesine (Pistol_L_BarrelGuard) eklenir.
/// Ateş edildiğinde sürgü geriye doğru fırlar ve hızla başlangıç yerine geri döner.
/// </summary>
public class PistolSlide : MonoBehaviour
{
    [Header("Sürgü Geri Gitme Ayarları")]
    [Tooltip("Sürgünün lokal olarak kaç birim geri gideceği (Z ekseni)")]
    public float slideBackDistance = 0.035f;

    [Tooltip("Sürgünün toplam geri gidip gelme süresi (saniye)")]
    public float slideDuration = 0.07f;

    [Header("Ekstra Parça (Sight / Gez - İsteğe bağlı)")]
    [Tooltip("Sürgüyle birlikte geri kayması gereken parça (ör: Pistol_L_Sight)")]
    public Transform extraSlidePart;

    [Header("Mermi Kovanı Fırlatma (İsteğe bağlı)")]
    public GameObject shellCasePrefab;
    public Transform shellEjectPoint;
    public float shellEjectForce = 3f;
    public float shellEjectTorque = 5f;

    private Vector3 _originLocalPos;
    private Vector3 _extraOriginLocalPos;
    private bool _isAnimating = false;
    public bool isAnimating => _isAnimating;
    private bool _initialized = false;

    void Awake()
    {
        InitializePositions();
    }

    void OnEnable()
    {
        InitializePositions();
    }

    private void InitializePositions()
    {
        if (!_initialized || transform.localPosition != Vector3.zero)
        {
            _originLocalPos = transform.localPosition;
            if (extraSlidePart != null)
            {
                _extraOriginLocalPos = extraSlidePart.localPosition;
            }
            _initialized = true;
        }
    }

    /// <summary>
    /// Karakter.cs ateş ettiğinde çağrılır: slideScript.Fire();
    /// </summary>
    public void Fire()
    {
        InitializePositions();

        // Önceki rutin devam ediyorsa baştan başlat
        StopAllCoroutines();
        StartCoroutine(SlideRoutine());

        EjectShell();
    }

    IEnumerator SlideRoutine()
    {
        _isAnimating = true;
        float half = Mathf.Max(0.01f, slideDuration * 0.5f);

        // Geriye doğru hareket (Tabancanın namlusu +Z, arkası -Z)
        Vector3 backPos = _originLocalPos - new Vector3(0f, 0f, slideBackDistance);
        Vector3 extraBackPos = extraSlidePart != null ? (_extraOriginLocalPos - new Vector3(0f, 0f, slideBackDistance)) : Vector3.zero;

        // 1. Sürgü Geriye fırlar
        float t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float factor = t / half;
            transform.localPosition = Vector3.Lerp(_originLocalPos, backPos, factor);
            if (extraSlidePart != null)
            {
                extraSlidePart.localPosition = Vector3.Lerp(_extraOriginLocalPos, extraBackPos, factor);
            }
            yield return null;
        }

        transform.localPosition = backPos;
        if (extraSlidePart != null) extraSlidePart.localPosition = extraBackPos;

        // 2. Sürgü Hızla İleriye kapanır
        t = 0f;
        while (t < half)
        {
            t += Time.deltaTime;
            float factor = t / half;
            transform.localPosition = Vector3.Lerp(backPos, _originLocalPos, factor);
            if (extraSlidePart != null)
            {
                extraSlidePart.localPosition = Vector3.Lerp(extraBackPos, _extraOriginLocalPos, factor);
            }
            yield return null;
        }

        transform.localPosition = _originLocalPos;
        if (extraSlidePart != null) extraSlidePart.localPosition = _extraOriginLocalPos;

        _isAnimating = false;
    }

    void EjectShell()
    {
        if (shellCasePrefab == null || shellEjectPoint == null) return;

        GameObject shell = Instantiate(shellCasePrefab, shellEjectPoint.position, shellEjectPoint.rotation);
        if (shell.TryGetComponent<Rigidbody>(out var rb))
        {
            Vector3 ejectDir = shellEjectPoint.right + shellEjectPoint.up * 0.5f;
            ejectDir += Random.insideUnitSphere * 0.2f;
            rb.AddForce(ejectDir.normalized * shellEjectForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * shellEjectTorque, ForceMode.Impulse);
        }

        Destroy(shell, 3f);
    }
}
