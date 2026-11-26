using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(PlayerStats))]
public class PlayerMovement : MonoBehaviour
{
    PlayerStats stats;
    Rigidbody2D rb;
    InputAction move;
    Vector2 input;

    void Awake()
    {
        stats = GetComponent<PlayerStats>();
        rb = GetComponent<Rigidbody2D>();

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
    }

    void FixedUpdate()
    {
        rb.linearVelocity = input * stats.moveSpeed;
    }
}