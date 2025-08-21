using UnityEngine;

public class WaterDrop : MonoBehaviour
{
    public int scoreValue = 10;
    public AudioClip collectSFX;

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player")) 
        {
            AudioSource playerAudio = collision.gameObject.GetComponent<AudioSource>();

            if (playerAudio != null && collectSFX != null)
            {
                playerAudio.priority = 0; // Higher priority to avoid delays
                playerAudio.pitch = 1.0f;  // Reset pitch to default before playing
                playerAudio.PlayOneShot(collectSFX, 1f);
            }
            else
            {
                Debug.LogWarning("Missing AudioSource or AudioClip on Player!");
            }

            FindObjectOfType<EnvironmentRegenerator>().IncreaseScore(scoreValue);
            Destroy(gameObject);
        }
    }
}
