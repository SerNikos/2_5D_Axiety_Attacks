using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private int speed;

    private PlayerControls playerControls;
    private Rigidbody rb;
    private Vector3 movement;
    private Vector3 facingDirection = Vector3.back;
    private DepthSort2D depthSort;

    public Vector3 FacingDirection => facingDirection;

    [Header("Sprites")]
    [SerializeField] private SpriteRenderer spriteRenderer;

    [SerializeField] private Sprite upSprite;
    [SerializeField] private Sprite downSprite;
    [SerializeField] private Sprite sideSprite;

    [Header("Anxiety Bar")]
    public AnxietyBar anxietyBar;
    public int maxAnxiety = 100;
    public int minAnxiety = 0;
    public int currentAnxiety;

    private void Awake()
    {
        playerControls = new PlayerControls();
        depthSort = GetComponent<DepthSort2D>();

        if (depthSort == null)
        {
            depthSort = gameObject.AddComponent<DepthSort2D>();
        }

        depthSort.SetSortDirection(DepthSort2D.SortDirection.CameraDepth);
        depthSort.SetSpritesOnly(true);
    }

    private void OnEnable()
    {
        playerControls.Enable();
    }

    private void Start()
    {
        rb = GetComponent<Rigidbody>();

        currentAnxiety = minAnxiety;
        anxietyBar.SetMinHealth(minAnxiety);
    }

    private void Update()
    {
        float x = playerControls.Player.Move.ReadValue<Vector2>().x;
        float z = playerControls.Player.Move.ReadValue<Vector2>().y;

        movement = new Vector3(x, 0, z).normalized;

        if (movement.sqrMagnitude > 0.01f)
        {
            facingDirection = movement;
        }

        UpdateSpriteDirection(x, z);
    }

    private void FixedUpdate()
    {
        rb.MovePosition(rb.position + movement * (speed * Time.fixedDeltaTime));
    }

    private void UpdateSpriteDirection(float x, float z)
    {
        // No movement
        if (movement.magnitude <= 0.01f)
            return;

        // PRIORITIZE VERTICAL MOVEMENT
        if (Mathf.Abs(z) > Mathf.Abs(x))
        {
            // UP
            if (z > 0)
            {
                spriteRenderer.sprite = upSprite;
            }
            // DOWN
            else
            {
                spriteRenderer.sprite = downSprite;
            }
        }
        else
        {
            // SIDE SPRITE
            spriteRenderer.sprite = sideSprite;

            // RIGHT
            if (x > 0)
            {
                spriteRenderer.flipX = false;
            }
            // LEFT
            else
            {
                spriteRenderer.flipX = true;
            }
        }
    }

    // Anxiety Bar Take
    public void TakeAnxiety(int anxiety)
    {
        currentAnxiety -= anxiety;
        anxietyBar.SetAnxiety(currentAnxiety);
    }

    // Anxiety Bar Give
    public void GiveAnxiety(int anxiety)
    {
        currentAnxiety += anxiety;
        anxietyBar.SetAnxiety(currentAnxiety);
    }
}