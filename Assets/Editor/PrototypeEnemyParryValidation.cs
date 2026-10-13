using System;
using System.Reflection;
using UnityEngine;

/// <summary>Prueba defensa fija, recuperación del atacante y respuestas reales del jugador.</summary>
public static class PrototypeEnemyParryValidation
{
    private const double StartTime = 10d;
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Se requiere Play para validar el parry enemigo.");
        var defense = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyParry>();
        var enemyAttack = defense.GetComponent<PrototypeEnemyAttack>();
        var enemyHealth = defense.GetComponent<PrototypeHealth>();
        var playerAttack = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
        var playerHealth = playerAttack.GetComponent<PrototypeHealth>();
        var playerParry = playerAttack.GetComponent<PrototypePlayerParry>();
        var dodge = playerAttack.GetComponent<PrototypePlayerDodge>();
        var motor = playerAttack.GetComponent<PrototypePlayerMovement>();
        var stamina = playerAttack.GetComponent<PrototypeStamina>();
        var session = playerAttack.GetComponent<PrototypeDuelSession>();
        var observer = playerAttack.GetComponent<PrototypeCombatDiagnostics>();
        var controller = playerAttack.GetComponent<CharacterController>();
        var previousSimulation = Physics.simulationMode;
        bool previousVisible = observer.IsVisible;
        GameObject floor = null, wall = null, extraCollider = null;
        int successes = 0, parriedQueries = 0;
        Action success = () => successes++;
        Action<PrototypeMeleeQuerySample> sampled = sample =>
        {
            if (sample.Attacker != playerAttack.transform || sample.ParryCount == 0) return;
            Check(sample.HitCount == 0 && sample.AppliedDamage == 0, "Desvío aplica daño"); parriedQueries += sample.ParryCount;
        };
        Vector3 origin = new Vector3(600f, 1.08f, 600f);
        double clock = StartTime;
        void Reset()
        {
            session.Restart(); defense.enabled = true;
            controller.enabled = false; playerAttack.transform.SetPositionAndRotation(origin, Quaternion.identity); controller.enabled = true;
            enemyAttack.transform.SetPositionAndRotation(origin + Vector3.forward * 1.5f, Quaternion.Euler(0f, 180f, 0f));
            wall.SetActive(false); defense.DefensiveVolume.enabled = true;
            Physics.SyncTransforms(); Physics.Simulate(0.02f); clock = StartTime;
        }
        void StartDefense()
        {
            Invoke(enemyAttack, "AdvanceAt", 0f, clock);
            Check(defense.Phase == PrototypeParryPhase.Windup && enemyAttack.Phase == PrototypeAttackPhase.Idle, "No empieza turno defensivo exclusivo");
        }
        void Step(float dt)
        {
            clock += dt;
            Invoke(playerParry, "AdvanceTo", clock);
            Invoke(defense, "AdvanceTo", clock);
            Invoke(playerAttack, "AdvanceAt", dt, clock);
            Invoke(enemyAttack, "AdvanceAt", dt, clock);
            Invoke(defense, "FinalizeFeedback"); Invoke(playerParry, "FinalizeFeedback");
        }
        void Deflect()
        {
            StartDefense(); Step(0.12f); Check(playerAttack.TryAttack(), "No inicia golpe para desvío"); Step(0.20f);
            Check(defense.Result == PrototypeParryResult.Success && playerAttack.IsStaggered
                && enemyHealth.Health == 100 && playerHealth.Health == 100 && Near(stamina.Current, 80f), "Desvío real incorrecto");
        }
        void Run(float dt, double attackTime)
        {
            StartDefense(); bool started = false;
            while (clock < StartTime + 0.72d)
            {
                if (!started && clock + dt >= StartTime + attackTime)
                {
                    float before = Mathf.Max(0f, (float)(StartTime + attackTime - clock));
                    if (before > 0f) Step(before);
                    Check(playerAttack.TryAttack(), "No acepta ataque de prueba"); started = true;
                    Step(Mathf.Max(0f, dt - before));
                }
                else Step(dt);
            }
        }
        defense.Succeeded += success; PrototypeMeleeHitQuery.Completed += sampled;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Suelo temporal defensa enemiga"; floor.transform.position = new Vector3(600f, -0.5f, 600f); floor.transform.localScale = new Vector3(20f, 1f, 20f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = origin + Vector3.forward * 0.5f; wall.transform.localScale = new Vector3(2f, 3f, 0.1f);
            extraCollider = new GameObject("Collider enemigo adicional de prueba"); extraCollider.transform.SetParent(enemyAttack.transform, false); extraCollider.AddComponent<CapsuleCollider>().height = 1.8f;
            observer.SetVisible(true);
            Check(defense.GetComponent<PrototypePlayerParry>() == null, "Feedback inyectó defensa de jugador en el enemigo");

            foreach (float dt in new[] { 1f / 30f, 1f / 60f, 1f / 120f })
            {
                foreach (double attackTime in new[] { 0d, 0.12d, 0.50d })
                {
                    Reset(); int before = successes; Run(dt, attackTime);
                    bool expected = attackTime == 0.12d;
                    Check(enemyHealth.Health == (expected ? 100 : 75) && successes == before + (expected ? 1 : 0)
                        && playerAttack.IsStaggered == expected && playerHealth.Health == 100 && Near(stamina.Current, 80f),
                        "Ventana enemiga depende de FPS: dt=" + dt + ", inicio=" + attackTime);
                    Debug.Log("ENEMY_PARRY_TIMING_SAMPLE: dt=" + dt + "; inicio ataque=" + attackTime + "; vida enemiga=" + enemyHealth.Health + "; desvío=" + expected);
                }
            }
            Reset(); Deflect(); Check(observer.TraceSnapshot.Contains("PARRY ENEMIGO exitoso") && observer.TraceSnapshot.Contains("parrys=1"), "Falta diagnóstico del desvío");
            Check(!playerAttack.TryAttack() && !dodge.TryDodge(Vector2.right) && !playerParry.TryParry() && Near(stamina.Current, 80f), "Recuperación permite acciones o vuelve a cobrar");
            Vector3 atImpact = playerAttack.transform.position;
            Invoke(motor, "AdvanceMovement", Vector2.right, 0.05f);
            Check(Vector3.ProjectOnPlane(playerAttack.transform.position - atImpact, Vector3.up).magnitude < 0.001f
                && playerAttack.transform.position.y < atImpact.y, "Recuperación permite caminar o elimina gravedad");
            Invoke(stamina, "Advance", 0.4f); Check(Near(stamina.Current, 80f), "Regenera stamina durante desvío");
            Step(0.59f); Check(!playerAttack.IsStaggered && enemyAttack.Phase == PrototypeAttackPhase.Windup, "No libera al jugador antes del golpe anunciado");
            Check(dodge.TryDodge(Vector2.right), "No permite esquivar después de recuperar");
            for (int i = 0; i < 20; i++)
            {
                Step(1f / 60f); Invoke(motor, "AdvanceMovement", Vector2.zero, 1f / 60f); Physics.SyncTransforms(); Physics.Simulate(0.02f);
            }
            Check(playerHealth.Health == 100 && Near(stamina.Current, 50f), "Respuesta anunciada no permite evasión real");

            Reset(); Deflect(); Step(0.66f);
            Check((bool)Invoke(playerParry, "TryParryAt", clock), "No acepta parry de respuesta");
            for (int i = 0; i < 9; i++) Step(1f / 60f);
            Check(playerParry.LastSucceeded && enemyAttack.IsExposed && playerHealth.Health == 100 && Near(stamina.Current, 65f), "No se puede desviar el ataque de respuesta");
            Step(0.31f); Check(playerAttack.TryAttack(), "No permite contraatacar tras parry propio");
            Step(0.18f); Check(enemyHealth.Health == 75 && Near(stamina.Current, 45f), "Enemigo desvía mientras está expuesto o cambia daño/costo");

            Reset(); StartDefense(); Step(0.12f); wall.SetActive(true); Physics.SyncTransforms(); Check(playerAttack.TryAttack(), "No prepara pared"); Step(0.20f);
            Check(enemyHealth.Health == 100 && !playerAttack.IsStaggered && defense.Result != PrototypeParryResult.Success, "Parry enemigo atraviesa pared");
            Reset(); StartDefense(); Step(0.12f);
            controller.enabled = false; playerAttack.transform.SetPositionAndRotation(origin + Vector3.forward * 3f, Quaternion.Euler(0f, 180f, 0f)); controller.enabled = true;
            Physics.SyncTransforms(); Physics.Simulate(0.02f); Check(playerAttack.TryAttack(), "No prepara espalda"); Step(0.20f);
            Check(enemyHealth.Health == 75 && !playerAttack.IsStaggered, "Defiende la espalda");
            Reset(); StartDefense(); Step(0.12f); defense.DefensiveVolume.enabled = false;
            Check(playerAttack.TryAttack(), "No prepara volumen deshabilitado"); Step(0.20f);
            Check(enemyHealth.Health == 75 && !playerAttack.IsStaggered, "Desvía sin contacto defensivo"); defense.DefensiveVolume.enabled = true;
            Reset(); StartDefense(); Step(0.12f); enemyAttack.transform.position = origin + Vector3.forward * 2.55f;
            Physics.SyncTransforms(); Physics.Simulate(0.02f); Check(playerAttack.TryAttack(), "No prepara defensa sin cuerpo"); Step(0.20f);
            Check(enemyHealth.Health == 100 && playerAttack.IsStaggered, "Defensa exige también alcanzar la cápsula");

            Reset(); StartDefense(); Step(0.12f); Check(playerAttack.TryAttack(), "No prepara hitch"); Step(0.50f);
            Check(enemyHealth.Health == 100 && playerAttack.IsStaggered && enemyAttack.Phase == PrototypeAttackPhase.Windup, "Hitch pierde contacto válido o consume recuperación incorrecta");
            Reset(); StartDefense(); Step(0.12f); Check(playerAttack.TryAttack(), "No prepara hitch completo"); Step(1.4f);
            Check(enemyHealth.Health == 100 && !playerAttack.IsStaggered && playerAttack.Phase == PrototypeAttackPhase.Idle, "Ataque cancelado reaparece después de hitch completo");

            Reset(); StartDefense(); Step(0.46f);
            Check(defense.Phase == PrototypeParryPhase.Recovery && enemyAttack.Phase == PrototypeAttackPhase.Idle, "Defensa y ataque se solapan");
            Step(0.56f); Check(enemyAttack.Phase == PrototypeAttackPhase.Windup && !defense.IsBusy, "No alterna a ataque normal");
            Check(!defense.TryBeginAt(clock), "Defiende durante ataque"); Step(2.1f); Step(0.01f);
            Check(defense.Phase == PrototypeParryPhase.Windup && enemyAttack.Phase == PrototypeAttackPhase.Idle, "No repite secuencia fija");

            Reset(); Deflect(); session.Restart();
            Check(!playerAttack.IsStaggered && !defense.IsBusy && !defense.IsShowingSuccess && playerHealth.Health == 100
                && enemyHealth.Health == 100 && Near(stamina.Current, 100f), "R conserva desvío o respuesta pendiente");
            Reset(); StartDefense(); enemyHealth.TakeDamage(100); Invoke(session, "Update");
            Check(!defense.IsBusy && !playerAttack.IsStaggered && session.HasEnded, "Victoria conserva defensa");
            Reset(); Deflect(); playerHealth.TakeDamage(100); Invoke(session, "Update");
            Check(!defense.IsBusy && !playerAttack.IsStaggered && session.HasEnded, "Derrota conserva desvío");
            Reset(); StartDefense(); defense.enabled = false; Check(!defense.IsBusy && defense.Result == PrototypeParryResult.None, "Deshabilitar conserva defensa"); defense.enabled = true;
            Check(successes == parriedQueries, "Eventos duplicados por colliders o suscripciones");
            Debug.Log("ENEMY_PARRY_VALIDATION_SUCCEEDED: ventana/contacto 30/60/120 FPS, temprano/tardío/espalda/pared, defensa sin cuerpo, desvío sin daño/refund, bloqueo/gravedad, respuesta evitable y parry de respuesta, recuperación/vulnerabilidad, alternancia/R/muerte y hitches sin golpe diferido.");
        }
        finally
        {
            defense.Succeeded -= success; PrototypeMeleeHitQuery.Completed -= sampled;
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            if (extraCollider != null) UnityEngine.Object.DestroyImmediate(extraCollider);
            defense.enabled = true; defense.DefensiveVolume.enabled = true;
            Physics.simulationMode = previousSimulation; session.Restart(); observer.SetVisible(previousVisible);
        }
    }
    public static void Capture()
    {
        const string scenePath = "Assets/Scenes/PrototypeArena.unity";
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
        var previousSimulation = Physics.simulationMode;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            var defense = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyParry>();
            var enemy = defense.GetComponent<PrototypeEnemyAttack>();
            var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerAttack>();
            player.GetComponent<PrototypeDuelSession>().Restart();
            var controller = player.GetComponent<CharacterController>(); controller.enabled = false;
            player.transform.SetPositionAndRotation(enemy.transform.position + Vector3.back * 1.4f + Vector3.up * 0.08f, Quaternion.identity); controller.enabled = true;
            player.GetComponent<PrototypeStamina>().ResetStamina();
            Physics.SyncTransforms(); Physics.Simulate(0.02f);
            Camera camera = Camera.main; camera.GetComponent<PrototypeFollowCamera>().Configure(player.transform);
            Invoke(enemy, "AdvanceAt", 0f, StartTime);
            Invoke(defense, "AdvanceTo", StartTime + 0.12d); Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "01-AvisoDefensa", "Logs/EnemyParry");
            Check(player.TryAttack(), "No prepara golpe de captura");
            Invoke(defense, "AdvanceTo", StartTime + 0.26d); Invoke(player, "AdvanceAt", 0.14f, StartTime + 0.26d);
            Invoke(player.GetComponent<PrototypeAttackPresentation>(), "ApplyPose"); Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "02-DefensaActiva", "Logs/EnemyParry");
            Invoke(defense, "AdvanceTo", StartTime + 0.301d); Invoke(player, "AdvanceAt", 0.041f, StartTime + 0.301d);
            Invoke(enemy, "AdvanceAt", 0.041f, StartTime + 0.301d);
            Invoke(player.GetComponent<PrototypeAttackPresentation>(), "ApplyPose"); Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            Check(player.IsStaggered && defense.Result == PrototypeParryResult.Success && defense.GetComponent<PrototypeHealth>().Health == 100, "Captura sin desvío real");
            PrototypeCombatVisualValidation.Capture(camera, "03-GolpeDesviado", "Logs/EnemyParry");
            Invoke(defense, "AdvanceTo", StartTime + 0.451d); Invoke(player, "AdvanceAt", 0.15f, StartTime + 0.451d); Invoke(enemy, "AdvanceAt", 0.15f, StartTime + 0.451d);
            Invoke(player.GetComponent<PrototypeAttackPresentation>(), "ApplyPose"); Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "04-RespuestaAnunciada", "Logs/EnemyParry");
            Debug.Log("ENEMY_PARRY_CAPTURE_SUCCEEDED: preparación, defensa cian, desvío sin daño enemigo y respuesta anunciada; capturas sin HUD OnGUI ni validación auditiva humana.");
        }
        finally { Physics.simulationMode = previousSimulation; UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath); }
    }
    private static bool Near(float value, float expected) => Mathf.Abs(value - expected) < 0.001f;
    private static object Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("ENEMY_PARRY_VALIDATION_FAILED: " + message);
    }
}
