using UnityEngine;
using UnityEngine.UI;

public class PlayerHUD : MonoBehaviour
{
    [SerializeField] private Color panelColor = new Color(0f, 0f, 0f, 0.72f);
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private Text label;

    private PlayerHealth health;
    private PlayerDamage damage;

    private void Awake()
    {
        BuildHud();
    }

    private void OnEnable()
    {
        TryBindToPlayer();
    }

    private void OnDisable()
    {
        Unbind();
    }

    private void Update()
    {
        if (health == null) TryBindToPlayer();
    }

    private void BuildHud()
    {
        if (label != null) return;

        GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas),
            typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panelObject = new GameObject("Stats", typeof(RectTransform), typeof(Image));
        panelObject.transform.SetParent(canvasObject.transform, false);
        RectTransform panel = panelObject.GetComponent<RectTransform>();
        panel.anchorMin = Vector2.one;
        panel.anchorMax = Vector2.one;
        panel.pivot = Vector2.one;
        panel.anchoredPosition = new Vector2(-24f, -24f);
        panel.sizeDelta = new Vector2(255f, 98f);
        panelObject.GetComponent<Image>().color = panelColor;

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textObject.transform.SetParent(panelObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(14f, 8f);
        textRect.offsetMax = new Vector2(-14f, -8f);

        label = textObject.GetComponent<Text>();
        label.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        label.fontSize = 24;
        label.alignment = TextAnchor.MiddleRight;
        label.color = textColor;
    }

    private void TryBindToPlayer()
    {
        PlayerHealth foundHealth = FindObjectOfType<PlayerHealth>();
        if (foundHealth == null) return;

        PlayerDamage foundDamage = foundHealth.GetComponent<PlayerDamage>();
        if (foundDamage == null) return;

        Unbind();
        health = foundHealth;
        damage = foundDamage;
        health.OnHealthChanged += HandleHealthChanged;
        health.OnDefenseChanged += HandleDefenseChanged;
        damage.OnStompDamageChanged += HandleDamageChanged;
        Refresh();
    }

    private void Unbind()
    {
        if (health != null) health.OnHealthChanged -= HandleHealthChanged;
        if (health != null) health.OnDefenseChanged -= HandleDefenseChanged;
        if (damage != null) damage.OnStompDamageChanged -= HandleDamageChanged;
        health = null;
        damage = null;
    }

    private void HandleHealthChanged(int current, int maximum) { Refresh(); }
    private void HandleDefenseChanged(int currentDefense) { Refresh(); }
    private void HandleDamageChanged(int currentDamage) { Refresh(); }

    private void Refresh()
    {
        if (label == null || health == null || damage == null) return;
        label.text = "HP  " + health.CurrentHealth + " / " + health.MaxHealth +
                     "\nDefense  " + health.Defense +
                     "\nStomp  " + damage.StompDamage;
    }
}
