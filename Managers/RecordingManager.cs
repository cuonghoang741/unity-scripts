using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor.Recorder;
using UnityEditor.Recorder.Input;
#endif

public class RecordingManager : BaseManager
{
    // Recording variables
    private bool isRecording = false;
    private bool isRecordingInBuild = false;
    private float recordingStartTime;
    private List<Texture2D> recordedFrames;
    private string recordingsPath;
#if UNITY_EDITOR
    private RecorderController recorderController;
    private RecorderControllerSettings controllerSettings;
#endif

    public override void Initialize()
    {
        // Setup recordings path
        recordingsPath = Path.Combine(Application.persistentDataPath, "Recordings");
        if (!Directory.Exists(recordingsPath))
        {
            Directory.CreateDirectory(recordingsPath);
        }
        recordedFrames = new List<Texture2D>();
        
        Debug.Log("RecordingManager initialized");
    }

    public void ToggleRecording()
    {
#if UNITY_EDITOR
        if (!isRecording)
        {
            StartRecording();
        }
        else
        {
            StopRecording();
        }
#else
        if (!isRecordingInBuild)
        {
            StartRecordingInBuild();
        }
        else
        {
            StopRecordingInBuild();
        }
#endif
    }

#if UNITY_EDITOR
    private void StartRecording()
    {
        if (recorderController != null)
        {
            Debug.LogWarning("Recording is already in progress!");
            return;
        }

        // Create recording settings
        controllerSettings = ScriptableObject.CreateInstance<RecorderControllerSettings>();
        var mediaOutputFolder = "Assets/Recordings";

        // Create movie settings
        var movieRecorderSettings = ScriptableObject.CreateInstance<MovieRecorderSettings>();
        movieRecorderSettings.name = "My Movie Recorder";
        movieRecorderSettings.Enabled = true;

        // Setup video settings
        movieRecorderSettings.OutputFormat = MovieRecorderSettings.VideoRecorderOutputFormat.MP4;
        movieRecorderSettings.VideoBitRateMode = VideoBitrateMode.High;
        movieRecorderSettings.ImageInputSettings = new GameViewInputSettings
        {
            OutputWidth = 1920,
            OutputHeight = 1080
        };

        // Setup output file
        movieRecorderSettings.OutputFile = $"{mediaOutputFolder}/recording_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");

        // Add to controller settings
        controllerSettings.AddRecorderSettings(movieRecorderSettings);
        controllerSettings.SetRecordModeToManual();
        controllerSettings.FrameRate = 60.0f;

        // Create the recorder controller
        recorderController = new RecorderController(controllerSettings);
        recorderController.PrepareRecording();
        recorderController.StartRecording();

        isRecording = true;
        Debug.Log("Started recording...");
    }

    private void StopRecording()
    {
        if (recorderController == null)
        {
            Debug.LogWarning("No active recording to stop!");
            return;
        }

        recorderController.StopRecording();
        recorderController = null;
        if (controllerSettings != null)
        {
            Object.DestroyImmediate(controllerSettings);
            controllerSettings = null;
        }
        isRecording = false;
        Debug.Log("Stopped recording.");
    }
#else
    private float captureInterval = 1f/20f; // Reduce to 20fps
    private float nextCaptureTime = 0f;
    private Queue<byte[]> frameQueue; // Store encoded bytes instead of textures
    private bool isSaving = false;
    private int maxQueueSize = 30;
    private RenderTexture renderTexture;
    private Texture2D screenShot;

    private void StartRecordingInBuild()
    {
        if (isRecordingInBuild) return;

        recordedFrames = new List<Texture2D>();
        frameQueue = new Queue<byte[]>();
        isRecordingInBuild = true;
        recordingStartTime = Time.time;
        nextCaptureTime = Time.time;

        // Create render texture and screenshot texture once
        int targetWidth = Screen.width / 3; // Reduce resolution
        int targetHeight = Screen.height / 3;
        renderTexture = new RenderTexture(targetWidth, targetHeight, 24);
        screenShot = new Texture2D(targetWidth, targetHeight, TextureFormat.RGB24, false);

        Debug.Log($"Started recording at {targetWidth}x{targetHeight}...");
        StartCoroutine(RecordFrames());
    }

    private IEnumerator RecordFrames()
    {
        while (isRecordingInBuild)
        {
            if (Time.time >= nextCaptureTime && frameQueue.Count < maxQueueSize)
            {
                yield return new WaitForEndOfFrame();

                try
                {
                    // Capture screen to render texture
                    Graphics.Blit(null, renderTexture);
                    
                    // Read pixels from render texture
                    RenderTexture.active = renderTexture;
                    screenShot.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                    screenShot.Apply(false);
                    RenderTexture.active = null;

                    // Encode to PNG and store bytes
                    byte[] bytes = screenShot.EncodeToPNG();
                    frameQueue.Enqueue(bytes);

                    nextCaptureTime = Time.time + captureInterval;

                    // Limit recording duration
                    if (frameQueue.Count > 600) // ~30 seconds at 20fps
                    {
                        Debug.Log("Recording limit reached, stopping...");
                        StopRecordingInBuild();
                        break;
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error during recording: {e.Message}");
                    StopRecordingInBuild();
                    break;
                }
            }
            yield return null;
        }
    }

    private void StopRecordingInBuild()
    {
        if (!isRecordingInBuild) return;

        isRecordingInBuild = false;
        Debug.Log("Stopping recording and saving frames...");

        // Cleanup render texture and screenshot texture
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (screenShot != null)
        {
            Destroy(screenShot);
        }

        StartCoroutine(SaveRecordingAsImages());
    }

    private IEnumerator SaveRecordingAsImages()
    {
        if (frameQueue.Count == 0)
        {
            Debug.LogWarning("No frames were recorded!");
            yield break;
        }

        if (isSaving)
        {
            Debug.LogWarning("Already saving frames, please wait...");
            yield break;
        }

        isSaving = true;
        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string sessionFolder = Path.Combine(recordingsPath, $"recording_{timestamp}");
        
        try
        {
            Directory.CreateDirectory(sessionFolder);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error creating directory: {e.Message}");
            isSaving = false;
            yield break;
        }

        Debug.Log($"Saving {frameQueue.Count} frames to {sessionFolder}");

        // Convert queue to array for easier processing
        byte[][] frames = frameQueue.ToArray();
        frameQueue.Clear();

        // Save frames in parallel using ThreadPool
        int completedFrames = 0;
        System.Threading.ManualResetEvent allFramesSaved = new System.Threading.ManualResetEvent(false);

        for (int i = 0; i < frames.Length; i++)
        {
            int frameIndex = i;
            byte[] frameData = frames[i];

            System.Threading.ThreadPool.QueueUserWorkItem(state =>
            {
                try
                {
                    string frameFilePath = Path.Combine(sessionFolder, $"frame_{frameIndex:D4}.png");
                    File.WriteAllBytes(frameFilePath, frameData);
                    System.Threading.Interlocked.Increment(ref completedFrames);

                    if (completedFrames == frames.Length)
                    {
                        allFramesSaved.Set();
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error saving frame {frameIndex}: {e.Message}");
                }
            });

            // Yield every 10 frames to prevent blocking
            if (i % 10 == 0)
            {
                yield return null;
            }
        }

        // Wait for all frames to be saved
        while (!allFramesSaved.WaitOne(100))
        {
            yield return null;
        }

        isSaving = false;
        Debug.Log($"Recording saved successfully to: {sessionFolder}");
    }
#endif

    public IEnumerator CaptureScreenshot()
    {
        GameObject preload = MainManager.GetPreloadObject();
        if (preload)
            preload.gameObject.SetActive(false);

        // Wait for the end of the frame to ensure all rendering is complete
        yield return new WaitForEndOfFrame();

        // Create a texture to capture the screen
        Texture2D screenTexture = new Texture2D(Screen.width, Screen.height, TextureFormat.RGB24, false);

        // Read the pixels from the screen
        screenTexture.ReadPixels(new Rect(0, 0, Screen.width, Screen.height), 0, 0);
        screenTexture.Apply();

        // Convert the texture to bytes
        byte[] bytes = screenTexture.EncodeToPNG();

        // Create a unique filename with timestamp
        string timestamp = System.DateTime.Now.ToString("yyyyMMdd-HHmmss");
        string filename = $"Screenshot_{timestamp}.png";
        string path = System.IO.Path.Combine(Application.persistentDataPath, filename);

        // Save the file
        System.IO.File.WriteAllBytes(path, bytes);

        // Clean up
        Destroy(screenTexture);

        Debug.Log($"Screenshot saved to: {path}");
        if (preload)
            preload.gameObject.SetActive(true);
    }

    void OnDestroy()
    {
#if UNITY_EDITOR
        if (isRecording)
        {
            StopRecording();
        }
#else
        if (isRecordingInBuild)
        {
            StopRecordingInBuild();
        }
#endif
    }

    void OnApplicationQuit()
    {
#if !UNITY_EDITOR
        if (isRecordingInBuild)
        {
            StopRecordingInBuild();
        }
#endif
    }

    public bool IsRecording()
    {
#if UNITY_EDITOR
        return isRecording;
#else
        return isRecordingInBuild;
#endif
    }
} 