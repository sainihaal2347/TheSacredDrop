using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnvironmentMovement : MonoBehaviour
{
    public float speed = -500;
    public float DestroyZone = -3000;
    // Start is called before the first frame update
    void Start()
    {
        // 
    }

    // Update is called once per frame
    void Update()
    {
        transform.position += (Vector3.forward*speed)*Time.deltaTime;
        if (transform.position.z < DestroyZone){
            Destroy(gameObject);
        }
    }
}
