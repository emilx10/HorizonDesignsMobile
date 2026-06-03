using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DiceUICanvas - Manages the Dice UI Canvas with tab-based sections
/// Organizes content into: Main, Upgrade, Spells, Settings, and Info sections
/// Each section has a header bar, content area, and bottom buttons
/// </summary>
public class DiceUICanvas : MonoBehaviour
{
    [SerializeField] private RectTransform contentArea;
    [SerializeField] private Color activeTabColor = new Color(0.8f, 0.2f, 0.4f); // Pink/Magenta
    [SerializeField] private Color inactiveTabColor = new Color(0.5f, 0.5f, 0.5f); // Gray

    private Canvas canvas;
    private CanvasScaler canvasScaler;
    private GraphicRaycaster graphicRaycaster;
    
    private Button[] tabButtons;
    private RectTransform[] tabContents;
    private int currentActiveTab = 0;

    private void Awake()
    {
        SetupCanvas();
        CreateTabLayout();
        SetupTabNavigation();
    }

    /// <summary>
    /// Sets up the main Canvas component with proper settings for mobile
    /// </summary>
    private void SetupCanvas()
    {
        // Get or add Canvas component
        canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = gameObject.AddComponent<Canvas>();
        }

        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        // Get or add CanvasScaler for responsive design
        canvasScaler = GetComponent<CanvasScaler>();
        if (canvasScaler == null)
        {
            canvasScaler = gameObject.AddComponent<CanvasScaler>();
        }

        // Configure Canvas Scaler - mobile phone aspect ratio
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1080, 1920); // Portrait mobile resolution

        // Get or add GraphicRaycaster for UI interaction
        graphicRaycaster = GetComponent<GraphicRaycaster>();
        if (graphicRaycaster == null)
        {
            graphicRaycaster = gameObject.AddComponent<GraphicRaycaster>();
        }

        // Setup RectTransform to fill screen
        RectTransform canvasRect = GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;
        canvasRect.localScale = Vector3.one;

        Debug.Log("Canvas setup complete");
    }

    /// <summary>
    /// Creates the tab-based layout structure
    /// Structure: Header → Tab Buttons → Content Area → Bottom Buttons
    /// </summary>
    private void CreateTabLayout()
    {
        // Create Header
        GameObject headerObj = new GameObject("Header");
        headerObj.transform.SetParent(transform, false);
        RectTransform headerRect = headerObj.AddComponent<RectTransform>();
        headerRect.anchorMin = new Vector2(0, 1);
        headerRect.anchorMax = new Vector2(1, 1);
        headerRect.offsetMin = new Vector2(0, -80);
        headerRect.offsetMax = Vector2.zero;
        
        Image headerBg = headerObj.AddComponent<Image>();
        headerBg.color = new Color(0.5f, 0.2f, 0.3f); // Dark maroon

        // Add title text to header
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(headerObj.transform, false);
        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchoredPosition = Vector2.zero;
        titleRect.sizeDelta = Vector2.zero;
        titleRect.anchorMin = Vector2.zero;
        titleRect.anchorMax = Vector2.one;
        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "DICE GAME";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 30;
        titleText.fontStyle = FontStyle.Bold;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.color = Color.white;

        // Create Tab Button Container
        GameObject tabContainer = new GameObject("TabButtons");
        tabContainer.transform.SetParent(transform, false);
        RectTransform tabContainerRect = tabContainer.AddComponent<RectTransform>();
        tabContainerRect.anchorMin = new Vector2(0, 1);
        tabContainerRect.anchorMax = new Vector2(1, 1);
        tabContainerRect.offsetMin = new Vector2(0, -160);
        tabContainerRect.offsetMax = new Vector2(0, -80);

        HorizontalLayoutGroup tabLayout = tabContainer.AddComponent<HorizontalLayoutGroup>();
        tabLayout.childForceExpandWidth = true;
        tabLayout.childForceExpandHeight = true;
        tabLayout.childControlSize = true;
        tabLayout.spacing = 5;
        tabLayout.padding = new RectOffset(5, 5, 5, 5);

        // Create 5 tab buttons
        string[] tabNames = { "Main", "Upgrade", "Spells", "Settings", "Info" };
        tabButtons = new Button[5];

        for (int i = 0; i < 5; i++)
        {
            GameObject tabButtonObj = new GameObject(tabNames[i] + "Tab");
            tabButtonObj.transform.SetParent(tabContainer.transform, false);
            
            RectTransform tabBtnRect = tabButtonObj.AddComponent<RectTransform>();
            tabBtnRect.anchorMin = Vector2.zero;
            tabBtnRect.anchorMax = Vector2.one;

            LayoutElement tabBtnLayout = tabButtonObj.AddComponent<LayoutElement>();
            tabBtnLayout.preferredWidth = 150;
            tabBtnLayout.preferredHeight = 70;

            Image tabBtnImage = tabButtonObj.AddComponent<Image>();
            tabBtnImage.color = inactiveTabColor;

            Button tabBtn = tabButtonObj.AddComponent<Button>();
            tabButtons[i] = tabBtn;
            
            int tabIndex = i; // Local copy for closure
            tabBtn.onClick.AddListener(() => SelectTab(tabIndex));

            // Add tab text
            GameObject tabTextObj = new GameObject("Text");
            tabTextObj.transform.SetParent(tabButtonObj.transform, false);
            RectTransform tabTextRect = tabTextObj.AddComponent<RectTransform>();
            tabTextRect.anchorMin = Vector2.zero;
            tabTextRect.anchorMax = Vector2.one;
            tabTextRect.offsetMin = Vector2.zero;
            tabTextRect.offsetMax = Vector2.zero;

            Text tabText = tabTextObj.AddComponent<Text>();
            tabText.text = tabNames[i];
            tabText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            tabText.fontSize = 16;
            tabText.fontStyle = FontStyle.Bold;
            tabText.alignment = TextAnchor.MiddleCenter;
            tabText.color = Color.white;
        }

        // Create Content Area (scrollable)
        GameObject contentAreaObj = new GameObject("ContentArea");
        contentAreaObj.transform.SetParent(transform, false);
        contentArea = contentAreaObj.AddComponent<RectTransform>();
        contentArea.anchorMin = new Vector2(0, 0.15f);
        contentArea.anchorMax = new Vector2(1, 0.85f);
        contentArea.offsetMin = Vector2.zero;
        contentArea.offsetMax = Vector2.zero;

        Image contentBg = contentAreaObj.AddComponent<Image>();
        contentBg.color = new Color(0.7f, 0.7f, 0.7f); // Light gray

        // Create ScrollRect for content
        ScrollRect scrollRect = contentAreaObj.AddComponent<ScrollRect>();
        
        GameObject viewport = new GameObject("Viewport");
        viewport.transform.SetParent(contentAreaObj.transform, false);
        RectTransform viewportRect = viewport.AddComponent<RectTransform>();
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.offsetMin = Vector2.zero;
        viewportRect.offsetMax = Vector2.zero;

        Image viewportImage = viewport.AddComponent<Image>();
        viewportImage.color = new Color(0.7f, 0.7f, 0.7f);

        Mask mask = viewport.AddComponent<Mask>();
        mask.showMaskGraphic = false;

        scrollRect.viewport = viewportRect;

        // Create 5 content panels for each tab
        tabContents = new RectTransform[5];
        
        for (int i = 0; i < 5; i++)
        {
            GameObject contentPanel = new GameObject(tabNames[i] + "Content");
            contentPanel.transform.SetParent(viewport.transform, false);
            
            RectTransform panelRect = contentPanel.AddComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            Image panelBg = contentPanel.AddComponent<Image>();
            panelBg.color = new Color(0.7f, 0.7f, 0.7f);

            VerticalLayoutGroup panelLayout = contentPanel.AddComponent<VerticalLayoutGroup>();
            panelLayout.childForceExpandHeight = true;
            panelLayout.childForceExpandWidth = true;
            panelLayout.childControlSize = true;
            panelLayout.padding = new RectOffset(10, 10, 10, 10);

            tabContents[i] = panelRect;

            // Add placeholder text
            GameObject placeholderObj = new GameObject("Placeholder");
            placeholderObj.transform.SetParent(contentPanel.transform, false);
            Text placeholder = placeholderObj.AddComponent<Text>();
            placeholder.text = $"{tabNames[i]} Section Content";
            placeholder.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            placeholder.fontSize = 20;
            placeholder.fontStyle = FontStyle.Bold;
            placeholder.alignment = TextAnchor.MiddleCenter;
            placeholder.color = Color.black;
        }

        scrollRect.content = tabContents[0];

        // Create Bottom Button Area
        GameObject bottomAreaObj = new GameObject("BottomArea");
        bottomAreaObj.transform.SetParent(transform, false);
        RectTransform bottomRect = bottomAreaObj.AddComponent<RectTransform>();
        bottomRect.anchorMin = new Vector2(0, 0);
        bottomRect.anchorMax = new Vector2(1, 0.15f);
        bottomRect.offsetMin = Vector2.zero;
        bottomRect.offsetMax = Vector2.zero;

        Image bottomBg = bottomAreaObj.AddComponent<Image>();
        bottomBg.color = new Color(0.6f, 0.2f, 0.35f); // Dark maroon

        HorizontalLayoutGroup bottomLayout = bottomAreaObj.AddComponent<HorizontalLayoutGroup>();
        bottomLayout.childForceExpandWidth = true;
        bottomLayout.childForceExpandHeight = true;
        bottomLayout.childControlSize = true;
        bottomLayout.spacing = 10;
        bottomLayout.padding = new RectOffset(10, 10, 10, 10);

        // Create 3 bottom buttons (will be customized per section)
        string[] bottomButtonNames = { "Button1", "Button2", "Button3" };
        for (int i = 0; i < 3; i++)
        {
            GameObject btnObj = new GameObject(bottomButtonNames[i]);
            btnObj.transform.SetParent(bottomAreaObj.transform, false);

            Button btn = btnObj.AddComponent<Button>();
            Image btnImage = btnObj.AddComponent<Image>();
            btnImage.color = new Color(1f, 0.7f, 0.2f); // Orange/Gold

            LayoutElement btnLayout = btnObj.AddComponent<LayoutElement>();
            btnLayout.preferredWidth = 100;
            btnLayout.preferredHeight = 60;

            GameObject btnTextObj = new GameObject("Text");
            btnTextObj.transform.SetParent(btnObj.transform, false);
            RectTransform btnTextRect = btnTextObj.AddComponent<RectTransform>();
            btnTextRect.anchorMin = Vector2.zero;
            btnTextRect.anchorMax = Vector2.one;
            btnTextRect.offsetMin = Vector2.zero;
            btnTextRect.offsetMax = Vector2.zero;

            Text btnText = btnTextObj.AddComponent<Text>();
            btnText.text = bottomButtonNames[i];
            btnText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btnText.fontSize = 14;
            btnText.fontStyle = FontStyle.Bold;
            btnText.alignment = TextAnchor.MiddleCenter;
            btnText.color = Color.white;
        }

        Debug.Log("Tab layout created successfully");
    }

    /// <summary>
    /// Sets up tab navigation and selection logic
    /// </summary>
    private void SetupTabNavigation()
    {
        // Select first tab by default
        SelectTab(0);
    }

    /// <summary>
    /// Selects a tab and updates UI accordingly
    /// </summary>
    private void SelectTab(int tabIndex)
    {
        if (tabIndex < 0 || tabIndex >= tabButtons.Length)
            return;

        // Deactivate all tabs
        for (int i = 0; i < tabContents.Length; i++)
        {
            tabContents[i].gameObject.SetActive(false);
            tabButtons[i].GetComponent<Image>().color = inactiveTabColor;
        }

        // Activate selected tab
        tabContents[tabIndex].gameObject.SetActive(true);
        tabButtons[tabIndex].GetComponent<Image>().color = activeTabColor;
        
        currentActiveTab = tabIndex;

        Debug.Log($"Selected tab: {tabIndex}");
    }

    /// <summary>
    /// Public method to get the content area of a specific tab for dynamic content loading
    /// </summary>
    public RectTransform GetTabContent(int tabIndex)
    {
        if (tabIndex >= 0 && tabIndex < tabContents.Length)
            return tabContents[tabIndex];
        return null;
    }

    /// <summary>
    /// Public method to get current active tab index
    /// </summary>
    public int GetActiveTabIndex()
    {
        return currentActiveTab;
    }
}
