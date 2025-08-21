using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;
using System;

public class ChestHandler : MonoBehaviour
{
    public GameObject chestObject;
    public Animator ChestAnimator;
    public TreasureChest treasureChest;
    public GameObject panelEventHandler;
    public WaterTapManager waterTapManager;

    public bool isPlayerNear = false;

    void Start()
    {
        waterTapManager = GameObject.FindGameObjectWithTag("waterTaps").GetComponent<WaterTapManager>();
    }

    

    void OnCollisionEnter(Collision collision)
    {
        if ( !ChestAnimator.GetBool("chestOpen") && collision.gameObject.CompareTag("Player") && waterTapManager.noOfTapsOn == 0 )
        {
            isPlayerNear = true;
            panelEventHandler?.SetActive(true);

        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            isPlayerNear = false;
            panelEventHandler?.SetActive(false);
        }
    }


    public void gameRestart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

}
