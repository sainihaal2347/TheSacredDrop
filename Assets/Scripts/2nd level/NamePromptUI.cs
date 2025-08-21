using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.Networking;

public class NamePromptUI : MonoBehaviour
{
    public GameObject nameInputPanel;
    public TMP_InputField nameInputField;
    public Button submitButton;
    public TMP_Text errorText;
    public GameObject loadingIndicator; // Reference to loading spinner/indicator
    public EnvironmentRegenerator gameManager;
    
    private bool isCheckingName = false;

    void Start()
    {
        if (errorText != null)
            errorText.gameObject.SetActive(false);
        
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
        
        if (submitButton != null)
            submitButton.onClick.AddListener(OnSubmitButtonClicked);
            
        if (nameInputField != null)
            nameInputField.onValueChanged.AddListener(OnNameInputChanged);
    }

    public void ShowNameInput()
    {
        if (nameInputPanel != null)
        {
            nameInputPanel.SetActive(true);
            
            // Auto-focus the input field
            if (nameInputField != null)
            {
                nameInputField.text = "";
                nameInputField.Select();
                nameInputField.ActivateInputField();
            }
            
            if (errorText != null)
                errorText.gameObject.SetActive(false);
                
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
        }
    }

    void OnNameInputChanged(string value)
    {
        // Hide error message when user starts typing
        if (errorText != null && errorText.gameObject.activeSelf)
            errorText.gameObject.SetActive(false);
    }
    
    public void OnSubmitButtonClicked()
    {
        string enteredName = nameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(enteredName))
        {
            ShowError("Please enter your name");
            return;
        }
        
        if (enteredName.Length < 2)
        {
            ShowError("Name must be at least 2 characters");
            return;
        }
        
        // Disable submit button while checking
        if (submitButton != null)
            submitButton.interactable = false;
            
        // Show loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(true);
            
        // Hide any previous error
        if (errorText != null)
            errorText.gameObject.SetActive(false);
            
        // Check if name is already in use
        StartCoroutine(CheckNameAvailability(enteredName));
    }

    private void ShowError(string message)
    {
        if (errorText != null)
        {
            errorText.text = message;
            errorText.gameObject.SetActive(true);
        }
        
        // Hide loading indicator if showing
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
    }
    
    private IEnumerator CheckNameAvailability(string name)
    {
        if (isCheckingName)
            yield break;
            
        isCheckingName = true;
        
        string apiUrl = "https://serverforgame-thesacreddrop.onrender.com/api/leaderboard";
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        
        yield return request.SendWebRequest();
        
        bool nameExists = false;
        
        if (request.result == UnityWebRequest.Result.Success)
        {
            string jsonResponse = request.downloadHandler.text;
            
            // Check if name exists in the response
            if (!string.IsNullOrEmpty(jsonResponse))
            {
                // Wrap the JSON array for parsing
                string wrappedJson = "{\"entries\":" + jsonResponse + "}";
                EnvironmentRegenerator.LeaderboardData leaderboard = 
                    JsonUtility.FromJson<EnvironmentRegenerator.LeaderboardData>(wrappedJson);
                
                if (leaderboard != null && leaderboard.entries != null)
                {
                    foreach (var entry in leaderboard.entries)
                    {
                        if (entry.name.ToLower() == name.ToLower())
                        {
                            nameExists = true;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.LogError("Error checking name availability: " + request.error);
            ShowError("Error checking name. Please try again.");
            isCheckingName = false;
            
            // Re-enable submit button
            if (submitButton != null)
                submitButton.interactable = true;
                
            // Hide loading indicator
            if (loadingIndicator != null)
                loadingIndicator.SetActive(false);
                
            yield break;
        }
        
        // Re-enable submit button
        if (submitButton != null)
            submitButton.interactable = true;
            
        // Hide loading indicator
        if (loadingIndicator != null)
            loadingIndicator.SetActive(false);
            
        isCheckingName = false;
        
        if (nameExists)
        {
            ShowError("This name is already taken. Please choose another name.");
        }
        else
        {
            SubmitName(name);
        }
    }

    private void SubmitName(string enteredName)
    {
        if (gameManager != null)
        {
            gameManager.SetPlayerNameAndSend(enteredName);
        }
        else
        {
            Debug.LogError("Game Manager reference not set in NamePromptUI!");
        }
    }
}
