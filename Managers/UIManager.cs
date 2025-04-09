using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class UIManager : BaseManager
{
    [SerializeField] private TextMeshProUGUI helpText;
    
    private bool isHotkeyHelpVisible = false;
    private bool showPreviewGuide = false;
    
    // Search modal variables
    private bool isSearchModalVisible = false;
    private string searchQuery = "";
    private Vector2 scrollPosition;
    private int selectedAnimationIndex = -1;
    private float lastClickTime;
    private const float doubleClickTime = 0.3f;
    private float keyRepeatDelay = 0.15f; // Delay between key repeats
    private float lastKeyPressTime = 0f;
    private float modalWidth = 400f;
    private float modalHeight = 500f;
    private float resultsAreaHeight = 380f; // Height of the results area in the search modal
    private float modalMargin = 20f; // Margin from screen edge
    
    // Styles
    private GUIStyle helpTextStyle;
    private GUIStyle searchBoxStyle;
    private GUIStyle searchResultStyle;

    public override void Initialize()
    {
        InitializeHelpTextStyle();
        Debug.Log("UIManager initialized");
    }

    private void InitializeHelpTextStyle()
    {
        helpTextStyle = new GUIStyle();
        helpTextStyle.fontSize = 20;
        helpTextStyle.normal.textColor = Color.white;
        helpTextStyle.alignment = TextAnchor.UpperLeft;
        helpTextStyle.wordWrap = true;
    }

    // Utility method to create a colored texture
    private Texture2D CreateColorTexture(Color color)
    {
        Texture2D texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    public void ToggleHotkeyHelp()
    {
        isHotkeyHelpVisible = !isHotkeyHelpVisible;
        Debug.Log($"Help visibility: {isHotkeyHelpVisible}");
    }

    public void TogglePreviewGuide(bool show)
    {
        showPreviewGuide = show;
    }

    public void ToggleSearchModal()
    {
        isSearchModalVisible = !isSearchModalVisible;
        if (isSearchModalVisible)
        {
            // Reset search and selection
            searchQuery = "";
            scrollPosition = Vector2.zero;
            
            // Update filtered animations
            MainManager.AnimationManager.UpdateFilteredAnimations();
            
            selectedAnimationIndex = MainManager.AnimationManager.GetFilteredAnimations().Count > 0 ? 0 : -1;
        }
    }

    void Update()
    {
        if (isSearchModalVisible)
        {
            HandleSearchModalKeyboard();
        }
    }

    private void HandleSearchModalKeyboard()
    {
        if (!isSearchModalVisible) return;
        
        List<string> filteredAnimations = MainManager.AnimationManager.GetFilteredAnimations();
        if (filteredAnimations.Count == 0) return;

        float currentTime = Time.time;
        bool canRepeatKey = (currentTime - lastKeyPressTime) > keyRepeatDelay;

        if (Keyboard.current != null)
        {
            bool keyPressed = false;

            // Handle Enter key to close modal
            if (Keyboard.current.enterKey.wasPressedThisFrame)
            {
                isSearchModalVisible = false;
                return;
            }

            // Only handle navigation if we're not typing in search
            if (!GUI.GetNameOfFocusedControl().Equals("SearchBox") && canRepeatKey)
            {
                // Handle up arrow
                if (Keyboard.current.upArrowKey.isPressed)
                {
                    selectedAnimationIndex--;
                    if (selectedAnimationIndex < 0) selectedAnimationIndex = filteredAnimations.Count - 1;
                    keyPressed = true;
                    // Auto select when moving with arrow keys
                    if (selectedAnimationIndex >= 0 && selectedAnimationIndex < filteredAnimations.Count)
                    {
                        MainManager.AnimationManager.SelectAnimation(filteredAnimations[selectedAnimationIndex]);
                    }
                }
                // Handle down arrow
                else if (Keyboard.current.downArrowKey.isPressed)
                {
                    selectedAnimationIndex++;
                    if (selectedAnimationIndex >= filteredAnimations.Count) selectedAnimationIndex = 0;
                    keyPressed = true;
                    // Auto select when moving with arrow keys
                    if (selectedAnimationIndex >= 0 && selectedAnimationIndex < filteredAnimations.Count)
                    {
                        MainManager.AnimationManager.SelectAnimation(filteredAnimations[selectedAnimationIndex]);
                    }
                }
            }

            if (keyPressed)
            {
                lastKeyPressTime = currentTime;
                // Ensure selected item is visible in scroll view
                if (selectedAnimationIndex >= 0)
                {
                    float itemTop = selectedAnimationIndex * 35;
                    float itemBottom = itemTop + 35;
                    
                    if (itemTop < scrollPosition.y)
                    {
                        scrollPosition.y = itemTop;
                    }
                    else if (itemBottom > scrollPosition.y + resultsAreaHeight)
                    {
                        scrollPosition.y = itemBottom - resultsAreaHeight;
                    }
                }
            }
        }
    }

    void OnGUI()
    {
        // Initialize styles if they haven't been initialized yet
        if (searchBoxStyle == null || searchBoxStyle.normal.background == null)
        {
            searchBoxStyle = new GUIStyle();
            searchBoxStyle.fontSize = GUI.skin.textField.fontSize;
            searchBoxStyle.font = GUI.skin.textField.font;
            searchBoxStyle.alignment = TextAnchor.MiddleLeft;
            searchBoxStyle.normal.textColor = Color.white;
            searchBoxStyle.padding = new RectOffset(10, 10, 5, 5);

            searchResultStyle = new GUIStyle(GUI.skin.button);
            searchResultStyle.fontSize = 16;
            searchResultStyle.alignment = TextAnchor.MiddleLeft;
            searchResultStyle.normal.textColor = Color.white;
            searchResultStyle.hover.textColor = Color.yellow;
            searchResultStyle.padding = new RectOffset(10, 10, 5, 5);
            
            // Make sure all states have proper colors and backgrounds
            Color btnColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            Texture2D normalTex = new Texture2D(1, 1);
            normalTex.SetPixel(0, 0, btnColor);
            normalTex.Apply();
            searchResultStyle.normal.background = normalTex;
            
            // Set hover background
            Texture2D hoverTex = new Texture2D(1, 1);
            hoverTex.SetPixel(0, 0, new Color(0.3f, 0.3f, 0.3f, 0.9f));
            hoverTex.Apply();
            searchResultStyle.hover.background = hoverTex;
            
            // Ensure the active state has a background too
            Texture2D activeTex = new Texture2D(1, 1);
            activeTex.SetPixel(0, 0, new Color(0.25f, 0.25f, 0.25f, 0.9f));
            activeTex.Apply();
            searchResultStyle.active.background = activeTex;
            searchResultStyle.active.textColor = Color.white;
        }

        // Show preview mode guide
        if (showPreviewGuide && MainManager.ObjectManager.IsPreloaded())
        {
            float padding = 20f;
            float guideHeight = 100f; // Increased height to accommodate status bar
            
            // Full width guide at bottom
            Rect guideRect = new Rect(0, Screen.height - guideHeight, Screen.width, guideHeight);
            
            // Semi-transparent gradient background
            GUI.color = new Color(0, 0, 0, 0.85f);
            GUI.DrawTexture(guideRect, Texture2D.whiteTexture);
            
            // Add a subtle top border line
            GUI.color = new Color(1, 1, 1, 0.3f);
            GUI.DrawTexture(new Rect(0, guideRect.y, Screen.width, 1), Texture2D.whiteTexture);
            
            // Reset color for text
            GUI.color = Color.white;

            // Status bar showing current character and animation
            GameObject[] animatedObjects = MainManager.GetAnimatedObjects();
            int indexAnimatedObject = MainManager.GetAnimatedObjectIndex();
            string characterName = animatedObjects[indexAnimatedObject].name;
            string animationName = MainManager.AnimationManager.GetCurrentAnimationName();
            
            // Draw status bar with golden color for values
            float statusY = Screen.height - guideHeight + padding/2;
            GUI.Label(new Rect(padding, statusY, Screen.width - padding * 2, 20),
                $"Current Character: <color=#FFD700>{characterName}</color>     Current Animation: <color=#FFD700>{animationName}</color>",
                new GUIStyle(helpTextStyle) { richText = true, fontSize = 14, alignment = TextAnchor.MiddleCenter });

            // Create three columns for controls (moved down to accommodate status bar)
            float columnWidth = (Screen.width - (padding * 4)) / 3;
            float controlsY = statusY + 25; // Position below status bar
            
            // Left column - Object Selection
            GUI.Label(new Rect(padding, controlsY, columnWidth, guideHeight - padding),
                "【Object Selection】\n" +
                "Q / E - Switch Objects",
                helpTextStyle);

            // Middle column - Transformation
            GUI.Label(new Rect(padding * 2 + columnWidth, controlsY, columnWidth, guideHeight - padding),
                "【Animation】\n" +
                "M / N - Switch     R - Rotate 15°",
                helpTextStyle);

            // Right column - Actions
            GUI.Label(new Rect(padding * 3 + columnWidth * 2, controlsY, columnWidth, guideHeight - padding),
                "【Actions】\n" +
                "Left Click - Place     P - Exit",
                helpTextStyle);
        }

        // Show hotkey help
        if (isHotkeyHelpVisible && helpTextStyle != null)
        {
            string helpText = "HOTKEY GUIDE\n\n" +
                            "Navigation:\n" +
                            "H - Toggle this help\n" +
                            "P - Toggle preview mode\n" +
                            "U - Switch scene\n" +
                            "L - Toggle log window\n\n" +
                            "Animation & Object:\n" +
                            "M / N - Switch animations\n" +
                            "Ctrl+F - Search animations\n" +
                            "Q/E - Switch objects\n" +
                            "N - Replay animation\n\n" +
                            "Transformation:\n" +
                            "+ / = - Scale up\n" +
                            "- - Scale down\n" +
                            "R - Rotate 15 degrees\n\n" +
                            "Actions:\n" +
                            "Left Click - Place object\n" +
                            "Right Click - Delete object\n" +
                            "Z - Take screenshot";

            // Create semi-transparent background
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(10, 10, 300, 400), "");
            
            // Reset color for text
            GUI.color = Color.white;
            GUI.Label(new Rect(20, 20, 280, 380), helpText, helpTextStyle);
        }

        // Draw search modal
        if (isSearchModalVisible)
        {
            // Calculate modal position - place at left corner
            float modalX = modalMargin;
            float modalY = modalMargin;

            // Draw modal background
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 1f); // Darker background color
            GUI.DrawTexture(new Rect(modalX, modalY, modalWidth, modalHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            // Modal title with count
            GUI.Label(
                new Rect(modalX + 20, modalY + 10, modalWidth - 100, 30),
                $"Search Animations: ({MainManager.AnimationManager.GetFilteredAnimations().Count}/{MainManager.AnimationManager.GetAnimationCount()})",
                new GUIStyle(GUI.skin.label) { fontSize = 18, normal = { textColor = Color.white } }
            );

            // Close button
            if (GUI.Button(new Rect(modalX + modalWidth - 80, modalY + 10, 60, 30), "Close"))
            {
                isSearchModalVisible = false;
            }

            // Search box with dark background and white text
            GUI.backgroundColor = new Color(0.2f, 0.2f, 0.2f, 1f);
            GUI.contentColor = Color.white;
            
            // Create custom style for search box
            GUIStyle searchStyle = new GUIStyle(GUI.skin.textField)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleLeft
            };
            searchStyle.normal.textColor = Color.white;
            searchStyle.normal.background = CreateColorTexture(new Color(0.2f, 0.2f, 0.2f, 1f));
            searchStyle.hover.background = CreateColorTexture(new Color(0.25f, 0.25f, 0.25f, 1f));
            searchStyle.focused.background = CreateColorTexture(new Color(0.3f, 0.3f, 0.3f, 1f));
            searchStyle.focused.textColor = Color.white;
            searchStyle.padding = new RectOffset(10, 10, 5, 5);

            GUI.SetNextControlName("SearchBox");
            string newSearchQuery = GUI.TextField(
                new Rect(modalX + 20, modalY + 50, modalWidth - 40, 30),
                searchQuery,
                searchStyle
            );

            // Reset GUI colors
            GUI.backgroundColor = Color.white;
            GUI.contentColor = Color.white;

            if (newSearchQuery != searchQuery)
            {
                searchQuery = newSearchQuery;
                MainManager.AnimationManager.UpdateFilteredAnimations(searchQuery);
                selectedAnimationIndex = MainManager.AnimationManager.GetFilteredAnimations().Count > 0 ? 0 : -1;
            }

            // Focus on search box when modal opens
            if (Event.current.type == EventType.Repaint)
            {
                GUI.FocusControl("SearchBox");
            }

            // Results area
            float resultsAreaY = modalY + 100;

            // Draw results background
            GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f); // Slightly lighter than modal background
            GUI.DrawTexture(new Rect(modalX + 20, resultsAreaY, modalWidth - 40, resultsAreaHeight), Texture2D.whiteTexture);
            GUI.color = Color.white;

            List<string> filteredAnimations = MainManager.AnimationManager.GetFilteredAnimations();
            if (filteredAnimations.Count == 0)
            {
                string message = string.IsNullOrEmpty(searchQuery) ? "Loading animations..." : "No animations found";
                GUI.Label(
                    new Rect(modalX + 20, resultsAreaY + 10, modalWidth - 40, 30),
                    message,
                    new GUIStyle(GUI.skin.label) { fontSize = 14, normal = { textColor = Color.white }, alignment = TextAnchor.MiddleCenter }
                );
            }
            else
            {
                // Begin scroll view
                scrollPosition = GUI.BeginScrollView(
                    new Rect(modalX + 20, resultsAreaY, modalWidth - 40, resultsAreaHeight),
                    scrollPosition,
                    new Rect(0, 0, modalWidth - 60, filteredAnimations.Count * 35)
                );

                try
                {
                    // Draw each animation button
                    for (int i = 0; i < filteredAnimations.Count; i++)
                    {
                        string animName = filteredAnimations[i];
                        if (string.IsNullOrEmpty(animName)) continue;

                        Rect buttonRect = new Rect(0, i * 35, modalWidth - 60, 30);
                        DrawAnimationButton(buttonRect, animName, i);
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error in animation list: {e.Message}\n{e.StackTrace}");
                }

                GUI.EndScrollView();
            }

            // Handle escape key to close
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
            {
                isSearchModalVisible = false;
                Event.current.Use();
            }

            // Prevent clicks outside from passing through
            if (Event.current.type == EventType.MouseDown &&
                !new Rect(modalX, modalY, modalWidth, modalHeight).Contains(Event.current.mousePosition))
            {
                isSearchModalVisible = false;
                Event.current.Use();
            }
        }
    }

    private void DrawAnimationButton(Rect buttonRect, string animName, int index)
    {
        bool isSelected = (index == selectedAnimationIndex);
        bool isCurrent = (MainManager.AnimationManager.GetCurrentAnimationName() == animName);

        // Draw button background
        if (isSelected)
        {
            // Selected item highlight with pulsing effect
            float pulse = Mathf.PingPong(Time.time * 2, 0.3f) + 0.7f;
            GUI.color = new Color(0.4f, 0.6f, 1f, pulse);
        }
        else
        {
            // Normal alternating colors
            GUI.color = (index % 2 == 0) ? new Color(0.3f, 0.3f, 0.3f, 1f) : new Color(0.25f, 0.25f, 0.25f, 1f);
        }
        GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
        GUI.color = Color.white;

        // Current animation indicator
        if (isCurrent)
        {
            GUI.color = new Color(0.4f, 0.6f, 1f, 0.3f);
            GUI.DrawTexture(buttonRect, Texture2D.whiteTexture);
            GUI.color = Color.white;
        }

        // Create button style with hover effect
        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.normal.background = null;
        buttonStyle.normal.textColor = isSelected ? Color.yellow : Color.white;
        buttonStyle.hover.textColor = Color.yellow;
        buttonStyle.alignment = TextAnchor.MiddleLeft;
        buttonStyle.padding = new RectOffset(10, 10, 0, 0);

        // Add a small play icon if this is the current animation
        string displayText = isCurrent ? "▶ " + animName : "  " + animName;

        // Draw the button
        if (GUI.Button(buttonRect, displayText, buttonStyle))
        {
            MainManager.AnimationManager.SelectAnimation(animName);
        }

        // Mouse hover detection to update selectedAnimationIndex and auto-select
        if (buttonRect.Contains(Event.current.mousePosition))
        {
            if (selectedAnimationIndex != index)
            {
                selectedAnimationIndex = index;
                MainManager.AnimationManager.SelectAnimation(animName); // Auto-select when hovering
            }
        }
    }

    public bool IsHotkeyHelpVisible() => isHotkeyHelpVisible;
    
    public bool IsShowingPreviewGuide() => showPreviewGuide;
    
    public bool IsSearchModalVisible() => isSearchModalVisible;
    
    public void SetSearchModalVisible(bool visible)
    {
        isSearchModalVisible = visible;
    }
} 