using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Animations;

public enum PlayerMovementState
{
    Idle = 0, // Hareketsiz duruş
    Walk = 1, // WASD ile yürüme / silah hazır (Double_Hand_Pistol_Sprint)
    Run = 2   // Shift + WASD ile hızlı koşu / depar (Double_Hand_Pistol_Run)
}

/// <summary>
/// Kod Tabanlı Silah ve Kol Animasyon Sistemi (Pure C# Playables API)
/// Mecanim AnimatorController'ın tüm kısıtlamalarını, gecikmelerini ve hatalarını aşar.
/// 
/// Özellikler:
/// 1. AnimatorController gerektirmez, doğrudan AnimationClip oynatır ve pürüzsüz harmanlar (Mixer).
/// 2. WASD basıldığı an Idle'ı ANINDA keser (sıfır gecikmeli Call of Duty tarzı geçiş).
/// 3. Ateş (Fire), Doldurma (Reload) ve Çekme (Equip) animasyonlarını anında tetikler ve kesebilir.
/// 4. Pompalı (Shotgun) için 3 aşamalı (Preparation -> Tek tek fişek doldurma loop -> Pompalama) sekansı destekler.
/// 5. Inspector veya kod üzerinden tüm klipleri otomatik bağlar.
/// </summary>
public class WeaponAnimationSystem : MonoBehaviour
{
    public static WeaponAnimationSystem Instance { get; private set; }

    [Header("=== HEDEF ANIMATOR ===")]
    [Tooltip("Kol modelinin üzerindeki Animator bileşeni (HandWithGloves)")]
    public Animator targetAnimator;

    [Header("=== TABANCA (PISTOL) KLİPLERİ ===")]
    public AnimationClip pistolIdle;
    [Tooltip("WASD basıldığında oynatılacak yürüme/sprint animasyonu")]
    public AnimationClip pistolWalk;
    [Tooltip("Shift + WASD basıldığında oynatılacak hızlı koşu/depar animasyonu")]
    public AnimationClip pistolRun;
    public AnimationClip pistolFire;
    public AnimationClip pistolReload;
    public AnimationClip pistolEquip;

    [Header("=== POMPALI (SHOTGUN) KLİPLERİ ===")]
    public AnimationClip shotgunIdle;
    public AnimationClip shotgunRun;
    public AnimationClip shotgunFire;
    public AnimationClip shotgunEquip;
    public AnimationClip shotgunPrepBefore;
    public AnimationClip shotgunReloadInsert;
    public AnimationClip shotgunPrepAfter;

    [Header("=== TÜFEK (RIFLE / AK-74) KLİPLERİ ===")]
    public AnimationClip rifleIdle;
    public AnimationClip rifleRun;
    public AnimationClip rifleFire;
    public AnimationClip rifleReload;
    public AnimationClip rifleEquip;

    [Header("=== GEÇİŞ VE YUMUŞATMA (SMOOTH CROSSFADE) ===")]
    [Tooltip("Hareketsizlik (Idle) duruşuna geçiş yumuşatma süresi (saniye)")]
    public float idleTransitionDuration = 0.18f;

    [Tooltip("Yürüme/Sprint (Walk) hazır pozisyonuna geçiş yumuşatma süresi (saniye)")]
    public float walkTransitionDuration = 0.18f;

    [Tooltip("Tabanca (Pistol) hızlı koşu hazırlık/geçiş süresi (saniye) - Hızlı ve çevik geçiş")]
    public float pistolRunTransitionDuration = 0.12f;

    [Tooltip("Hızlı Koşu (Run) depar pozisyonuna genel geçiş süresi (saniye)")]
    public float runTransitionDuration = 0.14f;

    [Tooltip("Ateş animasyonuna giriş yumuşatma süresi (saniye)")]
    public float fireFadeInDuration = 0.04f;

    [Tooltip("Ateş sonrası normale dönüş süresi (saniye)")]
    public float fireFadeOutDuration = 0.10f;

    [Tooltip("Şarjör değiştirme (Reload) giriş yumuşatma süresi (saniye)")]
    public float reloadFadeInDuration = 0.08f;

    [Tooltip("Şarjör değiştirme (Reload) çıkış yumuşatma süresi (saniye)")]
    public float reloadFadeOutDuration = 0.12f;

    [Tooltip("Silah çekme (Equip) giriş yumuşatma süresi (saniye)")]
    public float equipFadeInDuration = 0.06f;

    [Tooltip("Silah çekme (Equip) çıkış yumuşatma süresi (saniye)")]
    public float equipFadeOutDuration = 0.08f;

    [Tooltip("Tabanca animasyon hız çarpanı")]
    public float pistolAnimSpeed = 1.0f;

    [Tooltip("Tabanca şarjör doldurma (Reload) animasyon hız çarpanı (Örn: 0.28 = ~2.08 sn, 0.30 = ~1.94 sn)")]
    public float pistolReloadAnimSpeed = 0.28f;

    // Playable Graph Altyapısı
    private PlayableGraph _graph;
    private AnimationMixerPlayable _rootMixer;
    private AnimationMixerPlayable _movementMixer;
    private AnimationClipPlayable _idlePlayable;
    private AnimationClipPlayable _walkPlayable;
    private AnimationClipPlayable _runPlayable;
    private AnimationClipPlayable _actionPlayable;

    private int _currentWeaponID = 1; // 1: Pistol, 2: Shotgun, 3: Rifle
    private PlayerMovementState _currentMovementState = PlayerMovementState.Idle;

    // Hareket Ağırlıkları (Crossfade)
    private float _targetWeightIdle = 1f;
    private float _targetWeightWalk = 0f;
    private float _targetWeightRun = 0f;

    private float _currentWeightIdle = 1f;
    private float _currentWeightWalk = 0f;
    private float _currentWeightRun = 0f;

    // SmoothDamp hız takip değişkenleri (Doğal ivmelenme ve yavaşlama için)
    private float _velWeightIdle = 0f;
    private float _velWeightWalk = 0f;
    private float _velWeightRun = 0f;

    private Coroutine _actionCoroutine = null;
    private bool _isActionPlaying = false;

    void Awake()
    {
        if (Instance == null) Instance = this;

        FindTargetAnimator();
        AutoLoadClipsIfNull();
    }

    void Start()
    {
        InitGraph();
        SetWeapon(_currentWeaponID, playEquip: false);
    }

    void OnEnable()
    {
        if (!_graph.IsValid() && targetAnimator != null)
        {
            InitGraph();
            SetWeapon(_currentWeaponID, playEquip: false);
        }
    }

    void OnDisable()
    {
        if (_graph.IsValid())
        {
            _graph.Stop();
        }
    }

    void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }
    }

    /// <summary>
    /// Kol objesindeki Animator'ı bulur ve Mecanim controller'ını sıfırlar.
    /// </summary>
    public void FindTargetAnimator()
    {
        if (targetAnimator == null)
        {
            targetAnimator = GetComponentInChildren<Animator>(true);
        }

        if (targetAnimator == null)
        {
            GameObject armsGo = GameObject.Find("HandWithGloves");
            if (armsGo != null) targetAnimator = armsGo.GetComponent<Animator>();
        }

        if (targetAnimator != null)
        {
            // Mecanim state machine ve controller'ı devre dışı bırakıyoruz ki arka planda çakışma ve uyarı vermesin
            targetAnimator.runtimeAnimatorController = null;
        }
    }

    /// <summary>
    /// PlayableGraph altyapısını kurar.
    /// Root Mixer (2 Port):
    ///   Port 0 -> Movement Mixer (Port 0: Idle, Port 1: Run)
    ///   Port 1 -> Action Playable (Fire, Reload, Equip vb.)
    /// </summary>
    private void InitGraph()
    {
        FindTargetAnimator();
        if (targetAnimator == null)
        {
            Debug.LogWarning("[WeaponAnimationSystem] targetAnimator bulunamadı!");
            return;
        }

        if (_graph.IsValid())
        {
            _graph.Destroy();
        }

        _graph = PlayableGraph.Create("WeaponAnimationSystemGraph");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(_graph, "WeaponAnimationOutput", targetAnimator);

        _rootMixer = AnimationMixerPlayable.Create(_graph, 2);
        output.SetSourcePlayable(_rootMixer);

        _movementMixer = AnimationMixerPlayable.Create(_graph, 3);
        _graph.Connect(_movementMixer, 0, _rootMixer, 0);

        _rootMixer.SetInputWeight(0, 1.0f);
        _rootMixer.SetInputWeight(1, 0.0f);

        _graph.Play();
    }

    void Update()
    {
        if (!_graph.IsValid() || !_movementMixer.IsValid()) return;

        // Ani sıçrama (snap/teleport) ve silahın bir anda havaya dikilmesini önleyen
        // Ease-In Ease-Out (SmoothDamp) organik ağırlık harmanlaması
        float dt = Time.deltaTime;
        if (dt > 0.0001f)
        {
            float activeRunDuration = (_currentWeaponID == 1) ? pistolRunTransitionDuration : runTransitionDuration;

            _currentWeightIdle = Mathf.SmoothDamp(_currentWeightIdle, _targetWeightIdle, ref _velWeightIdle, idleTransitionDuration, Mathf.Infinity, dt);
            _currentWeightWalk = Mathf.SmoothDamp(_currentWeightWalk, _targetWeightWalk, ref _velWeightWalk, walkTransitionDuration, Mathf.Infinity, dt);
            _currentWeightRun  = Mathf.SmoothDamp(_currentWeightRun,  _targetWeightRun,  ref _velWeightRun,  activeRunDuration,      Mathf.Infinity, dt);

            // Ağırlıkların toplamını normalize et (kolların küçülmesini veya deformasyonunu önler)
            float total = _currentWeightIdle + _currentWeightWalk + _currentWeightRun;
            if (total > 0.0001f)
            {
                _movementMixer.SetInputWeight(0, _currentWeightIdle / total);
                _movementMixer.SetInputWeight(1, _currentWeightWalk / total);
                _movementMixer.SetInputWeight(2, _currentWeightRun  / total);
            }
        }
    }

    /// <summary>
    /// Karakterin hareket durumuna göre yumuşak ve akıcı (Smooth Crossfade) geçiş başlatır.
    /// Idle, Walk (WASD) veya Run (Shift+WASD)
    /// </summary>
    public void SetMovementState(PlayerMovementState state)
    {
        _currentMovementState = state;

        _targetWeightIdle = (state == PlayerMovementState.Idle) ? 1.0f : 0.0f;
        _targetWeightWalk = (state == PlayerMovementState.Walk) ? 1.0f : 0.0f;
        _targetWeightRun  = (state == PlayerMovementState.Run)  ? 1.0f : 0.0f;
    }

    /// <summary>
    /// Karakterin hareket durumuna göre animasyon hızını günceller.
    /// speed: 0.0f = Duruyor (Idle), 0.5f = Yürüyor (Walk), 1.0f = Koşuyor (Run)
    /// </summary>
    public void SetMovementSpeed(float speed)
    {
        if (speed <= 0.05f) SetMovementState(PlayerMovementState.Idle);
        else if (speed <= 0.75f) SetMovementState(PlayerMovementState.Walk);
        else SetMovementState(PlayerMovementState.Run);
    }

    /// <summary>
    /// Silah değiştiğinde çağrılır (1: Pistol, 2: Shotgun, 3: Rifle).
    /// </summary>
    public void SetWeapon(int weaponID, bool playEquip = true)
    {
        _currentWeaponID = weaponID;
        InterruptAction();
        UpdateMovementPlayables();

        if (playEquip)
        {
            PlayEquip();
        }
    }

    private void UpdateMovementPlayables()
    {
        if (!_graph.IsValid() || !_movementMixer.IsValid()) return;

        AnimationClip idleClip = null;
        AnimationClip walkClip = null;
        AnimationClip runClip = null;

        if (_currentWeaponID == 1)
        {
            idleClip = pistolIdle;
            walkClip = pistolWalk != null ? pistolWalk : pistolRun;
            runClip = pistolRun;
        }
        else if (_currentWeaponID == 2)
        {
            idleClip = shotgunIdle;
            walkClip = shotgunRun;
            runClip = shotgunRun;
        }
        else if (_currentWeaponID == 3)
        {
            idleClip = rifleIdle;
            walkClip = rifleRun;
            runClip = rifleRun;
        }

        // Eski playables'ları temizle
        if (_idlePlayable.IsValid())
        {
            _graph.Disconnect(_movementMixer, 0);
            _idlePlayable.Destroy();
        }
        if (_walkPlayable.IsValid())
        {
            _graph.Disconnect(_movementMixer, 1);
            _walkPlayable.Destroy();
        }
        if (_runPlayable.IsValid())
        {
            _graph.Disconnect(_movementMixer, 2);
            _runPlayable.Destroy();
        }

        // Yeni klipleri bağla
        if (idleClip != null)
        {
            _idlePlayable = AnimationClipPlayable.Create(_graph, idleClip);
            _graph.Connect(_idlePlayable, 0, _movementMixer, 0);
        }
        if (walkClip != null)
        {
            _walkPlayable = AnimationClipPlayable.Create(_graph, walkClip);
            _graph.Connect(_walkPlayable, 0, _movementMixer, 1);
        }
        if (runClip != null)
        {
            _runPlayable = AnimationClipPlayable.Create(_graph, runClip);
            _graph.Connect(_runPlayable, 0, _movementMixer, 2);
        }

        SetMovementState(PlayerMovementState.Idle);
        _currentWeightIdle = 1f;
        _currentWeightWalk = 0f;
        _currentWeightRun  = 0f;
        _velWeightIdle = 0f;
        _velWeightWalk = 0f;
        _velWeightRun  = 0f;
        _movementMixer.SetInputWeight(0, 1.0f);
        _movementMixer.SetInputWeight(1, 0.0f);
        _movementMixer.SetInputWeight(2, 0.0f);
    }

    // =========================================================================
    // AKSİYON METOTLARI (FIRE, RELOAD, EQUIP, SHOTGUN RELOAD)
    // =========================================================================

    /// <summary>
    /// Aktif silah için anlık ateş animasyonunu oynatır.
    /// </summary>
    public void PlayFire(Action onComplete = null)
    {
        AnimationClip clip = null;
        float speed = 1.0f;

        if (_currentWeaponID == 1)
        {
            clip = pistolFire;
            speed = pistolAnimSpeed;
        }
        else if (_currentWeaponID == 2)
        {
            clip = shotgunFire;
        }
        else if (_currentWeaponID == 3)
        {
            clip = rifleFire;
        }

        if (clip != null)
        {
            PlayAction(clip, speed, fireFadeInDuration, fireFadeOutDuration, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Aktif silah için şarjör değiştirme animasyonunu oynatır.
    /// </summary>
    public void PlayReload(Action onComplete = null)
    {
        AnimationClip clip = null;
        float speed = 1.0f;

        if (_currentWeaponID == 1)
        {
            clip = pistolReload;
            speed = pistolReloadAnimSpeed;
        }
        else if (_currentWeaponID == 3)
        {
            clip = rifleReload;
        }

        if (clip != null)
        {
            PlayAction(clip, speed, reloadFadeInDuration, reloadFadeOutDuration, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Silahı çekme (Equip) animasyonunu oynatır.
    /// </summary>
    public void PlayEquip(Action onComplete = null)
    {
        AnimationClip clip = null;
        if (_currentWeaponID == 1) clip = pistolEquip;
        else if (_currentWeaponID == 2) clip = shotgunEquip;
        else if (_currentWeaponID == 3) clip = rifleEquip;

        if (clip != null)
        {
            PlayAction(clip, 1.0f, equipFadeInDuration, equipFadeOutDuration, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Pompalı için doldurma başlangıç hazırlığı (Preparation Before)
    /// </summary>
    public void PlayShotgunPrepBefore(Action onComplete = null)
    {
        if (shotgunPrepBefore != null)
        {
            PlayAction(shotgunPrepBefore, 1.0f, 0.02f, 0.02f, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Pompalı için tek bir fişek takma animasyonu (Reload Insert)
    /// </summary>
    public void PlayShotgunReloadInsert(Action onComplete = null)
    {
        if (shotgunReloadInsert != null)
        {
            PlayAction(shotgunReloadInsert, 1.0f, 0.01f, 0.02f, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Pompalı için dolum sonrası pompalama ve eski konuma dönüş (Preparation After / Pump)
    /// </summary>
    public void PlayShotgunPrepAfter(Action onComplete = null)
    {
        if (shotgunPrepAfter != null)
        {
            PlayAction(shotgunPrepAfter, 1.0f, 0.02f, 0.05f, onComplete);
        }
        else
        {
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// Tek seferlik bir animasyon klibini kök mikserdeki Action katmanında oynatır.
    /// Önceki aksiyon varsa anında keser (interrupt).
    /// </summary>
    public void PlayAction(AnimationClip clip, float speed = 1.0f, float fadeIn = 0.02f, float fadeOut = 0.08f, Action onComplete = null)
    {
        if (clip == null || !_graph.IsValid() || !_rootMixer.IsValid())
        {
            onComplete?.Invoke();
            return;
        }

        if (_actionCoroutine != null)
        {
            StopCoroutine(_actionCoroutine);
            _actionCoroutine = null;
        }

        _actionCoroutine = StartCoroutine(ActionRoutine(clip, speed, fadeIn, fadeOut, onComplete));
    }

    private IEnumerator ActionRoutine(AnimationClip clip, float speed, float fadeIn, float fadeOut, Action onComplete)
    {
        _isActionPlaying = true;

        if (_actionPlayable.IsValid())
        {
            _graph.Disconnect(_rootMixer, 1);
            _actionPlayable.Destroy();
        }

        _actionPlayable = AnimationClipPlayable.Create(_graph, clip);
        _actionPlayable.SetSpeed(speed);
        _actionPlayable.SetTime(0);
        _graph.Connect(_actionPlayable, 0, _rootMixer, 1);

        float clipDuration = clip.length / Mathf.Max(0.01f, Mathf.Abs(speed));

        // Fade In: Hızlıca veya anında Action katmanına geç
        if (fadeIn <= 0.002f)
        {
            _rootMixer.SetInputWeight(0, 0f);
            _rootMixer.SetInputWeight(1, 1f);
        }
        else
        {
            float t = 0f;
            while (t < fadeIn)
            {
                t += Time.deltaTime;
                float w = Mathf.Clamp01(t / fadeIn);
                _rootMixer.SetInputWeight(0, 1f - w);
                _rootMixer.SetInputWeight(1, w);
                yield return null;
            }
            _rootMixer.SetInputWeight(0, 0f);
            _rootMixer.SetInputWeight(1, 1f);
        }

        // Klibi sonundaki fadeOut payı kadar oynat
        float actualFadeIn = fadeIn > 0.002f ? fadeIn : 0f;
        float playDuration = Mathf.Max(0f, clipDuration - actualFadeIn - fadeOut);
        if (playDuration > 0f)
        {
            yield return new WaitForSeconds(playDuration);
        }

        // Fade Out: Yumuşakça ana hareket katmanına (Idle/Run) geri dön
        if (fadeOut <= 0.002f)
        {
            _rootMixer.SetInputWeight(0, 1f);
            _rootMixer.SetInputWeight(1, 0f);
        }
        else
        {
            float t = 0f;
            while (t < fadeOut)
            {
                t += Time.deltaTime;
                float w = Mathf.Clamp01(t / fadeOut);
                _rootMixer.SetInputWeight(0, w);
                _rootMixer.SetInputWeight(1, 1f - w);
                yield return null;
            }
            _rootMixer.SetInputWeight(0, 1f);
            _rootMixer.SetInputWeight(1, 0f);
        }

        if (_actionPlayable.IsValid())
        {
            _graph.Disconnect(_rootMixer, 1);
            _actionPlayable.Destroy();
        }

        _isActionPlaying = false;
        _actionCoroutine = null;
        onComplete?.Invoke();
    }

    /// <summary>
    /// Çalışan herhangi bir aksiyon animasyonunu (ör: reload, equip) durdurur ve Idle/Run'a yumuşakça döner.
    /// </summary>
    public void InterruptAction(bool smooth = true)
    {
        if (_actionCoroutine != null)
        {
            StopCoroutine(_actionCoroutine);
            _actionCoroutine = null;
        }

        if (smooth && gameObject.activeInHierarchy && _actionPlayable.IsValid())
        {
            StartCoroutine(FastFadeOutRoutine(0.06f));
        }
        else
        {
            HardStopAction();
        }
    }

    private IEnumerator FastFadeOutRoutine(float duration)
    {
        float t = 0f;
        float startActionWeight = _rootMixer.IsValid() ? _rootMixer.GetInputWeight(1) : 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float factor = Mathf.Clamp01(t / duration);
            if (_rootMixer.IsValid())
            {
                _rootMixer.SetInputWeight(0, factor);
                _rootMixer.SetInputWeight(1, (1f - factor) * startActionWeight);
            }
            yield return null;
        }

        HardStopAction();
    }

    private void HardStopAction()
    {
        if (_actionPlayable.IsValid() && _graph.IsValid())
        {
            _graph.Disconnect(_rootMixer, 1);
            _actionPlayable.Destroy();
        }

        if (_rootMixer.IsValid())
        {
            _rootMixer.SetInputWeight(0, 1f);
            _rootMixer.SetInputWeight(1, 0f);
        }

        _isActionPlaying = false;
    }

    // =========================================================================
    // SÜRE BİLGİSİ (DURATIONS)
    // =========================================================================

    public float GetShotgunPrepBeforeDuration() => shotgunPrepBefore != null ? shotgunPrepBefore.length : 0.13f;
    public float GetShotgunInsertDuration() => shotgunReloadInsert != null ? shotgunReloadInsert.length : 0.17f;
    public float GetShotgunPrepAfterDuration() => shotgunPrepAfter != null ? shotgunPrepAfter.length : 0.31f;

    public float GetReloadDuration(int weaponID)
    {
        if (weaponID == 1)
        {
            float len = pistolReload != null ? pistolReload.length : 0.5833f;
            return len / Mathf.Max(0.05f, pistolReloadAnimSpeed);
        }
        if (weaponID == 3) return rifleReload != null ? rifleReload.length : 0.67f;
        return 1.0f;
    }

    public bool IsActionPlaying() => _isActionPlaying;

    /// <summary>
    /// Editörde veya çalışma anında klipleri otomatik Assets/Game/Animations altından yükler.
    /// </summary>
    public void AutoLoadClipsIfNull()
    {
#if UNITY_EDITOR
        if (pistolIdle == null) pistolIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Idle_Fixed.anim");
        if (pistolWalk == null) pistolWalk = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Sprint_Fixed.anim");
        if (pistolWalk == null) pistolWalk = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Double_Hand_Pistol_Sprint.fbx");
        if (pistolRun == null) pistolRun = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Run_Fixed.anim");
        if (pistolFire == null) pistolFire = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Fire_Fixed.anim");
        if (pistolReload == null) pistolReload = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Reload_Fixed.anim");
        if (pistolEquip == null) pistolEquip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Pistol_Equip_Fixed.anim");

        if (shotgunIdle == null) shotgunIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Idle_Fixed.anim");
        if (shotgunRun == null) shotgunRun = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Run_Fixed.anim");
        if (shotgunFire == null) shotgunFire = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Fire_Fixed.anim");
        if (shotgunEquip == null) shotgunEquip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Equip_Fixed.anim");
        if (shotgunPrepBefore == null) shotgunPrepBefore = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Preparation_Before_Fixed.anim");
        if (shotgunReloadInsert == null) shotgunReloadInsert = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Reload_Insert_Fixed.anim");
        if (shotgunPrepAfter == null) shotgunPrepAfter = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Shotgun_Preparation_After_Fixed.anim");

        if (rifleIdle == null) rifleIdle = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Rifle_Idle_Fixed.anim");
        if (rifleRun == null) rifleRun = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Rifle_Run_Fixed.anim");
        if (rifleFire == null) rifleFire = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Rifle_Fire_Fixed.anim");
        if (rifleReload == null) rifleReload = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Rifle_Reload_Fixed.anim");
        if (rifleEquip == null) rifleEquip = UnityEditor.AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/Game/Animations/Rifle_Equip_Fixed.anim");
#endif
    }

    void Reset()
    {
        FindTargetAnimator();
        AutoLoadClipsIfNull();
    }
}
