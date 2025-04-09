#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using UnityEngine.UI;
using TMPro;
using UnityEngine.InputSystem;

public class Manager : MonoBehaviour
{
    // Singleton instance
    private static Manager instance;
    public static Manager Instance => instance;

    // References to other managers
    [HideInInspector] public AnimationManager AnimationManager { get; private set; }
    [HideInInspector] public CameraController CameraController { get; private set; }
    [HideInInspector] public UIManager UIManager { get; private set; }
    [HideInInspector] public ObjectManager ObjectManager { get; private set; }
    [HideInInspector] public LogManager LogManager { get; private set; }
    [HideInInspector] public RecordingManager RecordingManager { get; private set; }

    // Core component references
    [SerializeField] private GameObject[] animatedObjects;
    [SerializeField] private string[] listScene;
    [SerializeField] private int indexAnimatedObject;
    [SerializeField] private int indexScene;
    [SerializeField] private GameObject preload;
    [SerializeField] private GameObject fps;
    [SerializeField] private Material preloadMaterial;
    [SerializeField] private TextMeshProUGUI animationNameText;
    [SerializeField] private TextMeshProUGUI helpText;

    // Scene loading state tracking
    private bool isLoadingScene = false;

    // Cursor visibility state
    private bool isCursorVisible = true;

    void Awake()
    {
        // Singleton pattern
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);

        // Find camera if not assigned
        if (fps == null)
        {
            fps = GameObject.FindGameObjectWithTag("MainCamera");
            if (fps != null)
            {
                Debug.Log("Found main camera in Awake");
                DontDestroyOnLoad(fps);
            }
            else
            {
                Debug.LogError("Failed to find main camera in Awake");
            }
        }

        // Subscribe to scene loaded event
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Create folders
        if (!Directory.Exists(Path.Combine(Application.persistentDataPath, "Recordings")))
        {
            Directory.CreateDirectory(Path.Combine(Application.persistentDataPath, "Recordings"));
        }

        // Initialize managers
        InitializeManagers();
    }

    private void InitializeManagers()
    {
        // Create and initialize all managers
        AnimationManager = gameObject.AddComponent<AnimationManager>();
        CameraController = gameObject.AddComponent<CameraController>();
        UIManager = gameObject.AddComponent<UIManager>();
        ObjectManager = gameObject.AddComponent<ObjectManager>();
        LogManager = gameObject.AddComponent<LogManager>();
        RecordingManager = gameObject.AddComponent<RecordingManager>();

        // Hook up serialized fields
        var animManagerField = AnimationManager.GetType().GetField("animationNameText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (animManagerField != null) animManagerField.SetValue(AnimationManager, animationNameText);

        var uiManagerField = UIManager.GetType().GetField("helpText", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (uiManagerField != null) uiManagerField.SetValue(UIManager, helpText);

        // Initialize with reference to this manager
        AnimationManager.Initialize();
        CameraController.Initialize();
        UIManager.Initialize();
        ObjectManager.Initialize();
        LogManager.Initialize();
        RecordingManager.Initialize();
    }

    void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"Scene loaded: {scene.name}, Mode: {mode}");
        
        // Delay execution to ensure scene is fully loaded
        StartCoroutine(DelayedSceneSetup());
    }
    
    private IEnumerator DelayedSceneSetup()
    {
        // Wait for end of frame to ensure scene is fully set up
        yield return new WaitForEndOfFrame();
        
        // Update camera references
        if (fps == null)
        {
            fps = GameObject.FindGameObjectWithTag("MainCamera");
            if (fps != null)
            {
                Debug.Log("Found main camera after scene load");
                DontDestroyOnLoad(fps);
            }
            else
            {
                Debug.LogError("Failed to find main camera after scene load");
            }
        }

        // Notify all managers of scene load
        AnimationManager.OnSceneLoaded();
        CameraController.OnSceneLoaded();
        UIManager.OnSceneLoaded();
        ObjectManager.OnSceneLoaded();
        LogManager.OnSceneLoaded();
        RecordingManager.OnSceneLoaded();
    }

    void Start()
    {
        // Disable crosshair
        DisableCrosshair();
        
        // Hide cursor by default
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Add method to disable crosshair
    private void DisableCrosshair()
    {
        // Find FirstPersonController component
        FirstPersonController fpsController = FindObjectOfType<FirstPersonController>();
        if (fpsController != null)
        {
            // Disable crosshair
            fpsController.crosshair = false;
            
            // Try to find and disable the crosshair image directly
            Image crosshairImage = fpsController.GetComponentInChildren<Image>();
            if (crosshairImage != null)
            {
                crosshairImage.gameObject.SetActive(false);
                Debug.Log("Crosshair disabled successfully");
            }
        }
    }

    void Update()
    {
        // Add cursor visibility toggle with V key
        if (Keyboard.current != null && Keyboard.current.vKey.wasPressedThisFrame)
        {
            isCursorVisible = !isCursorVisible;
            Cursor.visible = isCursorVisible;
            Cursor.lockState = isCursorVisible ? CursorLockMode.None : CursorLockMode.Locked;
        }

        // Handle camera position switching with Tab key
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            CameraController.SwitchCameraPosition();
        }

        // Handle recording with Ctrl + R
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && Keyboard.current.ctrlKey.isPressed)
        {
            RecordingManager.ToggleRecording();
        }

        // Handle log window toggle with L key
        if (Keyboard.current != null && Keyboard.current.lKey.wasPressedThisFrame)
        {
            LogManager.ToggleLogWindow();
        }

        // Handle search modal toggle with Ctrl+F
        if (Keyboard.current != null && 
            Keyboard.current.ctrlKey.isPressed && 
            Keyboard.current.fKey.wasPressedThisFrame)
        {
            UIManager.ToggleSearchModal();
            Event.current?.Use(); // Prevent the default Ctrl+F behavior
        }

        // Handle hotkey help toggle with H key
        if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            UIManager.ToggleHotkeyHelp();
        }

        // Handle animation replay with N key
        if (Keyboard.current != null && Keyboard.current.nKey.wasPressedThisFrame)
        {
            AnimationManager.ReplayCurrentAnimation();
        }

        // Handle preview mode toggle with P key
        if (Keyboard.current != null && Keyboard.current.pKey.wasPressedThisFrame)
        {
            ObjectManager.TogglePreloadMode();
            UIManager.TogglePreviewGuide(ObjectManager.IsPreloaded());
        }

        // Handle scale adjustment with + and - keys
        if (Keyboard.current != null)
        {
            if (Keyboard.current.equalsKey.wasPressedThisFrame ||
                Keyboard.current.numpadPlusKey.wasPressedThisFrame)
            {
                ObjectManager.IncrementScale(0.1f);
            }
            else if (Keyboard.current.minusKey.wasPressedThisFrame ||
                     Keyboard.current.numpadMinusKey.wasPressedThisFrame)
            {
                ObjectManager.IncrementScale(-0.1f);
            }
        }

        // Handle scene loading with U key
        if (!isLoadingScene && Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            if (listScene != null && listScene.Length > 0)
            {
                CleanupCurrentScene();
                StartCoroutine(LoadSceneAsync(listScene[indexScene]));
                indexScene = (indexScene + 1) % listScene.Length;
            }
        }

        // Handle preload object controls
        if (ObjectManager.IsPreloaded())
        {
            ObjectManager.HandlePreloadControls();
            
            // Handle object switching with Q/E keys
            if (Keyboard.current != null)
            {
                if (Keyboard.current.qKey.wasPressedThisFrame)
                {
                    indexAnimatedObject = (indexAnimatedObject - 1 + animatedObjects.Length) % animatedObjects.Length;
                    ObjectManager.Preload();
                }
                else if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    indexAnimatedObject = (indexAnimatedObject + 1) % animatedObjects.Length;
                    ObjectManager.Preload();
                }
            }
        }

        // Handle animation switching with M/N keys
        if (Keyboard.current != null)
        {
            if (Keyboard.current.mKey.wasPressedThisFrame)
            {
                AnimationManager.SwitchAnimation(-1); // Previous animation
            }
            else if (Keyboard.current.nKey.wasPressedThisFrame)
            {
                AnimationManager.SwitchAnimation(1); // Next animation
            }
        }

        // Handle object instantiation with left mouse button
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame && ObjectManager.IsPreloaded())
        {
            ObjectManager.HandleObjectInstantiation();
        }

        // Handle rotation adjustment with R key
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame && !Keyboard.current.ctrlKey.isPressed)
        {
            // Increase rotation by 15 degrees around Y axis
            ObjectManager.IncrementRotation(15f);
        }

        // Handle screenshot with Z key
        if (Keyboard.current != null && Keyboard.current.zKey.wasPressedThisFrame)
        {
            StartCoroutine(RecordingManager.CaptureScreenshot());
        }

        // Handle object deletion with right mouse button
        if (Mouse.current != null && Mouse.current.rightButton.wasPressedThisFrame)
        {
            ObjectManager.HandleObjectDeletion();
        }
    }

    // Clean up the current scene to help prevent dictionary key conflicts
    private void CleanupCurrentScene()
    {
        ObjectManager.CleanupInstantiatedObjects();
        
        // Destroy any preload object
        if (preload != null)
        {
            Destroy(preload);
            preload = null;
        }
        
        // Resources cleanup
        Resources.UnloadUnusedAssets();
        System.GC.Collect();
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError("Scene name is null or empty!");
            yield break;
        }

        isLoadingScene = true;
        AsyncOperation loadOperation = null;

        try
        {
            loadOperation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error loading scene {sceneName}: {e.Message}\n{e.StackTrace}");
            isLoadingScene = false;
            yield break;
        }

        if (loadOperation == null)
        {
            Debug.LogError($"Failed to start loading scene: {sceneName}");
            isLoadingScene = false;
            yield break;
        }

        // Show loading progress
        while (!loadOperation.isDone)
        {
            float progress = Mathf.Clamp01(loadOperation.progress / 0.9f);
            // Update any loading UI here if needed
            yield return null;
        }

        // Give Unity a frame to properly set up the scene
        yield return new WaitForEndOfFrame();

        try
        {
            // Activate scene objects
            Scene newScene = SceneManager.GetSceneByName(sceneName);
            if (!newScene.IsValid())
            {
                Debug.LogError($"Failed to get valid scene: {sceneName}");
                isLoadingScene = false;
                yield break;
            }

            ActivateSceneObjects();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error during scene activation: {e.Message}\n{e.StackTrace}");
            isLoadingScene = false;
            yield break;
        }

        isLoadingScene = false;
    }

    // Activate necessary objects in the loaded scene
    private void ActivateSceneObjects()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        GameObject[] rootObjects = currentScene.GetRootGameObjects();
        GameObject spawnPoint = null;

        spawnPoint = GameObject.FindWithTag("SpawnPoint");

        Debug.Log($"SpawnPoint found: {spawnPoint != null}");

        foreach (GameObject rootObj in rootObjects)
        {
            if (rootObj != null)
            {
                // First pass: find spawn point and activate necessary objects
                foreach (Transform child in rootObj.transform)
                {
                    if (child.CompareTag("SpawnPoint") ||
                        child.name.Contains("Collider") || child.name.Contains("Floor"))
                    {
                        child.gameObject.SetActive(true);
                    }
                }
            }
        }

        // Position camera at spawn point if found
        if (spawnPoint != null && fps != null)
        {
            fps.transform.position = spawnPoint.transform.position;
            fps.transform.rotation = spawnPoint.transform.rotation;
            Debug.Log($"Camera positioned at SpawnPoint - Position: {fps.transform.position}, Rotation: {fps.transform.rotation.eulerAngles}");
            
            // Important: Store this as the new original camera position and rotation
            CameraController.UpdateOriginalPosition(fps.transform.position, fps.transform.rotation);
        }
        else
        {
            Debug.LogWarning("SpawnPoint not found in scene or fps camera is null");
        }
    }

    // Accessors for manager components
    public GameObject GetFPSCamera() => fps;
    public GameObject[] GetAnimatedObjects() => animatedObjects;
    public int GetAnimatedObjectIndex() => indexAnimatedObject;
    public void SetAnimatedObjectIndex(int index) => indexAnimatedObject = index;
    public Material GetPreloadMaterial() => preloadMaterial;
    public GameObject GetPreloadObject() => preload;
    public void SetPreloadObject(GameObject obj) => preload = obj;
}
