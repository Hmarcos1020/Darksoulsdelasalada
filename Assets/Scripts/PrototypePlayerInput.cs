using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Comandos lógicos del prototipo; cada jugador habilita su propia copia del mapa.</summary>
[DefaultExecutionOrder(-300)]
public sealed class PrototypePlayerInput : MonoBehaviour
{
    [SerializeField] private InputActionAsset actions;
    private InputActionAsset runtimeActions;
    private InputActionMap map;
    private InputAction move;
    private InputAction attack;
    private InputAction dodge;
    private InputAction parry;
    private InputAction restart;
    private InputAction diagnostics;
    private InputAction slowMotion;
    private InputAction exportTrace;

    public Vector2 Move => move != null && isActiveAndEnabled ? Vector2.ClampMagnitude(move.ReadValue<Vector2>(), 1f) : Vector2.zero;
    public bool AttackPressed => isActiveAndEnabled && attack != null && attack.WasPressedThisFrame();
    public bool DodgePressed => isActiveAndEnabled && dodge != null && dodge.WasPressedThisFrame();
    public bool ParryPressed => isActiveAndEnabled && parry != null && parry.WasPressedThisFrame();
    public bool RestartPressed => isActiveAndEnabled && restart != null && restart.WasPressedThisFrame();

    public bool DiagnosticsPressed => isActiveAndEnabled && diagnostics != null && diagnostics.WasPressedThisFrame();
    public bool SlowMotionPressed => isActiveAndEnabled && slowMotion != null && slowMotion.WasPressedThisFrame();
    public bool ExportTracePressed => isActiveAndEnabled && exportTrace != null && exportTrace.WasPressedThisFrame();

    public void Configure(InputActionAsset asset) => actions = asset;

    private void Awake()
    {
        if (actions == null)
        {
            Debug.LogError("Asigná PrototypeCombat.inputactions al jugador.", this);
            enabled = false;
            return;
        }
        runtimeActions = Instantiate(actions);
        map = runtimeActions.FindActionMap("Prototype", true);
        move = map.FindAction("Move", true);
        attack = map.FindAction("Attack", true);
        dodge = map.FindAction("Dodge", true);
        parry = map.FindAction("Parry", true);
        restart = map.FindAction("Restart", true);
        diagnostics = map.FindAction("Diagnostics", true);
        slowMotion = map.FindAction("SlowMotion", true);
        exportTrace = map.FindAction("ExportTrace", true);
    }

    private void OnEnable() => map?.Enable();
    private void OnDisable() => map?.Disable();
    private void OnDestroy()
    {
        if (runtimeActions == null)
            return;
        runtimeActions.Disable();
        if (Application.isPlaying)
            Destroy(runtimeActions);
        else
            DestroyImmediate(runtimeActions);
    }
}
