using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class SceneTransition : MonoBehaviour
{
    public Image fadePanel; // Assign your UI Panel's Image in Inspector
    public float fadeDuration = 1f;

    void Start()
    {
    }

    public void LoadSceneWithFade(string sceneName)
    {
        StartCoroutine(FadeOut(sceneName));
    }

    IEnumerator FadeOut(string sceneName)
    {
        fadePanel.gameObject.SetActive(true);
        float alpha = 0f;
        while (alpha < 1)
        {
            alpha += Time.deltaTime / fadeDuration;
            fadePanel.color = new Color(0, 0, 0, alpha);
            
        }
        SceneManager.LoadScene(sceneName);
        return null;
    }
}
