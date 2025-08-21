using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class LogicHandler : MonoBehaviour
{
    public GameObject instructionPanel;
    public Button skipButton;

    public WaterTapManager waterTapManager;
    public UseBucketTrigger useBucketTrigger;
    public ROInteraction rOInteraction;

    public Image tapTaskBorder;
    public Image bucketTaskBorder;
    public Image roTaskBorder;

    public Color completedColor = Color.green;
    public Color incompleteColor = Color.red;

    public float hideDelayAfterTasks = 3f;

    private bool hasSkipped = false;
    private bool hasTriggeredCompletion = false;
    public GameObject playerJoystick;
    public GameObject restartButton;
    public GameObject jumpButton;
    public GameObject rainImage;
    public GameObject roImage;
    public GameObject tapImage;

    void Start()
    {
        instructionPanel.SetActive(true);
        playerJoystick.SetActive(false);
        restartButton.SetActive(false);
        jumpButton.SetActive(false);
        Time.timeScale = 0f;
        skipButton.onClick.AddListener(SkipIntro);

        UpdateTaskVisuals();
    }

    void Update()
    {
        if (!hasSkipped) return;

        UpdateTaskVisuals();

        if (!hasTriggeredCompletion &&
            waterTapManager.taskCompleted &&
            useBucketTrigger.taskCompleted &&
            rOInteraction.taskCompleted)
        {
            hasTriggeredCompletion = true;

            FindObjectOfType<ChestNavigationPath>()?.GeneratePath();
            StartCoroutine(HideTaskImagesAfterDelay());
        }
    }

    void SkipIntro()
    {
        instructionPanel.SetActive(false);
        playerJoystick.SetActive(true);
        restartButton.SetActive(true);
        jumpButton.SetActive(true);
        rainImage.SetActive(true);
        roImage.SetActive(true);
        tapImage.SetActive(true);
        Time.timeScale = 1f;
        hasSkipped = true;
    }

    void UpdateTaskVisuals()
    {
        if (tapTaskBorder != null)
            tapTaskBorder.color = waterTapManager.taskCompleted ? completedColor : incompleteColor;

        if (bucketTaskBorder != null)
            bucketTaskBorder.color = useBucketTrigger.taskCompleted ? completedColor : incompleteColor;

        if (roTaskBorder != null)
            roTaskBorder.color = rOInteraction.taskCompleted ? completedColor : incompleteColor;
    }

    IEnumerator HideTaskImagesAfterDelay()
    {
        yield return new WaitForSeconds(hideDelayAfterTasks);

        if (tapTaskBorder != null) tapTaskBorder.gameObject.SetActive(false);
        if (bucketTaskBorder != null) bucketTaskBorder.gameObject.SetActive(false);
        if (roTaskBorder != null) roTaskBorder.gameObject.SetActive(false);
    }
}
