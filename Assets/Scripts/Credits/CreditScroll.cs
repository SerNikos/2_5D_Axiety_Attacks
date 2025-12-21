using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreditScroll : MonoBehaviour
{
    public float scrollSpeed = 10f;  // Speed of scrolling
    private RectTransform rectTransform;
    public float sec = 2f;
    private bool canScroll = false;

    public CanvasGroup canvasGroup;
    public float fadeDuration = 1.3f;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup.alpha = 0f; // Start transparent

        StartCoroutine(WaitForSec(sec));
    }

    void Update()
    {
        if (canScroll)
        {
            ScrollCredits();
        }
    }

    private void ScrollCredits()
    {
        rectTransform.Translate(Vector3.up * scrollSpeed * Time.deltaTime);
    }

    IEnumerator WaitForSec(float sec)
    {
        yield return new WaitForSeconds(sec);
        StartCoroutine(FadeCanvas(canvasGroup, fadeDuration));
    }

    IEnumerator FadeCanvas(CanvasGroup canvasGroup, float duration)
    {
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, time / duration);
            yield return null;
        }
        canvasGroup.alpha = 1f; // Ensure fully visible at the end
        canScroll = true;
    }
}
