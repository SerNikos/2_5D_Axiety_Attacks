using System;
using System.Collections;
using UnityEngine;

public class AnxietyBreathingClickHold : MonoBehaviour
{
    [Header("Sprites")]
    public Sprite normal;
    public Sprite[] inhaleFrames; // 5 frames
    public Sprite[] exhaleFrames; // 4 frames

    [Header("Visual")]
    public SpriteRenderer character;

    [Header("Duration Settings")]
    [SerializeField] private float targetDuration = 4f; // Ορίζουμε 4 δευτερόλεπτα

    // EVENTS
    public Action OnBreathingStarted;
    public Action OnInhaleComplete;
    public Action OnCycleFinished;

    public bool IsActive { get; private set; }

    private bool isHolding;
    private bool breathingStarted;
    private bool inhaleCompleted;
    private bool exhaleStarted;

    private float timer;
    private int index;

    private Coroutine exhaleRoutine;

    [SerializeField] private DollyZoom dollyZoom;

    private void Update()
    {
        if (!IsActive) return;

        HandleInput();
        UpdateInhale();
    }

    public void EnableBreathing()
    {
        IsActive = true;
    }

    public void DisableBreathing()
    {
        IsActive = false;
        ResetState();
    }

    void ResetState()
    {
        isHolding = false;
        breathingStarted = false;
        inhaleCompleted = false;
        exhaleStarted = false;

        index = 0;
        timer = 0;

        character.sprite = normal;

        if (exhaleRoutine != null)
        {
            StopCoroutine(exhaleRoutine);
            exhaleRoutine = null;
        }
    }

    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
            StartInhale();

        if (Input.GetMouseButtonUp(0))
            StartExhale();
    }

    void StartInhale()
    {
        if (!IsActive) return;

        isHolding = true;

        if (!breathingStarted)
        {
            breathingStarted = true;
            OnBreathingStarted?.Invoke();
        }

        // Σταματάμε το κούνημα της κάμερας
        if (dollyZoom != null) dollyZoom.shakeSpeed = 0;

        inhaleCompleted = false;

        index = 0;
        timer = 0;
    }

    void StartExhale()
    {
        if (!IsActive || exhaleStarted) return;

        isHolding = false;
        exhaleStarted = true;

        exhaleRoutine = StartCoroutine(ExhaleRoutine());
    }

    void UpdateInhale()
    {
        if (!isHolding) return;

        timer += Time.unscaledDeltaTime;

        // Δυναμικός υπολογισμός frame rate για την εισπνοή
        float inhaleFrameRate = inhaleFrames.Length / targetDuration;

        if (timer >= 1f / inhaleFrameRate)
        {
            timer = 0f;

            if (index < inhaleFrames.Length - 1)
            {
                index++;
            }
            else
            {
                character.sprite = inhaleFrames[index];

                if (!inhaleCompleted)
                {
                    inhaleCompleted = true;
                    OnInhaleComplete?.Invoke();
                }

                return;
            }

            character.sprite = inhaleFrames[index];
        }
    }

    IEnumerator ExhaleRoutine()
    {
        index = 0;
        timer = 0;

        // Δυναμικός υπολογισμός frame rate για την εκπνοή
        float exhaleFrameRate = exhaleFrames.Length / targetDuration;

        while (index < exhaleFrames.Length)
        {
            timer += Time.unscaledDeltaTime;

            if (timer >= 1f / exhaleFrameRate)
            {
                timer = 0f;

                character.sprite = exhaleFrames[index];
                index++;
            }

            yield return null;
        }

        character.sprite = normal;
        exhaleStarted = false;

        OnCycleFinished?.Invoke();
    }
}