using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class WaterTap : MonoBehaviour
{
    public ParticleSystem RunningWater;
    private bool isPlayerNear;
    public GameObject CloseTapButton;

    public WaterTapManager waterTapManager;

    void Start()
    {
        // RunningWater.Stop();
        waterTapManager = GameObject.FindGameObjectWithTag("waterTaps").GetComponent<WaterTapManager>();
    }

    public void closeTheTap(){
        RunningWater.Stop();
    }

       void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            isPlayerNear = true;
            CloseTapButton.SetActive(true);
        }
    }
    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player") && RunningWater.isPlaying){
            waterTapManager.currentTapToClose = RunningWater;
            CloseTapButton.SetActive(true);
        }
    }
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            CloseTapButton.SetActive(false); // Hide the panel when the player moves away
        }
    }

    void Update()
    {
        // if (inReach && isClosed && Input.GetButtonDown("Interact"))
        // {
            
        //     RunningWater.Play();
        // }

        // else if (inReach && isOpen && Input.GetButtonDown("Interact"))
        // {
        //     openSound.Pause();
        //     RunningWater.Stop();
        // }
    }
}
