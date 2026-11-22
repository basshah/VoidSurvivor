using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LevelUpUI : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [Serializable]
    public class CardRefs
    {
        public GameObject root;
        public Button button;
        public Image borderImage;
        public Image backgroundImage;
        public Image iconImage;
        public TextMeshProUGUI titleText;
        public TextMeshProUGUI levelText;
        public TextMeshProUGUI descText;
        public GameObject evolutionBadge;
    }

    [Serializable]
    public class IconEntry
    {
        public string key;
        public Sprite sprite;
    }

    [Header("Panel & Cards")]
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private CardRefs[] cards = new CardRefs[3];
    [SerializeField] private List<IconEntry> icons = new List<IconEntry>();

    [Header("Colors")]
    [SerializeField] private Color normalBorderColor = new Color(0.28f, 0.38f, 0.56f, 1f);
    [SerializeField] private Color normalBgColor = new Color(0.08f, 0.10f, 0.16f, 1f);
    [SerializeField] private Color normalLevelColor = new Color(0.55f, 0.85f, 1f, 1f);

    [SerializeField] private Color evoBorderColor = new Color(1f, 0.82f, 0.24f, 1f);
    [SerializeField] private Color evoBgColor = new Color(0.18f, 0.14f, 0.05f, 1f);
    [SerializeField] private Color evoLevelColor = new Color(1f, 0.85f, 0.25f, 1f);

    private readonly Dictionary<string, Sprite> iconLookup = new Dictionary<string, Sprite>();
    private List<UpgradeOption> currentOptions;
    private Action<UpgradeOption> onSelectedCallback;
    private float unlockClickTime;
    private bool initialized;
    private bool isShowing;

    private void Awake()
    {
        EnsureInitialized();

        // Yalnız Show() çağırılmayıbsa başlanğıcda gizlət
        if (!isShowing && panelRoot != null)
        {
            panelRoot.SetActive(false);
            IsOpen = false;
        }
    }

    private void EnsureInitialized()
    {
        if (initialized) return;
        initialized = true;

        RebuildIconLookup();

        for (int i = 0; i < cards.Length; i++)
        {
            int idx = i;
            if (cards[i] != null && cards[i].button != null)
            {
                cards[i].button.onClick.RemoveAllListeners();
                cards[i].button.onClick.AddListener(() => OnCardClicked(idx));
            }
        }
    }

    private void RebuildIconLookup()
    {
        iconLookup.Clear();
        foreach (IconEntry entry in icons)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.key) && entry.sprite != null)
            {
                iconLookup[entry.key] = entry.sprite;
            }
        }
    }

    public void Show(List<UpgradeOption> options, Action<UpgradeOption> onSelected)
    {
        isShowing = true;
        EnsureInitialized();

        currentOptions = options;
        onSelectedCallback = onSelected;

        // Joystick-də barmaq olarkən ilk frame-də təsadüfi klikin qarşısını alır
        unlockClickTime = Time.unscaledTime + 0.25f;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null || cards[i].root == null) continue;

            if (i < options.Count)
            {
                cards[i].root.SetActive(true);
                BindCard(cards[i], options[i]);
            }
            else
            {
                cards[i].root.SetActive(false);
            }
        }

        IsOpen = true;
        Time.timeScale = 0f;

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
            panelRoot.transform.SetAsLastSibling();
        }
    }

    public void Hide(bool restoreTimeScale = true)
    {
        isShowing = false;

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }

        IsOpen = false;

        bool shouldStayPaused = GameManager.Instance != null &&
                                (GameManager.Instance.IsPaused || GameManager.Instance.IsGameOver);
        if (restoreTimeScale && !shouldStayPaused)
        {
            Time.timeScale = 1f;
        }
    }

    private void BindCard(CardRefs card, UpgradeOption opt)
    {
        if (card.titleText != null) card.titleText.text = opt.title;
        if (card.levelText != null)
        {
            card.levelText.text = opt.levelTag;
            card.levelText.color = opt.isEvolution ? evoLevelColor : normalLevelColor;
        }
        if (card.descText != null) card.descText.text = opt.description;

        if (card.borderImage != null)
            card.borderImage.color = opt.isEvolution ? evoBorderColor : normalBorderColor;
        if (card.backgroundImage != null)
            card.backgroundImage.color = opt.isEvolution ? evoBgColor : normalBgColor;
        if (card.evolutionBadge != null)
            card.evolutionBadge.SetActive(opt.isEvolution);

        if (card.iconImage != null)
        {
            if (iconLookup.TryGetValue(opt.iconKey, out Sprite sprite) && sprite != null)
            {
                card.iconImage.sprite = sprite;
                card.iconImage.color = Color.white;
            }
            else
            {
                card.iconImage.sprite = null;
                card.iconImage.color = opt.isEvolution
                    ? new Color(1f, 0.82f, 0.24f, 0.35f)
                    : new Color(0.45f, 0.65f, 0.9f, 0.25f);
            }
        }
    }

    private void OnCardClicked(int index)
    {
        if (Time.unscaledTime < unlockClickTime) return;
        if (currentOptions == null || index < 0 || index >= currentOptions.Count) return;

        UpgradeOption chosen = currentOptions[index];
        onSelectedCallback?.Invoke(chosen);
    }

    private void OnDestroy()
    {
        if (IsOpen)
        {
            IsOpen = false;
            Time.timeScale = 1f;
        }
    }
}