using UnityEngine;
using System.Collections;
using System.Collections.Generic;


public class CollectableObject : MonoBehaviour
{
    public GameObject Player;
    public GameObject Object;
    public void OnCollisionEnter(Collision a)
    {
        GameObject playerObject = GameObject.FindWithTag("Player");
        if (a.gameObject.CompareTag("Player"))
        {
            Destroy(Object);
            Player1 playerScript = playerObject.GetComponent<Player1>();
            playerScript.RedKey = true;
        }
    }
    void Start()
    {
        
    }
    void Update()
    {
        
    }
}
