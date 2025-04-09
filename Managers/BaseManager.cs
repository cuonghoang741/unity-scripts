using UnityEngine;

public abstract class BaseManager : MonoBehaviour
{
    protected static Manager MainManager => Manager.Instance;

    public virtual void Initialize() { }
    public virtual void OnSceneLoaded() { }
} 