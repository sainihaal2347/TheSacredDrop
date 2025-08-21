using UnityEngine;

public class LoadingSpinner : MonoBehaviour
{
    public float rotationSpeed = 200f; // Degrees per second
    public bool clockwise = true;
    
    private RectTransform rectTransform;
    
    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (rectTransform == null)
        {
            Debug.LogWarning("LoadingSpinner should be attached to a UI element with a RectTransform");
            enabled = false;
        }
    }
    
    void Update()
    {
        if (rectTransform != null)
        {
            float direction = clockwise ? -1f : 1f;
            rectTransform.Rotate(0, 0, direction * rotationSpeed * Time.unscaledDeltaTime);
        }
    }
} 