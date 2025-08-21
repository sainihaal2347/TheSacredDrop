using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EnvironmentRegenerator : MonoBehaviour
{
    // --- Public Fields for Game Logic ---
    public GameObject environmentPrefab;
    public GameObject waterDropPrefab;
    public Transform spawnPoint;
    public Transform player;
    public int winScoreThreshold = 20;      // Score needed to win the game

    public TMP_Text scoreText;              // Displays game score
    public TMP_Text highScoreText;          // Displays high score
    public TMP_Text gameOverLeaderboardText; // TMP text in game over panel for leaderboard
    public TMP_Text gameWonLeaderboardText;  // TMP text in game won panel for leaderboard
    public TMP_Text leaderboardTextPanel;   // Displays leaderboard (top 5 entries)

    [Header("Obstacle & Drop Settings")]
    public float obstacleSpawnInterval = 2f;
    public float initialSpeed = 5f;
    public float speedIncreaseRate = 0.5f;
    public float speedIncreaseInterval = 10f;
    public float destroyThreshold = -10f;
    public float dropHeight = 3825f;
    public float arcHeight = 3855f;
    public int numArcDrops = 7;
    public int numStraightDrops = 5;
    public float dropSpacing = 4f;
    public Vector3 dropRotationOffset = new Vector3(0f, 0f, 0f);
    public float[] lanePositions = { -2280f, -1977f, -1645f };
    public Vector3 dropColliderSize = new Vector3(0.5f, 0.5f, 0.5f);
    public float dropSpawnProbability = 0.5f;

    [System.Serializable]
    public struct ObstacleData
    {
        public GameObject obstaclePrefab;
        public Vector3 positionOffset;
        public Vector3 rotation;
        public bool hasArcDrops;
        public int lane;
    }
    public List<ObstacleData> obstaclesData = new List<ObstacleData>();
    public float minObstacleSpawnInterval = 1.5f;
    public float maxObstacleSpawnInterval = 3.5f;

    // --- UI Panels for Game Over & Name Prompt ---
    [Header("UI Panels")]
    public GameObject gameOverPanel;      // Panel that is shown on Game Over
    public GameObject gameWonPanel;       // Panel that is shown when player wins (score >= 200)
    public GameObject namePromptPanel;    // Panel with input for the player's name

    [Header("Quiz Integration")]
    public MonoBehaviour waterConservationQuizComponent;  // Reference to the quiz component
    public Button startQuizButton;                        // Button to start the quiz from game won panel

    // --- Private Variables ---
    private List<GameObject> activeSections = new List<GameObject>();
    private List<GameObject> activeObstacles = new List<GameObject>();
    private List<GameObject> activeDrops = new List<GameObject>();

    private float currentSpeed;
    private float speedTimer = 0f;
    public int score = 0;
    public int highScore;
    private bool isGameOver = false; // Flag to track if game over has been triggered
    private bool isGameWon = false;  // Flag to track if game won has been triggered

    // --- Leaderboard Server Settings ---
    private const string nameKey = "PlayerName";
    // Using your provided API endpoint URL.
    private const string apiUrl = "https://serverforgame-thesacreddrop.onrender.com/api/leaderboard";

    // Data structure matching a leaderboard entry.
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string _id; // MongoDB document ID
        public string name;
        public int score;
    }

    [System.Serializable]
    public class ScoreSubmission
    {
        public string name;
        public int score;
        public string playerId; // Optional, used to update existing scores
    }

    // Wrapper class for JsonUtility.
    [System.Serializable]
    public class LeaderboardData
    {
        public List<LeaderboardEntry> entries;
    }

    private bool isSubmittingScore = false;

    void Start()
    {
        // Load saved high score.
        highScore = PlayerPrefs.GetInt("HighScore", 0);
        Debug.Log("Loaded High Score: " + highScore);
        UpdateScoreUI();
        UpdateHighScoreUI();

        // Start spawning obstacles.
        StartCoroutine(SpawnObstacles());

        // Hide panels initially.
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);
        if (gameWonPanel != null)
            gameWonPanel.SetActive(false);
        if (namePromptPanel != null)
            namePromptPanel.SetActive(false);
            
        // Find leaderboard text components if not assigned
        FindLeaderboardTextComponents();

        currentSpeed = initialSpeed;
        
        // Log UI references to debug
        CheckUIReferences();
    }

    /// <summary>
    /// Find and cache leaderboard text components in panels if not already assigned
    /// </summary>
    void FindLeaderboardTextComponents()
    {
        // Try to find game over leaderboard text
        if (gameOverLeaderboardText == null && gameOverPanel != null)
        {
            TMP_Text[] textComponents = gameOverPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in textComponents)
            {
                if (text.name.ToLower().Contains("leaderboard") || 
                    text.name.ToLower().Contains("score"))
                {
                    gameOverLeaderboardText = text;
                    Debug.Log("Found game over leaderboard text: " + text.name);
                    break;
                }
            }
            
            // If no matching text found, use the first TMP_Text component
            if (gameOverLeaderboardText == null && textComponents.Length > 0)
            {
                gameOverLeaderboardText = textComponents[0];
                Debug.Log("Using first text component as game over leaderboard: " + textComponents[0].name);
            }
        }
        
        // Try to find game won leaderboard text
        if (gameWonLeaderboardText == null && gameWonPanel != null)
        {
            TMP_Text[] textComponents = gameWonPanel.GetComponentsInChildren<TMP_Text>(true);
            foreach (TMP_Text text in textComponents)
            {
                if (text.name.ToLower().Contains("leaderboard") || 
                    text.name.ToLower().Contains("score"))
                {
                    gameWonLeaderboardText = text;
                    Debug.Log("Found game won leaderboard text: " + text.name);
                    break;
                }
            }
            
            // If no matching text found, use the first TMP_Text component
            if (gameWonLeaderboardText == null && textComponents.Length > 0)
            {
                gameWonLeaderboardText = textComponents[0];
                Debug.Log("Using first text component as game won leaderboard: " + textComponents[0].name);
            }
        }
    }

    // Debug method to check UI references
    void CheckUIReferences()
    {
        Debug.Log("Checking UI References:");
        Debug.Log("gameOverPanel: " + (gameOverPanel != null ? "Assigned" : "NULL"));
        Debug.Log("gameWonPanel: " + (gameWonPanel != null ? "Assigned" : "NULL"));
        Debug.Log("gameOverLeaderboardText: " + (gameOverLeaderboardText != null ? "Assigned" : "NULL"));
        Debug.Log("gameWonLeaderboardText: " + (gameWonLeaderboardText != null ? "Assigned" : "NULL"));
        Debug.Log("leaderboardTextPanel: " + (leaderboardTextPanel != null ? "Assigned" : "NULL"));
    }

    void Update()
    {
        // Skip game logic if game is over or won
        if (isGameOver || isGameWon)
            return;
            
        // Increase speed after each interval.
        if (speedTimer < speedIncreaseInterval)
            speedTimer += Time.deltaTime;
        else
        {
            currentSpeed += speedIncreaseRate;
            speedTimer = 0f;
            Debug.Log("Increased currentSpeed to: " + currentSpeed);
        }

        MoveEnvironment();
        MoveObstacles();
        MoveWaterDrops();

        // Check for win condition
        if (score >= winScoreThreshold)
        {
            GameWon();
        }

        // For testing, press 'G' to trigger Game Over.
        if (Input.GetKeyDown(KeyCode.G))
        {
            GameOver();
        }
        
        // For testing, press 'W' to trigger Game Won.
        if (Input.GetKeyDown(KeyCode.W))
        {
            GameWon();
        }
    }

    // --- Spawning, Movement, and Object Management ---

    public void SpawnEnvironment()
    {
        GameObject newSection = Instantiate(environmentPrefab, transform.position, Quaternion.identity);
        activeSections.Add(newSection);
        Debug.Log("Spawned new environment section.");
    }

    void MoveEnvironment()
    {
        foreach (var section in activeSections)
        {
            section.transform.position += Vector3.back * currentSpeed * Time.deltaTime;
        }

        if (activeSections.Count > 0 && activeSections[0].transform.position.z < destroyThreshold)
        {
            Debug.Log("Destroying an environment section.");
            Destroy(activeSections[0]);
            activeSections.RemoveAt(0);
        }
    }

    IEnumerator SpawnObstacles()
    {
        while (true)
        {
            float spawnDelay = Random.Range(minObstacleSpawnInterval, maxObstacleSpawnInterval);
            yield return new WaitForSeconds(spawnDelay);

            if (obstaclesData.Count == 0)
                continue;

            ObstacleData selectedObstacle = obstaclesData[Random.Range(0, obstaclesData.Count)];
            Vector3 obstaclePosition = spawnPoint.position + selectedObstacle.positionOffset;

            GameObject newObstacle = Instantiate(
                selectedObstacle.obstaclePrefab,
                obstaclePosition,
                Quaternion.Euler(selectedObstacle.rotation)
            );
            activeObstacles.Add(newObstacle);

            Debug.Log("Spawned an obstacle at " + obstaclePosition);

            if (Random.value < dropSpawnProbability)
            {
                StartCoroutine(SpawnDropsForObstacle(newObstacle, selectedObstacle));
            }
        }
    }

    IEnumerator SpawnDropsForObstacle(GameObject obstacle, ObstacleData data)
    {
        yield return new WaitForSeconds(Random.Range(0.3f, 0.8f));

        Vector3 obsPos = obstacle.transform.position;
        if (data.lane < 0 || data.lane >= lanePositions.Length)
            yield break;

        if (data.hasArcDrops)
        {
            Debug.Log("Generating arc drops for obstacle at " + obsPos);
            GenerateArcDrops(obsPos, data.lane);
        }
        else
        {
            Debug.Log("Generating straight drops for obstacle at " + obsPos);
            GenerateStraightDrops(obsPos, data.lane);
        }
    }

    void GenerateStraightDrops(Vector3 startPosition, int lane)
    {
        for (int i = 0; i < numStraightDrops; i++)
        {
            Vector3 dropPos = new Vector3(
                lanePositions[lane],
                dropHeight,
                startPosition.z + (i * dropSpacing)
            );
            if (!IsDropPositionOccupied(dropPos))
            {
                GameObject drop = Instantiate(waterDropPrefab, dropPos, Quaternion.Euler(dropRotationOffset));
                drop.tag = "WaterDrop";
                activeDrops.Add(drop);
                Debug.Log("Spawned straight drop at " + dropPos);
            }
        }
    }

    void GenerateArcDrops(Vector3 obstaclePosition, int lane)
    {
        float startZ = obstaclePosition.z - 600f;
        float endZ = obstaclePosition.z + 600f;

        for (int i = 0; i < numArcDrops; i++)
        {
            float t = (float)i / (numArcDrops - 1);
            float zPos = Mathf.Lerp(startZ, endZ, t);
            float height = Mathf.Lerp(dropHeight, arcHeight, Mathf.Sin(t * Mathf.PI));

            Vector3 dropPos = new Vector3(
                lanePositions[lane],
                height,
                zPos
            );
            if (!IsDropPositionOccupied(dropPos))
            {
                GameObject drop = Instantiate(waterDropPrefab, dropPos, Quaternion.Euler(dropRotationOffset));
                drop.tag = "WaterDrop";
                activeDrops.Add(drop);
                Debug.Log("Spawned arc drop at " + dropPos);
            }
        }
    }

    bool IsDropPositionOccupied(Vector3 pos)
    {
        Collider[] hits = Physics.OverlapBox(pos, dropColliderSize * 0.5f, Quaternion.identity);
        foreach (Collider hit in hits)
        {
            if (hit.CompareTag("Obstacles"))
                return true;
        }
        return false;
    }

    void MoveWaterDrops()
    {
        for (int i = activeDrops.Count - 1; i >= 0; i--)
        {
            GameObject drop = activeDrops[i];
            if (drop != null)
        {
            drop.transform.position += Vector3.back * currentSpeed * Time.deltaTime;
            if (drop.transform.position.z < destroyThreshold)
                {
                Destroy(drop);
                    activeDrops.RemoveAt(i);
                    Debug.Log("Destroyed water drop.");
                }
            }
        }
    }

    void MoveObstacles()
    {
        for (int i = activeObstacles.Count - 1; i >= 0; i--)
        {
            GameObject obstacle = activeObstacles[i];
            if (obstacle != null)
        {
            obstacle.transform.position += Vector3.back * currentSpeed * Time.deltaTime;
                if (obstacle.transform.position.z < destroyThreshold)
                {
                    Destroy(obstacle);
                    activeObstacles.RemoveAt(i);
                    Debug.Log("Destroyed obstacle.");
                }
            }
        }
    }

    // --- Score Management ---
    public void IncreaseScore(int amount)
    {
        score += amount;
        Debug.Log("Score increased by " + amount + ", new score: " + score);
        UpdateScoreUI();

        if (score > highScore)
        {
            highScore = score;
            PlayerPrefs.SetInt("HighScore", highScore);
            UpdateHighScoreUI();
            Debug.Log("New high score: " + highScore);
        }
    }

    public void UpdateScoreUI()
    {
        if (scoreText)
            scoreText.text = "Score: " + score;
    }

    void UpdateHighScoreUI()
    {
        if (highScoreText)
            highScoreText.text = "High Score: " + highScore;
    }

    // --- Game Over and Leaderboard Handling ---
    public void GameOver()
    {
        if (isGameOver)
            return;

        isGameOver = true;
        isGameWon = false; // Make sure game won state is reset
        Time.timeScale = 0f;
        
        Debug.Log("Game Over triggered. Score: " + score);
        
        // Make sure all panels are initially closed
        if (gameWonPanel != null && gameWonPanel.activeSelf)
        {
            Debug.Log("Deactivating game won panel to prevent conflict");
            gameWonPanel.SetActive(false);
        }
        
        if (namePromptPanel != null && namePromptPanel.activeSelf)
        {
            Debug.Log("Deactivating name prompt panel that was already active");
            namePromptPanel.SetActive(false);
        }
        
        StartCoroutine(HandleGameOver());
    }

    // Update the existing HandleGameOver to use the new method
    IEnumerator HandleGameOver()
    {
        string playerName = PlayerPrefs.GetString(nameKey, "");
        Debug.Log("HandleGameOver - Player name from PlayerPrefs: '" + playerName + "'");

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Player name not found in PlayerPrefs for Game Over. Showing name prompt panel.");
            
            // Hide game over panel if it's active
            if (gameOverPanel != null)
            {
                if (gameOverPanel.activeSelf)
                {
                    gameOverPanel.SetActive(false);
                    Debug.Log("Deactivated game over panel");
                }
            }
                
            // Show name prompt panel
            PromptForPlayerName();
            
            // Since we're prompting for name, we should not continue this coroutine
            yield break;
        }
        else
        {
            Debug.Log("Player name found: '" + playerName + "'. Showing game over panel.");
            
            // Ensure name prompt panel is hidden
            if (namePromptPanel != null && namePromptPanel.activeSelf)
            {
                namePromptPanel.SetActive(false);
                Debug.Log("Deactivated name prompt panel");
            }
            
            // Show game over panel
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                Debug.Log("Activated game over panel");
            }
            else
            {
                Debug.LogError("Game over panel is null! Cannot show it.");
            }
                
            Debug.Log("Submitting game over score: " + score);
            
            // Check if we're already submitting a score
            if (!isSubmittingScore)
            {
                yield return StartCoroutine(SendScoreToServerAndRefreshLeaderboards(playerName, score));
            }
            else
            {
                Debug.LogWarning("Score submission already in progress, not starting another one from HandleGameOver");
                yield return StartCoroutine(GetAndShowLeaderboard());
                
                // Still try to refresh any GameOverLeaderboard components
                GameOverLeaderboard[] leaderboardComponents = FindObjectsOfType<GameOverLeaderboard>(true);
                if (leaderboardComponents != null && leaderboardComponents.Length > 0)
                {
                    foreach (GameOverLeaderboard board in leaderboardComponents)
                    {
                        board.RefreshLeaderboard();
                    }
                }
            }
        }
    }

    /// <summary>
    /// Enables the UI panel to prompt the player for their name.
    /// </summary>
    void PromptForPlayerName()
    {
        if (namePromptPanel != null)
        {
            namePromptPanel.SetActive(true);
            Debug.Log("Name prompt panel activated.");
        }
        else
        {
            Debug.LogError("Name prompt panel not assigned!");
        }
    }

    /// <summary>
    /// Called from the name prompt UI's submit button.
    /// </summary>
    /// <param name="playerName">The player's name entered in the UI.</param>
    public void SetPlayerNameAndSend(string playerName)
    {
        Debug.Log("SetPlayerNameAndSend called with: '" + playerName + "'");
        
        // Name should already be validated by the NamePromptUI component
        // but we'll double check here just to be safe
        if (string.IsNullOrWhiteSpace(playerName))
        {
            Debug.LogWarning("Attempted to submit empty player name");
            return;
        }

        // Start coroutine to check for duplicate name before saving
        StartCoroutine(CheckNameAndProceed(playerName));
    }

    // Coroutine to check if the name exists on the server
    private IEnumerator CheckNameAndProceed(string playerName)
    {
        // Fetch leaderboard from server
        UnityEngine.Networking.UnityWebRequest request = UnityEngine.Networking.UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityEngine.Networking.UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard for name check. Error: " + request.error);
            yield break;
        }

        string jsonResponse = request.downloadHandler.text;
        string wrappedJson = "{\"entries\":" + jsonResponse + "}";
        LeaderboardData leaderboardData = JsonUtility.FromJson<LeaderboardData>(wrappedJson);
        bool nameExists = false;
        string foundObjectId = null;
        if (leaderboardData != null && leaderboardData.entries != null)
        {
            foreach (LeaderboardEntry entry in leaderboardData.entries)
            {
                if (entry.name == playerName)
                {
                    nameExists = true;
                    foundObjectId = entry._id;
                    break;
                }
            }
        }

        if (nameExists)
        {
            // Store the objectid in PlayerPrefs for future updates
            if (!string.IsNullOrEmpty(foundObjectId))
            {
                PlayerPrefs.SetString("PlayerId", foundObjectId);
                PlayerPrefs.Save();
                Debug.Log("Stored existing objectid for player: " + foundObjectId);
            }
            Debug.LogWarning("Player name already exists. Please choose another name.");
            // TODO: Show UI message to user to pick another name
            if (namePromptPanel != null)
            {
                namePromptPanel.SetActive(true);
                // Optionally, set a TMP_Text in the panel to show the error
            }
            yield break;
        }

        // Store the player name
        PlayerPrefs.SetString(nameKey, playerName);
        PlayerPrefs.Save();
        Debug.Log("Saved player name to PlayerPrefs: '" + playerName + "'");

        // Hide name input panel
        if (namePromptPanel != null)
        {
            namePromptPanel.SetActive(false);
            Debug.Log("Deactivated name prompt panel");
        }
        else
        {
            Debug.LogWarning("namePromptPanel is null when trying to hide it");
        }
            
        // Show the appropriate panel based on game state
        if (isGameWon)
        {
            Debug.Log("Game is in won state, activating game won panel");
            if (gameWonPanel != null)
            {
                gameWonPanel.SetActive(true);
                Debug.Log("Activated game won panel");
                
                // Start the quiz directly instead of setting up button
                SetupQuizButton();
            }
            else
            {
                Debug.LogError("gameWonPanel is null when trying to show it");
            }
        }
        else // Default to game over
        {
            Debug.Log("Game is in game over state, activating game over panel");
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(true);
                Debug.Log("Activated game over panel");
            }
            else
            {
                Debug.LogError("gameOverPanel is null when trying to show it");
            }
        }

        // Check if we're already submitting a score
        if (isSubmittingScore)
        {
            Debug.LogWarning("Score submission already in progress, not starting another one");
            yield break;
        }

        Debug.Log("Starting score submission and leaderboard update");
        // Submit score and update leaderboard
        StartCoroutine(SendScoreToServerAndRefreshLeaderboards(playerName, score));
    }
    
    /// <summary>
    /// Submit score and ensure all leaderboards get refreshed
    /// </summary>
    private IEnumerator SendScoreToServerAndRefreshLeaderboards(string playerName, int scoreToSend)
    {
        // First submit the score
        yield return StartCoroutine(SendScoreToServer(playerName, scoreToSend));
        
        // Then refresh our own leaderboard
        yield return StartCoroutine(GetAndShowLeaderboard());
        
        // Finally, find and refresh any GameOverLeaderboard components
        GameOverLeaderboard[] leaderboardComponents = FindObjectsOfType<GameOverLeaderboard>(true);
        if (leaderboardComponents != null && leaderboardComponents.Length > 0)
        {
            Debug.Log($"Found {leaderboardComponents.Length} GameOverLeaderboard components to refresh");
            foreach (GameOverLeaderboard board in leaderboardComponents)
            {
                board.RefreshLeaderboard();
            }
        }
        else
        {
            Debug.Log("No separate GameOverLeaderboard components found in the scene");
        }
    }

    /// <summary>
    /// Sends the player's score to the server.
    /// </summary>
    IEnumerator SendScoreToServer(string playerName, int scoreToSend)
    {
        // Prevent sending empty names or zero scores
        if (string.IsNullOrWhiteSpace(playerName) || scoreToSend <= 0)
        {
            Debug.LogWarning("Invalid data for leaderboard: Name=" + playerName + ", Score=" + scoreToSend);
            yield break;
        }
        
        // Check if we already have a player ID stored
        string playerId = PlayerPrefs.GetString("PlayerId", "");
        bool foundExistingPlayer = !string.IsNullOrEmpty(playerId);
        
        // If no playerId, try to find it by name
        if (!foundExistingPlayer)
        {
            Debug.Log("No existing player ID found, searching for player by name: " + playerName);
            // Fetch leaderboard from server
            UnityWebRequest getRequest = UnityWebRequest.Get(apiUrl);
            yield return getRequest.SendWebRequest();
            if (getRequest.result == UnityWebRequest.Result.Success)
            {
                string jsonResponse = getRequest.downloadHandler.text;
                string wrappedJson = "{\"entries\":" + jsonResponse + "}";
                LeaderboardData leaderboard = JsonUtility.FromJson<LeaderboardData>(wrappedJson);
                if (leaderboard != null && leaderboard.entries != null)
                {
                    foreach (var entry in leaderboard.entries)
                    {
                        if (entry.name == playerName && !string.IsNullOrEmpty(entry._id))
                        {
                            playerId = entry._id;
                            PlayerPrefs.SetString("PlayerId", playerId);
                            PlayerPrefs.Save();
                            foundExistingPlayer = true;
                            Debug.Log("Found playerId by name: " + playerId);
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            Debug.Log("Using existing player ID: " + playerId);
        }
        
        // Use a mutex to prevent concurrent submissions
        if (isSubmittingScore)
        {
            Debug.LogWarning("Already submitting a score, skipping duplicate submission");
            yield break;
        }
        
        isSubmittingScore = true;

        if (foundExistingPlayer)
        {
            // Use PUT to update existing player with ID in URL
            // FIXED FORMAT: PUT /api/leaderboard/[playerId] with { "score": scoreToSend }
            string requestUrl = apiUrl + "/" + playerId;
            Debug.Log($"Using PUT request to {requestUrl} with score {scoreToSend}");
            
            // Create simple payload with just the score
            string jsonData = "{\"score\":" + scoreToSend + "}";
            
            UnityWebRequest request = new UnityWebRequest(requestUrl, "PUT");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();
            
            isSubmittingScore = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to send score. Error: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
                Debug.LogError($"Request URL: {requestUrl}");
                Debug.LogError($"Request Data: {jsonData}");
            }
            else
            {
                Debug.Log("Score submitted successfully! Server Response: " + request.downloadHandler.text);
            }
        }
        else
        {
            // New player - use POST to create new entry with name and score
            ScoreSubmission submission = new ScoreSubmission
            {
                name = playerName,
                score = scoreToSend
            };

            string jsonData = JsonUtility.ToJson(submission);
            Debug.Log($"Using POST request to {apiUrl} for new player {playerName}");
            
            UnityWebRequest request = new UnityWebRequest(apiUrl, "POST");
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;

            yield return request.SendWebRequest();
            
            isSubmittingScore = false;

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError("Failed to send score for new player. Error: " + request.error);
                Debug.LogError("Response Code: " + request.responseCode);
            }
            else
            {
                Debug.Log("New player score submitted successfully! Server Response: " + request.downloadHandler.text);
                
                // Try to extract the player ID from the response if available
                try {
                    LeaderboardEntry response = JsonUtility.FromJson<LeaderboardEntry>(request.downloadHandler.text);
                    if (!string.IsNullOrEmpty(response._id)) {
                        PlayerPrefs.SetString("PlayerId", response._id);
                        PlayerPrefs.Save();
                        Debug.Log("Saved player ID: " + response._id);
                    }
                } 
                catch (System.Exception e) {
                    Debug.LogWarning("Could not parse player ID from response: " + e.Message);
                }
            }
        }
    }

    /// <summary>
    /// Explicitly update the leaderboard in the game over panel
    /// </summary>
    public void UpdateGameOverLeaderboard()
    {
        Debug.Log("Manually updating game over leaderboard");
        StartCoroutine(GetAndShowLeaderboard());
    }

    /// <summary>
    /// Fetches the leaderboard data from the server, wraps the raw JSON array, sorts it, and updates the UI.
    /// </summary>
    IEnumerator GetAndShowLeaderboard()
    {
        Debug.Log("Fetching leaderboard from: " + apiUrl);
        UnityWebRequest request = UnityWebRequest.Get(apiUrl);
        yield return request.SendWebRequest();

        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError("Failed to fetch leaderboard. Error: " + request.error);
        }
        else
        {
            string jsonResponse = request.downloadHandler.text;
            Debug.Log("Leaderboard raw JSON: " + jsonResponse);

            // Wrap the JSON array so we can parse it.
            string wrappedJson = "{\"entries\":" + jsonResponse + "}";
            Debug.Log("Wrapped JSON: " + wrappedJson);

            LeaderboardData leaderboardData = JsonUtility.FromJson<LeaderboardData>(wrappedJson);
            if (leaderboardData != null && leaderboardData.entries != null)
            {
                leaderboardData.entries.Sort((a, b) => b.score.CompareTo(a.score));
                int count = Mathf.Min(10, leaderboardData.entries.Count); // Show up to 10 entries
                
                string titleText = isGameWon ? 
                    "Congratulations! You Won!\nLeaderboard:" : 
                    "Game Over\nLeaderboard:";
                    
                string leaderboardText = titleText + "\n";
                
                // Get the player's ID if available
                string currentPlayerId = PlayerPrefs.GetString("PlayerId", "");
                
                for (int i = 0; i < count; i++)
                {
                    LeaderboardEntry entry = leaderboardData.entries[i];
                    string highlightPrefix = !string.IsNullOrEmpty(currentPlayerId) && 
                                            currentPlayerId == entry._id ? "► " : "";
                    leaderboardText += $"{highlightPrefix}{i+1}. {entry.name}: {entry.score}\n";
                }
                
                Debug.Log($"Leaderboard text generated: {leaderboardText}");
                Debug.Log($"Is game won: {isGameWon}");
                
                // Update all leaderboard displays with the text
                // This helps ensure at least one will work
                
                // First, try the specific panel based on game state
                if (isGameWon)
                {
                    if (gameWonLeaderboardText != null)
                    {
                        gameWonLeaderboardText.text = leaderboardText;
                        Debug.Log("Updated game won panel leaderboard: " + gameWonLeaderboardText.name);
                    }
                    else if (gameWonPanel != null)
                    {
                        // Find TextMeshPro in the panel
                        TMP_Text[] texts = gameWonPanel.GetComponentsInChildren<TMP_Text>(true);
                        if (texts.Length > 0)
                        {
                            texts[0].text = leaderboardText;
                            Debug.Log("Updated first text in game won panel: " + texts[0].name);
                        }
                    }
                }
                else // Game Over
                {
                    // Use a more selective approach to update only the leaderboard text in game over panel
                    // Don't overwrite all text elements
                    if (gameOverLeaderboardText != null)
                    {
                        gameOverLeaderboardText.text = leaderboardText;
                        Debug.Log("Updated game over leaderboard text selectively");
                    }
                    else
                    {
                        // Find a text element that likely contains leaderboard information
                        TMP_Text[] allTexts = gameOverPanel.GetComponentsInChildren<TMP_Text>(true);
                        bool found = false;
                        
                        // First try to find a text component with "leaderboard" or "score" in its name
                        foreach (TMP_Text text in allTexts)
                        {
                            if (text.name.ToLower().Contains("leaderboard") || 
                                text.name.ToLower().Contains("score"))
                            {
                                text.text = leaderboardText;
                                Debug.Log("Found and updated specific leaderboard text: " + text.name);
                                gameOverLeaderboardText = text; // Save for future use
                                found = true;
                                break;
                            }
                        }
                        
                        // If no specific text was found, update only the first text element
                        if (!found && allTexts.Length > 0)
                        {
                            allTexts[0].text = leaderboardText;
                            Debug.Log("Updated first text element in game over panel: " + allTexts[0].name);
                            gameOverLeaderboardText = allTexts[0]; // Save for future use
                        }
                    }
                }
                
                // Finally, update the main leaderboard if it exists
                if (leaderboardTextPanel != null)
                {
                    leaderboardTextPanel.text = leaderboardText;
                    Debug.Log("Updated main leaderboard text");
                }
            }
            else
            {
                Debug.LogError("Failed to parse leaderboard data. Check the API response format.");
            }
        }
    }

    // Save high score when the application quits.
    void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("HighScore", highScore);
        Debug.Log("Saved high score on exit: " + highScore);
    }

    public void GameWon()
    {
        if (isGameWon)
            return;

        isGameWon = true;
        isGameOver = false; // Make sure game over state is reset
        Time.timeScale = 0f;
        
        Debug.Log("Game Won triggered. Score: " + score);
        
        // Make sure all panels are initially closed
        if (gameOverPanel != null && gameOverPanel.activeSelf)
        {
            Debug.Log("Deactivating game over panel to prevent conflict");
            gameOverPanel.SetActive(false);
        }
        
        if (namePromptPanel != null && namePromptPanel.activeSelf)
        {
            Debug.Log("Deactivating name prompt panel that was already active");
            namePromptPanel.SetActive(false);
        }
        
        StartCoroutine(HandleGameWon());
    }

    IEnumerator HandleGameWon()
    {
        string playerName = PlayerPrefs.GetString(nameKey, "");

        if (string.IsNullOrEmpty(playerName))
        {
            Debug.Log("Player name not found in PlayerPrefs for Game Won.");
            // Hide game won panel if it's active
            if (gameWonPanel != null && gameWonPanel.activeSelf)
                gameWonPanel.SetActive(false);
                
            // Show name prompt panel
            PromptForPlayerName();
            yield break;
        }
        else
        {
            // Show game won panel
            if (gameWonPanel != null)
            {
                gameWonPanel.SetActive(true);
                
                // Set up quiz button if available
                SetupQuizButton();
            }
                
            Debug.Log("Player name found: " + playerName + ". Submitting winning score: " + score);
            
            // Check if we're already submitting a score
            if (!isSubmittingScore)
            {
                yield return StartCoroutine(SendScoreToServerAndRefreshLeaderboards(playerName, score));
            }
            else
            {
                Debug.LogWarning("Score submission already in progress, not starting another one from HandleGameWon");
                yield return StartCoroutine(GetAndShowLeaderboard());
                
                // Still try to refresh any GameOverLeaderboard components
                GameOverLeaderboard[] leaderboardComponents = FindObjectsOfType<GameOverLeaderboard>(true);
                if (leaderboardComponents != null && leaderboardComponents.Length > 0)
                {
                    foreach (GameOverLeaderboard board in leaderboardComponents)
                    {
                        board.RefreshLeaderboard();
                    }
                }
            }
        }
    }
    
    // Set up the quiz button in the game won panel
    private void SetupQuizButton()
    {
        Debug.Log("Setting up quiz button in game won panel");

        // Find the start quiz button if not already assigned
        if (startQuizButton == null && waterConservationQuizComponent != null)
        {
            WaterConservationQuiz quizComponent = waterConservationQuizComponent as WaterConservationQuiz;
            if (quizComponent != null && quizComponent.startQuizButton != null)
            {
                startQuizButton = quizComponent.startQuizButton;
                Debug.Log("Found start quiz button from quiz component");
            }
            else
            {
                // Try to find button in the game won panel
                if (gameWonPanel != null)
                {
                    Button[] buttons = gameWonPanel.GetComponentsInChildren<Button>(true);
                    foreach (Button btn in buttons)
                    {
                        TMP_Text btnText = btn.GetComponentInChildren<TMP_Text>();
                        if (btnText != null && 
                            (btnText.text.ToLower().Contains("quiz") || 
                             btnText.text.ToLower().Contains("start quiz")))
                        {
                            startQuizButton = btn;
                            Debug.Log("Found quiz button in game won panel: " + btn.name);
                            break;
                        }
                    }
                }
            }
        }

        // Make sure start quiz button is visible and active
        if (startQuizButton != null)
        {
            startQuizButton.gameObject.SetActive(true);
            
            // Make sure the button has text that says "Start Quiz"
            TMP_Text buttonText = startQuizButton.GetComponentInChildren<TMP_Text>();
            if (buttonText != null)
            {
                buttonText.text = "Start Quiz";
            }
            
            // Make sure the button's onClick event is set up
            if (waterConservationQuizComponent != null)
            {
                WaterConservationQuiz quizComponent = waterConservationQuizComponent as WaterConservationQuiz;
                if (quizComponent != null)
                {
                    // Clear existing listeners and set up the click handler
                    startQuizButton.onClick.RemoveAllListeners();
                    startQuizButton.onClick.AddListener(quizComponent.StartQuiz);
                    Debug.Log("Set up quiz button click handler successfully");
                }
            }
        }
        else
        {
            Debug.LogWarning("Start quiz button not found or assigned! Quiz may not be accessible.");
        }
    }

    /// <summary>
    /// Restarts the current scene/level
    /// </summary>
    public void RestartGame()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Reset flags
        isGameOver = false;
        isGameWon = false;
        isSubmittingScore = false;
        
        // Reload the current scene
        Scene currentScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(currentScene.buildIndex);
    }
    
    /// <summary>
    /// Returns to the main menu (first scene in build)
    /// </summary>
    public void ReturnToMainMenu()
    {
        // Reset time scale
        Time.timeScale = 1f;
        
        // Load the first scene (assumed to be main menu)
        SceneManager.LoadScene(0);
    }

    /// <summary>
    /// Public method to refresh the leaderboard display - can be called from UI buttons
    /// </summary>
    public void RefreshLeaderboard()
    {
        Debug.Log("Refresh leaderboard button clicked");
        
        // Always use GetAndShowLeaderboard for consistency
        StartCoroutine(GetAndShowLeaderboard());
        
        // Force to find text components if not already found
        if ((isGameWon && gameWonLeaderboardText == null) || 
            (!isGameWon && gameOverLeaderboardText == null))
        {
            FindLeaderboardTextComponents();
        }
    }
    
    /// <summary>
    /// Method to directly set leaderboard text - use for debugging
    /// </summary>
    public void SetLeaderboardTextDirectly(string text)
    {
        if (isGameWon && gameWonLeaderboardText != null)
        {
            gameWonLeaderboardText.text = text;
        }
        else if (gameOverLeaderboardText != null)
        {
            gameOverLeaderboardText.text = text;
        }
        else if (leaderboardTextPanel != null)
        {
            leaderboardTextPanel.text = text;
        }
        else
        {
            Debug.LogError("No leaderboard text components assigned!");
        }
    }

    // Debug function to reset player name
    void ResetPlayerName()
    {
        PlayerPrefs.DeleteKey(nameKey);
        PlayerPrefs.DeleteKey("PlayerId");
        PlayerPrefs.Save();
        Debug.Log("!!! RESET: Deleted player name and ID from PlayerPrefs !!!");
    }

    /// <summary>
    /// Public method for the WaterConservationQuiz to submit updated scores to the server
    /// </summary>
    public void UpdateScoreOnServer()
    {
        Debug.Log("UpdateScoreOnServer called with current score: " + score);
        
        // Get player name from PlayerPrefs
        string playerName = PlayerPrefs.GetString(nameKey, "");
        string playerId = PlayerPrefs.GetString("PlayerId", "");
        
        if (string.IsNullOrEmpty(playerName))
        {
            Debug.LogWarning("Cannot update score on server - no player name found in PlayerPrefs");
            return;
        }
        
        Debug.Log($"Updating score for player: {playerName}, ID: {playerId}, Score: {score}");
        
        // Use a more direct approach to update the server
        StartCoroutine(DirectScoreUpdateToServer(playerName, playerId, score));
    }
    
    /// <summary>
    /// Direct method to update score on server with explicit PUT request
    /// </summary>
    private IEnumerator DirectScoreUpdateToServer(string playerName, string playerId, int scoreToUpdate)
    {
        if (string.IsNullOrEmpty(playerId))
        {
            Debug.LogWarning("Cannot update score without player ID. Falling back to regular update method.");
            yield return StartCoroutine(SendScoreToServerAndRefreshLeaderboards(playerName, scoreToUpdate));
            yield break;
        }
        
        // FIXED FORMAT: PUT /api/leaderboard/[playerId] with { "score": scoreToUpdate }
        string requestUrl = apiUrl + "/" + playerId;
        
        // Create simple payload with just the score as confirmed by server format
        string jsonData = "{\"score\":" + scoreToUpdate + "}";
        
        Debug.Log($"Sending direct score update with PUT: Score {scoreToUpdate} to {requestUrl}");
        
        UnityWebRequest request = new UnityWebRequest(requestUrl, "PUT");
        byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        request.downloadHandler = new DownloadHandlerBuffer();
        request.SetRequestHeader("Content-Type", "application/json");
        request.timeout = 15; // Longer timeout
        
        yield return request.SendWebRequest();
        
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"Failed to update score on server. Error: {request.error}, Code: {request.responseCode}");
            Debug.LogError($"Request URL: {requestUrl}");
            Debug.LogError($"Request Data: {jsonData}");
            
            // No need for fallback mechanisms since we have the correct API format now
        }
        else
        {
            Debug.Log($"Score successfully updated! Server response: {request.downloadHandler.text}");
            
            // Refresh the leaderboard to show the updated score
            yield return StartCoroutine(GetAndShowLeaderboard());
            
            // Update any standalone leaderboard components
            GameOverLeaderboard[] leaderboardComponents = FindObjectsOfType<GameOverLeaderboard>(true);
            if (leaderboardComponents != null && leaderboardComponents.Length > 0)
            {
                foreach (GameOverLeaderboard board in leaderboardComponents)
                {
                    board.RefreshLeaderboard();
                }
            }
        }
    }
}
