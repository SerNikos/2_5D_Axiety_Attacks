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

    [Header("Spell Images")]
    [SerializeField] private Image[] spellImages;
    [SerializeField, Range(0f, 1f)] private float spellLockThreshold = 0.5f;
    [SerializeField] private Color grayedOutSpellColor = new Color(0.5f, 0.5f, 0.5f, 1f);
    [SerializeField] private float spellColorFadeDuration = 0.3f;

    private float currentAnxiety;   // real value (game logic)
    private float shownAnxiety;     // visual smoothed value
    private Color[] availableSpellColors;
    private bool spellsLocked;

    public float Current => currentAnxiety;
    public bool SpellsAvailable => !spellsLocked;

    private void Start()
    {
        CacheAvailableSpellColors();
        SetMaxAnxiety(maxAnxiety);
        SetAnxiety(0f);

        shownAnxiety = currentAnxiety;
        slider.value = shownAnxiety;
        UpdateSpellAvailability(true);
    }

    private void Update()
    {
        // 🔥 smooth UI toward real value
        shownAnxiety = Mathf.Lerp(shownAnxiety, currentAnxiety, Time.deltaTime * smoothSpeed);

        slider.value = shownAnxiety;
        UpdateColor();
        UpdateSpellAvailability(false);
    }

    private void CacheAvailableSpellColors()
    {
        if (spellImages == null)
        {
            availableSpellColors = new Color[0];
            return;
        }

        availableSpellColors = new Color[spellImages.Length];

        for (int i = 0; i < spellImages.Length; i++)
        {
            if (spellImages[i] != null)
            {
                availableSpellColors[i] = spellImages[i].color;
            }
        }
    }

    private void UpdateSpellAvailability(bool instant)
    {
        float anxietyPercent = maxAnxiety > 0f ? shownAnxiety / maxAnxiety : 0f;
        bool shouldLockSpells = anxietyPercent > spellLockThreshold;

        if (!instant && shouldLockSpells == spellsLocked)
        {
            return;
        }

        spellsLocked = shouldLockSpells;

        if (spellImages == null)
        {
            return;
        }

        for (int i = 0; i < spellImages.Length; i++)
        {
            Image spellImage = spellImages[i];

            if (spellImage == null)
            {
                continue;
            }

            Color targetColor = spellsLocked ? grayedOutSpellColor : availableSpellColors[i];

            if (instant || spellColorFadeDuration <= 0f)
            {
                spellImage.color = targetColor;
            }
            else
            {
                spellImage.CrossFadeColor(targetColor, spellColorFadeDuration, true, true);
            }
        }
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