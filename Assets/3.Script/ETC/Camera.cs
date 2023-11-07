using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Camera : MonoBehaviour
{
    [SerializeField] private GameObject player;

    private void Start()
    {
        transform.rotation = Quaternion.Euler(10f, 0, 0);
    }
    void Update()
    {
        transform.position = player.transform.position + new Vector3(0, 0, 1.5f);
        
    }
}
