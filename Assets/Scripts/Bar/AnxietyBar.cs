using UnityEngine;
using UnityEngine.UI;

public class AnxietyBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private Image fillImage;

    [Header("Settings")]
    [SerializeField] private float maxAnxiety = 100f;

    [Header("Gradient")]
    public Gradient gradient;

    [Header("Smoothing")]
    [SerializeField] private float smoothSpeed = 6f;

    private float currentAnxiety;   // real value (game logic)
    private float shownAnxiety;     // visual smoothed value

    public float Current => currentAnxiety;

    private void Start()
    {
        SetMaxAnxiety(maxAnxiety);
        SetAnxiety(0f);

        shownAnxiety = currentAnxiety;
        slider.value = shownAnxiety;
    }

    private void Update()
    {
        // 🔥 smooth UI toward real value
        shownAnxiety = Mathf.Lerp(shownAnxiety, currentAnxiety, Time.deltaTime * smoothSpeed);

        slider.value = shownAnxiety;
        UpdateColor();
    }

    public void SetMaxAnxiety(float max)
    {
        maxAnxiety = max;
        slider.maxValue = maxAnxiety;
    }

    public void SetAnxiety(float value)
    {
        currentAnxiety = Mathf.Clamp(value, 0f, maxAnxiety);
    }

    public void AddAnxiety(float value)
    {
        SetAnxiety(currentAnxiety + value);
    }

    public void ReduceAnxietyPercent(float percent)
    {
        SetAnxiety(currentAnxiety * (1f - percent));
    }

    private void UpdateColor()
    {
        float t = shownAnxiety / maxAnxiety;
        fillImage.color = gradient.Evaluate(t);
    }

    public void SetMinHealth(float value)
    {
        SetAnxiety(value);
    }
}