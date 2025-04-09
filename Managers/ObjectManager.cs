using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ObjectManager : BaseManager
{
    private List<GameObject> instantiatedObjects = new List<GameObject>();
    private float currentScale = 1f;
    private float currentRotation = 0f;
    private bool isPreloaded = false;

    public override void Initialize()
    {
        currentScale = 1f;
        currentRotation = 0f;
        instantiatedObjects = new List<GameObject>();
    }

    public void HandleObjectInstantiation()
    {
        GameObject[] animatedObjects = MainManager.GetAnimatedObjects();
        int indexAnimatedObject = MainManager.GetAnimatedObjectIndex();
        
        if (animatedObjects == null || animatedObjects.Length == 0 || 
            indexAnimatedObject < 0 || indexAnimatedObject >= animatedObjects.Length)
            return;

        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Increase spawn height by 0.2f to prevent clipping
            Vector3 spawnPosition = new Vector3(hit.point.x, hit.point.y + 0.2f, hit.point.z);
            GameObject obj = Instantiate(animatedObjects[indexAnimatedObject], spawnPosition, Quaternion.Euler(0f, currentRotation, 0f));
            obj.transform.localScale = new Vector3(currentScale, currentScale, currentScale);

            // Apply current animation
            Animator animator = obj.GetComponent<Animator>();
            if (animator != null)
            {
                animator.runtimeAnimatorController = MainManager.AnimationManager.GetCurrentAnimatorController();
                Debug.Log($"Applied animation to new object: {MainManager.AnimationManager.GetCurrentAnimationName()}");
            }
            else
            {
                Debug.LogWarning("Failed to apply animation to new object - Animator component missing or no animations loaded");
            }

            instantiatedObjects.Add(obj);
        }
    }

    public void HandleObjectDeletion()
    {
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            GameObject hitObject = hit.collider.gameObject;
            
            // Check if the hit object is one of our instantiated objects
            if (instantiatedObjects.Contains(hitObject))
            {
                Debug.Log($"Deleting object: {hitObject.name}");
                instantiatedObjects.Remove(hitObject);
                Destroy(hitObject);
            }
            // If it's a child of an instantiated object, find and delete the parent
            else
            {
                Transform parent = hitObject.transform.parent;
                while (parent != null)
                {
                    if (instantiatedObjects.Contains(parent.gameObject))
                    {
                        Debug.Log($"Deleting parent object: {parent.gameObject.name}");
                        instantiatedObjects.Remove(parent.gameObject);
                        Destroy(parent.gameObject);
                        break;
                    }
                    parent = parent.parent;
                }
            }
        }
    }

    public void Preload()
    {
        GameObject preload = MainManager.GetPreloadObject();
        if (preload != null)
        {
            Object.Destroy(preload);
        }

        GameObject[] animatedObjects = MainManager.GetAnimatedObjects();
        int indexAnimatedObject = MainManager.GetAnimatedObjectIndex();
        
        if (animatedObjects == null || animatedObjects.Length == 0 || 
            indexAnimatedObject < 0 || indexAnimatedObject >= animatedObjects.Length)
        {
            Debug.LogError("Cannot preload - invalid animated objects or index");
            return;
        }

        preload = Instantiate(animatedObjects[indexAnimatedObject], Vector3.zero, Quaternion.Euler(0f, currentRotation, 0f));
        preload.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
        
        // Apply preview material
        Material preloadMaterial = MainManager.GetPreloadMaterial();
        if (preloadMaterial != null)
        {
            Renderer[] renderers = preload.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                Material[] materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    materials[i] = preloadMaterial;
                }
                renderer.materials = materials;
            }
        }
        
        // Apply current animation
        Animator animator = preload.GetComponent<Animator>();
        if (animator != null)
        {
            animator.runtimeAnimatorController = MainManager.AnimationManager.GetCurrentAnimatorController();
            Debug.Log($"Applied animation to preview: {MainManager.AnimationManager.GetCurrentAnimationName()}");
        }
        else
        {
            Debug.LogWarning("Failed to apply animation to preview - Animator component missing or no animations loaded");
        }

        // Set reference in main manager
        MainManager.SetPreloadObject(preload);
        isPreloaded = true;
    }

    public void UpdatePreloadScale()
    {
        GameObject preload = MainManager.GetPreloadObject();
        if (preload != null)
        {
            preload.transform.localScale = new Vector3(currentScale, currentScale, currentScale);
        }
    }

    public void UpdatePreloadPosition()
    {
        GameObject preload = MainManager.GetPreloadObject();
        // Skip updating preload position if camera is transitioning
        if (preload == null || MainManager.CameraController.IsCameraTransitioning()) return;

        // Update preload position with increased height
        Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            preload.transform.position = new Vector3(hit.point.x, hit.point.y + 0.2f, hit.point.z);
        }
    }

    public void UpdatePreloadRotation()
    {
        GameObject preload = MainManager.GetPreloadObject();
        if (preload != null)
        {
            preload.transform.rotation = Quaternion.Euler(0f, currentRotation, 0f);
        }
    }

    public void TogglePreloadMode()
    {
        isPreloaded = !isPreloaded;
        if (isPreloaded)
        {
            Preload();
        }
        else
        {
            GameObject preload = MainManager.GetPreloadObject();
            if (preload != null)
            {
                Destroy(preload);
                MainManager.SetPreloadObject(null);
            }
        }
    }

    public void IncrementScale(float amount)
    {
        currentScale += amount;
        if (currentScale > 5f) currentScale = 5f;
        if (currentScale < 0.1f) currentScale = 0.1f;
        UpdatePreloadScale();
    }

    public void IncrementRotation(float amount)
    {
        currentRotation += amount;
        if (currentRotation >= 360f)
        {
            currentRotation = 0f;
        }
        UpdatePreloadRotation();
    }

    public void CleanupInstantiatedObjects()
    {
        foreach (GameObject obj in instantiatedObjects)
        {
            if (obj != null)
            {
                Destroy(obj);
            }
        }
        instantiatedObjects.Clear();
    }

    public List<GameObject> GetInstantiatedObjects() => instantiatedObjects;
    
    public float GetCurrentScale() => currentScale;
    
    public float GetCurrentRotation() => currentRotation;
    
    public bool IsPreloaded() => isPreloaded;
    
    public void SetIsPreloaded(bool value) => isPreloaded = value;
    
    public void HandlePreloadControls()
    {
        GameObject preload = MainManager.GetPreloadObject();
        // Check for null preload object before performing operations
        if (preload == null)
        {
            return;
        }

        UpdatePreloadPosition();
    }

    void Update()
    {
        // Update preload position if in preload mode
        if (isPreloaded && MainManager.GetPreloadObject() != null)
        {
            UpdatePreloadPosition();
        }
    }
} 