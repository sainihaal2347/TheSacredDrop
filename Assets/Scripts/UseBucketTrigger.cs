using UnityEngine;
using UnityEngine.UI;

public class UseBucketTrigger : MonoBehaviour
{
    public GameObject useBucketButton;
    public GameObject bucketObject;
    public RainAreaTrigger rainTrigger;
    public bool taskCompleted = false;

    private Vector3 bucketTargetPosition = new Vector3(1.72f, 0.04f, -1.89f);
    private bool playerInside = false;
    private bool bucketUsed = false;

    private void Start()
    {
        useBucketButton?.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !bucketUsed)
        {
            playerInside = true;
            useBucketButton.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInside = false;
            useBucketButton.SetActive(false);
        }
    }

    public void OnUseBucketClicked()
    {
        if (playerInside && !bucketUsed)
        {
            bucketUsed = true;
            useBucketButton.SetActive(false);

            if (bucketObject != null)
                bucketObject.transform.position = bucketTargetPosition;

            taskCompleted = true;

            if (rainTrigger != null)
                rainTrigger.StopRainWithDelay(10f);
        }
    }
}
