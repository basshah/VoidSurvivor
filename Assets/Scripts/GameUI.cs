using System.Globalization;
using TMPro;
using UnityEngine;

public class GameUI : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text timerText;
    public TMP_Text killText;
    public TMP_Text levelText;
    public RectTransform xpFill;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject recordPanel;
    public GameObject gameOverPanel;
    public GameObject infoPanel;

    [Header("Pause")]
    public TMP_Text pauseLevel;
    public TMP_Text pauseTime;
    public TMP_Text pauseKills;
    public StatsPanel pauseStats;
    public RecordPanel pauseRecord;

    [Header("Game Over")]
    public TMP_Text overTime;
    public TMP_Text overKills;
    public RecordPanel overRecord;

    [Header("Character Info")]
    public TMP_Text infoLevel;
    public StatsPanel infoStats;

    int lastSec = -1, lastKills = -1, lastLevel = -1;

    void Start()
    {
        pausePanel.SetActive(false);
        recordPanel.SetActive(false);
        gameOverPanel.SetActive(false);
        infoPanel.SetActive(false);
        SetXP(0f);

        var gm = GameManager.Instance;
        gm.onPauseChanged += OnPauseChanged;
        gm.onGameOver += ShowGameOver;
        if (XPManager.Instance != null) XPManager.Instance.onXPChanged += SetXP;
    }

    void OnDestroy()
    {
        if (XPManager.Instance != null) XPManager.Instance.onXPChanged -= SetXP;
        var gm = GameManager.Instance;
        if (gm == null) return;
        gm.onPauseChanged -= OnPauseChanged;
        gm.onGameOver -= ShowGameOver;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        int sec = (int)gm.ElapsedTime;
        if (sec != lastSec) { lastSec = sec; timerText.text = FormatTime(gm.ElapsedTime); }
        if (gm.Kills != lastKills) { lastKills = gm.Kills; killText.text = gm.Kills.ToString(); }
        if (gm.Level != lastLevel) { lastLevel = gm.Level; levelText.text = "Lv." + gm.Level; }
    }

    // Gün 3-də XP sistemi bunu çağıracaq (0..1)
    public void SetXP(float fraction)
    {
        xpFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
    }

    // Düymələrin OnClick-ləri bunlara bağlanır
    public void Pause() { GameManager.Instance.SetPaused(true); }
    public void Resume() { GameManager.Instance.SetPaused(false); }
    public void Restart() { GameManager.Instance.Restart(); }
    public void OpenRecord() { pauseRecord.Refresh(); recordPanel.SetActive(true); }
    public void CloseRecord() { recordPanel.SetActive(false); }
    public void CloseInfo() { infoPanel.SetActive(false); }

    public void OpenInfo()
    {
        infoLevel.text = "Character Level : " + GameManager.Instance.Level;
        infoStats.Refresh();
        infoPanel.SetActive(true);
    }

    void OnPauseChanged()
    {
        var gm = GameManager.Instance;
        pausePanel.SetActive(gm.IsPaused);
        if (gm.IsPaused)
        {
            pauseLevel.text = "Current Level : " + gm.Level;
            pauseTime.text = "Time Survived " + FormatTime(gm.ElapsedTime);
            pauseKills.text = "Enemies killed " + gm.Kills;
            pauseStats.Refresh();
        }
        else recordPanel.SetActive(false);
    }

    void ShowGameOver()
    {
        var gm = GameManager.Instance;
        pausePanel.SetActive(false);
        recordPanel.SetActive(false);
        overTime.text = FormatTime(gm.ElapsedTime);
        overKills.text = gm.Kills.ToString("N0", CultureInfo.InvariantCulture);
        overRecord.Refresh();
        gameOverPanel.SetActive(true);
    }

    public static string FormatTime(float t)
    {
        int m = (int)(t / 60f);
        int s = (int)(t % 60f);
        return $"{m:00}:{s:00}";
    }
}