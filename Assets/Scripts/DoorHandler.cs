using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DoorHandler : MonoBehaviour
{
    public Animator DoorAnimator;

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            DoorAnimator.SetTrigger("DoorOpen");
            StartCoroutine(ChangeSceneAfterDelay(3f)); // Start coroutine with 3-second delay
        }
    }

    IEnumerator ChangeSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay); // Wait for the delay
        SceneManager.LoadScene("BossOfWar_continuous_line"); // Load new scene
    }
}
