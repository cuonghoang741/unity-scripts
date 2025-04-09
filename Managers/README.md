# Modular Manager Architecture

This folder contains a modular version of the original `Manager.cs` file. Each module focuses on a specific responsibility, making the code more maintainable and easier to understand.

## Folder Structure

- `BaseManager.cs` - Abstract base class that all managers inherit from
- `AnimationManager.cs` - Handles animation loading and control
- `CameraController.cs` - Manages camera positions and transitions
- `ObjectManager.cs` - Handles object instantiation, deletion, and management
- `LogManager.cs` - Manages logging and the log window
- `UIManager.cs` - Handles UI and GUI elements
- `RecordingManager.cs` - Manages screenshots and video recording

## Migration Process

To migrate from the monolithic `Manager.cs` to this modular architecture:

1. Make sure all the files in the Managers folder exist and have the correct content
2. Run the "Tools/Migrate Manager to Modular Structure" menu item to check if all files are ready
3. Update the `Manager.cs` file to use the modular structure
4. Test each scene to ensure everything works correctly

## Benefits of Modular Architecture

- **Maintainability**: Easier to understand and modify specific functionality
- **Testability**: Each module can be tested independently
- **Readability**: Smaller files with focused responsibilities
- **Extensibility**: New features can be added without modifying existing code
- **Collaboration**: Multiple developers can work on different modules simultaneously

## How to Use

The main `Manager` class still acts as the central point of control, but now delegates specific tasks to each specialized manager. You can access these managers through the `Manager.Instance` property:

```csharp
// Animation example
Manager.Instance.AnimationManager.SwitchAnimation(1);

// Camera example
Manager.Instance.CameraController.SwitchCameraPosition();

// Object example
Manager.Instance.ObjectManager.HandleObjectInstantiation();

// UI example
Manager.Instance.UIManager.ToggleHotkeyHelp();

// Logging example
Manager.Instance.LogManager.ToggleLogWindow();

// Recording example
Manager.Instance.RecordingManager.ToggleRecording();
``` 