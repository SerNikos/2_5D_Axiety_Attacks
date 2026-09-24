using System;
using UnityEngine;
using TMPro; // Required for TextMeshPro
using DG.Tweening; // Uses your premium DOTween installation

public class ChargeCircle : MonoBehaviour
{
    [Header("Breathing Gate")]
    [SerializeField] private AnxietyBreathingClickHold breathing; // optional reference to breathing controller
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI timerText;     // Countdown text
    [SerializeField] private TextMeshProUGUI actionText;    // Status text (CLICK / Breathing in... / Hold... / Breathing Out...)

    [Header("Timing Settings")]
    [SerializeField] private float duration = 4.0f;         // 4 seconds per step
    [SerializeField] private float releaseTimeWindow = 1f; // 2 seconds to release before losing

    [Header("Scaling Settings")]
    [SerializeField] private float minScale = 0.1f;
    [SerializeField] private float maxScale = 0.5f;

    [Header("Color Settings")]
    [SerializeField] private Color startColor = Color.black;              // Pure Black for Text & Circle init
    [SerializeField] private Color chargeColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color releaseColor = Color.green;

    [Header("Text Readability")]
    [SerializeField] private Color textOutlineColor = Color.black;
    [SerializeField, Range(0f, 1f)] private float textOutlineWidth = 0.2f;

    private SpriteRenderer spriteRenderer;
    private Sequence currentSequence;
    private Tween releaseWindowTween; // Tracks the 2-second timeout

    // Game state tracking variables
    private bool canReleaseSafely = false;
    private bool isGameOver = false;
    private bool attemptCompleted = false;

    public Action<bool> OnAttemptCompleted;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        // Try to auto-resolve breathing controller if not assigned in inspector
        if (breathing == null)
        {
            breathing = GetComponent<AnxietyBreathingClickHold>() ?? FindObjectOfType<AnxietyBreathingClickHold>();
        }

        if (actionText != null)
        {
            actionText.outlineColor = textOutlineColor;
            actionText.outlineWidth = textOutlineWidth;
        }

        ResetUI();
        DOTween.Init();
    }

    void Update()
    {
        // If a loss sequence is playing out, block inputs until reset
        if (isGameOver || attemptCompleted) return;

        // Gate input behind the breathing event. If breathing is not present or not active, ignore clicks.
        if (breathing == null || !breathing.IsActive) return;

        // 1. CLICK & HOLD: Sequence start
        if (Input.GetMouseButtonDown(0))
        {
            KillActiveTweens();
            ResetUI();

            canReleaseSafely = false;

            currentSequence = DOTween.Sequence();
            currentSequence.SetUpdate(true);

            // --- STEP 1: "Breathing in..." (0 to 4 seconds) ---
            UpdateActionText("Breathing in...", Color.black);
            actionText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 10, 1f).SetUpdate(true);

            // Circle scales up and turns Orange
            currentSequence.Append(transform.DOScale(maxScale, duration).SetEase(Ease.Linear).SetUpdate(true));
            if (spriteRenderer != null)
            {
                currentSequence.Join(spriteRenderer.DOColor(chargeColor, duration).SetEase(Ease.Linear).SetUpdate(true));
            }
            // Timer countdown (4 -> 0)
            currentSequence.Join(DOVirtual.Float(duration, 0f, duration, UpdateTimerText).SetEase(Ease.Linear).SetUpdate(true));
            // Text color fades smoothly from Black to Orange
            currentSequence.Join(actionText.DOColor(chargeColor, duration).SetEase(Ease.Linear).SetUpdate(true));

            // --- STEP 2: "Hold..." (4 to 8 seconds) ---
            currentSequence.AppendCallback(() =>
            {
                actionText.text = "Hold...";
                actionText.transform.DOPunchScale(new Vector3(0.15f, 0.15f, 0f), 0.3f, 10, 1f).SetUpdate(true);
            });

            // Circle automatically moves to the Green target phase
            if (spriteRenderer != null)
            {
                currentSequence.Append(spriteRenderer.DOColor(releaseColor, duration).SetEase(Ease.Linear).SetUpdate(true));
            }
            // Timer countdown restarts (4 -> 0)
            currentSequence.Join(DOVirtual.Float(duration, 0f, duration, UpdateTimerText).SetEase(Ease.Linear).SetUpdate(true));
            // Text color fades smoothly from Orange to Green
            currentSequence.Join(actionText.DOColor(releaseColor, duration).SetEase(Ease.Linear).SetUpdate(true));

            // --- TIME IS UP: GUIDE PLAYER TO RELEASE SAFELY ---
            currentSequence.OnComplete(() =>
            {
                canReleaseSafely = true;
                actionText.text = "RELEASE!";
                actionText.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.4f, 8, 1f).SetUpdate(true);

                // Start the 2-second countdown window. If it finishes without player letting go, trigger loss.
                releaseWindowTween = DOVirtual.DelayedCall(releaseTimeWindow, () =>
                {
                    if (!isGameOver)
                    {
                        TriggerLoss();
                    }
                }).SetUpdate(true);
            });
        }

        // 2. RELEASE: Check if they timed it perfectly or failed
        if (Input.GetMouseButtonUp(0))
        {
            // Kill the timeout check immediately so it doesn't fire after release
            if (releaseWindowTween != null)
            {
                releaseWindowTween.Kill();
            }

            KillActiveTweens();

            if (!canReleaseSafely)
            {
                TriggerLoss();
            }
            else
            {
                TriggerSuccess();
            }
        }
    }

    private void TriggerSuccess()
    {
        CompleteAttempt(true);
        UpdateActionText("Breathing Out...", releaseColor);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = releaseColor;
        }

        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);

        // Simultaneously shrink scale down and return circle to Black over 4 seconds
        currentSequence.Append(transform.DOScale(minScale, duration).SetEase(Ease.Linear).SetUpdate(true));
        if (spriteRenderer != null)
        {
            currentSequence.Join(spriteRenderer.DOColor(startColor, duration).SetEase(Ease.Linear).SetUpdate(true));
        }

        // Restart the countdown from 4 to 0 during the shrink phase
        currentSequence.Join(DOVirtual.Float(duration, 0f, duration, UpdateTimerText).SetEase(Ease.Linear).SetUpdate(true));

        // Keep the success message visible until the instruction panel fades out.
    }

    private void TriggerLoss()
    {
        isGameOver = true;

        if (breathing != null)
        {
            breathing.DisableBreathing();
        }

        CompleteAttempt(false);

        Debug.Log("Player lost! Released too early or failed to release in time.");

        // Clean, grounded loss animation: No shaking, just a dynamic punch scale feedback
        UpdateActionText("LOST!", Color.red);
        actionText.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.3f, 5, 1f).SetUpdate(true);

        if (spriteRenderer != null)
        {
            spriteRenderer.color = Color.gray;
        }
        if (timerText != null)
        {
            timerText.text = "X";
            timerText.transform.DOPunchScale(new Vector3(0.25f, 0.25f, 0f), 0.3f, 5, 1f).SetUpdate(true);
        }

        // Keep the loss feedback sequence independent while the panel remains visible.
        currentSequence = DOTween.Sequence();
        currentSequence.SetUpdate(true);
        currentSequence.AppendInterval(1.5f);
    }

    public void ResetAfterPanelFade()
    {
        KillActiveTweens();
        ResetUI();
    }

    public void ShowClickAndHoldPrompt()
    {
        UpdateActionText("CLICK & HOLD", startColor);
    }

    private void CompleteAttempt(bool success)
    {
        if (attemptCompleted) return;

        attemptCompleted = true;
        OnAttemptCompleted?.Invoke(success);
    }

    private void UpdateTimerText(float value)
    {
        if (timerText != null)
        {
            timerText.text = Mathf.CeilToInt(value).ToString();
        }
    }

    private void UpdateActionText(string text, Color targetColor)
    {
        if (actionText != null)
        {
            actionText.transform.DOKill();
            actionText.DOKill();

            actionText.text = text;
            actionText.color = targetColor;
        }
    }

    private void ResetUI()
    {
        canReleaseSafely = false;
        isGameOver = false;
        attemptCompleted = false;

        transform.localScale = Vector3.one * minScale;

        if (spriteRenderer != null)
        {
            Color resetColor = startColor;
            resetColor.a = spriteRenderer.color.a;
            spriteRenderer.color = resetColor;
        }
        if (timerText != null)
        {
            timerText.transform.localScale = Vector3.one;
            timerText.text = Mathf.CeilToInt(duration).ToString();
        }

        UpdateActionText("CLICK", startColor);
        actionText.transform.localScale = Vector3.one;
    }

    private void KillActiveTweens()
    {
        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }
        if (releaseWindowTween != null && releaseWindowTween.IsActive())
        {
            releaseWindowTween.Kill();
        }

        transform.DOKill();
        if (spriteRenderer != null) spriteRenderer.DOKill();
        if (actionText != null)
        {
            actionText.transform.DOKill();
            actionText.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillActiveTweens();
    }
}