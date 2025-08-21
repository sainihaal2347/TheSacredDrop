using System.Collections;
using UnityEngine;
using DigitalRuby.RainMaker; // Required namespace for RainScript

public class RainAreaTrigger : MonoBehaviour
{
    [SerializeField] private ParticleSystem rainParticleSystem; // optional visual rain
    [SerializeField] private RainScript rainScript; // assign your RainDropPrefab's RainScript here

    private Coroutine stopRainCoroutine;

    private void Start()
    {
        if (rainScript != null)
        {
            rainScript.RainIntensity = 0f;
            Debug.Log("Rain intensity set to 0 at start.");
        }

        if (rainParticleSystem != null)
        {
            rainParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player entered rain trigger area.");

            if (rainParticleSystem != null && !rainParticleSystem.isPlaying)
                rainParticleSystem.Play();

            if (rainScript != null)
                rainScript.RainIntensity = 0.3f;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Player exited rain trigger area.");

            if (rainParticleSystem != null && rainParticleSystem.isPlaying)
                rainParticleSystem.Stop();

            if (rainScript != null)
                rainScript.RainIntensity = 0f;

            if (stopRainCoroutine != null)
                StopCoroutine(stopRainCoroutine);
        }
    }

    public void StopRainWithDelay(float delay)
    {
        if (stopRainCoroutine != null)
            StopCoroutine(stopRainCoroutine);

        stopRainCoroutine = StartCoroutine(StopRainAfterDelay(delay));
    }

    private IEnumerator StopRainAfterDelay(float delay)
    {
        Debug.Log($"Delaying rain stop by {delay} seconds due to bucket use...");
        yield return new WaitForSeconds(delay);

        if (rainScript != null)
        {
            rainScript.RainIntensity = 0f;
            Debug.Log("Rain stopped after bucket use.");
        }

        if (rainParticleSystem != null && rainParticleSystem.isPlaying)
            rainParticleSystem.Stop();
    }
}
