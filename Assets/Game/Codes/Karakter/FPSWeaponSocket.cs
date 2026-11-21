using UnityEngine;

/// <summary>
/// Bu script silahı el kemiğine (bone'a) sabitler.
/// Silah objesinin üstüne koy, Pistol'u el kemiğinin child'ı yapar.
/// </summary>
[ExecuteAlways]
public class FPSWeaponSocket : MonoBehaviour
{
    [Header("Hedef Kemik İsmi")]
    [Tooltip("Sağ el için: hand_L.R | Sol el (Dual) için: hand_L.L")]
    public string targetBoneName = "hand_L.R";

    [Header("Silah Konum & Açı Ayarı (Inspector'dan canlı ayarla)")]
    public Vector3 positionOffset = new Vector3(0.015f, -0.05f, 0.08f);
    public Vector3 rotationOffset = new Vector3(-85f, -90f, 0f);
    public Vector3 scaleOffset    = new Vector3(0.05f, 0.05f, 0.05f);

    void Awake()
    {
        if (scaleOffset == Vector3.one && transform.localScale != Vector3.one && transform.localScale != Vector3.zero)
        {
            scaleOffset = transform.localScale;
        }
    }

    void Start()
    {
        EnsureParentedToHand();
        DisableColliders();
    }

    public void EnsureParentedToHand()
    {
        // Hedef kemiğe zaten bağlıysa tekrar arama yapma
        if (transform.parent != null && transform.parent.name.Equals(targetBoneName, System.StringComparison.OrdinalIgnoreCase))
            return;

        GameObject charGo = GameObject.Find("Character");
        if (charGo != null)
        {
            Transform[] all = charGo.GetComponentsInChildren<Transform>(true);
            Transform targetBone = null;

            // 1. Önce tam hedef kemik ismini ara (ör: "hand_L.R" veya "hand_L.L")
            foreach (var t in all)
            {
                if (t.name.Equals(targetBoneName, System.StringComparison.OrdinalIgnoreCase))
                {
                    targetBone = t;
                    break;
                }
            }

            // 2. Bulamazsa sadece deforming el kemiklerini ara (Asla IK kemiği alma!)
            if (targetBone == null)
            {
                foreach (var t in all)
                {
                    if (t.name.Equals("hand_L.R", System.StringComparison.OrdinalIgnoreCase))
                    {
                        targetBone = t;
                        break;
                    }
                }
            }

            if (targetBone != null)
            {
                transform.SetParent(targetBone, false);
                ApplyOffsets();
            }
        }
    }

    // Animator kemik override'larını LateUpdate'te düzeltiriz
    void LateUpdate()
    {
        // Çalışma anında hedef kemikten koptuysa yeniden bağla
        if (Application.isPlaying && (transform.parent == null || !transform.parent.name.Equals(targetBoneName, System.StringComparison.OrdinalIgnoreCase)))
        {
            EnsureParentedToHand();
        }

        ApplyOffsets();
    }

    void OnValidate()
    {
        ApplyOffsets();
    }

    public void ApplyOffsets()
    {
        transform.localPosition    = positionOffset;
        transform.localEulerAngles = rotationOffset;
        if (scaleOffset != Vector3.zero)
        {
            transform.localScale = scaleOffset;
        }
    }

    public void DisableColliders()
    {
        // Fizik motorunun silahı fırlatmasını önle
        foreach (var col in GetComponentsInChildren<Collider>(true))
            col.enabled = false;

        foreach (var rb in GetComponentsInChildren<Rigidbody>(true))
            rb.isKinematic = true;
    }
}
