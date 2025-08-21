using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Networking;
using Newtonsoft.Json;

public class TreasureChest : MonoBehaviour
{
    [Header("Chest Settings")]
    public List<GameObject> chests; // Assign in Inspector
    public GameObject selectedChest;
    public Animator chestAnimator;
    public ChestHandler chestHandler;

    [Header("UI Elements")]
    public GameObject panelEventHandler;
    public Text questionText;
    public Text helpText;
    public Image[] questionDots; // Assign in Inspector

    public Image fadePanel; // Assign your UI Panel's Image in Inspector
    public float fadeDuration = 3f;
    private bool isPlayerNear = false;
    private string correctAnswer = "Yes";
    private int currentQuestionIndex = 0;
    private int wrongAnswers = 0;
    private const int totalQuestions = 5;
    private const int maxWrongAnswers = 3;
    public SceneTransition st;
    private List<QuestionData> questionsList = new List<QuestionData>();

    [Header("API Settings")]
    private string geminiEndpoint = "https://generativelanguage.googleapis.com/v1beta/models/gemini-2.0-flash:generateContent?key=";
    private string geminiApiKey = "AIzaSyC_uXWIh-_J0xOZq9MGtS9pMy5a0zEZXNo";

    void Start()
    {
        SelectRandomChest();
        // FadeOut("1stTo2nd");
        StartCoroutine(FetchQuestionsFromGemini());
    }

    void SelectRandomChest()
    {
        if (chests.Count == 0)
        {
            Debug.LogError("No chests assigned!");
            return;
        }

        int randomIndex = Random.Range(0, chests.Count);
        selectedChest = chests[randomIndex];

        foreach (GameObject chest in chests)
        {
            chest.SetActive(chest == selectedChest);
        }

        chestAnimator = selectedChest.GetComponent<Animator>();
        if (chestAnimator == null)
        {
            Debug.LogError("No Animator component found on selected chest!");
        }
    }

    IEnumerator FetchQuestionsFromGemini()
    {
        string prompt = "Generate 5 yes/no simple new questions about daily water conservation. Respond in JSON format: { \"questions\": [ { \"question\": \"Your question here\", \"answer\": \"Yes or No\" }, ... ] }";

        var requestBody = new
        {
            contents = new[]
            {
                new
                {
                    parts = new[]
                    {
                        new { text = prompt }
                    }
                }
            }
        };

        string jsonData = JsonConvert.SerializeObject(requestBody);

        using (UnityWebRequest request = new UnityWebRequest(geminiEndpoint + geminiApiKey, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("Gemini API Response: " + request.downloadHandler.text);
                ParseGeminiResponse(request.downloadHandler.text);
            }
            else
            {
                Debug.LogError("Gemini API Error: " + request.error);
                LoadFallbackQuestions();
            }
        }
    }

    void ParseGeminiResponse(string jsonResponse)
    {
        try
        {
            if (string.IsNullOrEmpty(jsonResponse))
            {
                Debug.LogError("Gemini response is empty or null.");
                LoadFallbackQuestions();
                return;
            }

            var responseDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonResponse);
            if (responseDict.ContainsKey("candidates"))
            {
                var candidates = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(responseDict["candidates"].ToString());

                if (candidates.Count > 0 && candidates[0].ContainsKey("content"))
                {
                    var content = JsonConvert.DeserializeObject<Dictionary<string, object>>(candidates[0]["content"].ToString());

                    if (content.ContainsKey("parts"))
                    {
                        var parts = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(content["parts"].ToString());

                        if (parts.Count > 0 && parts[0].ContainsKey("text"))
                        {
                            string questionsJson = parts[0]["text"].ToString();
                            questionsJson = questionsJson.Replace("```json", "").Replace("```", "").Trim();

                            var questionData = JsonConvert.DeserializeObject<QuestionListData>(questionsJson);
                            if (questionData != null && questionData.questions.Count > 0)
                            {
                                questionsList = questionData.questions;
                                ShowNextQuestion();
                                return;
                            }
                        }
                    }
                }
            }

            Debug.LogError("Failed to extract valid questions from Gemini response.");
            LoadFallbackQuestions();
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing Gemini response: " + e.Message);
            LoadFallbackQuestions();
        }
    }

    void LoadFallbackQuestions()
    {
        Debug.LogWarning("Loading fallback questions...");

        questionsList = new List<QuestionData>
        {
            new QuestionData { question = "Is it okay to water your lawn every day?", answer = "No" },
            new QuestionData { question = "Can you reuse bathwater to water plants?", answer = "Yes" },
            new QuestionData { question = "Is it necessary to flush the toilet every time you use it?", answer = "No" },
            new QuestionData { question = "Should you leave the water running while washing dishes?", answer = "No" },
            new QuestionData { question = "Does taking shorter showers help conserve water?", answer = "Yes" }
        };

        ShowNextQuestion();
    }

    void ShowNextQuestion()
    {
        if (currentQuestionIndex < totalQuestions)
        {
            questionText.text = questionsList[currentQuestionIndex].question;
            correctAnswer = questionsList[currentQuestionIndex].answer;
            helpText.text = "";
        }
        else
        {
            OpenChest();
        }
    }

    public void UpdateAnsYes() => EvaluateAnswer("Yes");
    public void UpdateAnsNo() => EvaluateAnswer("No");

    void EvaluateAnswer(string playerAnswer)
    { 
        questionDots[currentQuestionIndex].color = (playerAnswer == correctAnswer) ? Color.green : Color.red;

        if (playerAnswer != correctAnswer)
            wrongAnswers++;

        currentQuestionIndex++;

        if (wrongAnswers >= maxWrongAnswers)
            gameRestart();
        else
            ShowNextQuestion();
    }

    void OpenChest()
    {
        chestAnimator.SetBool("chestOpen", true);
        panelEventHandler?.SetActive(false);
        
        // Destroy the selected chest after 5 seconds
        StartCoroutine(DestroyChestAfterDelay(2f));
    }

    IEnumerator DestroyChestAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (selectedChest != null)
        {
            Destroy(selectedChest);
            FadeOut("1stTo2nd");
            Debug.Log("Selected chest destroyed.");
        }
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

    public void gameRestart()
    {
        SceneManager.LoadScene("Mysterious Dungeon");
    }

    [System.Serializable]
    public class QuestionData
    {
        public string question;
        public string answer;
    }

    [System.Serializable]
    public class QuestionListData
    {
        public List<QuestionData> questions;
    }
}
