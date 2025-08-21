using System.Collections.Generic;
using UnityEngine;

public class WaterTapManager : MonoBehaviour
{
    [System.Serializable]
    public class Tap
    {
        public GameObject tapObject;
        public ParticleSystem waterFlow;
        public bool isPlayerNear = false;
    }

    public List<Tap> taps;
    public GameObject closeTapButton;
    public int minActiveTaps = 1;
    public int maxActiveTaps = 3;

    public ParticleSystem currentTapToClose;
    public bool taskCompleted = false;

    public int noOfTapsOn;

    void Start()
    {
        InitializeTaps();
    }

    void InitializeTaps()
    {
        int activeCount = Random.Range(minActiveTaps, maxActiveTaps + 1);
        noOfTapsOn = activeCount;

        List<int> selectedIndices = new List<int>();

        while (selectedIndices.Count < activeCount)
        {
            int index = Random.Range(0, taps.Count);
            if (!selectedIndices.Contains(index))
            {
                selectedIndices.Add(index);
                taps[index].waterFlow.Play();
            }
        }
    }

    public void CloseTheTap()
    {
        if (currentTapToClose != null)
        {
            currentTapToClose.Stop();
            closeTapButton.SetActive(false);
            noOfTapsOn--;

            if (noOfTapsOn <= 0)
                taskCompleted = true;
        }
    }
}
