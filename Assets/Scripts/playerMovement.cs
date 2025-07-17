using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class playerMovement : MonoBehaviour
{
    [Header("Health System")]
    public int maxHealth = 100;
    private int currentHealth;
    public TextMeshProUGUI healthText;

    [Header("Lives System")]
    public int maxLives = 5;
    private int currentLives;
    public TextMeshProUGUI livesText;

    [Header("Key System")]
    public int totalKeys = 0;
    public TextMeshProUGUI keyText;

    [Header("Knockback Settings")]
    [SerializeField] private float knockBackTime = 0.2f;
    [SerializeField] private float knockBackThrust = 10f;
    private bool isKnockedBack = false;

    [Header("Player Movement")]
    public float moveSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpForce = 10f;

    private Rigidbody2D rb;
    private Animator anim;
    private SpriteRenderer sprite;
    private PlayerController playerController;

    private float mobileInputX = 0f;
    private Vector2 moveInput;
    private bool isJumping = false;

    private enum MovementState { idle, walk, jump, fall, run }

    [Header("Jump Settings")]
    [SerializeField] private LayerMask jumpableGround;
    private BoxCollider2D coll;

    [Header("Next Level Panel")]
    public GameObject nextLevelPanel;
    public Button nextLevelButton;

    private string nextSceneName;
    private int requiredKeys;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<Animator>();
        sprite = GetComponent<SpriteRenderer>();
        coll = GetComponent<BoxCollider2D>();

        playerController = new PlayerController();
    }

    private void OnEnable()
    {
        playerController.Enable();
        playerController.movement.move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        playerController.movement.move.canceled += ctx => moveInput = Vector2.zero;
        playerController.movement.jump.performed += ctx => Jump();
    }

    private void OnDisable()
    {
        playerController.Disable();
    }

    private void Start()
    {
        currentHealth = maxHealth;
        currentLives = maxLives;
        UpdateUI();

        if (nextLevelPanel != null)
            nextLevelPanel.SetActive(false);

        // Atur jumlah key & scene tujuan berdasarkan level saat ini
        string currentScene = SceneManager.GetActiveScene().name;

        if (currentScene == "level1")
        {
            requiredKeys = 5;
            nextSceneName = "level2";
        }
        else if (currentScene == "level2")
        {
            requiredKeys = 15;
            nextSceneName = "level3";
        }
        else if (currentScene == "level3")
        {
            requiredKeys = 25;
            nextSceneName = "mainmenu";
        }
    }

    private void Update()
    {
        if (Application.isMobilePlatform)
        {
            moveInput = new Vector2(mobileInputX, 0f);
        }
        else
        {
            moveInput = playerController.movement.move.ReadValue<Vector2>();
        }

        if (transform.position.y < -10f)
        {
            currentLives--;

            if (currentLives <= 0)
            {
                Debug.Log("Game Over!");
                gameObject.SetActive(false);
            }
            else
            {
                transform.position = new Vector3(0f, 2f, 0f);
            }

            UpdateUI();
        }
    }

    private void FixedUpdate()
    {
        Vector2 targetVelocity = new Vector2((moveInput.x + mobileInputX) * moveSpeed, rb.velocity.y);
        rb.velocity = targetVelocity;

        UpdateAnimation();

        if (isGrounded() && Mathf.Abs(rb.velocity.y) < 0.01f)
        {
            isJumping = false;
        }
    }

    private void UpdateAnimation()
    {
        MovementState state;
        float horizontal = moveInput.x != 0 ? moveInput.x : mobileInputX;

        if (horizontal > 0f)
        {
            state = MovementState.walk;
            sprite.flipX = false;
        }
        else if (horizontal < 0f)
        {
            state = MovementState.walk;
            sprite.flipX = true;
        }
        else
        {
            state = MovementState.idle;
        }

        if (rb.velocity.y > 0.1f)
        {
            state = MovementState.jump;
        }
        else if (rb.velocity.y < -0.1f)
        {
            state = MovementState.fall;
        }

        anim.SetInteger("state", (int)state);
    }

    private bool isGrounded()
    {
        return Physics2D.BoxCast(coll.bounds.center, coll.bounds.size, 0f, Vector2.down, .1f, jumpableGround);
    }

    private void Jump()
    {
        if (isGrounded())
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            isJumping = true;
        }
    }

    public void MoveRight(bool isPressed)
    {
        if (isPressed)
            mobileInputX = 1f;
        else if (mobileInputX == 1f)
            mobileInputX = 0f;
    }

    public void MoveLeft(bool isPressed)
    {
        if (isPressed)
            mobileInputX = -1f;
        else if (mobileInputX == -1f)
            mobileInputX = 0f;
    }

    public void MobileJump()
    {
        if (isGrounded())
        {
            Jump();
        }
    }

    public void TakeDamage(int damage, Vector2 direction)
    {
        if (isKnockedBack) return;

        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            currentHealth = 0;
            Debug.Log("Player Mati");
        }

        StartCoroutine(HandleKnockback(direction.normalized));
        UpdateHealthUI();
    }

    private void UpdateHealthUI()
    {
        if (healthText != null)
            healthText.text = "Health: " + currentHealth;
    }

    private IEnumerator HandleKnockback(Vector2 direction)
    {
        isKnockedBack = true;
        rb.velocity = Vector2.zero;

        Vector2 force = direction * knockBackThrust * rb.mass;
        rb.AddForce(force, ForceMode2D.Impulse);

        yield return new WaitForSeconds(knockBackTime);
        rb.velocity = Vector2.zero;
        isKnockedBack = false;
    }

    private void UpdateUI()
    {
        if (livesText != null)
            livesText.text = " " + currentLives;

        if (keyText != null)
            keyText.text = " " + totalKeys;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Key"))
        {
            totalKeys++;
            UpdateUI();
            Destroy(collision.gameObject);

            if (totalKeys >= requiredKeys)
            {
                ShowNextLevelPanel();
            }
        }
    }

    private void ShowNextLevelPanel()
    {
        if (nextLevelPanel != null)
        {
            nextLevelPanel.SetActive(true);
            rb.velocity = Vector2.zero;
            this.enabled = false;

            if (nextLevelButton != null)
            {
                nextLevelButton.onClick.RemoveAllListeners();
                nextLevelButton.onClick.AddListener(LoadNextLevel);
            }
        }
    }

    public void LoadNextLevel()
    {
        SceneManager.LoadScene(nextSceneName);
    }
}
