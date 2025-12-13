using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using MultiplayerGame.UI;

/// <summary>
/// Editor utility to automatically set up RoomUI components
/// </summary>
public class RoomUISetup : EditorWindow
{
    private RoomUI targetRoomUI;

    [MenuItem("Tools/Setup Room UI")]
    public static void ShowWindow()
    {
        GetWindow<RoomUISetup>("Room UI Setup");
    }

    private void OnGUI()
    {
        GUILayout.Label("Room UI Setup Utility", EditorStyles.boldLabel);
        GUILayout.Space(10);

        targetRoomUI = (RoomUI)EditorGUILayout.ObjectField("Target RoomUI", targetRoomUI, typeof(RoomUI), true);

        GUILayout.Space(10);

        if (GUILayout.Button("Auto-Setup Missing Components", GUILayout.Height(30)))
        {
            if (targetRoomUI != null)
            {
                SetupRoomUI();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a RoomUI component first!", "OK");
            }
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Create Complete Room UI from Scratch", GUILayout.Height(30)))
        {
            CreateCompleteRoomUI();
        }
    }

    private void SetupRoomUI()
    {
        SerializedObject so = new SerializedObject(targetRoomUI);
        
        // Check if panels exist, create them if not
        SerializedProperty createRoomPanelProp = so.FindProperty("createRoomPanel");
        GameObject panel = createRoomPanelProp.objectReferenceValue as GameObject;
        
        if (panel == null)
        {
            Debug.Log("[RoomUISetup] Create Room Panel not assigned. Creating panel structure...");
            
            // Find or create Canvas
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();
            }

            // Create panels if they don't exist
            CreatePanelsIfMissing(so, targetRoomUI.transform);
            so.ApplyModifiedProperties();
            
            // Reload the panel reference
            panel = createRoomPanelProp.objectReferenceValue as GameObject;
            
            if (panel == null)
            {
                Debug.LogError("[RoomUISetup] Failed to create Create Room Panel!");
                EditorUtility.DisplayDialog("Error", "Failed to create panels. Please use 'Create Complete Room UI from Scratch' instead.", "OK");
                return;
            }
        }

        Transform panelTransform = panel.transform;

        // Setup missing components
        SetupInputFieldIfMissing(so, "roomNameInput", panelTransform, "RoomNameInput");
        SetupInputFieldIfMissing(so, "maxPlayersInput", panelTransform, "MaxPlayersInput");
        SetupToggleIfMissing(so, "privateToggle", panelTransform, "PrivateToggle");
        SetupInputFieldIfMissing(so, "passwordInput", panelTransform, "PasswordInput");
        SetupButtonIfMissing(so, "confirmCreateButton", panelTransform, "ConfirmCreateButton");
        SetupButtonIfMissing(so, "cancelCreateButton", panelTransform, "CancelCreateButton");

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(targetRoomUI);
        
        Debug.Log("[RoomUISetup] Setup complete!");
        EditorUtility.DisplayDialog("Success", "RoomUI components have been set up!", "OK");
    }

    private void CreatePanelsIfMissing(SerializedObject so, Transform parent)
    {
        CreatePanelIfMissing(so, "mainMenuPanel", parent, "MainMenuPanel");
        CreatePanelIfMissing(so, "createRoomPanel", parent, "CreateRoomPanel");
        CreatePanelIfMissing(so, "roomBrowserPanel", parent, "RoomBrowserPanel");
        CreatePanelIfMissing(so, "lobbyPanel", parent, "LobbyPanel");
    }

    private void CreatePanelIfMissing(SerializedObject so, string propertyName, Transform parent, string panelName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop.objectReferenceValue == null)
        {
            GameObject panel = CreatePanel(panelName, parent);
            prop.objectReferenceValue = panel;
            Debug.Log($"[RoomUISetup] Created {panelName}");
        }
    }

    private void SetupButtonIfMissing(SerializedObject so, string propertyName, Transform parent, string childName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop.objectReferenceValue == null)
        {
            // Try to find existing
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Button button = existing.GetComponent<Button>();
                if (button != null)
                {
                    prop.objectReferenceValue = button;
                    Debug.Log($"[RoomUISetup] Found and assigned existing {childName}");
                    return;
                }
            }

            // Create new
            Vector2 position = childName.Contains("Confirm") ? new Vector2(-110, -150) : new Vector2(110, -150);
            Color color = childName.Contains("Confirm") ? Color.green : Color.red;
            string text = childName.Contains("Confirm") ? "Create" : "Cancel";
            
            GameObject buttonObj = CreateButton(childName, parent, position, text, color);
            prop.objectReferenceValue = buttonObj.GetComponent<Button>();
            Debug.Log($"[RoomUISetup] Created new {childName}");
        }
    }

    private void SetupToggleIfMissing(SerializedObject so, string propertyName, Transform parent, string childName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop.objectReferenceValue == null)
        {
            // Try to find existing
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                Toggle toggle = existing.GetComponent<Toggle>();
                if (toggle != null)
                {
                    prop.objectReferenceValue = toggle;
                    Debug.Log($"[RoomUISetup] Found and assigned existing {childName}");
                    return;
                }
            }

            // Create new
            GameObject toggleObj = CreateToggle(childName, parent);
            prop.objectReferenceValue = toggleObj.GetComponent<Toggle>();
            Debug.Log($"[RoomUISetup] Created new {childName}");
        }
    }

    private void SetupInputFieldIfMissing(SerializedObject so, string propertyName, Transform parent, string childName)
    {
        SerializedProperty prop = so.FindProperty(propertyName);
        if (prop.objectReferenceValue == null)
        {
            // Try to find existing
            Transform existing = parent.Find(childName);
            if (existing != null)
            {
                TMP_InputField inputField = existing.GetComponent<TMP_InputField>();
                if (inputField != null)
                {
                    prop.objectReferenceValue = inputField;
                    Debug.Log($"[RoomUISetup] Found and assigned existing {childName}");
                    return;
                }
            }

            // Create new
            GameObject inputObj = CreateInputField(childName, parent);
            prop.objectReferenceValue = inputObj.GetComponent<TMP_InputField>();
            Debug.Log($"[RoomUISetup] Created new {childName}");
        }
    }

    private GameObject CreateToggle(string name, Transform parent)
    {
        GameObject toggleObj = new GameObject(name);
        toggleObj.transform.SetParent(parent, false);
        
        RectTransform rt = toggleObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 30);
        
        Toggle toggle = toggleObj.AddComponent<Toggle>();
        
        // Background
        GameObject background = new GameObject("Background");
        background.transform.SetParent(toggleObj.transform, false);
        Image bgImage = background.AddComponent<Image>();
        bgImage.color = new Color(0.2f, 0.2f, 0.2f, 1f);
        RectTransform bgRect = background.GetComponent<RectTransform>();
        bgRect.sizeDelta = new Vector2(20, 20);
        bgRect.anchoredPosition = new Vector2(-80, 0);
        
        // Checkmark
        GameObject checkmark = new GameObject("Checkmark");
        checkmark.transform.SetParent(background.transform, false);
        Image checkImage = checkmark.AddComponent<Image>();
        checkImage.color = Color.green;
        RectTransform checkRect = checkmark.GetComponent<RectTransform>();
        checkRect.sizeDelta = new Vector2(16, 16);
        
        // Label
        GameObject label = new GameObject("Label");
        label.transform.SetParent(toggleObj.transform, false);
        TextMeshProUGUI labelText = label.AddComponent<TextMeshProUGUI>();
        labelText.text = name.Replace("Toggle", "");
        labelText.fontSize = 14;
        labelText.color = Color.white;
        RectTransform labelRect = label.GetComponent<RectTransform>();
        labelRect.sizeDelta = new Vector2(150, 30);
        labelRect.anchoredPosition = new Vector2(10, 0);
        
        toggle.targetGraphic = bgImage;
        toggle.graphic = checkImage;
        
        return toggleObj;
    }

    private GameObject CreateInputField(string name, Transform parent)
    {
        GameObject inputObj = new GameObject(name);
        inputObj.transform.SetParent(parent, false);
        
        RectTransform rt = inputObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(200, 30);
        
        Image image = inputObj.AddComponent<Image>();
        image.color = new Color(0.15f, 0.15f, 0.15f, 1f);
        
        TMP_InputField inputField = inputObj.AddComponent<TMP_InputField>();
        
        // Text Area
        GameObject textArea = new GameObject("Text Area");
        textArea.transform.SetParent(inputObj.transform, false);
        RectTransform textAreaRect = textArea.AddComponent<RectTransform>();
        textAreaRect.anchorMin = Vector2.zero;
        textAreaRect.anchorMax = Vector2.one;
        textAreaRect.sizeDelta = Vector2.zero;
        textAreaRect.offsetMin = new Vector2(10, 6);
        textAreaRect.offsetMax = new Vector2(-10, -7);
        
        // Placeholder
        GameObject placeholder = new GameObject("Placeholder");
        placeholder.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI placeholderText = placeholder.AddComponent<TextMeshProUGUI>();
        placeholderText.text = $"Enter {name.Replace("Input", "")}...";
        placeholderText.fontSize = 14;
        placeholderText.color = new Color(0.5f, 0.5f, 0.5f, 1f);
        placeholderText.fontStyle = FontStyles.Italic;
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.sizeDelta = Vector2.zero;
        
        // Text
        GameObject text = new GameObject("Text");
        text.transform.SetParent(textArea.transform, false);
        TextMeshProUGUI textComponent = text.AddComponent<TextMeshProUGUI>();
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        
        inputField.textViewport = textAreaRect;
        inputField.textComponent = textComponent;
        inputField.placeholder = placeholderText;
        
        // Special settings for password field
        if (name.Contains("Password"))
        {
            inputField.contentType = TMP_InputField.ContentType.Password;
        }
        else if (name.Contains("MaxPlayers"))
        {
            inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
            placeholderText.text = "4";
        }
        
        return inputObj;
    }

    private void CreateCompleteRoomUI()
    {
        // Find Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
        }

        // Create RoomUI GameObject
        GameObject roomUIObj = new GameObject("RoomUI");
        roomUIObj.transform.SetParent(canvas.transform, false);
        RoomUI roomUI = roomUIObj.AddComponent<RoomUI>();

        // Create panels
        GameObject mainMenuPanel = CreatePanel("MainMenuPanel", roomUIObj.transform);
        GameObject createRoomPanel = CreatePanel("CreateRoomPanel", roomUIObj.transform);
        GameObject roomBrowserPanel = CreatePanel("RoomBrowserPanel", roomUIObj.transform);
        GameObject lobbyPanel = CreatePanel("LobbyPanel", roomUIObj.transform);

        // Setup Create Room Panel
        SetupCreateRoomPanel(createRoomPanel.transform);

        SerializedObject so = new SerializedObject(roomUI);
        so.FindProperty("mainMenuPanel").objectReferenceValue = mainMenuPanel;
        so.FindProperty("createRoomPanel").objectReferenceValue = createRoomPanel;
        so.FindProperty("roomBrowserPanel").objectReferenceValue = roomBrowserPanel;
        so.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
        so.ApplyModifiedProperties();

        EditorUtility.SetDirty(roomUI);
        Selection.activeGameObject = roomUIObj;

        Debug.Log("[RoomUISetup] Complete Room UI created!");
        EditorUtility.DisplayDialog("Success", "Complete Room UI has been created! Please assign remaining components in the Inspector.", "OK");
    }

    private GameObject CreatePanel(string name, Transform parent)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        
        RectTransform rt = panel.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        
        panel.SetActive(false);
        
        return panel;
    }

    private void SetupCreateRoomPanel(Transform panel)
    {
        float yPos = 150;
        float spacing = -60;

        // Title
        CreateText("Title", panel, new Vector2(0, 200), "Create Room", 24, FontStyles.Bold);

        // Room Name Input
        CreateText("RoomNameLabel", panel, new Vector2(0, yPos), "Room Name:", 16, FontStyles.Normal);
        GameObject roomNameInput = CreateInputField("RoomNameInput", panel);
        roomNameInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos + spacing);

        yPos += spacing * 2;

        // Max Players Input
        CreateText("MaxPlayersLabel", panel, new Vector2(0, yPos), "Max Players:", 16, FontStyles.Normal);
        GameObject maxPlayersInput = CreateInputField("MaxPlayersInput", panel);
        maxPlayersInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos + spacing);

        yPos += spacing * 2;

        // Private Toggle
        GameObject privateToggle = CreateToggle("PrivateToggle", panel);
        privateToggle.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos);

        yPos += spacing;

        // Password Input
        GameObject passwordInput = CreateInputField("PasswordInput", panel);
        passwordInput.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, yPos);

        yPos += spacing * 2;

        // Buttons
        GameObject confirmBtn = CreateButton("ConfirmCreateButton", panel, new Vector2(-110, yPos), "Create", Color.green);
        GameObject cancelBtn = CreateButton("CancelCreateButton", panel, new Vector2(110, yPos), "Cancel", Color.red);
    }

    private GameObject CreateText(string name, Transform parent, Vector2 position, string text, int fontSize, FontStyles style)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        
        RectTransform rt = textObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(300, 30);
        rt.anchoredPosition = position;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return textObj;
    }

    private GameObject CreateButton(string name, Transform parent, Vector2 position, string text, Color color)
    {
        GameObject btnObj = new GameObject(name);
        btnObj.transform.SetParent(parent, false);
        
        RectTransform rt = btnObj.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(100, 40);
        rt.anchoredPosition = position;
        
        Image image = btnObj.AddComponent<Image>();
        image.color = color;
        
        Button button = btnObj.AddComponent<Button>();
        button.targetGraphic = image;
        
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(btnObj.transform, false);
        
        RectTransform textRt = textObj.AddComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.sizeDelta = Vector2.zero;
        
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        
        return btnObj;
    }
}
