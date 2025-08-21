using UnityEngine;
using TMPro;

public class WaterConservationTips : MonoBehaviour
{
    public TMP_Text tipsText; // UI Text component to display tips
    public EnvironmentRegenerator environmentRegenerator; // Reference to EnvironmentRegenerator script

    private string[] tips = {
        "Turn off the tap while brushing your teeth.",
        "Fix leaks around your home to save water.",
        "Take shorter showers to reduce water usage.",
        "Use a broom instead of a hose to clean driveways.",
        "Wash your car with a bucket, not a hose.",
        "Water your plants early in the morning or late in the evening.",
        "Use water-efficient appliances and fixtures.",
        "Collect rainwater for gardening or cleaning."
    };

    private int currentTipIndex = 0;
    public int scoreThreshold = 15; // Score threshold to show the next tip

    private int lastScore = 0; // Track the last score to check if a new threshold is crossed

    void Start()
    {
        if (tipsText != null)
        {
            tipsText.text = ""; // Initially hide the tip text
        }
        else
        {
            Debug.LogWarning("TipsText is not assigned in the Inspector!");
        }

        if (environmentRegenerator == null)
        {
            Debug.LogWarning("EnvironmentRegenerator reference is not assigned in the Inspector!");
        }
    }

    void Update()
    {
        // Check if the score has crossed the threshold and prevent displaying tips multiple times without resetting
        if (environmentRegenerator != null && environmentRegenerator.score >= scoreThreshold)
        {
            // Check if the score has crossed the threshold and the new score is different from the last one
            if (environmentRegenerator.score > lastScore && environmentRegenerator.score % scoreThreshold == 0)
            {
                ShowNextTip();
                lastScore = environmentRegenerator.score; // Update lastScore after displaying the tip
            }
        }
    }

    // Display the next tip
    void ShowNextTip()
    {
        tipsText.text = tips[currentTipIndex];
        currentTipIndex = (currentTipIndex + 1) % tips.Length; // Loop through tips
    }
}
