using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Color y destello del objetivo; la vida pertenece a PrototypeHealth.</summary>
[RequireComponent(typeof(Renderer), typeof(PrototypeHealth))]
public sealed class PrototypeTrainingTarget : MonoBehaviour
{
    private PrototypeHealth health;
    private Renderer targetRenderer;
    private MaterialPropertyBlock properties;
    private float flashRemaining;

    public void Configure(PrototypeHealth source)
    {
        Unsubscribe();
        health = source;
        Subscribe();
        ResetColor();
    }

    private void OnEnable()
    {
        health = GetComponent<PrototypeHealth>();
        Subscribe();
        ResetColor();
    }

    private void Subscribe()
    {
        if (health == null)
            return;
        Unsubscribe();
        health.Damaged += OnDamaged;
        health.ResetCompleted += ResetColor;
    }

    private void Unsubscribe()
    {
        if (health == null)
            return;
        health.Damaged -= OnDamaged;
        health.ResetCompleted -= ResetColor;
    }

    private void OnDamaged(int amount)
    {
        flashRemaining = 0.2f;
        RefreshColor();
    }

    private void ResetColor()
    {
        flashRemaining = 0f;
        RefreshColor();
    }

    private void Update()
    {
        // Conserva el reinicio local del ejercicio H1 cuando no hay un enemigo activo.
        if (GetComponent<PrototypeEnemyAttack>() == null && Keyboard.current != null
            && Keyboard.current.rKey.wasPressedThisFrame)
            health.ResetHealth();
        if (flashRemaining > 0f)
        {
            flashRemaining -= Time.deltaTime;
            RefreshColor();
        }
    }

    private void RefreshColor()
    {
        if (health == null)
            return;
        if (targetRenderer == null)
            targetRenderer = GetComponent<Renderer>();
        if (properties == null)
            properties = new MaterialPropertyBlock();
        Color color = health.IsDefeated ? Color.gray : flashRemaining > 0f
            ? Color.white : new Color(0.1f, 0.8f, 0.45f);
        properties.SetColor("_BaseColor", color);
        targetRenderer.SetPropertyBlock(properties);
    }

    private void OnDisable() => Unsubscribe();
}
