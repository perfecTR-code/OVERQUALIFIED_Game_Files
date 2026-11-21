using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DoorScript : MonoBehaviour
{
    public float OpenAngle = 90f;
    public float OpenSpeed = 2f;
    public bool IsOpen = false;
    private Quaternion ClosedRotation;
    private Quaternion OpenRotation;
    public Coroutine CurrentCoroutine;
 
    
    void Start()
    {
        ClosedRotation = transform.rotation;
        OpenRotation = Quaternion.Euler(transform.eulerAngles + new Vector3(0, OpenAngle, 0));
    }

    void Update()
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        Player1 playerScript = playerObject.GetComponent<Player1>();
        bool a = playerScript.RedKey;
        if (a == true) { 
            if (Input.GetKeyDown(KeyCode.E))
            {
                if (CurrentCoroutine != null) StopCoroutine(CurrentCoroutine);
                CurrentCoroutine = StartCoroutine(ToggleDoor());
            }

        }
    }
    private IEnumerator ToggleDoor()
    {
        Quaternion targetRotation = IsOpen ? ClosedRotation : OpenRotation;
        IsOpen = !IsOpen;

        while (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * OpenSpeed);
            yield return null;  // Bu satýr coroutine'in çalýþmasý için gerekli
        }

        transform.rotation = targetRotation; // Son hali tam oturt
    }
}
