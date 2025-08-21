using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody), typeof(Animator), typeof(CapsuleCollider))]
public class PlayerJoystick : MonoBehaviour
{
    [SerializeField] private Rigidbody playerrigidbody;
    [SerializeField] private FixedJoystick joystick;
    [SerializeField] private Transform cameraHolder;
    [SerializeField] private float minPitch = -30f;
    [SerializeField] private float maxPitch = 60f;
    [SerializeField] private float pitchSpeed = 100f;

    [Header("Touch Sensitivity")]
    [SerializeField] private float rotationSensitivity = 2.5f; // Add sensitivity multiplier
    [SerializeField] private float rotationSmoothing = 15f;    // Higher = smoother rotation

    private float pitch = 0f;

    [SerializeField] private Animator animator;
    [SerializeField] private CapsuleCollider playerCollider;

    [SerializeField] private float movementSpeed = 5f;
    [SerializeField] private float acceleration = 8f;
    [SerializeField] private float deceleration = 10f;
    [SerializeField] private float rotationSpeed = 150f;
    [SerializeField] private float jumpForce = 8f;
    [SerializeField] private float slideDuration = 0.5f;
    [SerializeField] private float slideSpeedMultiplier = 1.5f;

    [Header("UI Buttons")]
    [SerializeField] private Button jumpButton;
    [SerializeField] private Button slideButton;

    private Vector3 velocity = Vector3.zero;
    private bool isSliding = false;
    private bool isJumping = false;
    private float rotationY = 0f;
    private int rotationFingerId = -1;

    private float originalColliderHeight;
    private Vector3 originalColliderCenter;
    private Vector3 originalScale;
    
    // For smooth rotation
    private Quaternion targetRotation;

    void Start()
    {
        playerrigidbody.freezeRotation = true;
        originalScale = transform.localScale;

        originalColliderHeight = playerCollider.height;
        originalColliderCenter = playerCollider.center;

        if (jumpButton != null) jumpButton.onClick.AddListener(Jump);
        if (slideButton != null) slideButton.onClick.AddListener(StartSlide);

        targetRotation = transform.rotation;
    }

    void FixedUpdate()
    {
        MovePlayer();
    }

    void Update()
    {
        HandleMultiTouchRotation();
        
        // Apply smooth rotation every frame
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSmoothing);
    }

    private void MovePlayer()
    {
        if (isSliding) return;

        Vector3 moveDirection = new Vector3(joystick.Horizontal, 0, joystick.Vertical).normalized;

        if (moveDirection.magnitude >= 0.1f)
        {
            Vector3 move = transform.right * moveDirection.x + transform.forward * moveDirection.z;

            velocity = Vector3.Lerp(velocity, move * movementSpeed, Time.deltaTime * acceleration);
            playerrigidbody.velocity = new Vector3(velocity.x, playerrigidbody.velocity.y, velocity.z);

            animator.SetBool("running", true);
        }
        else
        {
            velocity = Vector3.Lerp(velocity, Vector3.zero, Time.deltaTime * deceleration);
            playerrigidbody.velocity = new Vector3(velocity.x, playerrigidbody.velocity.y, velocity.z);

            animator.SetBool("running", false);
        }
    }

    private void HandleMultiTouchRotation()
    {
        // Handle keyboard rotation in Editor/PC for testing
        #if UNITY_EDITOR || UNITY_STANDALONE
        if (Input.GetKey(KeyCode.Q))
        {
            rotationY -= rotationSpeed * Time.deltaTime;
            targetRotation = Quaternion.Euler(0, rotationY, 0);
        }
        if (Input.GetKey(KeyCode.E))
        {
            rotationY += rotationSpeed * Time.deltaTime;
            targetRotation = Quaternion.Euler(0, rotationY, 0);
        }
        if (Input.GetKey(KeyCode.R))
        {
            pitch = Mathf.Clamp(pitch + pitchSpeed * Time.deltaTime, minPitch, maxPitch);
            if (cameraHolder != null) cameraHolder.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        if (Input.GetKey(KeyCode.F))
        {
            pitch = Mathf.Clamp(pitch - pitchSpeed * Time.deltaTime, minPitch, maxPitch);
            if (cameraHolder != null) cameraHolder.localRotation = Quaternion.Euler(pitch, 0, 0);
        }
        #endif

        // Handle touch input
        for (int i = 0; i < Input.touchCount; i++)
        {
            Touch touch = Input.GetTouch(i);

            // Only use touches on the right side of the screen
            if (touch.position.x > Screen.width / 2)
            {
                // Initialize or continue tracking this finger
                if (rotationFingerId == -1 || rotationFingerId == touch.fingerId)
                {
                    rotationFingerId = touch.fingerId;

                    // Only process movement
                    if (touch.phase == TouchPhase.Moved)
                    {
                        // Apply sensitivity to horizontal rotation
                        float horizontalDelta = touch.deltaPosition.x * rotationSensitivity;
                        rotationY += horizontalDelta * rotationSpeed / Screen.width;

                        // Vertical camera pitch with improved sensitivity
                        float verticalDelta = touch.deltaPosition.y * rotationSensitivity;
                        pitch -= verticalDelta * pitchSpeed / Screen.height;
                        pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

                        // Set target rotation (will be smoothed in Update)
                        targetRotation = Quaternion.Euler(0, rotationY, 0);

                        // Update camera pitch immediately
                        if (cameraHolder != null)
                        {
                            cameraHolder.localRotation = Quaternion.Euler(pitch, 0, 0);
                        }
                    }
                    
                    // Reset finger tracking when touch ends
                    if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                    {
                        if (touch.fingerId == rotationFingerId)
                        {
                            rotationFingerId = -1;
                        }
                    }
                }
            }
        }

        // Reset rotation finger if no touches are detected
        if (Input.touchCount == 0)
        {
            rotationFingerId = -1;
        }
    }

    public void Jump()
    {
        if (!isJumping)
        {
            isJumping = true;
            animator.SetTrigger("jump");

            playerrigidbody.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            playerCollider.center += new Vector3(0, 0.8f, 0);

            StartCoroutine(ResetJump());
        }
    }

    private IEnumerator ResetJump()
    {
        yield return new WaitForSeconds(0.8f);

        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;

        isJumping = false;
    }

    public void StartSlide()
    {
        if (!isSliding)
        {
            StartCoroutine(Slide());
        }
    }

    private IEnumerator Slide()
    {
        isSliding = true;

        playerCollider.height /= 2;
        playerCollider.center -= new Vector3(0, 0.2f, 0);
        transform.localScale = new Vector3(originalScale.x, originalScale.y / 2, originalScale.z);

        yield return new WaitForSeconds(slideDuration);

        transform.localScale = originalScale;
        playerCollider.height = originalColliderHeight;
        playerCollider.center = originalColliderCenter;

        isSliding = false;
    }
}
