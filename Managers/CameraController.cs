using UnityEngine;
using System.Collections;

public class CameraController : BaseManager
{
    // Camera position variables
    private Vector3[] cameraPositions;
    private Quaternion[] cameraRotations;
    private int currentCameraIndex = 0;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private bool isCameraTransitioning = false;
    private FirstPersonController fpsController; // Reference to FPS controller

    // Variables to store last positions
    private Vector3 lastDefaultPosition;
    private Quaternion lastDefaultRotation;
    private Vector3 lastRandom1Position; 
    private Quaternion lastRandom1Rotation;
    private Vector3 lastRandom2Position;
    private Quaternion lastRandom2Rotation;
    private Vector3 lastHighPosition;
    private Quaternion lastHighRotation;
    private bool hasLastDefaultPosition = false;
    private bool hasLastRandom1Position = false;
    private bool hasLastRandom2Position = false;
    private bool hasLastHighPosition = false;

    // Camera position popup
    private bool showCameraPopup = false;
    private float popupTimer = 0f;
    private const float POPUP_DURATION = 3f;
    private string[] cameraPositionNames = new string[] {
        "Original Position",
        "Bird's Eye View",
        "Random Position 1", 
        "Random Position 2"
    };

    public override void Initialize()
    {
        GameObject camera = MainManager.GetFPSCamera();
        if (camera != null)
        {
            // Store original camera position and rotation
            originalCameraPosition = camera.transform.position;
            originalCameraRotation = camera.transform.rotation;
            
            // Get the FPS controller reference
            fpsController = camera.GetComponentInParent<FirstPersonController>();
            if (fpsController == null)
            {
                // Try to find it in the scene
                fpsController = FindObjectOfType<FirstPersonController>();
                Debug.Log($"FPS Controller found in Initialize: {fpsController != null}");
            }
            
            InitializeCameraPositions();
            Debug.Log("Camera positions initialized");
        }
        else
        {
            Debug.LogError("Failed to find camera reference in Initialize");
        }
    }

    public override void OnSceneLoaded()
    {
        GameObject camera = MainManager.GetFPSCamera();
        if (camera != null)
        {
            // Update original camera position and rotation
            originalCameraPosition = camera.transform.position;
            originalCameraRotation = camera.transform.rotation;
            
            // Update FPS controller reference
            fpsController = camera.GetComponentInParent<FirstPersonController>();
            if (fpsController == null)
            {
                fpsController = FindObjectOfType<FirstPersonController>();
                Debug.Log($"FPS Controller found after scene load: {fpsController != null}");
            }
            
            // Reinitialize camera positions
            InitializeCameraPositions();
            currentCameraIndex = 0;
            Debug.Log("Camera positions reinitialized after scene load");
        }
    }

    private void InitializeCameraPositions()
    {
        // Initialize arrays for camera positions
        cameraPositions = new Vector3[4];
        cameraRotations = new Quaternion[4];

        // Position 0: Original position
        cameraPositions[0] = originalCameraPosition;
        cameraRotations[0] = originalCameraRotation;

        // Position 1: High position (Bird's eye view) - Looking straight down
        cameraPositions[1] = originalCameraPosition + Vector3.up * 100f;
        // Set rotation to look straight down (90 degrees on X axis)
        cameraRotations[1] = Quaternion.Euler(90f, 0f, 0f);
        Debug.Log($"Bird's eye position set at {cameraPositions[1]}, rotation: {cameraRotations[1].eulerAngles}");

        // Position 2: First random position
        float randomX1 = Random.Range(-50f, 50f);
        float randomY1 = Random.Range(10f, 30f);
        float randomZ1 = Random.Range(-50f, 50f);
        cameraPositions[2] = originalCameraPosition + new Vector3(randomX1, randomY1, randomZ1);
        Vector3 directionToOriginal1 = (originalCameraPosition - cameraPositions[2]).normalized;
        cameraRotations[2] = Quaternion.LookRotation(directionToOriginal1, Vector3.up);

        // Position 3: Second random position
        float randomX2 = Random.Range(-50f, 50f);
        float randomY2 = Random.Range(10f, 30f);
        float randomZ2 = Random.Range(-50f, 50f);
        cameraPositions[3] = originalCameraPosition + new Vector3(randomX2, randomY2, randomZ2);
        Vector3 directionToOriginal2 = (originalCameraPosition - cameraPositions[3]).normalized;
        cameraRotations[3] = Quaternion.LookRotation(directionToOriginal2, Vector3.up);
    }

    public void SwitchCameraPosition()
    {
        // Find all cameras with MainCamera tag
        GameObject[] allMainCameras = GameObject.FindGameObjectsWithTag("MainCamera");
        Debug.Log($"Found {allMainCameras.Length} cameras with MainCamera tag");

        if (allMainCameras.Length == 0)
        {
            Debug.LogError("No cameras with MainCamera tag found!");
            return;
        }

        if (cameraPositions == null || cameraRotations == null) 
        {
            Debug.LogError("Cannot switch camera position - camera positions/rotations arrays are null");
            return;
        }

        Debug.Log("Starting camera position switch...");

        // Save current position and rotation before switching
        foreach (GameObject camera in allMainCameras)
        {
            switch (currentCameraIndex)
            {
                case 0: // Default position
                    lastDefaultPosition = camera.transform.position;
                    lastDefaultRotation = camera.transform.rotation;
                    hasLastDefaultPosition = true;
                    Debug.Log($"Saved default position for camera {camera.name}: {lastDefaultPosition}, rotation: {lastDefaultRotation.eulerAngles}");
                    break;
                case 1: // Fly cam position
                    lastHighPosition = camera.transform.position;
                    lastHighRotation = camera.transform.rotation;
                    hasLastHighPosition = true;
                    Debug.Log($"Saved fly cam position for camera {camera.name}: {lastHighPosition}, rotation: {lastHighRotation.eulerAngles}");
                    break;
                case 2: // First random position
                    lastRandom1Position = camera.transform.position;
                    lastRandom1Rotation = camera.transform.rotation;
                    hasLastRandom1Position = true;
                    Debug.Log($"Saved random position 1 for camera {camera.name}: {lastRandom1Position}, rotation: {lastRandom1Rotation.eulerAngles}");
                    break;
                case 3: // Second random position
                    lastRandom2Position = camera.transform.position;
                    lastRandom2Rotation = camera.transform.rotation;
                    hasLastRandom2Position = true;
                    Debug.Log($"Saved random position 2 for camera {camera.name}: {lastRandom2Position}, rotation: {lastRandom2Rotation.eulerAngles}");
                    break;
            }
        }

        // Update camera index
        currentCameraIndex = (currentCameraIndex + 1) % cameraPositions.Length;
        Debug.Log($"Switching to camera position {currentCameraIndex}");

        // Show popup
        showCameraPopup = true;
        popupTimer = POPUP_DURATION;

        // Set position based on camera index
        Vector3 targetPosition;
        Quaternion targetRotation;

        switch (currentCameraIndex)
        {
            case 0: // Default position
                targetPosition = hasLastDefaultPosition ? lastDefaultPosition : cameraPositions[0];
                targetRotation = hasLastDefaultPosition ? lastDefaultRotation : cameraRotations[0];
                Debug.Log($"Moving to default position: {targetPosition}, rotation: {targetRotation.eulerAngles}");
                break;
            case 1: // Fly cam position
                targetPosition = hasLastHighPosition ? lastHighPosition : cameraPositions[1];
                targetRotation = hasLastHighPosition ? lastHighRotation : cameraRotations[1];
                Debug.Log($"Moving to fly cam position: {targetPosition}, rotation: {targetRotation.eulerAngles}");
                break;
            case 2: // First random position
                targetPosition = hasLastRandom1Position ? lastRandom1Position : cameraPositions[2];
                targetRotation = hasLastRandom1Position ? lastRandom1Rotation : cameraRotations[2];
                Debug.Log($"Moving to random position 1: {targetPosition}, rotation: {targetRotation.eulerAngles}");
                break;
            case 3: // Second random position
                targetPosition = hasLastRandom2Position ? lastRandom2Position : cameraPositions[3];
                targetRotation = hasLastRandom2Position ? lastRandom2Rotation : cameraRotations[3];
                Debug.Log($"Moving to random position 2: {targetPosition}, rotation: {targetRotation.eulerAngles}");
                break;
            default:
                targetPosition = cameraPositions[currentCameraIndex];
                targetRotation = cameraRotations[currentCameraIndex];
                break;
        }

        // Process each camera with MainCamera tag
        foreach (GameObject camera in allMainCameras)
        {
            Debug.Log($"Processing camera: {camera.name} at position: {camera.transform.position}");
            
            FirstPersonController cameraController = camera.GetComponentInParent<FirstPersonController>();
            Transform cameraParent = camera.transform.parent;

            if (cameraController != null)
            {
                Debug.Log($"Found FirstPersonController for camera: {camera.name}");
            }

            if (cameraParent != null)
            {
                Debug.Log($"Camera {camera.name} has parent: {cameraParent.name}");

                // Move parent to new position
                Vector3 offset = targetPosition - camera.transform.position;
                cameraParent.position += offset;

                // Apply new rotation to camera
                if (currentCameraIndex == 1) // Bird's eye view
                {
                    // Look straight down for bird's eye view
                    camera.transform.rotation = Quaternion.Euler(90f, camera.transform.eulerAngles.y, 0f);
                }
                else
                {
                    // Use saved rotation or default rotation
                    camera.transform.rotation = targetRotation;
                }

                if (cameraController != null)
                {
                    // Calculate yaw and pitch from camera rotation
                    Vector3 eulerAngles = camera.transform.eulerAngles;
                    float yaw = eulerAngles.y;
                    float pitch = eulerAngles.x;
                    
                    // Normalize pitch to be within -90 to 90 degrees
                    if (pitch > 180f) pitch -= 360f;
                    
                    // Set values in FPS controller
                    cameraController.SetRotationValues(yaw, pitch);
                    Debug.Log($"Applied rotation to FPS controller for camera {camera.name} - yaw: {yaw}, pitch: {pitch}");
                }
            }
            else
            {
                Debug.Log($"Camera {camera.name} has no parent - applying world space transform directly");
                // If camera has no parent, apply transform directly
                camera.transform.position = targetPosition;

                // Apply rotation
                if (currentCameraIndex == 1) // Bird's eye view
                {
                    camera.transform.rotation = Quaternion.Euler(90f, camera.transform.eulerAngles.y, 0f);
                }
                else
                {
                    camera.transform.rotation = targetRotation;
                }
                
                if (cameraController != null)
                {
                    Vector3 eulerAngles = camera.transform.eulerAngles;
                    float yaw = eulerAngles.y;
                    float pitch = eulerAngles.x;
                    if (pitch > 180f) pitch -= 360f;
                    
                    cameraController.SetRotationValues(yaw, pitch);
                    Debug.Log($"Applied direct rotation to FPS controller for camera {camera.name} - yaw: {yaw}, pitch: {pitch}");
                }
            }

            // Enable fly mode when switching to fly cam position (position 1)
            if (cameraController != null && currentCameraIndex == 1)
            {
                Debug.Log($"Checking fly mode for camera: {camera.name}");
                bool isCurrentlyFlying = cameraController.IsFlying();
                Debug.Log($"Current fly mode status for camera {camera.name}: {isCurrentlyFlying}");

                if (!isCurrentlyFlying)
                {
                    Debug.Log($"Attempting to enable fly mode for camera: {camera.name}");
                    try
                    {
                        var isFlying = cameraController.GetType().GetField("isFlying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                        if (isFlying != null)
                        {
                            isFlying.SetValue(cameraController, true);
                            var rb = cameraController.GetComponent<Rigidbody>();
                            if (rb != null)
                            {
                                rb.useGravity = false;
                                rb.linearVelocity = Vector3.zero;
                            }
                            Debug.Log($"Successfully enabled fly mode for camera: {camera.name}");
                        }
                    }
                    catch (System.Exception e)
                    {
                        Debug.LogError($"Error enabling fly mode for camera {camera.name}: {e.Message}\n{e.StackTrace}");
                    }
                }
            }
        }
    }

    public void UpdateOriginalPosition(Vector3 position, Quaternion rotation)
    {
        originalCameraPosition = position;
        originalCameraRotation = rotation;
        
        // Reinitialize camera positions
        InitializeCameraPositions();
        
        // Reset camera index to 0 (original position)
        currentCameraIndex = 0;
        showCameraPopup = false;
        
        // Reset saved positions
        hasLastDefaultPosition = false;
        hasLastRandom1Position = false;
        hasLastRandom2Position = false;
        hasLastHighPosition = false;
        
        Debug.Log($"Updated original camera position to {position}, rotation to {rotation.eulerAngles}");
    }

    void Update()
    {
        // Update popup timer
        if (showCameraPopup)
        {
            popupTimer -= Time.deltaTime;
            if (popupTimer <= 0)
            {
                showCameraPopup = false;
            }
        }
    }

    void OnGUI()
    {
        // Show camera position popup
        if (showCameraPopup)
        {
            // Calculate popup dimensions and position
            float popupWidth = 300;
            float popupHeight = 60;
            float popupX = (Screen.width - popupWidth) / 2;
            float popupY = 50;

            // Create semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.8f);
            GUI.DrawTexture(new Rect(popupX, popupY, popupWidth, popupHeight), Texture2D.whiteTexture);
            
            // Create text style
            GUIStyle popupStyle = new GUIStyle(GUI.skin.label);
            popupStyle.alignment = TextAnchor.MiddleCenter;
            popupStyle.fontSize = 20;
            popupStyle.normal.textColor = Color.white;

            // Draw text
            GUI.color = Color.white;
            GUI.Label(new Rect(popupX, popupY, popupWidth, popupHeight), 
                     $"Camera: {cameraPositionNames[currentCameraIndex]}", 
                     popupStyle);
        }
    }

    public bool IsCameraTransitioning() => isCameraTransitioning;
    
    public int GetCurrentCameraIndex() => currentCameraIndex;
    
    public bool IsShowingCameraPopup() => showCameraPopup;
}

// Extension methods for FirstPersonController
public static class FirstPersonControllerExtensions
{
    public static void SetRotationValues(this FirstPersonController controller, float yaw, float pitch)
    {
        var yawField = controller.GetType().GetField("yaw", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var pitchField = controller.GetType().GetField("pitch", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        yawField.SetValue(controller, yaw);
        pitchField.SetValue(controller, pitch);
    }

    public static bool IsFlying(this FirstPersonController controller)
    {
        var isFlying = controller.GetType().GetField("isFlying", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (isFlying != null)
        {
            return (bool)isFlying.GetValue(controller);
        }
        return false; // Return false if field is not found
    }
} 