using UnityEngine;

public enum PrototypeStaminaAction { Attack, Dodge, Parry }

/// <summary>Único dueño del recurso. Las acciones deciden si pueden empezar antes de pagar.</summary>
[RequireComponent(typeof(PrototypeHealth))]
[DefaultExecutionOrder(-150)]
public sealed class PrototypeStamina : MonoBehaviour
{
    [SerializeField, Min(1f)] private float maximum = 100f;
    [SerializeField, Min(0f)] private float attackCost = 20f;
    [SerializeField, Min(0f)] private float dodgeCost = 30f;
    [SerializeField, Min(0f)] private float parryCost = 15f;
    [SerializeField, Min(0f)] private float regenerationPerSecond = 25f;
    [SerializeField, Min(0f), Tooltip("Espera tras dejar de actuar; una pulsación rechazada no reinicia esta pausa.")]
    private float regenerationDelay = 0.8f;
    private PrototypeHealth health;
    private float delayRemaining;
    private double refusalUntil;
    private string refusal = "";
    private PrototypeStaminaAction refusedAction;
    public float Current { get; private set; }
    public float Maximum => Mathf.Max(1f, maximum);
    public float RegenerationDelayRemaining => delayRemaining;
    public string RefusalMessage => Time.timeAsDouble < refusalUntil && Current < Cost(refusedAction) ? refusal : "";
    public event System.Action<PrototypeStaminaAction, float, float> Spent;
    public event System.Action<PrototypeStaminaAction, float, float> Refused;

    private void Awake() { health = GetComponent<PrototypeHealth>(); ResetStamina(); }
    private void OnEnable()
    {
        if (health == null) health = GetComponent<PrototypeHealth>();
        health.ResetCompleted += ResetStamina;
    }
    private void OnDisable() { if (health != null) health.ResetCompleted -= ResetStamina; }
    private void Update() => Advance(Time.deltaTime);

    public float Cost(PrototypeStaminaAction action)
    {
        switch (action)
        {
            case PrototypeStaminaAction.Attack: return Mathf.Max(0f, attackCost);
            case PrototypeStaminaAction.Dodge: return Mathf.Max(0f, dodgeCost);
            case PrototypeStaminaAction.Parry: return Mathf.Max(0f, parryCost);
            default: throw new System.ArgumentOutOfRangeException(nameof(action));
        }
    }

    public bool TrySpend(PrototypeStaminaAction action)
    {
        if (!isActiveAndEnabled || HasEnded()) return false;
        float cost = Cost(action);
        if (Current < cost)
        {
            refusal = "Sin stamina para " + ActionLabel(action) + " (necesitás " + cost.ToString("0") + ").";
            refusalUntil = Time.timeAsDouble + 1.2d;
            refusedAction = action;
            Refused?.Invoke(action, cost, Current);
            return false;
        }
        Current = Mathf.Clamp(Current - cost, 0f, Maximum);
        delayRemaining = Mathf.Max(0f, regenerationDelay);
        refusal = ""; refusalUntil = 0d;
        Spent?.Invoke(action, cost, Current);
        return true;
    }

    private bool HasEnded()
    {
        var session = GetComponent<PrototypeDuelSession>();
        return (health != null && health.IsDefeated) || (session != null && session.HasEnded);
    }

    private void Advance(float deltaTime)
    {
        if (!isActiveAndEnabled || HasEnded()) return;
        var attack = GetComponent<PrototypePlayerAttack>();
        var dodge = GetComponent<PrototypePlayerDodge>();
        var parry = GetComponent<PrototypePlayerParry>();
        if ((attack != null && attack.IsAttacking) || (dodge != null && dodge.IsBusy) || (parry != null && parry.IsBusy))
        {
            delayRemaining = Mathf.Max(0f, regenerationDelay);
            return;
        }
        float available = Mathf.Max(0f, deltaTime);
        float waiting = Mathf.Min(available, delayRemaining);
        delayRemaining -= waiting;
        available -= waiting;
        Current = Mathf.Clamp(Current + available * Mathf.Max(0f, regenerationPerSecond), 0f, Maximum);
    }

    public void ResetStamina()
    {
        Current = Maximum; delayRemaining = 0f; refusalUntil = 0d; refusal = "";
    }

    public static string ActionLabel(PrototypeStaminaAction action) => action == PrototypeStaminaAction.Attack ? "atacar"
        : action == PrototypeStaminaAction.Dodge ? "esquivar" : "hacer parry";
}
