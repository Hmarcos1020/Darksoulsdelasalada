using UnityEngine;

/// <summary>Seguimiento independiente de la rotación del jugador.</summary>
public sealed class PrototypeFollowCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField, Min(0.1f)] private float distance = 7f;
    [SerializeField, Min(0.1f)] private float height = 6f;
    [SerializeField, Min(0.01f)] private float smoothTime = 0.15f;

    private Vector3 velocity;

    public void Configure(Transform followTarget)
    {
        target = followTarget;
        SnapToTarget();
    }

    private void Start()
    {
        if (target == null)
        {
            Debug.LogError("Asigná el jugador a PrototypeFollowCamera.", this);
            enabled = false;
            return;
        }
        SnapToTarget();
    }

    private void SnapToTarget()
    {
        if (target == null)
            return;
        transform.position = target.position + new Vector3(0f, height, -distance);
        transform.rotation = Quaternion.LookRotation(new Vector3(0f, -height, distance), Vector3.up);
        velocity = Vector3.zero;
    }

    private void LateUpdate()
    {
        Vector3 desiredPosition = target.position + new Vector3(0f, height, -distance);
        transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, smoothTime);
        // La inclinación depende del offset configurado, nunca de la rotación del jugador.
        transform.rotation = Quaternion.LookRotation(new Vector3(0f, -height, distance), Vector3.up);
    }
}
