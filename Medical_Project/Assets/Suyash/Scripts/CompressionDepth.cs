using Meta.XR.ImmersiveDebugger.UserInterface.Generic;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CompressionDepth : MonoBehaviour
{
    [Header("References")]
    public GameObject Interactable; // The CPR button/interactable
    public GameObject Affected; // The body/object being affected
    public VRPressureHandler pressureHandler; // Reference to the VRPressureHandler
    public RectTransform compressionRateIndicator; // UI indicator for compression rate

    [Header("Compression Rate Settings")]
    public float targetCompressionRate = 100f; // Target CPR rate (compressions per minute)
    public float minRateThreshold = 60f; // Below this is too slow (red) - REDUCED
    public float maxRateThreshold = 120f; // Above this is too fast (yellow)
    public float maxRateIndicatorYChange = 200f; // Maximum Y movement for rate indicator
    public float rateDisplaySmoothing = 8f; // Smoothing for rate display - INCREASED 

    [Header("Compression Count Settings")]
    public TMPro.TextMeshProUGUI compressionCountText; // Optional: display count 
    public float compressionTimeWindow = 10f; // Window to calculate rate (seconds) - REDUCED

    // Private variables
    private Vector2 initialRateIndicatorPosition;
    private bool wasOverSufficientThreshold = false;
    private List<float> compressionTimestamps = new List<float>();
    private int compressionCount = 0;
    private float currentCompressionRate = 0f;
    private bool isInitialized = false;
    private bool reachedGreenZone = false; // Track if current compression reached green

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        // Initialize values
        if (compressionRateIndicator != null)
        {
            initialRateIndicatorPosition = compressionRateIndicator.anchoredPosition;
        }

        if (pressureHandler == null && Interactable != null)
        {
            pressureHandler = Interactable.GetComponent<VRPressureHandler>();
        }

        if (pressureHandler == null)
        {
            Debug.LogError("CompressionDepth: No VRPressureHandler found!");
        }
        else
        {
            isInitialized = true;
        }

        // Initialize compression count display
        UpdateCompressionCountDisplay();
    }

    // Update is called once per frame
    void Update()
    {
        if (!isInitialized || pressureHandler == null) return;

        // Track compressions based on sufficient threshold crossing
        TrackCompressions();

        // Update rate indicator
        UpdateRateIndicator();
    }

    private void TrackCompressions()
    {
        // Get current depth from pressure handler
        float currentDepth = pressureHandler.CurrentPressDepth;
        float sufficientThreshold = pressureHandler.sufficientThreshold;

        // Check if we've reached the "green zone" (sufficient depth) during this compression
        if (!reachedGreenZone && currentDepth >= sufficientThreshold)
        {
            reachedGreenZone = true;
            Debug.Log("Compression reached sufficient depth (green zone)");
        }

        // Check if we've crossed the threshold
        if (!wasOverSufficientThreshold && currentDepth >= sufficientThreshold * 0.8f)
        {
            // Just crossed over the detection threshold (slightly lower than sufficient for tracking)
            wasOverSufficientThreshold = true;
        }
        else if (wasOverSufficientThreshold && currentDepth < sufficientThreshold * 0.5f)
        {
            // Just crossed back under the release threshold - compression cycle completed
            wasOverSufficientThreshold = false;

            // Only count it if it reached the green zone during this compression
            if (reachedGreenZone)
            {
                // Record timestamp and increment count
                compressionTimestamps.Add(Time.time);
                compressionCount++;

                // Update display
                UpdateCompressionCountDisplay();

                Debug.Log($"Successful compression counted: {compressionCount}");
            }
            else
            {
                Debug.Log("Compression not deep enough - not counted");
            }

            // Reset the green zone flag for the next compression
            reachedGreenZone = false;
        }

        // Clean up old timestamps
        while (compressionTimestamps.Count > 0 &&
               Time.time - compressionTimestamps[0] > compressionTimeWindow)
        {
            compressionTimestamps.RemoveAt(0);
        }

        // Calculate current rate with more sensitivity
        if (compressionTimestamps.Count > 1)
        {
            float windowDuration = Mathf.Min(compressionTimeWindow,
                                          Time.time - compressionTimestamps[0]);

            if (windowDuration > 0)
            {
                // Calculate rate as compressions per minute with a slightly increased multiplier
                float newRate = (compressionTimestamps.Count / windowDuration) * 60f;

                // Apply enhanced smoothing for faster response
                float lerpFactor = Mathf.Min(1.0f, Time.deltaTime * rateDisplaySmoothing * 1.2f);
                currentCompressionRate = Mathf.Lerp(currentCompressionRate, newRate, lerpFactor);
            }
        }
        else if (compressionTimestamps.Count <= 1 && Time.time > 3f)
        {
            // Gradually reduce rate if no compressions
            currentCompressionRate = Mathf.Lerp(currentCompressionRate, 0,
                                             Time.deltaTime * rateDisplaySmoothing * 0.5f);
        }
    }

    private void UpdateRateIndicator()
    {
        if (compressionRateIndicator == null || pressureHandler == null) return;

        // Normalize rate (0-1 range) where 0.5 is target rate
        float normalizedRate = currentCompressionRate / (targetCompressionRate * 2f);
        normalizedRate = Mathf.Clamp01(normalizedRate);

        // Calculate target position
        float targetY = initialRateIndicatorPosition.y - (normalizedRate * maxRateIndicatorYChange);

        // Update position with smoothing
        Vector2 newPosition = compressionRateIndicator.anchoredPosition;
        newPosition.y = Mathf.Lerp(newPosition.y, targetY,
                                 Time.deltaTime * rateDisplaySmoothing);
        compressionRateIndicator.anchoredPosition = newPosition;

        // Get reference to the Image component of compressionRateIndicator's GameObject
        UnityEngine.UI.Image rateImage = compressionRateIndicator.GetComponent<UnityEngine.UI.Image>();
        if (rateImage != null)
        {
            // Set color based on rate thresholds
            if (currentCompressionRate >= minRateThreshold && currentCompressionRate <= maxRateThreshold)
            {
                // Optimal rate (green)
                rateImage.color = pressureHandler.sufficientMaterial.color;
            }
            else if (currentCompressionRate > maxRateThreshold)
            {
                // Too fast (yellow)
                rateImage.color = pressureHandler.insufficientMaterial.color;
            }
            else
            {
                // Too slow (red)
                rateImage.color = pressureHandler.excessiveMaterial.color;
            }
        }
    }

    private void UpdateCompressionCountDisplay()
    {
        if (compressionCountText != null)
        {
            compressionCountText.text = $"Compressions\n{compressionCount}";
        }
    }

    public void ResetCompressionTracking()
    {
        compressionCount = 0;
        compressionTimestamps.Clear();
        currentCompressionRate = 0f;
        wasOverSufficientThreshold = false;
        reachedGreenZone = false;

        if (compressionRateIndicator != null)
        {
            compressionRateIndicator.anchoredPosition = initialRateIndicatorPosition;
        }

        UpdateCompressionCountDisplay();
    }

    // You can call this method to get the current rate for other components
    public float GetCurrentRate()
    {
        return currentCompressionRate;
    }

    // Check if the current rate is in the optimal zone
    public bool IsRateOptimal()
    {
        return currentCompressionRate >= minRateThreshold &&
               currentCompressionRate <= maxRateThreshold;
    }
}
