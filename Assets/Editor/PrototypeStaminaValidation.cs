using System;
using System.Reflection;
using UnityEngine;

/// <summary>Recurso real de la arena, gates de acciones y regeneración en segundos.</summary>
public static class PrototypeStaminaValidation
{
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Stamina requiere validación en Play.");
        var stamina = UnityEngine.Object.FindFirstObjectByType<PrototypeStamina>();
        var attack = stamina.GetComponent<PrototypePlayerAttack>();
        var dodge = stamina.GetComponent<PrototypePlayerDodge>();
        var parry = stamina.GetComponent<PrototypePlayerParry>();
        var movement = stamina.GetComponent<PrototypePlayerMovement>();
        var health = stamina.GetComponent<PrototypeHealth>();
        var session = stamina.GetComponent<PrototypeDuelSession>();
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        var observer = stamina.GetComponent<PrototypeCombatDiagnostics>();
        bool previousVisible = observer.IsVisible;
        GameObject floor = null;
        int spends = 0, refusals = 0;
        Action<PrototypeStaminaAction, float, float> spent = (action, cost, remaining) => spends++;
        Action<PrototypeStaminaAction, float, float> refused = (action, cost, remaining) => refusals++;
        void Reset() { stamina.enabled = true; session.Restart(); Check(Near(stamina.Current, 100f), "R no restaura 100"); }
        void Advance(float dt) => Invoke(stamina, "Advance", dt);
        void Exhaust()
        {
            for (int i = 0; i < 3; i++) Check(stamina.TrySpend(PrototypeStaminaAction.Dodge), "No paga gasto de preparación");
            Check(Near(stamina.Current, 10f), "Tres gastos de 30 no dejan 10");
        }
        stamina.Spent += spent; stamina.Refused += refused;
        try
        {
            observer.SetVisible(true);
            Reset(); int before = spends;
            Check(attack.TryAttack() && Near(stamina.Current, 80f), "Ataque no cuesta 20 al iniciar");
            Check(!attack.TryAttack() && !dodge.TryDodge(Vector2.right) && !parry.TryParry()
                && Near(stamina.Current, 80f) && spends == before + 1, "Acción ocupada cobra otra vez");
            Advance(2f); Check(Near(stamina.Current, 80f), "Regenera durante ataque");
            attack.CancelAttack(); Check(Near(stamina.Current, 80f), "Cancelar devuelve costo");
            Advance(0.79f); Check(Near(stamina.Current, 80f), "No respeta pausa tras acción");
            Advance(0.02f); Check(Near(stamina.Current, 80.25f), "Cruce de pausa no regenera solo sobrante");

            Reset(); Check(dodge.TryDodge(Vector2.zero) && Near(stamina.Current, 70f), "Esquiva no cuesta 30");
            Check(!attack.TryAttack() && !parry.TryParry() && Near(stamina.Current, 70f), "Esquiva cobra intentos bloqueados");
            Advance(1f); Check(Near(stamina.Current, 70f), "Regenera durante esquiva");
            dodge.CancelDodge();
            Reset(); Check(parry.TryParry() && Near(stamina.Current, 85f), "Parry no cuesta 15");
            Check(!attack.TryAttack() && !dodge.TryDodge(Vector2.right) && !parry.TryParry() && Near(stamina.Current, 85f), "Parry cobra durante recuperación");
            Advance(1f); Check(Near(stamina.Current, 85f), "Regenera durante parry");
            parry.CancelParry();

            Reset(); Exhaust(); int rejected = refusals;
            float wait = stamina.RegenerationDelayRemaining;
            Quaternion rotation = stamina.transform.rotation;
            Check(!attack.TryAttack() && !dodge.TryDodge(Vector2.right) && !parry.TryParry(), "Acción inicia sin energía suficiente");
            Check(Near(stamina.Current, 10f) && Near(stamina.RegenerationDelayRemaining, wait)
                && !attack.IsAttacking && !dodge.IsBusy && !parry.IsBusy
                && Quaternion.Angle(rotation, stamina.transform.rotation) < 0.1f && refusals == rejected + 3,
                "Rechazo cobra, gira, inicia fase o reinicia pausa");
            Check(observer.TraceSnapshot.Contains("STAMINA insuficiente") && stamina.RefusalMessage.Contains("parry"), "Falta feedback de agotamiento");
            Advance(0.3f); float partialWait = stamina.RegenerationDelayRemaining;
            Check(!stamina.TrySpend(PrototypeStaminaAction.Dodge) && Near(stamina.RegenerationDelayRemaining, partialWait), "Repetir rechazo retrasa regeneración");
            Advance(1f); Check(Near(stamina.Current, 22.5f), "Espera parcial y sobrante incorrectos");

            foreach (float dt in new[] { 1f / 30f, 1f / 60f, 1f / 120f })
            {
                Reset(); Exhaust();
                int steps = Mathf.RoundToInt(2.4f / dt);
                for (int i = 0; i < steps; i++) Advance(dt);
                Check(Near(stamina.Current, 50f), "Tasa depende del FPS: dt=" + dt + ", energía=" + stamina.Current);
                Debug.Log("STAMINA_RATE_SAMPLE: dt=" + dt + "; tiempo=2,4; energía=" + stamina.Current);
            }
            Reset(); Exhaust(); Advance(2.4f); Check(Near(stamina.Current, 50f), "Frame largo pierde sobrante de regeneración");
            Advance(-1f); Check(Near(stamina.Current, 50f), "Delta negativo aumenta o reduce recurso");
            Advance(10f); Check(Near(stamina.Current, 100f) && stamina.RefusalMessage == "", "Supera máximo o conserva rechazo ya asequible");

            Reset(); for (int i = 0; i < 4; i++) Check(stamina.TrySpend(PrototypeStaminaAction.Attack), "No prepara umbral exacto");
            Check(attack.TryAttack() && Near(stamina.Current, 0f), "No acepta costo exactamente disponible");
            attack.CancelAttack(); Check(!attack.TryAttack() && Near(stamina.Current, 0f), "Permite saldo negativo");

            Reset(); Exhaust();
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Suelo temporal stamina";
            floor.transform.position = new Vector3(500f, -0.5f, 500f); floor.transform.localScale = new Vector3(20f, 1f, 20f);
            var controller = stamina.GetComponent<CharacterController>();
            controller.enabled = false; stamina.transform.position = new Vector3(500f, 1.08f, 500f); controller.enabled = true;
            Physics.SyncTransforms(); Vector3 start = stamina.transform.position;
            Invoke(movement, "AdvanceMovement", Vector2.right, 0.05f);
            Check(Vector3.ProjectOnPlane(stamina.transform.position - start, Vector3.up).magnitude > 0.1f && Near(stamina.Current, 10f), "Agotamiento bloquea caminar o caminar consume");

            Reset(); Check(stamina.TrySpend(PrototypeStaminaAction.Attack), "No prepara derrota");
            health.TakeDamage(100); Advance(5f);
            Check(Near(stamina.Current, 80f) && !stamina.TrySpend(PrototypeStaminaAction.Dodge), "Derrota regenera o permite pagar acciones");
            Reset(); Check(stamina.TrySpend(PrototypeStaminaAction.Attack), "No prepara victoria");
            enemy.GetComponent<PrototypeHealth>().TakeDamage(100); Advance(5f);
            Check(Near(stamina.Current, 80f) && !stamina.TrySpend(PrototypeStaminaAction.Parry), "Victoria regenera o permite pagar acciones");
            Reset(); Check(stamina.TrySpend(PrototypeStaminaAction.Attack), "No prepara deshabilitación");
            stamina.enabled = false; Advance(5f);
            Check(!attack.TryAttack() && Near(stamina.Current, 80f), "Deshabilitar recurso elimina gate");
            stamina.enabled = true; Check(Near(stamina.Current, 80f), "Rehabilitar recarga gratis");
            health.ResetHealth(); Check(Near(stamina.Current, 100f) && stamina.RefusalMessage == "", "Reset de vida no restablece recurso/mensaje");
            Debug.Log("STAMINA_VALIDATION_SUCCEEDED: costos únicos, gates sin cola/cobro, no refund, pausas/recuperación, tasa 30/60/120 FPS y frame largo, umbral exacto, caminar, derrota/victoria/R y feedback.");
        }
        finally
        {
            stamina.Spent -= spent; stamina.Refused -= refused;
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            stamina.enabled = true; session.Restart(); observer.SetVisible(previousVisible);
        }
    }
    private static bool Near(float value, float expected) => Mathf.Abs(value - expected) < 0.001f;
    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition)
    {
        if (!condition) throw new InvalidOperationException("STAMINA_VALIDATION_FAILED");
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("STAMINA_VALIDATION_FAILED: " + message);
    }
}
