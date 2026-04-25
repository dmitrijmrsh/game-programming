using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] DepthTracker depthTracker;
    [SerializeField] HullIntegrity hullIntegrity;
    [SerializeField] CreatureBuff creatureBuff;

    [Header("Depth")]
    [SerializeField] Image depthFill;
    [SerializeField] TextMeshProUGUI depthText;

    [Header("Hull")]
    [SerializeField] Image hullFill;
    [SerializeField] TextMeshProUGUI hullText;

    [Header("Crystals")]
    [SerializeField] TextMeshProUGUI crystalText;

    [Header("Pressure")]
    [SerializeField] TextMeshProUGUI pressureText;
    [SerializeField] Image pressureFrame;

    [Header("Buffs")]
    [SerializeField] Image healerIcon;
    [SerializeField] Image speederIcon;
    [SerializeField] Image armorerIcon;
    [SerializeField] Image scoutIcon;

    [Header("Screens")]
    [SerializeField] GameObject victoryPanel;
    [SerializeField] TextMeshProUGUI victoryStatsText;
    [SerializeField] GameObject gameOverPanel;
    [SerializeField] Button retryButton;

    void Start()
    {
        if (victoryPanel != null) victoryPanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);

        if (retryButton != null)
            retryButton.onClick.AddListener(() => GameManager.Instance?.Restart());

        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnCrystalCollected += UpdateCrystals;
            gm.OnVictory += ShowVictory;
            gm.OnGameOver += ShowGameOver;
        }
    }

    void Update()
    {
        UpdateDepth();
        UpdateHull();
        UpdatePressure();
        UpdateBuffs();
    }

    void UpdateDepth()
    {
        if (depthTracker == null) return;

        float norm = depthTracker.NormalizedPressure;
        if (depthFill != null) depthFill.fillAmount = norm;
        if (depthText != null)
            depthText.text = $"{Mathf.Abs(depthTracker.CurrentDepth):F0}m";
    }

    void UpdateHull()
    {
        if (hullIntegrity == null) return;

        float hp = hullIntegrity.HullPercent;
        if (hullFill != null)
        {
            hullFill.fillAmount = hp;
            hullFill.color = hp > 0.5f
                ? Color.Lerp(Color.yellow, Color.green, (hp - 0.5f) * 2f)
                : Color.Lerp(Color.red, Color.yellow, hp * 2f);
        }
        if (hullText != null)
            hullText.text = $"{hullIntegrity.CurrentHull:F0} / {hullIntegrity.MaxHull:F0}";
    }

    void UpdatePressure()
    {
        if (depthTracker == null) return;

        float p = depthTracker.PressureCoefficient;
        if (pressureText != null)
            pressureText.text = $"{p:F1} ATM";

        if (pressureFrame != null)
        {
            float t = depthTracker.NormalizedPressure;
            if (t <= 0.7f)
            {
                pressureFrame.color = Color.clear;
            }
            else
            {
                float warningT = (t - 0.7f) / 0.3f;
                Color tint = Color.Lerp(
                    new Color(1f, 0.9f, 0.25f, 0.04f),
                    new Color(1f, 0.15f, 0.05f, 0.12f),
                    warningT);
                pressureFrame.color = tint;
            }
        }
    }

    void UpdateBuffs()
    {
        if (creatureBuff == null) return;

        SetBuffIcon(healerIcon, creatureBuff.HealerActive, new Color(0.2f, 1f, 0.4f));
        SetBuffIcon(speederIcon, creatureBuff.SpeederActive, new Color(1f, 0.9f, 0.2f));
        SetBuffIcon(armorerIcon, creatureBuff.ArmorerActive, new Color(0.3f, 0.5f, 1f));
        SetBuffIcon(scoutIcon, creatureBuff.ScoutActive, new Color(0.8f, 0.3f, 1f));
    }

    void SetBuffIcon(Image icon, bool active, Color color)
    {
        if (icon == null) return;
        icon.color = active ? color : new Color(color.r, color.g, color.b, 0.2f);
    }

    void UpdateCrystals(int collected, int total)
    {
        if (crystalText != null)
            crystalText.text = $"{collected} / {total}";
    }

    void ShowVictory()
    {
        if (victoryPanel != null) victoryPanel.SetActive(true);
        if (victoryStatsText != null)
        {
            var gm = GameManager.Instance;
            float mins = gm.PlayTime / 60f;
            victoryStatsText.text = $"КАНЬОН ИССЛЕДОВАН!\n\nВремя: {mins:F1} мин\nКристаллов: {gm.CollectedCrystals}/{gm.TotalCrystals}";
        }
    }

    void ShowGameOver()
    {
        if (gameOverPanel != null) gameOverPanel.SetActive(true);
    }

    void OnDestroy()
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            gm.OnCrystalCollected -= UpdateCrystals;
            gm.OnVictory -= ShowVictory;
            gm.OnGameOver -= ShowGameOver;
        }
    }
}
