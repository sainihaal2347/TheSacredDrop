using UnityEngine;
using UnityEngine.Video;
using UnityEngine.SceneManagement;

public class IntroVideoPlayer : MonoBehaviour
{
    public VideoPlayer videoPlayer;
    public string nextSceneName = "Mysterious Dungeon"; // Change to your next scene

    void Start()
    {
        videoPlayer.loopPointReached += OnVideoEnd; // Event for when the video finishes
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        SceneManager.LoadScene(nextSceneName); // Load the next scene
    }
    public void SkipVideo(){
        SceneManager.LoadScene(nextSceneName);
    }
}
