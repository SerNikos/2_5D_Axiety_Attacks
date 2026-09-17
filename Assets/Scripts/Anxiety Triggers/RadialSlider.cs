using UnityEngine;
using UnityEngine.UI;
using TMPro; // Required for TextMeshPro
using DG.Tweening; // Required for DOTween Pro / V2 Features

public class RadialSlider : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image fillImage;               // Drag your 'Fill Area' image here
    [SerializeField] private TextMeshProUGUI timerText;     // Drag your countdown 'Text (TMP)' here
    [SerializeField] private TextMeshProUGUI actionText;    // Drag your action state text here (CLICK/HOLD/RELEASE)

    [Header("Timing Settings")]
    [SerializeField] private float stepDuration = 4.0f;     // Duration in seconds for each phase

    [Header("Color Settings")]
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color orangeColor = new Color(1f, 0.5f, 0f); // Custom Orange
    [SerializeField] private Color greenColor = Color.green;

    private Sequence currentSequence;

    // Game state tracking variables
    private bool canReleaseSafely = false;
    private bool isGameOver = false;

    void Start()
    {
        ResetUI();
        DOTween.Init();
    }

    void Update()
    {
        // If a loss sequence is playing out its display timer, block inputs until reset
        if (isGameOver) return;

        // 1. CLICK & HOLD: Radial fills (Yellow -> Orange) with a 4 to 0 countdown
        if (Input.GetMouseButtonDown(0))
        {
            KillActiveTweens();
            ResetUI();

            UpdateActionText("HOLD");
            canReleaseSafely = false; // Reset safety check; player must wait until Orange finishes

            currentSequence = DOTween.Sequence();

            // --- STAGE 1: YELLOW FILL (0 to 4 seconds) ---
            fillImage.color = yellowColor;
            currentSequence.Append(fillImage.DOFillAmount(1f, stepDuration).SetEase(Ease.Linear));
            currentSequence.Join(DOVirtual.Float(stepDuration, 0f, stepDuration, UpdateTimerText).SetEase(Ease.Linear));

            // --- TRANSITION: Instantly swap to Orange and empty the layer for Stage 2 ---
            currentSequence.AppendCallback(() =>
            {
                fillImage.color = orangeColor;
                fillImage.fillAmount = 0f;
            });

            // --- STAGE 2: ORANGE FILL (4 to 8 seconds) ---
            currentSequence.Append(fillImage.DOFillAmount(1f, stepDuration).SetEase(Ease.Linear));
            currentSequence.Join(DOVirtual.Float(stepDuration, 0f, stepDuration, UpdateTimerText).SetEase(Ease.Linear));

            // --- TIME IS UP: GUIDE PLAYER TO RELEASE SAFELY ---
            currentSequence.OnComplete(() =>
            {
                canReleaseSafely = true; // The player successfully survived the full hold!
                UpdateActionText("RELEASE");
            });
        }

        // 2. RELEASE: Check if they timed it perfectly or failed
        if (Input.GetMouseButtonUp(0))
        {
            KillActiveTweens();

            // LOSS CONDITION: If they release before the orange stage completed
            if (!canReleaseSafely)
            {
                TriggerLoss();
            }
            // WIN CONDITION: They successfully waited for the prompt and released
            else
            {
                TriggerGreenSuccess();
            }
        }
    }

    private void TriggerGreenSuccess()
    {
        // Change to green and make sure fill amount starts at 1 (full) so it can drain backward
        fillImage.color = greenColor;
        fillImage.fillAmount = 1f;

        currentSequence = DOTween.Sequence();

        // Radially drain the green image back to 0 over 4 seconds
        currentSequence.Append(fillImage.DOFillAmount(0f, stepDuration).SetEase(Ease.Linear));

        // Force the timer text to restart at 4 and drop down to 0 over those same 4 seconds
        currentSequence.Join(DOVirtual.Float(stepDuration, 0f, stepDuration, UpdateTimerText).SetEase(Ease.Linear));

        // Revert the status text back to "CLICK" (idle state) once the green drain finishes completely
        currentSequence.OnComplete(() => ResetUI());
    }

    private void TriggerLoss()
    {
        isGameOver = true;

        Debug.Log("Player lost! Released too early.");
        UpdateActionText("LOST!");

        // Flash or turn the circle to a clear warning color (like white/gray or red) to indicate failure
        if (fillImage != null)
        {
            fillImage.color = Color.gray;
            fillImage.fillAmount = 0f;
        }
        if (timerText != null)
        {
            timerText.text = "X";
        }

        // Wait a brief moment (1.5 seconds) so the player sees the "LOST!" message, then reset the loop
        currentSequence = DOTween.Sequence();
        currentSequence.AppendInterval(1.5f);
        currentSequence.OnComplete(() => ResetUI());
    }

    private void UpdateTimerText(float value)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(value).ToString();
        }
    }

    private void UpdateActionText(string text)
    {
        if (actionText != null)
        {
            actionText.text = text;
        }
    }

    private void ResetUI()
    {
        canReleaseSafely = false;
        isGameOver = false;

        if (fillImage != null)
        {
            fillImage.fillAmount = 0f;
            fillImage.color = yellowColor;
        }
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(stepDuration).ToString();
        }

        UpdateActionText("CLICK");
    }

    private void KillActiveTweens()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        if (fillImage != null)
        {
            fillImage.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillActiveTweens();
    }
}