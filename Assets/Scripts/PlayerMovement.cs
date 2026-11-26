using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    static readonly int IsMovingHash = Animator.StringToHash("IsMoving");

    PlayerStats stats;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Animator anim;
    DirectionalVisual directionalVisual;
    InputAction move;
    Vector2 input;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();

        Transform visual = transform.Find("VisualRoot");
        sr = visual != null ? visual.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        anim = visual != null ? visual.GetComponent<Animator>() : GetComponentInChildren<Animator>();
        directionalVisual = visual != null ? visual.GetComponent<DirectionalVisual>() : GetComponentInChildren<DirectionalVisual>();
        if (directionalVisual != null) directionalVisual.ResetVisual();
        if (anim != null) anim.enabled = anim.runtimeAnimatorController != null;

        move = new InputAction("Move", InputActionType.Value);
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow")
            .With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow")
            .With("Right", "<Keyboard>/rightArrow");
        move.AddBinding("<Gamepad>/leftStick"); // ekran joysticki də bunu istifadə edir
    }

    void OnEnable() { move.Enable(); }
    void OnDisable() { move.Disable(); }
    void OnDestroy() { move?.Dispose(); }

    void Update()
    {
        input = move.ReadValue<Vector2>();
        if (input.sqrMagnitude > 1f) input.Normalize();

        if (Time.timeScale > 0f)
        {
            if (directionalVisual != null)
            {
                Vector2 moveVel = stats != null ? input * stats.moveSpeed : input;
                directionalVisual.TickVisual(moveVel, Time.deltaTime);
            }
            else if (sr != null && Mathf.Abs(input.x) > 0.05f)
            {
                sr.flipX = input.x < 0f;
            }

            if (anim != null)
            {
                if (anim.runtimeAnimatorController != null)
                {
                    if (!anim.enabled) anim.enabled = true;
                    anim.SetBool(IsMovingHash, input.sqrMagnitude > 0.0025f);
                }
                else if (anim.enabled)
                {
                    anim.enabled = false;
                }
            }
        }
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * stats.moveSpeed;
    }
}