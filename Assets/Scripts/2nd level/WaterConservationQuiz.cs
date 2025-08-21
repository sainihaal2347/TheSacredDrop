using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WaterConservationQuiz : MonoBehaviour
{
    [Header("UI References")]
    public GameObject quizPanel;
    public TMP_Text questionText;      // Text area to display both question and options
    public TMP_Text scoreText;         // Text to display current score
    public TMP_Text feedbackText;      // Text to display feedback on answers
    public Button[] optionButtons;     // Buttons for selecting answers (no text, just clickable areas)
    public Button finishButton;        // Button to close the quiz
    public Button startQuizButton;     // Button in the won panel to start the quiz
    public TMP_Text leaderboardText;   // Separate text area for displaying leaderboard

    [Header("Game References")]
    public EnvironmentRegenerator environmentRegenerator;

    [Header("Quiz Settings")]
    public int questionsPerQuiz = 5;   // Number of questions to ask
    public int pointsPerCorrectAnswer = 2;  // Points awarded for correct answers
    
    // Private variables
    private List<QuizQuestion> quizQuestions = new List<QuizQuestion>();
    private int currentQuestionIndex = 0;
    private int quizScore = 0;
    private bool canAnswer = true;

    [System.Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options = new string[4];
        public int correctOptionIndex;

        public QuizQuestion(string q, string[] o, int correctIndex)
        {
            question = q;
            options = o;
            correctOptionIndex = correctIndex;
        }
    }

    private void Start()
    {
        // Initialize quiz questions based on water conservation tips
        InitializeQuestions();
        
        // Set up button listeners
        SetupButtons();
        
        // Hide quiz panel and leaderboard text initially
        if (quizPanel != null)
            quizPanel.SetActive(false);
            
        if (leaderboardText != null)
            leaderboardText.gameObject.SetActive(false);
            
        // Setup start quiz button
        if (startQuizButton != null)
        {
            startQuizButton.onClick.RemoveAllListeners();
            startQuizButton.onClick.AddListener(StartQuiz);
            startQuizButton.onClick.AddListener(() => {
                Debug.Log("Start quiz button clicked - disabling button and showing quiz elements");
                questionText.gameObject.SetActive(true);
                feedbackText.gameObject.SetActive(true);
                scoreText.gameObject.SetActive(true);
                foreach (Button button in optionButtons) {
                    if (button != null) {
                        button.gameObject.SetActive(true);
                    }
                }
                startQuizButton.gameObject.SetActive(false);
            });
        }
    }

    private void InitializeQuestions()
    {
        // Add questions based on water conservation tips
        quizQuestions.Add(new QuizQuestion(
            "What should you do while brushing your teeth to save water?",
            new string[] {
                "1. Let the water run continuously",
                "2. Turn off the tap",
                "3. Use a cup of water",
                "4. Brush faster"
            },
            1  // Turn off the tap
        ));

        quizQuestions.Add(new QuizQuestion(
            "Which action helps save water at home?",
            new string[] {
                "1. Ignore small leaks",
                "2. Take longer showers",
                "3. Fix leaking faucets",
                "4. Wash dishes individually"
            },
            2  // Fix leaking faucets
        ));

        quizQuestions.Add(new QuizQuestion(
            "What's the best way to clean your driveway to conserve water?",
            new string[] {
                "1. Pressure washer",
                "2. Garden hose on full blast",
                "3. Broom",
                "4. Sprinkler"
            },
            2  // Broom
        ));

        quizQuestions.Add(new QuizQuestion(
            "When is the best time to water plants to reduce water waste?",
            new string[] {
                "1. Midday when it's hottest",
                "2. Early morning or late evening",
                "3. Random times throughout the day",
                "4. Whenever you remember"
            },
            1  // Early morning or late evening
        ));

        quizQuestions.Add(new QuizQuestion(
            "Which is a water-efficient way to wash your car?",
            new string[] {
                "1. With a running hose",
                "2. With a bucket of water",
                "3. In the rain",
                "4. At home daily"
            },
            1  // With a bucket of water
        ));

        quizQuestions.Add(new QuizQuestion(
            "What's a good way to collect water for gardening?",
            new string[] {
                "1. Collect rainwater",
                "2. Always use tap water",
                "3. Buy bottled water",
                "4. Use water from swimming pools"
            },
            0  // Collect rainwater
        ));

        quizQuestions.Add(new QuizQuestion(
            "Which type of appliances help conserve water?",
            new string[] {
                "1. Older models",
                "2. Manual wash only",
                "3. Water-efficient models",
                "4. Standard models"
            },
            2  // Water-efficient models
        ));

        // Shuffle questions
        ShuffleQuestions();
    }

    private void ShuffleQuestions()
    {
        // Fisher-Yates shuffle algorithm
        for (int i = quizQuestions.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            QuizQuestion temp = quizQuestions[i];
            quizQuestions[i] = quizQuestions[j];
            quizQuestions[j] = temp;
        }
    }

    private void SetupButtons()
    {
        // Set up option buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                int optionIndex = i;
                optionButtons[i].onClick.RemoveAllListeners();
                optionButtons[i].onClick.AddListener(() => OnOptionSelected(optionIndex));
            }
        }

        // Set up finish button
        if (finishButton != null)
        {
            finishButton.onClick.RemoveAllListeners();
            finishButton.onClick.AddListener(CloseQuiz);
            finishButton.gameObject.SetActive(false);
        }
    }

    // This method is now directly connected to the start quiz button
    public void StartQuiz()
    {
        Debug.Log("Quiz start button pressed - starting quiz immediately");
        
        // Make sure Time is paused
        Time.timeScale = 0f;

        // Reset quiz state
        currentQuestionIndex = 0;
        quizScore = 0;
        
        // Hide elements in the game won panel except the panel itself
        if (environmentRegenerator != null && environmentRegenerator.gameWonPanel != null)
        {
            HideElementsInWonPanel();
        }
        
        // Show quiz panel
        if (quizPanel != null)
        {
            quizPanel.SetActive(true);
            
            // Make sure panel is at front
            Canvas quizCanvas = quizPanel.GetComponent<Canvas>();
            if (quizCanvas != null)
                quizCanvas.sortingOrder = 100;
        }
        
        // Make sure the start quiz button is hidden
        if (startQuizButton != null)
        {
            startQuizButton.gameObject.SetActive(false);
            Debug.Log("Explicitly disabling start quiz button in StartQuiz method");
        }
        
        // Display first question
        ShowCurrentQuestion();
        UpdateScoreDisplay();
        
        // Force UI update
        Canvas.ForceUpdateCanvases();
    }
    
    // Helper method to hide elements in the game won panel except the panel itself
    private void HideElementsInWonPanel()
    {
        if (environmentRegenerator != null && environmentRegenerator.gameWonPanel != null)
        {
            // Get all immediate children of the game won panel
            for (int i = 0; i < environmentRegenerator.gameWonPanel.transform.childCount; i++)
            {
                GameObject child = environmentRegenerator.gameWonPanel.transform.GetChild(i).gameObject;
                
                // Check if this is the start quiz button - keep it visible
                if (startQuizButton != null && 
                    (child == startQuizButton.gameObject || 
                     child.GetComponentInChildren<Button>() == startQuizButton))
                {
                    // Keep the start button visible
                    continue;
                }
                
                // Hide all other elements
                child.SetActive(false);
            }
            
            // Keep the panel itself active
            environmentRegenerator.gameWonPanel.SetActive(true);
        }
    }

    private void ShowCurrentQuestion()
    {
        // Check if we're out of questions or reached the limit
        if (currentQuestionIndex >= quizQuestions.Count || currentQuestionIndex >= questionsPerQuiz)
        {
            FinishQuiz();
            return;
        }
        
        // Reset button interactability
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
            {
                optionButtons[i].interactable = true;
                optionButtons[i].gameObject.SetActive(true);
            }
        }
        
        // Allow answering again
        canAnswer = true;

        // Get the current question
        QuizQuestion currentQuestion = quizQuestions[currentQuestionIndex];
        
        // Format question with options included
        string formattedQuestion = $"Question {currentQuestionIndex + 1}/{questionsPerQuiz}:\n\n{currentQuestion.question}\n\n";
        
        // Add options directly to the question text
        for (int i = 0; i < currentQuestion.options.Length; i++)
        {
            formattedQuestion += $"{currentQuestion.options[i]}\n";
        }
        
        // Display the question with options
        if (questionText != null)
        {
            questionText.text = formattedQuestion;
            questionText.gameObject.SetActive(true);
            
            // Log for debugging
            Debug.Log($"Question set: {formattedQuestion}");
        }
        else
        {
            Debug.LogError("Question Text component is null!");
        }
        
        // We don't need separate feedback text anymore
        if (feedbackText != null)
        {
            feedbackText.gameObject.SetActive(false);
        }
        
        // Force UI update
        Canvas.ForceUpdateCanvases();
    }

    private void OnOptionSelected(int selectedIndex)
    {
        // Ignore if can't answer or invalid state
        if (!canAnswer || currentQuestionIndex >= quizQuestions.Count || currentQuestionIndex >= questionsPerQuiz)
        {
            Debug.LogWarning("OnOptionSelected called but canAnswer is false or index out of range");
            return;
        }

        // Prevent multiple answers
        canAnswer = false;

        // Disable all buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
                optionButtons[i].interactable = false;
        }

        // Get current question
        QuizQuestion currentQuestion = quizQuestions[currentQuestionIndex];
        bool isCorrect = (selectedIndex == currentQuestion.correctOptionIndex);
        
        // Log for debugging
        Debug.Log($"Option selected: {selectedIndex}, Correct index: {currentQuestion.correctOptionIndex}, IsCorrect: {isCorrect}");

        // Get the current question text to append feedback
        string currentQuestionText = questionText.text;
        
        // Handle correct/incorrect answer
        if (isCorrect)
        {
            // Increase score
            quizScore += pointsPerCorrectAnswer;
            UpdateScoreDisplay();
            
            // Show feedback in question text area
        if (questionText != null)
        {
                questionText.text = currentQuestionText + "\n\n<color=green>Correct!</color>";
            }
        }
        else
        {
            // Show feedback for incorrect answer
            if (questionText != null)
            {
                questionText.text = currentQuestionText + "\n\n<color=red>Incorrect!</color> The correct answer is: " + 
                    currentQuestion.options[currentQuestion.correctOptionIndex];
            }
        }
        
        // Force UI update immediately
        Canvas.ForceUpdateCanvases();
        
        // Clear any existing coroutines
        if (this.isActiveAndEnabled)
        {
            StopAllCoroutines();
            StartCoroutine(MoveToNextQuestion(2.5f)); // Longer delay to read feedback
        }
    }

    private IEnumerator MoveToNextQuestion(float delay)
    {
        // Wait for the specified delay to show feedback
        yield return new WaitForSecondsRealtime(delay);
        
        // Check if this was the last question
        if (currentQuestionIndex >= questionsPerQuiz - 1 || 
            currentQuestionIndex >= quizQuestions.Count - 1)
        {
            // This was the last question, finish quiz
            FinishQuiz();
        }
        else
        {
            // Add a transitional message
            if (questionText != null)
            {
                questionText.text = "Loading next question...";
                
                // Short delay before showing the next question
                yield return new WaitForSecondsRealtime(1.0f);
            }
            
            // Move to next question
            currentQuestionIndex++;
            
            // Log for debugging
            Debug.Log($"Moving to question {currentQuestionIndex + 1}");
            
            // Show next question
            ShowCurrentQuestion();
            
            // Force UI update
            Canvas.ForceUpdateCanvases();
        }
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {quizScore}";
        }
    }

    private void FinishQuiz()
    {
        // Show quiz results
        if (questionText != null)
        {
            questionText.text = "Quiz Completed!";
        }

        // Show final score and message in the question text area
        if (questionText != null)
        {
            float percentage = (float)quizScore / (questionsPerQuiz * pointsPerCorrectAnswer) * 100;
            string message;
            
            if (percentage >= 80)
                message = $"\n\nExcellent! You scored {quizScore} points.\nYou're a water conservation expert!";
            else if (percentage >= 60)
                message = $"\n\nGood job! You scored {quizScore} points.\nYou know your water conservation facts!";
            else
                message = $"\n\nYou scored {quizScore} points.\nKeep learning about water conservation!";
                
            // Append the message to the existing text
            questionText.text += message;
            
            // Hide the separate feedback text if it exists
            if (feedbackText != null)
                feedbackText.gameObject.SetActive(false);
        }
        
        // Hide option buttons
        for (int i = 0; i < optionButtons.Length; i++)
        {
            if (optionButtons[i] != null)
                optionButtons[i].gameObject.SetActive(false);
        }
        
        // Show finish button
        if (finishButton != null)
        {
            finishButton.gameObject.SetActive(true);
            
            // Update button text
            TMP_Text buttonText = finishButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
                buttonText.text = "Return to Game";
        }
        
        // Add quiz score to the game score BEFORE updating the server
        if (environmentRegenerator != null)
        {
            // Get current game score before adding quiz score
            int previousScore = environmentRegenerator.score;
            
            // Add quiz score to the total score
            environmentRegenerator.score += quizScore;
            
            // Update the score UI immediately
            environmentRegenerator.UpdateScoreUI();
            
            // Get updated game score
            int newTotalScore = environmentRegenerator.score;
            
            Debug.Log($"Quiz score: {quizScore}, Previous game score: {previousScore}, New total score: {newTotalScore}");
            
            // Check if this is a new high score
            int highScore = PlayerPrefs.GetInt("HighScore", 0);
            if (newTotalScore > highScore)
            {
                Debug.Log($"New high score achieved: {newTotalScore}. Updating server and local high score.");
                
                // Update local high score
                PlayerPrefs.SetInt("HighScore", newTotalScore);
                PlayerPrefs.Save();
            }
            
            // Always update the score on the server when quiz is finished, regardless of high score
            Debug.Log($"Updating total score on server after quiz completion: {newTotalScore}");
            environmentRegenerator.UpdateScoreOnServer();
        }
        
        // Start the coroutine to show leaderboard after a delay
        if (environmentRegenerator != null)
        {
            StartCoroutine(ShowLeaderboardAfterDelay(3.0f));
        }
    }
    
    private IEnumerator ShowLeaderboardAfterDelay(float delay)
    {
        // Wait for the specified delay to show quiz completion message
        yield return new WaitForSecondsRealtime(delay);
        
        if (questionText != null)
        {
            questionText.text = "Loading Leaderboard...";
            yield return new WaitForSecondsRealtime(1.0f);
        }
        
        // Only proceed if we have the environment regenerator
        if (environmentRegenerator != null)
        {
            // First refresh the leaderboard
            environmentRegenerator.RefreshLeaderboard();
            
            // Then wait a bit for the refresh to complete
            yield return new WaitForSecondsRealtime(1.0f);
            
            // Get leaderboard text from either the game won panel or main leaderboard
            string leaderboardContent = "";
            
            if (environmentRegenerator.gameWonLeaderboardText != null)
            {
                leaderboardContent = environmentRegenerator.gameWonLeaderboardText.text;
            }
            else if (environmentRegenerator.leaderboardTextPanel != null)
            {
                leaderboardContent = environmentRegenerator.leaderboardTextPanel.text;
            }
            
            // Hide question text and show leaderboard text instead
            if (questionText != null)
            {
                questionText.gameObject.SetActive(false);
            }
            
            // Show the separate leaderboard text area
            if (leaderboardText != null)
            {
                leaderboardText.gameObject.SetActive(true);
                
                // Display leaderboard content in the dedicated leaderboard text area
                if (!string.IsNullOrEmpty(leaderboardContent))
                {
                    leaderboardText.text = leaderboardContent;
                }
                else
                {
                    // Fallback if we couldn't get the leaderboard text
                    leaderboardText.text = "Leaderboard refreshing...\n\nYour final quiz score: " + quizScore + 
                        "\n\nCheck the main leaderboard for rankings!";
                }
            }
            else
            {
                // Fallback to using question text if leaderboard text component not found
                if (questionText != null)
                {
                    questionText.gameObject.SetActive(true);
                    
                    if (!string.IsNullOrEmpty(leaderboardContent))
                    {
                        questionText.text = "Leaderboard:\n\n" + leaderboardContent;
                    }
                    else
                    {
                        questionText.text = "Leaderboard refreshing...\n\nYour final quiz score: " + quizScore + 
                            "\n\nCheck the main leaderboard for rankings!";
                    }
                }
            }
        }
    }

    public void CloseQuiz()
    {
        // Hide quiz panel
        if (quizPanel != null)
            quizPanel.SetActive(false);

        // Show game over panel again with all elements
        if (environmentRegenerator != null && environmentRegenerator.gameWonPanel != null)
        {
            environmentRegenerator.gameWonPanel.SetActive(true);
            
            // Reactivate all children
            for (int i = 0; i < environmentRegenerator.gameWonPanel.transform.childCount; i++)
            {
                environmentRegenerator.gameWonPanel.transform.GetChild(i).gameObject.SetActive(true);
            }
        }
            
        // Time scale remains 0 until player restarts or returns to menu
    }
    
    // Public method that can be called to manually force display of the first question
    public void ForceDisplayFirstQuestion()
    {
        // Only do this if quiz panel is active
        if (quizPanel != null && quizPanel.activeSelf)
        {
            Debug.Log("Force displaying first question");
            
            // Reset question index
            currentQuestionIndex = 0;
            
            // Show question
            ShowCurrentQuestion();
            
            // Force UI update
            Canvas.ForceUpdateCanvases();
        }
    }
} 