using UnityEngine;

public class KarakterKontroluDuzeltilmis : MonoBehaviour
{
    public bool RedKey = false;
    public float charspeed = 10f;
    private Rigidbody physicalbody;
    private Player1 player1;
    private Vector3 movement;
    private float JumpVelocity = 12f;

    // Shift'e basılıp basılmadığını burada saklayacağız
    private bool isSprinting = false;
    private bool isJumpingright = false;
    private bool isColliding = false;
    private void OnCollisionEnter(Collision collision)
    {
        isColliding = true;
    }
    private void OnCollisionExit(Collision collision)
    {
        isColliding=false;
    }
    void Start()
    {
        physicalbody = GetComponent<Rigidbody>();
        player1 = GetComponent<Player1>();
        if (physicalbody != null)
        {
            // Kamera dönüşü ve hareket sırasında fizik-render micro-stutter (drop) olmasını önler
            physicalbody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    void Update()
    {
        // 1. ADIM: Tüm Girdileri (Input) Update içinde al
        float VerticalInput = Input.GetAxisRaw("Vertical");
        float HorizontalInput = Input.GetAxisRaw("Horizontal");

        // Hareket yönünü hesapla
        Vector3 RawMovement = ((HorizontalInput * transform.right) + (VerticalInput * transform.forward));
        bool hasMovement = (RawMovement != Vector3.zero);

        // Shift tuşuna basılı tutuluyor mu ve Player1 koşuya izin veriyor mu?
        // Koşarken ateş edildiğinde Player1 IsSprintAllowed'ı false yapar ve karakterin hızı anında normal yürüme hızına iner!
        bool shiftHeld = Input.GetKey(KeyCode.LeftShift);
        if (player1 != null)
        {
            isSprinting = shiftHeld && hasMovement && player1.IsSprintAllowed;
        }
        else
        {
            isSprinting = shiftHeld && hasMovement;
        }

        if (Input.GetKeyDown(KeyCode.Space) && isColliding == true)
        {
            isJumpingright = true;
        }

        if (hasMovement)
        {
            movement = RawMovement.normalized;
        }
        else
        {
            movement = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        // 2. ADIM: Fiziği burada uygula

        float currentSpeed = charspeed;

        // Koşuluyorsa hızı 2 ile çarp; ateş edildiyse veya koşu kesildiyse anında normal yürüme hızına düşür
        if (isSprinting && (player1 == null || player1.IsSprintAllowed))
        {
            currentSpeed = charspeed * 2;
        }
       
        Vector3 TargetSpeed = movement * currentSpeed;

        // Y eksenindeki hızı (yerçekimi) koru, yoksa karakter havada asılı kalır veya yavaş düşer
        // NOT: Unity 2022 ve öncesi için 'linearVelocity' yerine 'velocity' kullanın.
        
        if (isJumpingright == true)
        {
            TargetSpeed.y=physicalbody.linearVelocity.y+JumpVelocity;
            isJumpingright=false;
        }
        else
        {
            TargetSpeed.y = physicalbody.linearVelocity.y;
        }

            physicalbody.linearVelocity = TargetSpeed;
    }
}