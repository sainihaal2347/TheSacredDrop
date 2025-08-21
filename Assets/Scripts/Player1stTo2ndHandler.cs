using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player1stTo2ndHandler : MonoBehaviour
{
    public int speed = 10;
    void Update()
    {
        transform.position += (Vector3.forward*speed)*Time.deltaTime;
    }
}
