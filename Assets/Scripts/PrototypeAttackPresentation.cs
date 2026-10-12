using UnityEngine;

/// <summary>Representa las fases existentes sin controlar impactos ni tiempos de gameplay.</summary>
[DefaultExecutionOrder(100)]
public sealed class PrototypeAttackPresentation : MonoBehaviour
{
    [SerializeField] private PrototypePlayerAttack attack;
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Vector3 restPosition = new Vector3(0.55f, -0.2f, 0.15f);
    [SerializeField] private Vector3 raisedPosition = new Vector3(0.65f, 0.5f, 0.6f);
    [SerializeField] private Vector3 finishPosition = new Vector3(0.65f, 0.15f, 0.6f);
    [SerializeField] private Vector3 restAngles = new Vector3(-5f, 15f, 0f);
    [SerializeField] private Vector3 raisedAngles = new Vector3(-55f, 60f, 0f);
    [SerializeField] private Vector3 finishAngles = new Vector3(15f, -55f, 0f);

    public void Configure(PrototypePlayerAttack source, Transform pivot)
    {
        attack = source;
        weaponPivot = pivot;
        ApplyPose();
    }

    private void LateUpdate() => ApplyPose();

    private void ApplyPose()
    {
        if (attack == null || weaponPivot == null)
            return;
        float blend = Mathf.SmoothStep(0f, 1f, attack.PhaseProgress);
        switch (attack.Phase)
        {
            case PrototypePlayerAttack.AttackPhase.Windup:
                SetPose(restPosition, raisedPosition, restAngles, raisedAngles, blend);
                break;
            case PrototypePlayerAttack.AttackPhase.Active:
                SetPose(raisedPosition, finishPosition, raisedAngles, finishAngles, blend);
                break;
            case PrototypePlayerAttack.AttackPhase.Recovery:
                SetPose(finishPosition, restPosition, finishAngles, restAngles, blend);
                break;
            default:
                SetPose(restPosition, restPosition, restAngles, restAngles, 0f);
                break;
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
