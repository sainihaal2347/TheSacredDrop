using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ROInteraction : MonoBehaviour
{
    public GameObject oldPipe;
    public GameObject newPipe;
    public GameObject useArrowWaterButton;
    public ParticleSystem waterParticle;
    public Transform plant;
    public Vector3 finalScale = new Vector3(1.510698f, 3.67551f, 3.67551f);
    public float plantGrowDuration = 2f;
    public bool taskCompleted = false;

    private bool isActivated = false;

    void Start()
    {
        useArrowWaterButton.SetActive(false);
        newPipe.SetActive(false);
        waterParticle.Stop();
        plant.localScale = Vector3.zero;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !taskCompleted)
        {
            useArrowWaterButton.SetActive(true);
            Button button = useArrowWaterButton.GetComponent<Button>();
            button.onClick.RemoveAllListeners(); // Prevent multiple listeners
            button.onClick.AddListener(OnUseArrowWaterPressed);
        }
    }
    public void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            useArrowWaterButton.SetActive(false);
        }
    }

    public void OnUseArrowWaterPressed()
    {
        useArrowWaterButton.SetActive(false);
        oldPipe.SetActive(false);
        newPipe.SetActive(true);
        waterParticle.Play();
        StartCoroutine(GrowPlant());
        taskCompleted = true;
    }

    IEnumerator GrowPlant()
    {
        Vector3 startScale = Vector3.zero;
        float elapsed = 0f;

        while (elapsed < plantGrowDuration)
        {
            plant.localScale = Vector3.Lerp(startScale, finalScale, elapsed / plantGrowDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }

        plant.localScale = finalScale;
    }
}
