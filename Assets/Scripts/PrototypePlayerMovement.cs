using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Movimiento relativo a la cámara, con gravedad y colisiones.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PrototypePlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float speed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;

    private CharacterController controller;
    private PrototypePlayerAttack attack;
    private float verticalSpeed;

    public void Configure(Transform movementCamera)
    {
        cameraTransform = movementCamera;
    }

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        attack = GetComponent<PrototypePlayerAttack>();
        if (cameraTransform == null)
        {
            Debug.LogError("Asigná la cámara a PrototypePlayerMovement.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Vector2 input = Vector2.zero;
        if (keyboard != null)
        {
            input.x = (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed ? 1f : 0f)
                - (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed ? 1f : 0f);
            input.y = (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed ? 1f : 0f)
                - (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed ? 1f : 0f);
        }
        // El ataque bloquea dirección, pero la gravedad continúa.
        if (attack != null && attack.IsAttacking)
            input = Vector2.zero;
        input = Vector2.ClampMagnitude(input, 1f);

        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        Vector3 direction = forward * input.y + right * input.x;

        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += gravity * Time.deltaTime;
        controller.Move((direction * speed + Vector3.up * verticalSpeed) * Time.deltaTime);

        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }
}
