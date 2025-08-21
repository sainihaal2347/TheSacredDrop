using System.Collections;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;

public class GameWonLeaderboard : MonoBehaviour
{
    public TMP_Text leaderboardText;
    private string apiUrl = "https://serverforgame-thesacreddrop.onrender.com/api/leaderboard";
    
    void OnEnable()
    {
        // Automatically update when panel becomes active
        Debug.Log("Game Won panel enabled, updating leaderboard");
        StartCoroutine(FetchAndDisplayLeaderboard());
    }
    
    public void RefreshLeaderboard()
    {
        Debug.Log("Refresh button clicked on Game Won panel");
        StartCoroutine(FetchAndDisplayLeaderboard());
    }
    
    IEnumerator FetchAndDisplayLeaderboard()
    {
        Debug.Log("Fetching leaderboard data for Game Won...");
        
        // Find the leaderboard text if not assigned
        if (leaderboardText == null)
        {
            leaderboardText = GetComponentInChildren<TMP_Text>();
            if (leaderboardText == null)
            {
                Debug.LogError("No TextMeshPro component found in game won panel!");
                yield break;
            }
            else
            {
                Debug.Log("Found TextMeshPro in Game Won panel: " + leaderboardText.name);
            }
        }
        
        // Set initial text
        leaderboardText.text = "Loading leaderboard...";
        
        // Fetch leaderboard data
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard: " + request.error);
            leaderboardText.text = "Failed to load leaderboard. Please try again.";
            yield break;
        }
        
        string jsonResponse = request.downloadHandler.text;
        Debug.Log("Received leaderboard data: " + jsonResponse);
        
        // Format leaderboard data
        string formattedLeaderboard = FormatLeaderboardData(jsonResponse);
        
        // Update UI
        leaderboardText.text = formattedLeaderboard;
        Debug.Log("Game Won leaderboard updated successfully");
    }
    
    private string FormatLeaderboardData(string jsonData)
    {
        if (string.IsNullOrEmpty(jsonData))
            return "No leaderboard data available";
            
        try
        {
            // Parse the JSON array
            string wrappedJson = "{\"entries\":" + jsonData + "}";
            EnvironmentRegenerator.LeaderboardData leaderboardData = 
                JsonUtility.FromJson<EnvironmentRegenerator.LeaderboardData>(wrappedJson);
                
            if (leaderboardData == null || leaderboardData.entries == null || leaderboardData.entries.Count == 0)
                return "No scores on the leaderboard yet";
                
            // Sort entries by score
            leaderboardData.entries.Sort((a, b) => b.score.CompareTo(a.score));
            
            // Format the leaderboard text
            string result = "Congratulations! You Won!\nLeaderboard:\n";
            
            // Get current player ID
            string playerId = PlayerPrefs.GetString("PlayerId", "");
            
            // Display up to 10 entries
            int count = Mathf.Min(10, leaderboardData.entries.Count);
            for (int i = 0; i < count; i++)
            {
                var entry = leaderboardData.entries[i];
                string prefix = "";
                
                // Highlight current player
                if (!string.IsNullOrEmpty(playerId) && entry._id == playerId)
                    prefix = "► ";
                    
                result += $"{prefix}{i+1}. {entry.name}: {entry.score}\n";
            }
            
            return result;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error parsing leaderboard data: " + e.Message);
            return "Error loading leaderboard";
        }
    }
} 