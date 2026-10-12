using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Objetivo inmóvil con vida, destello de impacto y reinicio local.</summary>
[RequireComponent(typeof(Renderer))]
public sealed class PrototypeTrainingTarget : MonoBehaviour
{
    [SerializeField, Min(1)] private int maximumHealth = 100;
    private Renderer targetRenderer;
    private MaterialPropertyBlock properties;
    private float flashRemaining;
    public int Health { get; private set; }
    public int MaximumHealth => maximumHealth;
    public bool IsDefeated => Health <= 0;

    private void Awake() => ResetTarget();

    public void ResetTarget()
    {
        Health = maximumHealth;
        flashRemaining = 0f;
        RefreshColor();
    }

    public void TakeDamage(int amount)
    {
        if (IsDefeated || amount <= 0)
            return;
        Health = Mathf.Max(0, Health - amount);
        flashRemaining = 0.2f;
        RefreshColor();
    }

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            ResetTarget();
        if (flashRemaining > 0f)
        {
            flashRemaining -= Time.deltaTime;
            RefreshColor();
        }
    }

    private void RefreshColor()
    {
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        if (properties == null)
            properties = new MaterialPropertyBlock();
        Color color = IsDefeated ? Color.gray : flashRemaining > 0f
            ? Color.white : new Color(0.1f, 0.8f, 0.45f);
        properties.SetColor("_BaseColor", color);
        targetRenderer.SetPropertyBlock(properties);
    }
}
