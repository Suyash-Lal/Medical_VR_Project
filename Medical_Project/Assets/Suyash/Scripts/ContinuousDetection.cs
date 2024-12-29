using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

public class VRPressureHandler : MonoBehaviour
{
    public RectTransform pressureIndicator; // Assign the UI object acting as the scroll
    public XRBaseInteractable firstButton; // Assign your first XR Simple Interactable button
    public XRBaseInteractable secondButton; // Assign your second XR Simple Interactable button
    public Transform firstButtonTransform; // Assign the first button's Transform
    public Transform secondButtonTransform; // Assign the second button's Transform
    public Rigidbody firstButtonRigidbody; // Assign the Rigidbody component of the first button
    public Rigidbody secondButtonRigidbody; // Assign the Rigidbody component of the second button
    public float pressedPositionYFirst = -0.05f; // Y position when the first button is fully pressed
    public float pressedPositionYSecond = -0.1f; // Y position when the second button is fully pressed
    public float maxIndicatorYChangeFirst = 200f; // Maximum Y movement for the first button
    public float amplificationFactor = 5f; // Amplify small button movements

    private float maxIndicatorYChangeSecond; // Dynamically calculated as double the first button's range
    private float restPositionYFirst; // Rest position for the first button
    private float restPositionYSecond; // Rest position for the second button
    private bool isFirstButtonPressing = false; // Tracks if the first button is being interacted with
    private bool isSecondButtonPressing = false; // Tracks if the second button is being interacted with
    private Vector2 initialIndicatorPosition; // Stores the original position of the indicator

    void Start()
    {
        // Dynamically set the maximum range for the second button
        maxIndicatorYChangeSecond = maxIndicatorYChangeFirst * 2;

        // Set rest positions dynamically
        restPositionYFirst = firstButtonTransform.localPosition.y;
        restPositionYSecond = secondButtonTransform.localPosition.y;

        // Store the initial position of the pressure indicator
        initialIndicatorPosition = pressureIndicator.anchoredPosition;

        // Subscribe to the first button's events
        firstButton.selectEntered.AddListener(OnFirstButtonPressed);
        firstButton.selectExited.AddListener(OnFirstButtonReleased);

        // Subscribe to the second button's events
        secondButton.selectEntered.AddListener(OnSecondButtonPressed);
        secondButton.selectExited.AddListener(OnSecondButtonReleased);
    }

    void FixedUpdate()
    {
        if (isFirstButtonPressing)
        {
            UpdateIndicatorPosition(firstButtonTransform, restPositionYFirst, pressedPositionYFirst, maxIndicatorYChangeFirst);
        }
        else if (isSecondButtonPressing)
        {
            UpdateIndicatorPosition(secondButtonTransform, restPositionYSecond, pressedPositionYSecond, maxIndicatorYChangeSecond);
        }
        else
        {
            // Reset the indicator smoothly to its initial position
            Vector2 resetPosition = pressureIndicator.anchoredPosition;
            resetPosition.y = Mathf.Lerp(resetPosition.y, initialIndicatorPosition.y, Time.fixedDeltaTime * 10f);
            pressureIndicator.anchoredPosition = resetPosition;
        }
    }

    private void UpdateIndicatorPosition(Transform buttonTransform, float restPositionY, float pressedPositionY, float maxIndicatorYChange)
    {
        // Get the current position of the button in local Y-axis
        float currentPositionY = buttonTransform.localPosition.y;

        // Calculate the normalized press depth (0 = rest, 1 = fully pressed)
        float pressDepth = Mathf.InverseLerp(restPositionY, pressedPositionY, currentPositionY);

        // Amplify the press depth for the slider movement
        float amplifiedPressDepth = Mathf.Clamp01(pressDepth * amplificationFactor);

        // Calculate the new Y position for the pressure indicator in UI space
        float indicatorY = amplifiedPressDepth * maxIndicatorYChange;

        // Smoothly update the position of the pressureIndicator
        Vector2 newPosition = pressureIndicator.anchoredPosition;
        newPosition.y = Mathf.Lerp(newPosition.y, indicatorY, Time.fixedDeltaTime * 10f); // Smooth movement
        pressureIndicator.anchoredPosition = newPosition;

        // Debug feedback
        Debug.Log($"Button: {buttonTransform.name}, Pressure applied: {amplifiedPressDepth * 100:F2}% (Indicator Y: {indicatorY:F2})");
    }

    private void OnFirstButtonPressed(SelectEnterEventArgs args)
    {
        isFirstButtonPressing = true; // Start detecting input for the first button
    }

    private void OnFirstButtonReleased(SelectExitEventArgs args)
    {
        isFirstButtonPressing = false; // Stop detecting input for the first button
    }

    private void OnSecondButtonPressed(SelectEnterEventArgs args)
    {
        isSecondButtonPressing = true; // Start detecting input for the second button
    }

    private void OnSecondButtonReleased(SelectExitEventArgs args)
    {
        isSecondButtonPressing = false; // Stop detecting input for the second button
    }

    void OnDestroy()
    {
        // Unsubscribe from events to avoid memory leaks
        firstButton.selectEntered.RemoveListener(OnFirstButtonPressed);
        firstButton.selectExited.RemoveListener(OnFirstButtonReleased);

        secondButton.selectEntered.RemoveListener(OnSecondButtonPressed);
        secondButton.selectExited.RemoveListener(OnSecondButtonReleased);
    }
}
