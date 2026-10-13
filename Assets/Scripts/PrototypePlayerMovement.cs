using UnityEngine;

/// <summary>Único dueño de CharacterController.Move: caminar, esquivar, gravedad y colisiones.</summary>
[RequireComponent(typeof(CharacterController))]
public sealed class PrototypePlayerMovement : MonoBehaviour
{
    [SerializeField] private Transform cameraTransform;
    [SerializeField, Min(0f)] private float speed = 5f;
    [SerializeField, Min(0f)] private float rotationSpeed = 12f;
    [SerializeField] private float gravity = -20f;
    private CharacterController controller;
    private PrototypePlayerAttack attack;
    private PrototypePlayerDodge dodge;
    private PrototypePlayerInput commands;
    private PrototypeHealth health;
    private float verticalSpeed;

    public void Configure(Transform movementCamera) => cameraTransform = movementCamera;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        attack = GetComponent<PrototypePlayerAttack>();
        dodge = GetComponent<PrototypePlayerDodge>();
        commands = GetComponent<PrototypePlayerInput>();
        health = GetComponent<PrototypeHealth>();
        if (cameraTransform == null)
        {
            Debug.LogError("Asigná la cámara a PrototypePlayerMovement.", this);
            enabled = false;
        }
    }

    private void Update() => AdvanceMovement(commands != null ? commands.Move : Vector2.zero, Time.deltaTime);
    public void ResetMovement() => verticalSpeed = 0f;

    public Vector3 GetMoveDirection(Vector2 input)
    {
        input = Vector2.ClampMagnitude(input, 1f);
        Vector3 forward = Vector3.ProjectOnPlane(cameraTransform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, forward);
        return forward * input.y + right * input.x;
    }

    private void AdvanceMovement(Vector2 input, float deltaTime)
    {
        deltaTime = Mathf.Max(0f, deltaTime);
        bool wasDodging = dodge != null && dodge.IsBusy;
        bool blocked = wasDodging || (attack != null && attack.IsAttacking) || (health != null && health.IsDefeated);
        Vector3 direction = GetMoveDirection(blocked ? Vector2.zero : input);
        Vector3 dodgeDisplacement = dodge != null ? dodge.ConsumeDisplacement(deltaTime) : Vector3.zero;
        if (controller.isGrounded && verticalSpeed < 0f)
            verticalSpeed = -2f;
        verticalSpeed += gravity * deltaTime;
        // El tiempo sobrante al terminar recuperación no se convierte en caminar en este frame.
        controller.Move(direction * speed * deltaTime + dodgeDisplacement + Vector3.up * verticalSpeed * deltaTime);
        if (direction.sqrMagnitude > 0.001f)
        {
            Quaternion desiredRotation = Quaternion.LookRotation(direction, Vector3.up);
            float blend = 1f - Mathf.Exp(-rotationSpeed * deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, desiredRotation, blend);
        }
    }
}
