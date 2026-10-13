using UnityEngine;

/// <summary>Representa las fases existentes sin controlar impactos ni tiempos de gameplay.</summary>
[DefaultExecutionOrder(100)]
public sealed class PrototypeAttackPresentation : MonoBehaviour
{
    [SerializeField] private PrototypePlayerAttack attack;
    [SerializeField] private PrototypeEnemyAttack enemyAttack;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Vector3 restPosition = new Vector3(0.55f, -0.2f, 0.15f);
    [SerializeField] private Vector3 raisedPosition = new Vector3(0.65f, 0.5f, 0.6f);
    [SerializeField] private Vector3 contactPosition = new Vector3(0.65f, -0.15f, 0.5f);
    [SerializeField] private Vector3 contactAngles = new Vector3(5f, -20f, 0f);
    private const float SwingStartProgress = 0.7f;
    [SerializeField] private Vector3 finishPosition = new Vector3(0.65f, 0.15f, 0.6f);
    [SerializeField] private Vector3 restAngles = new Vector3(-5f, 15f, 0f);
    [SerializeField] private Vector3 raisedAngles = new Vector3(-55f, 60f, 0f);
    [SerializeField] private Vector3 finishAngles = new Vector3(15f, -55f, 0f);
    [SerializeField, Min(0.01f)] private float parryRecoilDuration = 0.12f;

    public void Configure(PrototypePlayerAttack source, Transform pivot)
    {
        attack = source;
        enemyAttack = null;
        weaponPivot = pivot;
        ApplyPose();
    }

    public void Configure(PrototypeEnemyAttack source, Transform pivot)
    {
        attack = null;
        enemyAttack = source;
        weaponPivot = pivot;
        ApplyPose();
    }

    private void LateUpdate() => ApplyPose();

    private void ApplyPose()
    {
        if ((attack == null && enemyAttack == null) || weaponPivot == null)
            return;
        if (attack != null && attack.IsStaggered)
        {
            float segment = Mathf.Min(parryRecoilDuration, attack.StaggerDuration * 0.5f);
            float elapsed = attack.StaggerElapsed;
            if (elapsed < segment)
                SetPose(contactPosition, raisedPosition, contactAngles, new Vector3(-25f, 35f, -20f), Mathf.SmoothStep(0f, 1f, elapsed / segment));
            else
                SetPose(raisedPosition, restPosition, new Vector3(-25f, 35f, -20f), restAngles,
                    Mathf.SmoothStep(0f, 1f, (elapsed - attack.StaggerDuration + segment) / segment));
            return;
        }
        if (enemyAttack != null && enemyAttack.IsExposed)
        {
            // El primer frame conserva contacto; después la espada retrocede por el desvío.
            SetPose(contactPosition, raisedPosition, contactAngles, new Vector3(-25f, 35f, -20f),
                Mathf.SmoothStep(0f, 1f, enemyAttack.ExposureElapsed / parryRecoilDuration));
            return;
        }
        var defense = enemyAttack != null ? enemyAttack.GetComponent<PrototypeEnemyParry>() : null;
        Vector3 guardPosition = new Vector3(0.55f, 0.25f, 0.55f);
        Vector3 guardAngles = new Vector3(-25f, -10f, 10f);
        if (defense != null && defense.IsBusy)
        {
            if (defense.Phase == PrototypeParryPhase.Windup)
                SetPose(restPosition, guardPosition, restAngles, guardAngles, Mathf.SmoothStep(0f, 1f, defense.PhaseProgress));
            else if (defense.Phase == PrototypeParryPhase.Active)
                SetPose(guardPosition, guardPosition, guardAngles, guardAngles, 0f);
            else
                SetPose(guardPosition, restPosition, guardAngles, restAngles, Mathf.SmoothStep(0f, 1f, defense.PhaseProgress));
            return;
        }
        var phase = attack != null ? attack.Phase : enemyAttack.Phase;
        float progress = attack != null ? attack.PhaseProgress : enemyAttack.PhaseProgress;
        float blend = Mathf.SmoothStep(0f, 1f, progress);
        switch (phase)
        {
            case PrototypeAttackPhase.Windup:
                // La preparación termina en contacto: al habilitar daño, el arma ya anuncia impacto.
                if (progress < SwingStartProgress)
                    SetPose(restPosition, raisedPosition, restAngles, raisedAngles,
                        Mathf.SmoothStep(0f, 1f, progress / SwingStartProgress));
                else
                    SetPose(raisedPosition, contactPosition, raisedAngles, contactAngles,
                        Mathf.SmoothStep(0f, 1f, (progress - SwingStartProgress) / (1f - SwingStartProgress)));
                break;
            case PrototypeAttackPhase.Active:
                SetPose(contactPosition, finishPosition, contactAngles, finishAngles, blend);
                break;
            case PrototypeAttackPhase.Recovery:
                SetPose(finishPosition, restPosition, finishAngles, restAngles, blend);
                break;
            default:
                SetPose(restPosition, restPosition, restAngles, restAngles, 0f);
                break;
        }
        if (defense != null && defense.Result == PrototypeParryResult.Success && defense.SuccessElapsed < parryRecoilDuration)
        {
            float transition = Mathf.SmoothStep(0f, 1f, defense.SuccessElapsed / parryRecoilDuration);
            weaponPivot.localPosition = Vector3.Lerp(guardPosition, weaponPivot.localPosition, transition);
            weaponPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(guardAngles), weaponPivot.localRotation, transition);
        }
    }

    private void SetPose(Vector3 fromPosition, Vector3 toPosition, Vector3 fromAngles, Vector3 toAngles, float blend)
    {
        weaponPivot.localPosition = Vector3.Lerp(fromPosition, toPosition, blend);
        weaponPivot.localRotation = Quaternion.Slerp(Quaternion.Euler(fromAngles), Quaternion.Euler(toAngles), blend);
    }

    private void OnDisable()
    {
        if (weaponPivot != null)
            SetPose(restPosition, restPosition, restAngles, restAngles, 0f);
    }
}
