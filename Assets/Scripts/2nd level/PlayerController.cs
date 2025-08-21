using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public AudioSource runningAudioSource; // Separate AudioSource for running sound

    public float laneSwitchSpeed = 10f; // Speed for switching lanes
    public float jumpForce = 200f;      // Jump force
    public float gravity = 40f;         // Custom gravity
    public float fastFallMultiplier = 2f; // Multiplier for fast fall speed
    
    [Header("Jump Physics")]
    public float fallMultiplier = 2.5f;  // Stronger gravity when falling
    public float lowJumpMultiplier = 2f; // Stronger gravity when releasing jump early
    public float jumpApexThreshold = 10f; // Velocity below which we're considered at the apex of a jump
    public float coyoteTime = 0.1f;      // Time window where player can still jump after leaving ground
    
    public float crouchDuration = 0.5f; // Duration for crouching

    private Rigidbody rb;
    private Animator animator;
    private int currentLane = 1;        // Start in the middle lane
    private Vector3 targetPosition;
    private bool isJumping = false;
    private bool wasJumping = false;    // Track the previous jumping state
    private bool isCrouching = false;
    private float lastGroundedTime;     // Track time since last grounded
    private float jumpStartHeight;      // Starting height of jump
    public GameObject gameOverScreen;

    public AudioSource audioSource;
    public AudioClip runningSFX;
    public AudioClip jumpSFX;
    public AudioClip laneChangeSFX;
    public AudioClip collideSFX;

    public EnvironmentRegenerator environmentRegenerator;

    // Lane positions (X values)
    private float[] lanePositions = { -2280f, -1977f, -1645f };

    private Vector2 touchStartPos;
    private bool swipeDetected = false;
    private float minSwipeDistance = 50f; // Minimum swipe distance in pixels
    
    [SerializeField] private float groundLevel = 3790f; // Y position of the ground

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        animator = GetComponent<Animator>();
        targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
        lastGroundedTime = Time.time;
        
        // Debug check for environmentRegenerator reference
        if (environmentRegenerator == null)
        {
            Debug.LogError("Warning: environmentRegenerator is not assigned in the PlayerController! Game over functionality will not work correctly.");
            
            // Try to find it in the scene if it's not assigned
            environmentRegenerator = FindObjectOfType<EnvironmentRegenerator>();
            if (environmentRegenerator != null)
            {
                Debug.Log("Found EnvironmentRegenerator in scene and assigned it automatically.");
            }
        }
        else
        {
            Debug.Log("EnvironmentRegenerator reference is properly assigned in PlayerController.");
        }
    }

    void Update()
    {
        HandleInput();
        ApplyRealisticGravity();
        CheckJumpState();

        // Smooth lane transition
        transform.position = new Vector3(
            Mathf.Lerp(transform.position.x, targetPosition.x, laneSwitchSpeed * Time.deltaTime),
            transform.position.y,
            transform.position.z
        );
    }

    void HandleInput()
    {
        // Handle Swipe Controls
        HandleSwipeInput();

        // Handle Keyboard Controls (for PC)
        if (Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.LeftArrow))
        {
            MoveLane(-1);
        }
        else if (Input.GetKeyDown(KeyCode.D) || Input.GetKeyDown(KeyCode.RightArrow))
        {
            MoveLane(1);
        }
        if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.UpArrow))
        {
            Jump();
        }
        if (Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.DownArrow))
        {
            CrouchOrFastFall();
        }
    }

    void HandleSwipeInput()
    {
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                touchStartPos = touch.position;
                swipeDetected = false;
            }
            else if (touch.phase == TouchPhase.Moved)
            {
                if (!swipeDetected)
                {
                    Vector2 touchEndPos = touch.position;
                    Vector2 swipeDelta = touchEndPos - touchStartPos;

                    if (swipeDelta.magnitude > minSwipeDistance)
                    {
                        swipeDetected = true;
                        if (Mathf.Abs(swipeDelta.x) > Mathf.Abs(swipeDelta.y)) // Horizontal swipe
                        {
                            if (swipeDelta.x > 0)
                                MoveLane(1);  // Swipe Right → Move Right
                            else
                                MoveLane(-1); // Swipe Left → Move Left
                        }
                        else // Vertical swipe
                        {
                            if (swipeDelta.y > 0)
                                Jump(); // Swipe Up → Jump
                            else
                                CrouchOrFastFall(); // Swipe Down → Fast Fall or Crouch
                        }
                    }
                }
            }
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("respawnEnvironment")){
            environmentRegenerator.SpawnEnvironment();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacles"))
        {
            if (collideSFX != null)
                audioSource.PlayOneShot(collideSFX, 0.7f);
            runningAudioSource.Stop();
            
            // Call the EnvironmentRegenerator's GameOver method instead of directly showing the game over screen
            if (environmentRegenerator != null)
            {
                environmentRegenerator.GameOver();
                Debug.Log("Player collision with obstacle - Called EnvironmentRegenerator.GameOver()");
            }
            else
            {
                Debug.LogError("EnvironmentRegenerator reference is missing on PlayerController!");
                
                // Fallback to directly showing game over screen if environmentRegenerator reference is missing
                gameOverScreen.SetActive(true);
                Time.timeScale = 0f;
            }
        }
    }

    public void gameRestart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    void MoveLane(int direction)
    {
        int targetLane = Mathf.Clamp(currentLane + direction, 0, 2);
        if (targetLane != currentLane)
        {
            currentLane = targetLane;
            targetPosition = new Vector3(lanePositions[currentLane], transform.position.y, transform.position.z);
            
            // Play lane change sound
            if (laneChangeSFX != null)
                audioSource.PlayOneShot(laneChangeSFX, 0.7f);
        }
    }

    void Jump()
    {
        // Allow jump if grounded or in coyote time
        if (!isJumping && Time.time - lastGroundedTime <= coyoteTime)
        {
            // Stop running sound
            runningAudioSource.Stop();
            
            // Store jump start height
            jumpStartHeight = transform.position.y;
            
            // Apply jump force
            rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z); // Cancel any downward velocity
            rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
            
            // Set animation
            animator.SetTrigger("Jump");
            isJumping = true;

            // Play jump sound
            if (jumpSFX != null)
                audioSource.PlayOneShot(jumpSFX, 1f);
        }
    }

    void CheckJumpState()
    {
        // Check if player just landed
        if (wasJumping && !isJumping)
        {
            // Player has just landed
            animator.SetTrigger("Land");
            
            // Resume running sound after landing
            if (!runningAudioSource.isPlaying)
                runningAudioSource.Play();
        }
        
        // Remember current jumping state for next frame
        wasJumping = isJumping;
        
        // Check if we're grounded
        if (transform.position.y <= groundLevel)
        {
            lastGroundedTime = Time.time;
            
            // Clamp position to ground level to prevent sinking
            if (transform.position.y < groundLevel)
            {
                transform.position = new Vector3(
                    transform.position.x,
                    groundLevel,
                    transform.position.z
                );
            }
            
            // Reset vertical velocity when grounded
            if (rb.velocity.y < 0)
            {
                rb.velocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            }
            
            if (isJumping)
            {
                isJumping = false;
            }
        }
    }

    void CrouchOrFastFall()
    {
        if (isJumping)
        {
            // Fast fall if in the air - apply immediate downward velocity for responsiveness
            rb.velocity = new Vector3(rb.velocity.x, -gravity * fastFallMultiplier * 0.1f, rb.velocity.z);
        }
        else if (!isCrouching)
        {
            // Crouch if on the ground
            StartCoroutine(Crouch());
        }
    }

    System.Collections.IEnumerator Crouch()
    {
        isCrouching = true;
        animator.SetBool("Crouch", true);
        yield return new WaitForSeconds(crouchDuration);
        animator.SetBool("Crouch", false);
        isCrouching = false;
    }

    void ApplyRealisticGravity()
    {
        // Only apply gravity when not grounded
        if (transform.position.y > groundLevel)
        {
            float gravityMultiplier = 1f;
            
            // Apply stronger gravity when falling
            if (rb.velocity.y < 0)
            {
                // Falling - increase gravity
                gravityMultiplier = fallMultiplier;
            }
            else if (rb.velocity.y > 0)
            {
                // Rising, but less force is being applied (player released button)
                if (Mathf.Abs(rb.velocity.y) < jumpApexThreshold)
                {
                    gravityMultiplier = lowJumpMultiplier;
                }
            }
            
            // Apply calculated gravity
            rb.velocity += Vector3.down * gravity * gravityMultiplier * Time.deltaTime;
            
            // Clamp maximum falling speed for better feel
            float maxFallSpeed = -gravity * 0.5f;
            if (rb.velocity.y < maxFallSpeed)
            {
                rb.velocity = new Vector3(rb.velocity.x, maxFallSpeed, rb.velocity.z);
            }
        }
    }
}
