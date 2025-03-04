using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.UI;

public class VRPressureHandler : MonoBehaviour
{
    [Header("UI References")]
    public RectTransform pressureIndicator; // UI indicator showing pressure level
    public float indicatorStartY = 0f; // Manual override for starting Y position
    public bool forceStartPosition = true; // Force a specific start position

    [Header("Button Configuration")]
    public XRBaseInteractable button; // The XR interactable button for CPR
    public Transform buttonTransform; // Button's transform component
    public Rigidbody buttonRigidbody; // Button's rigidbody component
    public ConfigurableJoint buttonJoint; // Optional: configurable joint for better physics

    [Header("Pressure Settings")]
    [Tooltip("This will be set automatically on Start()")]
    public float restPositionY = 0f; // Y position when button is not pressed
    public float pressedPositionY = -0.05f; // Y position when button is fully pressed
    public float maxIndicatorYChange = 200f; // Maximum Y movement for the indicator
    public float amplificationFactor = 5f; // Amplify small button movements
    public float smoothingFactor = 15f; // Higher value = smoother movement
    public float returnSpeed = 5f; // Speed at which the button returns to its rest position
    public bool invertIndicatorMovement = false; // Invert direction of indicator movement

    [Header("Physics Settings")]
    public bool useDirectInteraction = true; // Use direct force application for more responsive interaction
    public float buttonSpringForce = 120f; // Spring force pushing button back to rest position
    public float buttonDamping = 12f; // Damping to prevent oscillation

    [Header("Visual Feedback")]
    public Renderer bodyRenderer; // Reference to the body mesh renderer
    public Color normalColor = Color.white; // Default color
    public Color insufficientColor = Color.yellow; // Insufficient pressure color
    public Color sufficientColor = Color.green; // Sufficient pressure color
    public Color excessiveColor = Color.red; // Excessive pressure color

    [Range(0f, 1f)]
    public float insufficientThreshold = 0.3f; // Below this is insufficient
    [Range(0f, 1f)]
    public float sufficientThreshold = 0.5f; // Above this is sufficient
    [Range(0f, 1f)]
    public float excessiveThreshold = 0.8f; // Above this is excessive

    [Header("Debug Options")]
    public bool showDebugInfo = true; // Show debug info on screen
    public bool logMovementValues = false; // Log movement values to console

    // Private variables
    private Vector2 initialIndicatorPosition; // Original position of the indicator
    private Material bodyMaterial; // Cached material for color changes
    private float currentPressDepth = 0f; // Current normalized press depth
    private Vector3 initialButtonPosition; // Store initial button position
    private bool isBeingInteracted = false; // Flag for active interaction
    private bool initialized = false; // Flag to ensure proper initialization
    private float lastButtonY; // Last recorded button Y position
    private bool hasLoggedInitialPositions = false; // Flag to log initial positions once

    void Awake()
    {
        initialIndicatorPosition = Vector2.zero;
        initialButtonPosition = Vector3.zero;
    }

    void Start()
    {
        // Delay initialization by one frame to ensure all components are loaded
        Invoke("Initialize", 0.1f);
    }

    void Initialize()
    {
        try
        {
            // Store initial button position and configure physics
            if (buttonTransform != null)
            {
                initialButtonPosition = buttonTransform.localPosition;
                restPositionY = initialButtonPosition.y;
                lastButtonY = restPositionY;

                if (buttonRigidbody != null)
                {
                    buttonRigidbody.isKinematic = false;
                    buttonRigidbody.useGravity = false;
                    buttonRigidbody.constraints = RigidbodyConstraints.FreezeRotation |
                                                 RigidbodyConstraints.FreezePositionX |
                                                 RigidbodyConstraints.FreezePositionZ;

                    // Configure joint if needed
                    if (buttonJoint == null && useDirectInteraction)
                    {
                        buttonJoint = buttonTransform.gameObject.AddComponent<ConfigurableJoint>();
                    }
                    if (buttonJoint != null) ConfigureButtonJoint();
                }
            }
            else
            {
                Debug.LogError("Button Transform not assigned!");
                return;
            }

            // Set up pressure indicator
            if (pressureIndicator != null)
            {
                // Get current position or use forced position
                if (forceStartPosition)
                {
                    initialIndicatorPosition = new Vector2(
                        pressureIndicator.anchoredPosition.x,
                        indicatorStartY
                    );
                    pressureIndicator.anchoredPosition = initialIndicatorPosition;
                }
                else
                {
                    initialIndicatorPosition = pressureIndicator.anchoredPosition;
                }

                Debug.Log($"Indicator initial position: {initialIndicatorPosition}");
            }
            else
            {
                Debug.LogError("Pressure Indicator not assigned!");
                return;
            }

            // Set up visual materials
            if (bodyRenderer != null)
            {
                bodyMaterial = bodyRenderer.material;
                bodyMaterial.color = normalColor;
            }

            // Set up interaction events
            if (button != null)
            {
                button.selectEntered.AddListener(OnButtonPressed);
                button.selectExited.AddListener(OnButtonReleased);
                button.hoverEntered.AddListener(OnButtonHovered);
            }
            else
            {
                Debug.LogError("XR Button Interactable not assigned!");
                return;
            }

            initialized = true;
            Debug.Log("VRPressureHandler initialized successfully");

            // Force an initial update to position indicator correctly
            currentPressDepth = 0;
            UpdateIndicatorPosition();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Initialization failed: {e.Message}\n{e.StackTrace}");
        }
    }

    private void ConfigureButtonJoint()
    {
        buttonJoint.anchor = Vector3.zero;
        buttonJoint.connectedAnchor = initialButtonPosition;
        buttonJoint.axis = Vector3.up;

        // Configure linear limits
        var limit = new SoftJointLimit
        {
            limit = Mathf.Abs(pressedPositionY - restPositionY)
        };
        buttonJoint.linearLimit = limit;

        // Configure Y-axis motion
        buttonJoint.xMotion = ConfigurableJointMotion.Locked;
        buttonJoint.yMotion = ConfigurableJointMotion.Limited;
        buttonJoint.zMotion = ConfigurableJointMotion.Locked;
        buttonJoint.angularXMotion = ConfigurableJointMotion.Locked;
        buttonJoint.angularYMotion = ConfigurableJointMotion.Locked;
        buttonJoint.angularZMotion = ConfigurableJointMotion.Locked;

        // Configure joint drive
        var drive = new JointDrive
        {
            positionSpring = buttonSpringForce,
            positionDamper = buttonDamping,
            maximumForce = 1000f
        };
        buttonJoint.yDrive = drive;
    }

    void Update()
    {
        if (!initialized) return;

        // Log initial positions once, after everything is surely initialized
        if (!hasLoggedInitialPositions && Time.timeSinceLevelLoad > 1f)
        {
            Debug.Log($"FINAL Button position: {buttonTransform.localPosition}, Rest Y: {restPositionY}");
            Debug.Log($"FINAL Indicator position: {pressureIndicator.anchoredPosition}, Initial: {initialIndicatorPosition}");
            hasLoggedInitialPositions = true;
        }

        TrackButtonPosition();
        UpdateIndicatorPosition();
        UpdateBodyColor();
    }

    void FixedUpdate()
    {
        if (!initialized) return;
        HandleButtonPhysics();
    }

    private void HandleButtonPhysics()
    {
        if (buttonRigidbody == null) return;

        // Constrain button movement
        var currentPos = buttonTransform.localPosition;

        // Prevent button from going beyond limits
        if (currentPos.y < pressedPositionY)
        {
            currentPos.y = pressedPositionY;
            buttonTransform.localPosition = currentPos;
            buttonRigidbody.linearVelocity = Vector3.zero;
        }
        else if (currentPos.y > restPositionY && !isBeingInteracted)
        {
            currentPos.y = restPositionY;
            buttonTransform.localPosition = currentPos;
            buttonRigidbody.linearVelocity = Vector3.zero;
        }

        // Apply return force when not interacting
        if (!isBeingInteracted && useDirectInteraction)
        {
            float distance = restPositionY - currentPos.y;
            float returnForce = distance * buttonSpringForce;

            // Only apply force if button isn't at rest position
            if (Mathf.Abs(distance) > 0.001f)
            {
                buttonRigidbody.AddForce(Vector3.up * returnForce, ForceMode.Acceleration);

                // Apply damping to prevent oscillation
                if (buttonRigidbody.linearVelocity.sqrMagnitude > 0.001f)
                {
                    Vector3 damping = -buttonRigidbody.linearVelocity * buttonDamping;
                    buttonRigidbody.AddForce(damping, ForceMode.Acceleration);
                }
            }
            else
            {
                // Ensure button stops completely at rest position
                buttonRigidbody.linearVelocity = Vector3.zero;
            }
        }
    }

    private void TrackButtonPosition()
    {
        if (buttonTransform == null) return;

        // Get current button position
        float currentY = buttonTransform.localPosition.y;

        // Check if position has changed significantly
        bool positionChanged = Mathf.Abs(currentY - lastButtonY) > 0.0005f;
        lastButtonY = currentY;

        // Calculate normalized press depth (0 = rest, 1 = fully pressed)
        float rawDepth = Mathf.InverseLerp(restPositionY, pressedPositionY, currentY);

        // Apply amplification and clamp to 0-1 range
        float targetDepth = Mathf.Clamp01(rawDepth * amplificationFactor);

        // Update current depth with either immediate or smooth change
        if (positionChanged || isBeingInteracted)
        {

            // More responsive during interaction or significant movement
            float lerpSpeed = isBeingInteracted ? 0.8f : Time.deltaTime * smoothingFactor;
            currentPressDepth = Mathf.Lerp(currentPressDepth, targetDepth, lerpSpeed);

            if (logMovementValues && positionChanged)
            {
                Debug.Log($"Button Y: {currentY:F4}, Raw depth: {rawDepth:F3}, Target: {targetDepth:F3}, Current: {currentPressDepth:F3}");
            }
        }
    }

    private void UpdateIndicatorPosition()
    {
        if (pressureIndicator == null) return;

        // Calculate target position based on pressure depth
        float targetY;

        if (invertIndicatorMovement)
        {
            // Move UP from initial position as button is pressed
            targetY = initialIndicatorPosition.y + (currentPressDepth * maxIndicatorYChange);
        }
        else
        {
            // Move DOWN from initial position as button is pressed
            targetY = initialIndicatorPosition.y - (currentPressDepth * maxIndicatorYChange);
        }

        // Get current position and update Y value
        Vector2 newPosition = pressureIndicator.anchoredPosition;

        // Apply either immediate update (when interacting) or smooth transition
        if (isBeingInteracted)
        {
            // More responsive during interaction
            newPosition.y = Mathf.Lerp(newPosition.y, targetY, 0.5f);
        }
        else
        {
            // Smooth transition when not interacting
            newPosition.y = Mathf.Lerp(newPosition.y, targetY, Time.deltaTime * smoothingFactor);
        }

        // Apply the new position
        pressureIndicator.anchoredPosition = newPosition;

        if (logMovementValues && Time.frameCount % 30 == 0)
        {
            Debug.Log($"Indicator: initialY={initialIndicatorPosition.y:F1}, " +
                      $"targetY={targetY:F1}, currentY={newPosition.y:F1}, " +
                      $"depth={currentPressDepth:F3}");
        }
    }

    private void UpdateBodyColor()
    {
        if (bodyMaterial == null || bodyRenderer == null) return;

        // Apply color based on pressure thresholds
        if (currentPressDepth >= excessiveThreshold)
        {
            bodyMaterial.color = excessiveColor; // Too much pressure (red)
        }
        else if (currentPressDepth >= sufficientThreshold)
        {
            bodyMaterial.color = sufficientColor; // Correct pressure (green)
        }
        else if (currentPressDepth >= insufficientThreshold)
        {
            bodyMaterial.color = insufficientColor; // Not enough pressure (yellow)
        }
        else
        {
            bodyMaterial.color = normalColor; // No significant pressure
        }
    }

    private void OnButtonPressed(SelectEnterEventArgs args)
    {
        isBeingInteracted = true;
        Debug.Log("Button interaction started");
    }

    private void OnButtonReleased(SelectExitEventArgs args)
    {
        isBeingInteracted = false;
        Debug.Log("Button interaction ended");
    }

    private void OnButtonHovered(HoverEnterEventArgs args)
    {
        // Optional: Add hover effects or feedback here
    }

    public void ResetAll()
    {
        if (buttonRigidbody != null)
        {
            buttonRigidbody.linearVelocity = Vector3.zero;
            buttonTransform.localPosition = initialButtonPosition;
        }

        if (pressureIndicator != null)
        {
            pressureIndicator.anchoredPosition = initialIndicatorPosition;
        }

        if (bodyMaterial != null)
        {
            bodyMaterial.color = normalColor;
        }

        currentPressDepth = 0f;
        isBeingInteracted = false;
    }

    void OnDestroy()
    {
        if (button != null)
        {
            button.selectEntered.RemoveListener(OnButtonPressed);
            button.selectExited.RemoveListener(OnButtonReleased);
            button.hoverEntered.RemoveListener(OnButtonHovered);
        }
    }

    // Optional: Visualize debug info on screen
    void OnGUI()
    {
        if (!showDebugInfo || !initialized) return;

        GUI.backgroundColor = Color.black;
        GUI.Box(new Rect(10, 10, 300, 120), "VR Pressure Handler Debug");

        GUI.contentColor = Color.white;
        GUI.Label(new Rect(20, 35, 290, 25), $"Button Y: {buttonTransform.localPosition.y:F3}");
        GUI.Label(new Rect(20, 60, 290, 25), $"Press Depth: {currentPressDepth:F3}");

        // Visualize pressure as a bar
        GUI.backgroundColor = Color.gray;
        GUI.Box(new Rect(20, 85, 200, 20), "");

        // Color based on thresholds
        if (currentPressDepth >= excessiveThreshold)
            GUI.backgroundColor = excessiveColor;
        else if (currentPressDepth >= sufficientThreshold)
            GUI.backgroundColor = sufficientColor;
        else if (currentPressDepth >= insufficientThreshold)
            GUI.backgroundColor = insufficientColor;
        else
            GUI.backgroundColor = normalColor;

        GUI.Box(new Rect(20, 85, 200 * currentPressDepth, 20), "");
    }
}