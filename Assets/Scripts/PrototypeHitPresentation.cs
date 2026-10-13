using UnityEngine;

/// <summary>Señal de daño real: destello geométrico y número flotante; sin colliders.</summary>
[DefaultExecutionOrder(100)]
public sealed class PrototypeHitPresentation : MonoBehaviour
{
    [SerializeField] private PrototypeHealth target;
    [SerializeField] private Transform impactGraphic;
    [SerializeField, Min(0.01f)] private float visibleDuration = 0.45f;
    private float remaining;
    private int lastDamage;
    private GUIStyle damageStyle;

    public void Configure(PrototypeHealth source, Transform graphic)
    {
        Unsubscribe();
        target = source;
        impactGraphic = graphic;
        ResetFeedback();
        if (isActiveAndEnabled)
            Subscribe();
    }

    private void OnEnable()
    {
        ResetFeedback();
        Subscribe();
    }

    private void Subscribe()
    {
        if (target == null)
            return;
        // Evita suscripciones duplicadas al reconfigurar o reactivar.
        Unsubscribe();
        target.Damaged += OnDamaged;
        target.ResetCompleted += ResetFeedback;
    }

    private void Unsubscribe()
    {
        if (target == null)
            return;
        target.Damaged -= OnDamaged;
        target.ResetCompleted -= ResetFeedback;
    }

    private void OnDamaged(int amount)
    {
        lastDamage = amount;
        remaining = Mathf.Max(0.01f, visibleDuration);
        UpdateGraphic();
    }

    private void ResetFeedback()
    {
        remaining = 0f;
        lastDamage = 0;
        if (impactGraphic != null)
            impactGraphic.gameObject.SetActive(false);
    }

    private void LateUpdate() => AdvanceVisual(Time.deltaTime);

    private void AdvanceVisual(float deltaTime)
    {
        remaining = Mathf.Max(0f, remaining - Mathf.Max(0f, deltaTime));
        UpdateGraphic();
    }

    private void UpdateGraphic()
    {
        if (impactGraphic == null)
            return;
        impactGraphic.gameObject.SetActive(remaining > 0f);
        float progress = 1f - remaining / Mathf.Max(0.01f, visibleDuration);
        impactGraphic.localPosition = new Vector3(0f, 1.3f + progress * 0.4f, 0f);
        impactGraphic.localScale = Vector3.one * Mathf.Lerp(0.65f, 1.2f, progress);
        Camera camera = Camera.main;
        if (camera != null)
            impactGraphic.rotation = camera.transform.rotation;
    }

    private void OnGUI()
    {
        if (remaining <= 0f || impactGraphic == null || Camera.main == null)
            return;
        Vector3 screenPoint = Camera.main.WorldToScreenPoint(impactGraphic.position + Vector3.up * 0.35f);
        if (screenPoint.z <= 0f)
            return;
        if (damageStyle == null)
        {
            damageStyle = new GUIStyle(GUI.skin.label) { fontSize = 26, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            damageStyle.normal.textColor = Color.yellow;
        }
        GUI.Label(new Rect(screenPoint.x - 50f, Screen.height - screenPoint.y - 18f, 100f, 36f), "-" + lastDamage, damageStyle);
    }

    private void OnDisable()
    {
        Unsubscribe();
        ResetFeedback();
    }
}
