using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Observa combate y poses; no mueve actores ni decide daño.</summary>
[DefaultExecutionOrder(200)]
public sealed class PrototypeCombatDiagnostics : MonoBehaviour
{
    [SerializeField] private PrototypeEnemyAttack enemy;
    [SerializeField] private Transform enemyWeapon;
    [SerializeField] private Material wireMaterial;
    private PrototypePlayerInput input;
    private PrototypePlayerDodge dodge;
    private PrototypePlayerParry parry;
    private PrototypeStamina stamina;
    private PrototypeEnemyParry enemyDefense;
    private PrototypePlayerAttack attack;
    private PrototypeHealth health;
    private readonly Queue<string> records = new Queue<string>(128);
    private MaterialPropertyBlock colorProperties;
    private LineRenderer liveVolume;
    private LineRenderer impactVolume;
    private LineRenderer damagePosition;
    private PrototypeMeleeQuerySample lastHit;
    private Vector3 positionAtDamage;
    public Vector3 LastDamagePosition => positionAtDamage;
    public Vector3 LastDamageEndPosition { get; private set; }
    private int damagedFrame = -1;
    private int observedFrame = -1;
    private bool hasHit;
    private bool hitFinalized;
    private float previousTimeScale;
    private float previousFixedStep;
    private GUIStyle textStyle;
    private static readonly int[] CubePath = { 0, 1, 3, 2, 0, 4, 5, 1, 5, 7, 3, 7, 6, 2, 6, 4, 0 };

    public bool IsVisible { get; private set; }
    public bool SlowMotion { get; private set; }
    public int RecordedCount => records.Count;
    public string TraceSnapshot => string.Join("\n", records.ToArray());
    public string LastImpact { get; private set; } = "Todavía no se registró daño enemigo.";
    public string LastEvent { get; private set; } = "Activá F3 antes de reproducir el intento.";
    public string LastExportPath { get; private set; } = "";

    public void Configure(PrototypeEnemyAttack opponent, Transform weapon, Material material)
    {
        enemy = opponent;
        enemyWeapon = weapon;
        wireMaterial = material;
    }

    private void Awake()
    {
        input = GetComponent<PrototypePlayerInput>();
        dodge = GetComponent<PrototypePlayerDodge>();
        parry = GetComponent<PrototypePlayerParry>();
        stamina = GetComponent<PrototypeStamina>();
        enemyDefense = enemy != null ? enemy.GetComponent<PrototypeEnemyParry>() : null;
        attack = GetComponent<PrototypePlayerAttack>();
        health = GetComponent<PrototypeHealth>();
    }

    private void OnEnable()
    {
        if (dodge != null) dodge.Attempted += OnDodgeAttempt;
        if (parry != null) { parry.Attempted += OnParryAttempt; parry.Succeeded += OnParrySuccess; parry.Failed += OnParryFailure; }
        if (stamina != null) { stamina.Spent += OnStaminaSpent; stamina.Refused += OnStaminaRefused; }
        if (enemyDefense != null) { enemyDefense.Started += OnEnemyDefenseStarted; enemyDefense.Succeeded += OnEnemyParrySuccess; }
        if (health != null) { health.Damaged += OnDamage; health.ResetCompleted += ClearRecords; }
        PrototypeMeleeHitQuery.Completed += OnQuery;
    }

    private void OnDisable()
    {
        if (dodge != null) dodge.Attempted -= OnDodgeAttempt;
        if (parry != null) { parry.Attempted -= OnParryAttempt; parry.Succeeded -= OnParrySuccess; parry.Failed -= OnParryFailure; }
        if (stamina != null) { stamina.Spent -= OnStaminaSpent; stamina.Refused -= OnStaminaRefused; }
        if (enemyDefense != null) { enemyDefense.Started -= OnEnemyDefenseStarted; enemyDefense.Succeeded -= OnEnemyParrySuccess; }
        if (health != null) { health.Damaged -= OnDamage; health.ResetCompleted -= ClearRecords; }
        PrototypeMeleeHitQuery.Completed -= OnQuery;
        SetSlowMotion(false);
        SetVisible(false);
    }

    private void Update()
    {
        if (input == null) return;
        if (input.DiagnosticsPressed) SetVisible(!IsVisible);
        if (input.SlowMotionPressed) SetSlowMotion(!SlowMotion);
        if (input.ExportTracePressed && IsVisible)
        {
            try
            {
                LastExportPath = ExportTrace(Path.Combine(Application.persistentDataPath, "CombatDiagnostics"));
                Debug.Log("COMBAT_TRACE_EXPORTED: " + LastExportPath);
            }
            catch (Exception exception) when (exception is IOException || exception is UnauthorizedAccessException)
            {
                LastEvent = "No se pudo guardar el registro: " + exception.Message;
                Debug.LogWarning(LastEvent, this);
            }
        }
    }

    public void SetVisible(bool visible)
    {
        IsVisible = visible;
        if (visible && liveVolume == null)
        {
            liveVolume = CreateLine("Volumen vivo de diagnóstico", 0.035f);
            impactVolume = CreateLine("Volumen al recibir daño", 0.022f);
            damagePosition = CreateLine("Posición al recibir daño", 0.03f);
        }
        if (!visible)
        {
            if (liveVolume != null) liveVolume.enabled = false;
            if (impactVolume != null) impactVolume.enabled = false;
            if (damagePosition != null) damagePosition.enabled = false;
        }
    }

    public void SetSlowMotion(bool slow)
    {
        if (slow == SlowMotion) return;
        if (slow)
        {
            previousTimeScale = Time.timeScale;
            previousFixedStep = Time.fixedDeltaTime;
            Time.timeScale = previousTimeScale * 0.2f;
            Time.fixedDeltaTime = previousFixedStep * 0.2f;
        }
        else
        {
            Time.timeScale = previousTimeScale;
            Time.fixedDeltaTime = previousFixedStep;
        }
        SlowMotion = slow;
    }

    public void ClearRecords()
    {
        records.Clear();
        damagedFrame = observedFrame = -1;
        hasHit = hitFinalized = false;
        LastImpact = "Todavía no se registró daño enemigo.";
        LastEvent = "Registro limpio; F3 diagnóstico, F4 lento, F5 guardar.";
        if (impactVolume != null) impactVolume.enabled = false;
        if (damagePosition != null) damagePosition.enabled = false;
    }

    private void OnDodgeAttempt(bool accepted, Vector3 direction)
        => Record("ESQUIVA " + (accepted ? "aceptada" : "rechazada") + "; p=" + Position(transform.position)
            + "; dirección=" + Position(direction) + "; ataque=" + attack.Phase + "; esquiva=" + dodge.Phase);

    private void OnParryAttempt(bool accepted) => Record("PARRY " + (accepted ? "aceptado" : "rechazado") + "; fase=" + parry.Phase
        + "; p=" + Position(transform.position) + "; frente=" + Position(transform.forward));
    private void OnParrySuccess() => Record("PARRY exitoso; daño evitado; enemigo expuesto; p=" + Position(transform.position)
        + "; contacto t=" + parry.LastContactTime.ToString("F6", CultureInfo.InvariantCulture)
        + "; anticipación ms=" + (parry.PressLeadSeconds * 1000d).ToString("F2", CultureInfo.InvariantCulture));
    private void OnParryFailure() => Record("PARRY fallido; " + parry.FailureReason);
    private void OnEnemyDefenseStarted() => Record("DEFENSA ENEMIGA iniciada; patrón fijo; p=" + Position(enemy.transform.position));
    private void OnEnemyParrySuccess() => Record("PARRY ENEMIGO exitoso; ataque jugador desviado; contacto t="
        + enemyDefense.ContactTime.ToString("F6", CultureInfo.InvariantCulture) + "; stamina=" + (stamina != null ? Number(stamina.Current) : "sin recurso"));
    private void OnStaminaSpent(PrototypeStaminaAction action, float cost, float remaining)
        => Record("STAMINA gasto=" + Number(cost) + "; acción=" + action + "; restante=" + Number(remaining));
    private void OnStaminaRefused(PrototypeStaminaAction action, float cost, float remaining)
        => Record("STAMINA insuficiente; acción=" + action + "; costo=" + Number(cost) + "; restante=" + Number(remaining));

    private void OnDamage(int amount)
    {
        if (!IsVisible) return;
        damagedFrame = Time.frameCount;
        positionAtDamage = transform.position;
        Record("DAÑO recibido=" + amount + "; vida=" + health.Health + "; p=" + Position(positionAtDamage)
            + "; esquiva=" + dodge.Phase + "; parry=" + (parry != null ? parry.Phase.ToString() : "sin configurar") + "; enemigo=" + enemy.Phase);
    }

    private void OnQuery(PrototypeMeleeQuerySample sample)
    {
        if (!IsVisible || (sample.Attacker != transform && (enemy == null || sample.Attacker != enemy.transform))) return;
        bool enemyQuery = enemy != null && sample.Attacker == enemy.transform;
        Record("CONSULTA " + (enemyQuery ? "enemigo" : "jugador") + "; impactos=" + sample.HitCount
            + "; daño=" + sample.AppliedDamage + "; parrys=" + sample.ParryCount + "; p=" + Position(transform.position)
            + "; centro=" + Position(sample.Center) + "; fase=" + (enemyQuery ? enemy.Phase : attack.Phase)
            + "; progreso=" + Number(enemyQuery ? enemy.PhaseProgress : attack.PhaseProgress)
            + "; enemigo expuesto=" + (enemy != null && enemy.IsExposed)
            + "; jugador desviado=" + attack.IsStaggered);
        if (enemyQuery) Record("TIEMPO contacto=" + sample.ContactTime.ToString("F6", CultureInfo.InvariantCulture));
        if (enemyQuery && sample.AppliedDamage > 0 && damagedFrame == Time.frameCount)
        {
            lastHit = sample;
            hasHit = true;
            hitFinalized = false;
            LastImpact = "Daño en frame " + damagedFrame + "; posición consultada=" + Position(positionAtDamage);
        }
    }

    private void LateUpdate()
    {
        if (!IsVisible) return;
        if (observedFrame == Time.frameCount)
        {
            Record("CIERRE frame; p=" + Position(transform.position) + "; esquiva=" + dodge.Phase
                + "; enemigo=" + enemy.Phase + "; progreso=" + Number(enemy.PhaseProgress)
                + "; espada local=" + (enemyWeapon != null ? Position(enemyWeapon.localEulerAngles) : "sin referencia"));
            observedFrame = -1;
        }
        if (hasHit && !hitFinalized && damagedFrame == Time.frameCount)
        {
            LastDamageEndPosition = transform.position;
            LastImpact += "\nPosición al cerrar frame=" + Position(transform.position)
                + "; espada=" + enemy.Phase + " " + Number(enemy.PhaseProgress);
            hitFinalized = true;
        }
        DrawVolumes();
    }

    private void DrawVolumes()
    {
        liveVolume.enabled = enemy != null && enemy.Phase != PrototypeAttackPhase.Idle;
        if (liveVolume.enabled)
            DrawBox(liveVolume, enemy.HitboxCenter, enemy.HitboxHalfExtents, enemy.transform.rotation,
                enemy.Phase == PrototypeAttackPhase.Active ? Color.red : enemy.Phase == PrototypeAttackPhase.Windup ? Color.yellow : Color.cyan);
        impactVolume.enabled = damagePosition.enabled = hasHit;
        if (!hasHit) return;
        DrawBox(impactVolume, lastHit.Center, lastHit.HalfExtents, lastHit.Rotation, new Color(1f, 0.45f, 0f));
        damagePosition.positionCount = 8;
        Vector3[] cross = { positionAtDamage + Vector3.left * 0.4f, positionAtDamage + Vector3.right * 0.4f, positionAtDamage,
            positionAtDamage + Vector3.back * 0.4f, positionAtDamage + Vector3.forward * 0.4f, positionAtDamage,
            positionAtDamage + Vector3.up, positionAtDamage + Vector3.down };
        damagePosition.SetPositions(cross);
        SetColor(damagePosition, Color.white);
    }

    private LineRenderer CreateLine(string label, float width)
    {
        var view = new GameObject(label);
        view.transform.SetParent(transform, false);
        var line = view.AddComponent<LineRenderer>();
        line.sharedMaterial = wireMaterial;
        line.useWorldSpace = true;
        line.startWidth = line.endWidth = width;
        line.shadowCastingMode = ShadowCastingMode.Off;
        line.receiveShadows = false;
        return line;
    }

    private void DrawBox(LineRenderer line, Vector3 center, Vector3 half, Quaternion rotation, Color color)
    {
        line.positionCount = CubePath.Length;
        for (int i = 0; i < CubePath.Length; i++)
        {
            int corner = CubePath[i];
            Vector3 point = new Vector3((corner & 1) == 0 ? -half.x : half.x,
                (corner & 2) == 0 ? -half.y : half.y, (corner & 4) == 0 ? -half.z : half.z);
            line.SetPosition(i, center + rotation * point);
        }
        SetColor(line, color);
    }
    private void SetColor(LineRenderer line, Color color)
    {
        if (colorProperties == null) colorProperties = new MaterialPropertyBlock();
        colorProperties.SetColor("_BaseColor", color);
        line.SetPropertyBlock(colorProperties);
    }

    private void Record(string detail)
    {
        if (!IsVisible) return;
        if (records.Count == 128) records.Dequeue();
        LastEvent = "f=" + Time.frameCount + " t=" + Time.timeAsDouble.ToString("F4", CultureInfo.InvariantCulture)
            + " real=" + Time.unscaledTimeAsDouble.ToString("F4", CultureInfo.InvariantCulture) + " | " + detail;
        records.Enqueue(LastEvent);
        observedFrame = Time.frameCount;
    }
    private static string Number(float value) => value.ToString("F3", CultureInfo.InvariantCulture);
    private static string Position(Vector3 value) => "(" + Number(value.x) + "," + Number(value.y) + "," + Number(value.z) + ")";

    public string ExportTrace(string directory)
    {
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, "combate-" + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fff", CultureInfo.InvariantCulture)
            + "-" + Guid.NewGuid().ToString("N").Substring(0, 6) + ".log");
        using (var stream = new FileStream(path, FileMode.CreateNew, FileAccess.Write))
        using (var writer = new StreamWriter(stream, new UTF8Encoding(false)))
        {
            writer.WriteLine("Diagnóstico de combate · Unity " + Application.unityVersion);
            writer.WriteLine("Capturas de eventos en Update y cierre en LateUpdate; la espada no gobierna daño.");
            writer.WriteLine(TraceSnapshot);
            writer.WriteLine("Último impacto:\n" + LastImpact);
        }
        return path;
    }

    private void OnGUI()
    {
        if (!IsVisible) return;
        if (textStyle == null) textStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
        GUI.Box(new Rect(12f, 276f, 710f, 212f), "Diagnóstico · F3 mostrar · F4 lento · F5 guardar registro");
        GUI.Label(new Rect(24f, 302f, 685f, 22f), "Velocidad: " + (SlowMotion ? "0,2x" : "normal")
            + " · Amarillo: preparación · Rojo: activo · Celeste: recuperación", textStyle);
        GUI.Label(new Rect(24f, 326f, 685f, 24f), "Naranja: volumen del último daño · Cruz blanca: posición al recibirlo · R limpia", textStyle);
        GUI.Label(new Rect(24f, 352f, 685f, 51f), LastImpact, textStyle);
        GUI.Label(new Rect(24f, 406f, 685f, 55f), LastEvent, textStyle);
        GUI.Label(new Rect(24f, 464f, 685f, 20f), string.IsNullOrEmpty(LastExportPath) ? "Registro circular: últimos 128 eventos." : "Guardado: " + Path.GetFileName(LastExportPath), textStyle);
    }
}
