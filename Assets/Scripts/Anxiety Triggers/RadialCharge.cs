using UnityEngine;
using UnityEngine.UI; // Required for UI Image component
using DG.Tweening;

public class RadialCharge : MonoBehaviour
{
    [Header("UI Components")]
    [SerializeField] private Image radialImage; // Assign your Filled UI Image here

    [Header("Timing Settings")]
    [SerializeField] private float stepDuration = 4.0f; // 4 seconds per phase

    [Header("Color Settings")]
    [SerializeField] private Color yellowColor = Color.yellow;
    [SerializeField] private Color orangeColor = new Color(1f, 0.5f, 0f); // Orange
    [SerializeField] private Color greenColor = Color.green;

    private Sequence chargeSequence;

    void Start()
    {
        if (radialImage == null)
        {
            radialImage = GetComponent<Image>();
        }

        ResetUI();
        DOTween.Init();
    }

    void Update()
    {
        // 1. CLICK: Start the 2-stage radial fill timeline
        if (Input.GetMouseButtonDown(0))
        {
            KillActiveTweens();
            ResetUI();

            chargeSequence = DOTween.Sequence();

            // STAGE 1: Fill yellow radially from 0 to 1 over 4 seconds
            radialImage.color = yellowColor;
            chargeSequence.Append(radialImage.DOFillAmount(1f, stepDuration).SetEase(Ease.Linear));

            // STAGE 2: Instantly prepare the orange stage
            // To mimic the stacking fill, we instantly snap fill to 0, swap color to orange, and fill again
            chargeSequence.AppendCallback(() =>
            {
                radialImage.color = orangeColor;
                radialImage.fillAmount = 0f;
            });

            // Fill orange radially from 0 to 1 over the next 4 seconds
            chargeSequence.Append(radialImage.DOFillAmount(1f, stepDuration).SetEase(Ease.Linear));
        }

        // 2. RELEASE: Turn green and empty radially back to 0
        if (Input.GetMouseButtonUp(0))
        {
            KillActiveTweens();

            // Change color to green instantly
            radialImage.color = greenColor;

            chargeSequence = DOTween.Sequence();

            // Radially un-fill/drain counter-clockwise back to 0 over 4 seconds
            chargeSequence.Append(radialImage.DOFillAmount(0f, stepDuration).SetEase(Ease.Linear));
        }
    }

    private void ResetUI()
    {
        if (radialImage != null)
        {
            radialImage.fillAmount = 0f;
            radialImage.color = yellowColor;
        }
    }

    private void KillActiveTweens()
    {
        if (chargeSequence != null && chargeSequence.IsActive())
        {
            chargeSequence.Kill();
        }

        if (radialImage != null)
        {
            radialImage.DOKill();
        }
    }

    private void OnDestroy()
    {
        KillActiveTweens();
    }
}