using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ChestNavigationPath : MonoBehaviour
{
    public TreasureChest TreasureChestHandler; // Reference to ChestHandler
    public NavMeshAgent playerAgent; // Assign the player's NavMeshAgent
    public WaterTapManager waterTapManager; // Reference to WaterTapManager
    public GameObject arrowPrefab; // Assign an arrow prefab to indicate direction
    public float arrowSpacing = 2f; // Distance between arrows
    public Vector3 arrowRotationOffset = Vector3.zero; // Custom rotation offset (Editable in Inspector)
    public float arrowHeightOffset = 0.5f; // Adjust the height of the arrows (Editable in Inspector)

    private List<GameObject> arrows = new List<GameObject>();
    private NavMeshPath navPath;
    private bool pathGenerated = false;
    private GameObject selectedChest; // Store selected chest

    void Start()
    {
        TreasureChestHandler = GameObject.FindGameObjectWithTag("TreasureChest").GetComponent<TreasureChest>();
        navPath = new NavMeshPath();
        
    }

    void Update()
    {
        if (pathGenerated)
        {
            UpdatePath();
        }
    }

    public void GeneratePath()
    {
        if (TreasureChestHandler != null)
        {
            selectedChest = TreasureChestHandler.selectedChest; // Get the random chest
        }
        if (selectedChest == null)
        {
            Debug.LogError("No chest selected! Make sure ChestHandler is assigned.");
        }
        if (selectedChest == null)
        {
            Debug.LogWarning("No chest available to navigate to.");
            return;
        }

        if (playerAgent != null)
        {
            Debug.Log("Generating path to chest...");
            NavMesh.CalculatePath(playerAgent.transform.position, selectedChest.transform.position, NavMesh.AllAreas, navPath);

            if (navPath.status == NavMeshPathStatus.PathComplete)
            {
                pathGenerated = true;
                PlaceArrows(navPath);
                Debug.Log("Path generated with " + navPath.corners.Length + " corners.");
            }
            else
            {
                Debug.LogWarning("Path could not be generated.");
            }
        }
    }

    void UpdatePath()
    {
        if (selectedChest == null) return;

        NavMesh.CalculatePath(playerAgent.transform.position, selectedChest.transform.position, NavMesh.AllAreas, navPath);
        if (navPath.status == NavMeshPathStatus.PathComplete)
        {
            PlaceArrows(navPath);
        }
    }

    void PlaceArrows(NavMeshPath path)
    {
        // Remove old arrows
        foreach (GameObject arrow in arrows)
        {
            Destroy(arrow);
        }
        arrows.Clear();

        for (int i = 0; i < path.corners.Length - 1; i++)
        {
            float distance = Vector3.Distance(path.corners[i], path.corners[i + 1]);
            int arrowCount = Mathf.FloorToInt(distance / arrowSpacing);

            for (int j = 1; j <= arrowCount; j++)
            {
                Vector3 position = Vector3.Lerp(path.corners[i], path.corners[i + 1], (float)j / arrowCount);
                position.y += arrowHeightOffset; // Apply height adjustment

                Vector3 direction = (path.corners[i + 1] - path.corners[i]).normalized; // Get direction vector

                Quaternion rotation = Quaternion.LookRotation(direction, Vector3.up); // Set correct arrow rotation
                
                // Apply custom rotation offset from Inspector
                rotation *= Quaternion.Euler(arrowRotationOffset);

                GameObject arrow = Instantiate(arrowPrefab, position, rotation);
                arrows.Add(arrow);
            }
        }
        Debug.Log("Arrows placed along the updated path with correct rotation and height offset.");
    }
}
