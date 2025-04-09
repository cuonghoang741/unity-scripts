using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

public class AnimationManager : BaseManager
{
    [SerializeField] private TextMeshProUGUI animationNameText;
    
    private List<RuntimeAnimatorController> animationControllers = new List<RuntimeAnimatorController>();
    private Dictionary<string, RuntimeAnimatorController> controllerCache = new Dictionary<string, RuntimeAnimatorController>();
    private List<string> allAnimationPaths = new List<string>();
    private bool isLoadingAnimations = false;
    private int batchSize = 50; // Number of animations to load per batch
    private float loadingProgress = 0f;
    private bool showLoadingProgress = false;
    
    private int currentAnimationIndex = 0; // Track current animation

    // Filtered animations for search
    private List<string> filteredAnimations = new List<string>();

    public override void Initialize()
    {
        Debug.Log("Initializing AnimationManager");
        InitializeAnimationLoading();
    }

    public void ReplayCurrentAnimation()
    {
        // Replay for preload object
        GameObject preload = MainManager.GetPreloadObject();
        if (preload != null)
        {
            Animator animator = preload.GetComponent<Animator>();
            if (animator != null)
            {
                animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
                Debug.Log("Replaying animation on preview object");
            }
        }

        // Replay for all instantiated objects
        foreach (GameObject obj in MainManager.ObjectManager.GetInstantiatedObjects())
        {
            if (obj != null)
            {
                Animator animator = obj.GetComponent<Animator>();
                if (animator != null)
                {
                    animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
                }
            }
        }
    }

    public void SwitchAnimation(int direction)
    {
        if (animationControllers.Count == 0) return;

        currentAnimationIndex = (currentAnimationIndex + direction + animationControllers.Count) % animationControllers.Count;
        string animName = animationControllers[currentAnimationIndex].name;
        Debug.Log($"Switching animation to: {animName}");

        // Apply to preload object
        GameObject preload = MainManager.GetPreloadObject();
        if (preload != null)
        {
            Animator animator = preload.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = animationControllers[currentAnimationIndex];
                Debug.Log($"Applied animation to preview: {animName}");
            }
            else
            {
                Debug.LogWarning("Failed to apply animation to preview - Animator component missing");
            }
        }

        UpdateAnimationNameText();
    }

    public void UpdateAnimationNameText()
    {
        if (animationNameText != null && animationControllers.Count > 0)
        {
            animationNameText.text = $"Current Animation: {animationControllers[currentAnimationIndex].name}";
        }
    }

    private void InitializeAnimationLoading()
    {
        Debug.Log("Starting animation loading...");

        // Try loading from Mixamo All Anim folder first
        Debug.Log("Attempting to load from Mixamo All Anim folder...");
        RuntimeAnimatorController[] controllers = Resources.LoadAll<RuntimeAnimatorController>("Mixamo All Anim");
        Debug.Log($"Found {controllers.Length} controllers in Mixamo All Anim folder");

        if (controllers.Length == 0)
        {
            // Try loading from root Resources folder
            Debug.Log("No controllers found in Mixamo All Anim, trying Resources root...");
            controllers = Resources.LoadAll<RuntimeAnimatorController>("");
            Debug.Log($"Found {controllers.Length} controllers in Resources root");
        }
        
        // Clear existing lists
        allAnimationPaths = new List<string>();
        animationControllers = new List<RuntimeAnimatorController>();
        controllerCache.Clear();

        // Create a sorted list of controllers by name (descending)
        var sortedControllers = controllers.OrderByDescending(c => c.name).ToArray();

        foreach (RuntimeAnimatorController controller in sortedControllers)
        {
            if (controller != null)
            {
                allAnimationPaths.Add(controller.name);
                animationControllers.Add(controller);
                controllerCache[controller.name] = controller;
            }
        }

        Debug.Log($"Total animation controllers loaded: {animationControllers.Count}");

        if (animationControllers.Count == 0)
        {
            Debug.LogError("No animation controllers found in any Resources folder!");
            
            // List all files in Resources folder to help debugging
            Object[] allResources = Resources.LoadAll("");
            Debug.Log($"Total resources found: {allResources.Length}");
            foreach (Object obj in allResources)
            {
                Debug.Log($"Resource found: {obj.name} of type {obj.GetType()}");
            }
            return;
        }

        // Initial animation setup
        if (animationControllers.Count > 0)
        {
            UpdateAnimationNameText();
            UpdateFilteredAnimations();
        }
    }

    private IEnumerator LoadAnimationsInBatches()
    {
        isLoadingAnimations = true;
        showLoadingProgress = true;

        int totalFiles = allAnimationPaths.Count;
        int processedFiles = 0;
        
        // Process files in batches
        for (int i = 0; i < allAnimationPaths.Count; i += batchSize)
        {
            int currentBatchSize = Mathf.Min(batchSize, allAnimationPaths.Count - i);
            
            // Update progress
            processedFiles += currentBatchSize;
            loadingProgress = (float)processedFiles / totalFiles;
            
            // Let the system breathe between batches
            yield return new WaitForSeconds(0.1f);
        }

        Debug.Log($"Finished loading {animationControllers.Count} animations");
        isLoadingAnimations = false;
        showLoadingProgress = false;
    }

    public void SelectAnimation(string animationName)
    {
        if (isLoadingAnimations)
        {
            Debug.Log("Still loading animations, please wait...");
            return;
        }

        // Find the index of the selected animation
        for (int i = 0; i < animationControllers.Count; i++)
        {
            if (animationControllers[i].name == animationName)
            {
                currentAnimationIndex = i;
                
                // Apply to preload with visual feedback
                GameObject preload = MainManager.GetPreloadObject();
                if (preload != null)
                {
                    Animator animator = preload.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.runtimeAnimatorController = animationControllers[currentAnimationIndex];
                        // Reset the animation
                        animator.Play(animator.GetCurrentAnimatorStateInfo(0).fullPathHash, -1, 0f);
                        Debug.Log($"Applied and reset animation: {animationName}");
                    }
                }

                UpdateAnimationNameText();
                break;
            }
        }
    }

    public void UpdateFilteredAnimations(string searchQuery = "")
    {
        // Clear the list first
        filteredAnimations.Clear();
        
        // Safety check
        if (animationControllers == null || animationControllers.Count == 0)
        {
            Debug.LogWarning("No animations loaded yet!");
            return;
        }

        // Handle empty search - show all animations
        if (string.IsNullOrEmpty(searchQuery))
        {
            foreach (var controller in animationControllers)
            {
                if (controller != null)
                {
                    filteredAnimations.Add(controller.name);
                }
            }
        }
        else
        {
            // Filter animations based on search query
            string searchLower = searchQuery.ToLower();
            foreach (RuntimeAnimatorController controller in animationControllers)
            {
                if (controller != null)
                {
                    string animName = controller.name;
                    if (animName.ToLower().Contains(searchLower))
                    {
                        filteredAnimations.Add(animName);
                    }
                }
            }
        }

        // Sort the filtered animations by name in descending order
        filteredAnimations.Sort((a, b) => string.Compare(b, a));
    }

    public RuntimeAnimatorController GetCurrentAnimatorController()
    {
        if (animationControllers.Count > 0 && currentAnimationIndex >= 0 && currentAnimationIndex < animationControllers.Count)
        {
            return animationControllers[currentAnimationIndex];
        }
        return null;
    }

    public List<string> GetFilteredAnimations() => filteredAnimations;
    
    public int GetCurrentAnimationIndex() => currentAnimationIndex;
    
    public string GetCurrentAnimationName()
    {
        if (animationControllers.Count > 0 && currentAnimationIndex >= 0 && currentAnimationIndex < animationControllers.Count)
        {
            return animationControllers[currentAnimationIndex].name;
        }
        return "None";
    }

    public bool IsAnimationLoading() => isLoadingAnimations;
    
    public float GetLoadingProgress() => loadingProgress;
    
    public bool ShouldShowLoadingProgress() => showLoadingProgress;
    
    public int GetAnimationCount() => animationControllers.Count;

#if UNITY_EDITOR
    public void SetAnimationToLoop(AnimationClip clip)
    {
        if (clip == null) return;

        SerializedObject serializedClip = new SerializedObject(clip);

        // Set loop time
        SerializedProperty loopTime = serializedClip.FindProperty("m_LoopTime");
        if (loopTime != null) loopTime.boolValue = true;

        // Set loop pose
        SerializedProperty loopPose = serializedClip.FindProperty("m_LoopPose");
        if (loopPose != null) loopPose.boolValue = true;

        // Set wrap mode
        SerializedProperty wrapMode = serializedClip.FindProperty("m_WrapMode");
        if (wrapMode != null) wrapMode.intValue = (int)WrapMode.Loop;

        // Apply changes
        serializedClip.ApplyModifiedProperties();

        // Mark the clip as dirty to ensure changes are saved
        EditorUtility.SetDirty(clip);

        Debug.Log($"Set loop for animation: {clip.name}");
    }
#else
    public void SetAnimationToLoop(AnimationClip clip)
    {
        if (clip != null)
        {
            clip.wrapMode = WrapMode.Loop;
            Debug.Log($"Set loop for animation: {clip.name}");
        }
    }
#endif
} 