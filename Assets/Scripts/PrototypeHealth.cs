using UnityEngine;

/// <summary>Vida y notificaciones de daño; compartida por jugador y enemigo.</summary>
[DefaultExecutionOrder(-250)]
public sealed class PrototypeHealth : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 100;
    public int Health { get; private set; }
    public int MaximumHealth => maximumHealth;
    public bool IsDefeated => Health <= 0;
    public event System.Action<int> Damaged;
    public event System.Action ResetCompleted;

    private void Awake() => ResetHealth();

    public void ResetHealth()
    {
        Health = maximumHealth;
        ResetCompleted?.Invoke();
    }

    public bool TakeDamage(int amount)
    {
        if (IsDefeated || amount <= 0)
            return false;
        int previous = Health;
        Health = Mathf.Max(0, Health - amount);
        Damaged?.Invoke(previous - Health);
        return true;
    }
}
