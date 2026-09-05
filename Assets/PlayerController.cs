using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float sprintSpeed = 9.5f;
    [SerializeField] private float rotationSpeed = 14f;

    [Header("Stamina")]
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float staminaDrainPerSecond = 28f;
    [SerializeField] private float staminaRecoveryPerSecond = 20f;
    [SerializeField] private float staminaRecoveryDelay = 0.7f;

    private Rigidbody playerRigidbody;
    private Vector2 moveInput;
    private bool sprintHeld;
    private float stamina;
    private float recoveryStartsAt;
    private float currentMoveSpeed;
    private Vector2 aiInput;

    public Vector3 MoveDirection { get; private set; }
    public float MoveSpeed => currentMoveSpeed;
    public float StaminaNormalized => maxStamina > 0f ? stamina / maxStamina : 0f;
    public bool IsSprinting { get; private set; }

    public void SetAIInput(Vector2 input) => aiInput = Vector2.ClampMagnitude(input, 1f);

    public void ClearInput()
    {
        moveInput = aiInput = Vector2.zero;
        sprintHeld = false;
        MoveDirection = Vector3.zero;
        IsSprinting = false;
    }

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
        playerRigidbody.constraints |= RigidbodyConstraints.FreezeRotationX |
                                       RigidbodyConstraints.FreezeRotationZ;
        stamina = maxStamina;
        currentMoveSpeed = moveSpeed;
    }

    private void Update()
    {
        if (FutsalGameManager.Instance != null && !FutsalGameManager.Instance.IsPlaying)
        {
            moveInput = Vector2.zero;
            sprintHeld = false;
            MoveDirection = Vector3.zero;
            IsSprinting = false;
            return;
        }

        FutsalPlayer member = GetComponent<FutsalPlayer>();
        bool human = member == null || member.IsHuman;
        moveInput = human ? ReadMoveInput() : aiInput;
        sprintHeld = human && ReadSprintInput();
    }

    private void FixedUpdate()
    {
        if (FutsalGameManager.Instance != null && !FutsalGameManager.Instance.IsPlaying)
            return;
        FutsalPlayer member = GetComponent<FutsalPlayer>();
        if (member != null && !member.IsHuman)
            moveInput = aiInput;
        Vector3 moveDirection = new Vector3(moveInput.x, 0f, moveInput.y);
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }

        MoveDirection = moveDirection;

        UpdateSprint(moveDirection.sqrMagnitude > 0.01f);

        Vector3 velocity = playerRigidbody.linearVelocity;
        velocity.x = moveDirection.x * currentMoveSpeed;
        velocity.z = moveDirection.z * currentMoveSpeed;
        playerRigidbody.linearVelocity = velocity;

        if (moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion smoothRotation = Quaternion.Slerp(
                playerRigidbody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime);
            playerRigidbody.MoveRotation(smoothRotation);
        }
    }

    private static Vector2 ReadMoveInput()
    {
        Vector2 input = Vector2.zero;

        if (Keyboard.current != null)
        {
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed)
                input.x -= 1f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed)
                input.x += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed)
                input.y -= 1f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed)
                input.y += 1f;
        }

        if (Gamepad.current != null)
        {
            Vector2 gamepadInput = Gamepad.current.leftStick.ReadValue();
            if (gamepadInput.sqrMagnitude > input.sqrMagnitude)
                input = gamepadInput;
        }

        return Vector2.ClampMagnitude(input, 1f);
    }

    private static bool ReadSprintInput()
    {
        bool keyboardSprint = Keyboard.current != null &&
                              (Keyboard.current.leftShiftKey.isPressed ||
                               Keyboard.current.rightShiftKey.isPressed);
        bool gamepadSprint = Gamepad.current != null && Gamepad.current.rightShoulder.isPressed;
        return keyboardSprint || gamepadSprint;
    }

    private void UpdateSprint(bool isMoving)
    {
        IsSprinting = sprintHeld && isMoving && stamina > 0.01f;

        if (IsSprinting)
        {
            currentMoveSpeed = sprintSpeed;
            stamina = Mathf.Max(0f, stamina - staminaDrainPerSecond * Time.fixedDeltaTime);
            recoveryStartsAt = Time.time + staminaRecoveryDelay;

            if (stamina <= 0f)
                IsSprinting = false;
        }
        else
        {
            currentMoveSpeed = moveSpeed;
            if (Time.time >= recoveryStartsAt)
            {
                stamina = Mathf.Min(
                    maxStamina,
                    stamina + staminaRecoveryPerSecond * Time.fixedDeltaTime);
            }
        }
    }
}
