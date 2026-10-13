using UnityEngine;

public enum PrototypeDodgePhase { Ready, Moving, Recovery }

/// <summary>Captura dirección y tiempo; el motor consume el desplazamiento y resuelve colisiones.</summary>
[DefaultExecutionOrder(-120)]
[RequireComponent(typeof(PrototypePlayerMovement))]
public sealed class PrototypePlayerDodge : MonoBehaviour
{
    [SerializeField, Min(0.01f)] private float distance = 3f;
    [SerializeField, Min(0.01f)] private float travelDuration = 0.25f;
    [SerializeField, Min(0.01f)] private float recoveryDuration = 0.2f;
    [SerializeField] private Renderer indicator;
    private PrototypePlayerMovement movement;
    private PrototypePlayerInput input;
    private PrototypePlayerAttack attack;
    private PrototypeHealth health;
    private PrototypeDuelSession duel;
    private Vector3 direction;
    private float remaining;
    private MaterialPropertyBlock properties;

    public event System.Action<bool, Vector3> Attempted;
    public PrototypeDodgePhase Phase { get; private set; }
    public bool IsBusy => Phase != PrototypeDodgePhase.Ready;
    public void Configure(Renderer renderer) { indicator = renderer; CancelDodge(); }

    private void Awake()
    {
        movement = GetComponent<PrototypePlayerMovement>();
        input = GetComponent<PrototypePlayerInput>();
        attack = GetComponent<PrototypePlayerAttack>();
        health = GetComponent<PrototypeHealth>();
        duel = GetComponent<PrototypeDuelSession>();
    }

    private void Update()
    {
        if (input != null && input.DodgePressed)
            TryDodge(input.Move);
    }

    public bool TryDodge(Vector2 intent)
    {
        var parry = GetComponent<PrototypePlayerParry>();
        if (!isActiveAndEnabled || IsBusy || (parry != null && parry.IsBusy) || (attack != null && attack.IsAttacking)
            || (health != null && health.IsDefeated) || (duel != null && duel.HasEnded))
        {
            Attempted?.Invoke(false, Vector3.zero);
            return false;
        }
        direction = movement.GetMoveDirection(intent);
        var stamina = GetComponent<PrototypeStamina>();
        if (stamina != null && !stamina.TrySpend(PrototypeStaminaAction.Dodge))
        {
            Attempted?.Invoke(false, Vector3.zero);
            return false;
        }
        if (direction.sqrMagnitude < 0.001f)
            direction = Vector3.ProjectOnPlane(-transform.forward, Vector3.up).normalized;
        else
        {
            direction.Normalize();
            transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }
        SetPhase(PrototypeDodgePhase.Moving, travelDuration);
        Attempted?.Invoke(true, direction);
        return true;
    }

    // Solo PrototypePlayerMovement avanza este reloj y llama a CharacterController.Move.
    public Vector3 ConsumeDisplacement(float deltaTime)
    {
        if (!isActiveAndEnabled || (health != null && health.IsDefeated) || (duel != null && duel.HasEnded))
        {
            CancelDodge();
            return Vector3.zero;
        }
        float available = Mathf.Max(0f, deltaTime);
        float traveledTime = 0f;
        while (IsBusy && available > 0f)
        {
            float used = Mathf.Min(available, remaining);
            if (Phase == PrototypeDodgePhase.Moving)
                traveledTime += used;
            available -= used;
            remaining -= used;
            if (remaining <= 0.000001f)
            {
                if (Phase == PrototypeDodgePhase.Moving)
                    SetPhase(PrototypeDodgePhase.Recovery, recoveryDuration);
                else
                    CancelDodge();
            }
        }
        return direction * (distance / travelDuration) * traveledTime;
    }

    public void CancelDodge() => SetPhase(PrototypeDodgePhase.Ready, 0f);
    private void OnDisable() => CancelDodge();

    private void SetPhase(PrototypeDodgePhase phase, float duration)
    {
        Phase = phase;
        remaining = duration;
        if (indicator == null)
            return;
        indicator.enabled = IsBusy;
        if (properties == null)
            properties = new MaterialPropertyBlock();
        properties.SetColor("_BaseColor", phase == PrototypeDodgePhase.Moving ? Color.cyan : new Color(0.25f, 0.4f, 0.5f));
        indicator.SetPropertyBlock(properties);
    }
}
