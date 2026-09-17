using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class GameManager : MonoBehaviour
{
    [Header("Scene References")]
    [SerializeField] private AnxietyBar anxietyBar;
    [SerializeField] private AnxietyBreathingClickHold breathing;
    [SerializeField] private DollyZoom dollyZoom;

    [Header("Input Settings")]
    [SerializeField] private float spaceAnxietyAmount = 20f;
    [SerializeField] private float spaceCooldown = 0.2f;

    [Header("Exhale Reduction")]
    [SerializeField] private float exhaleReduceAmount = 50f;
    [SerializeField] private float zoomReduceDuration = 1f;

    private float cooldown;

    public static GameManager Instance { get; private set; }

    // =========================================================
    // AWAKE
    // =========================================================
    private void Awake()
    {
        // Singleton setup
        if (Instance == null)
        {
            Instance = this;

            // GameManager survives scene changes
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    // =========================================================
    // ENABLE / DISABLE
    // =========================================================
    private void OnEnable()
    {
        // Listen for scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    // =========================================================
    // START
    // =========================================================
    private void Start()
    {
        // Find scene references the first time
        FindSceneReferences();

        InitializeAnxietyBar();
    }

    // =========================================================
    // UPDATE
    // =========================================================
    private void Update()
    {
        cooldown -= Time.deltaTime;

        // SPACE → increase anxiety
        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("SPACE PRESSED");

            // Safety check
            if (anxietyBar == null)
            {
                Debug.LogError("AnxietyBar reference is NULL!");
                return;
            }

            // Cooldown check
            if (cooldown <= 0f)
            {
                anxietyBar.AddAnxiety(spaceAnxietyAmount);

                cooldown = spaceCooldown;

                Debug.Log("Added Anxiety");
            }
        }

        // ESCAPE → quit
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            QuitGame();
        }
    }

    // =========================================================
    // SCENE LOADED
    // =========================================================
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("Scene Loaded: " + scene.name);

        // IMPORTANT:
        // Reconnect references because scene objects get destroyed
        FindSceneReferences();

        InitializeAnxietyBar();
    }

    // =========================================================
    // FIND REFERENCES
    // =========================================================
    void FindSceneReferences()
    {
        anxietyBar = FindObjectOfType<AnxietyBar>();
        breathing = FindObjectOfType<AnxietyBreathingClickHold>();
        dollyZoom = FindObjectOfType<DollyZoom>();

        // Reconnect breathing event safely
        if (breathing != null)
        {
            breathing.OnCycleFinished -= HandleExhaleFinished;
            breathing.OnCycleFinished += HandleExhaleFinished;
        }

        Debug.Log("References Reconnected");
    }

    // =========================================================
    // INITIALIZE UI
    // =========================================================
    void InitializeAnxietyBar()
    {
        if (anxietyBar == null)
        {
            Debug.LogError("AnxietyBar not found!");
            return;
        }

        anxietyBar.SetMaxAnxiety(100f);
        anxietyBar.SetAnxiety(0f);

        Debug.Log("Anxiety Bar Initialized");
    }

    // =========================================================
    // EXHALE EVENT
    // =========================================================
    void HandleExhaleFinished()
    {
        StartCoroutine(EndBreathSequence());
    }

    // =========================================================
    // EXHALE SEQUENCE
    // =========================================================
    IEnumerator EndBreathSequence()
    {
        // Emotional delay
        yield return new WaitForSeconds(0.3f);

        // World reaction delay
        yield return new WaitForSeconds(1f);

        // Disable anxiety effects
        if (dollyZoom != null)
        {
            dollyZoom.DisableAnxiety();
        }

        // Reduce anxiety smoothly
        float t = 0f;

        while (t < zoomReduceDuration)
        {
            t += Time.deltaTime;

            if (anxietyBar != null)
            {
                anxietyBar.AddAnxiety(-exhaleReduceAmount * Time.deltaTime);
            }

            yield return null;
        }
    }

    // =========================================================
    // LOAD SCENE
    // =========================================================
    public void LoadScene(int sceneIndex)
    {
        Debug.Log("Loading Scene: " + sceneIndex);

        if (sceneIndex >= 0 &&
            sceneIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(sceneIndex);
        }
        else
        {
            Debug.LogError("Invalid Scene Index");
        }
    }

    // =========================================================
    // QUIT GAME
    // =========================================================
    public void QuitGame()
    {
        Debug.Log("QUIT GAME");

        Application.Quit();
    }
}