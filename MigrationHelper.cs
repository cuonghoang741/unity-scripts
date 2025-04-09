using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

#if UNITY_EDITOR
public class MigrationHelper : MonoBehaviour
{
    [MenuItem("Tools/Migrate Manager to Modular Structure")]
    public static void MigrateToModularStructure()
    {
        Debug.Log("Starting migration process for Manager.cs...");
        
        // Check if Managers directory exists
        string managersDir = "Assets/Scripts/Managers";
        if (!Directory.Exists(managersDir))
        {
            Directory.CreateDirectory(managersDir);
            Debug.Log("Created Managers directory");
        }
        
        // Check if files already exist
        Dictionary<string, bool> files = new Dictionary<string, bool>
        {
            { "BaseManager.cs", false },
            { "AnimationManager.cs", false },
            { "CameraController.cs", false },
            { "UIManager.cs", false },
            { "ObjectManager.cs", false },
            { "LogManager.cs", false },
            { "RecordingManager.cs", false }
        };
        
        foreach (var file in files.Keys)
        {
            string path = Path.Combine(managersDir, file);
            files[file] = File.Exists(path);
        }
        
        // Display result
        string message = "Migration Helper\n\n" +
                        "To complete the migration process, make sure all these files exist:\n\n";
        
        bool allFilesExist = true;
        foreach (var pair in files)
        {
            message += pair.Value ? 
                "✓ " + pair.Key + " (exists)\n" : 
                "✗ " + pair.Key + " (missing)\n";
            
            if (!pair.Value)
                allFilesExist = false;
        }
        
        if (allFilesExist)
        {
            message += "\nAll files exist! Now you need to:\n" +
                      "1. Make sure Manager.cs is using the modular structure\n" +
                      "2. Update any references in your scenes to use the new modular structure\n" +
                      "3. Test your scenes to ensure everything works correctly";
        }
        else
        {
            message += "\nSome files are missing. Please create all the required files in the Managers folder.";
        }
        
        EditorUtility.DisplayDialog("Migration Helper", message, "OK");
    }
}
#endif 