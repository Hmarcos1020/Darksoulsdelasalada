using System;
using System.Reflection;
using UnityEngine;

/// <summary>Recepción y timing sobre la arena real, con un reloj compartido explícito.</summary>
public static class PrototypeParryValidation
{
    public static void Validate()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Se requiere Play para validar parry.");
        var parry = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerParry>();
        var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
        var player = parry.GetComponent<PrototypeHealth>();
        var enemyHealth = enemy.GetComponent<PrototypeHealth>();
        var attack = parry.GetComponent<PrototypePlayerAttack>();
        var dodge = parry.GetComponent<PrototypePlayerDodge>();
        var movement = parry.GetComponent<PrototypePlayerMovement>();
        var session = parry.GetComponent<PrototypeDuelSession>();
        var controller = parry.GetComponent<CharacterController>();
        var observer = parry.GetComponent<PrototypeCombatDiagnostics>();
        bool previousVisible = observer.IsVisible;
        var previousSimulation = Physics.simulationMode;
        GameObject floor = null, wall = null, extra = null;
        int successes = 0, failures = 0, parriedQueries = 0;
        double lastContact = double.NaN;
        Action succeeded = () => successes++;
        Action failed = () => failures++;
        Action<PrototypeMeleeQuerySample> sampled = sample =>
        {
            if (sample.Attacker != enemy.transform) return;
            if (sample.AppliedDamage > 0 || sample.ParryCount > 0) lastContact = sample.ContactTime;
            if (sample.ParryCount == 0) return;
            Check(sample.AppliedDamage == 0 && sample.HitCount == 0, "Desvío también descuenta vida");
            parriedQueries += sample.ParryCount;
        };
        Vector3 origin = new Vector3(400f, 1.08f, 400f);
        double clock = 0d;
        void Reset(float yaw = 0f)
        {
            parry.CancelParry(); attack.CancelAttack(); dodge.CancelDodge(); enemy.CancelAttack();
            player.ResetHealth(); enemyHealth.ResetHealth(); movement.ResetMovement();
            controller.enabled = false;
            player.transform.SetPositionAndRotation(origin, Quaternion.Euler(0f, yaw, 0f));
            controller.enabled = true;
            enemy.transform.SetPositionAndRotation(origin + Vector3.forward * 1.5f, Quaternion.Euler(0f, 180f, 0f));
            wall.SetActive(false);
            Physics.SyncTransforms(); Physics.Simulate(0.02f);
            clock = 0d; lastContact = double.NaN;
        }
        void Step(float dt)
        {
            clock += dt;
            Invoke(parry, "AdvanceTo", clock);
            Invoke(enemy, "AdvanceAt", dt, clock);
            Invoke(parry, "FinalizeFeedback");
        }
        void Begin(double time)
        {
            Check((bool)Invoke(parry, "TryParryAt", time), "No inicia defensa libre");
        }
        // Activa ataque en t=0 sin consumir preparación; contacto esperado cerca de t=0,8.
        void EnemyStart() => Invoke(enemy, "AdvanceAt", 0f, 0d);
        void Run(float dt, double pressTime)
        {
            EnemyStart(); bool pressed = false;
            while (clock < 0.96d)
            {
                double end = clock + dt;
                // Permite medir el evento entre frames sin cuantizar también la pulsación de la prueba.
                if (!pressed && pressTime <= end) { Begin(pressTime); pressed = true; }
                Step(dt);
            }
        }
        parry.Succeeded += succeeded; parry.Failed += failed;
        PrototypeMeleeHitQuery.Completed += sampled;
        try
        {
            Physics.simulationMode = SimulationMode.Script;
            floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.transform.position = new Vector3(400f, -0.5f, 400f);
            floor.transform.localScale = new Vector3(20f, 1f, 20f);
            wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.transform.position = origin + new Vector3(0f, 0f, 1.05f);
            wall.transform.localScale = new Vector3(2f, 3f, 0.1f);
            extra = new GameObject("Collider adicional de regresión");
            extra.transform.SetParent(player.transform, false);
            extra.AddComponent<CapsuleCollider>().height = 1.8f;
            observer.SetVisible(true);
            Check(parry.DefensiveVolume != null && parry.DefensiveVolume.isTrigger, "Falta volumen defensivo real");
            var feedback = parry.GetComponent<PrototypeParryFeedback>();
            Check(feedback != null && !parry.GetComponent<AudioSource>().playOnAwake, "Feedback sonoro no configurado");
            foreach (string cue in new[] { "success", "failure" })
            {
                var clip = (AudioClip)typeof(PrototypeParryFeedback).GetField(cue, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(feedback);
                Check(clip != null && clip.length > 0.05f, "Falta clip de " + cue);
                var samples = new float[clip.samples];
                Check(clip.GetData(samples, 0) && Array.Exists(samples, value => Mathf.Abs(value) > 0.1f), "Clip de " + cue + " vacío");
            }

            foreach (float dt in new[] { 1f / 30f, 1f / 60f, 1f / 120f })
            {
                foreach (double lead in new[] { 0.30d, 0.22d, 0.185d, 0.175d, 0.12d, 0.065d, 0.055d, 0.02d, -0.005d })
                {
                    Reset(); int before = successes;
                    Run(dt, 0.8d - lead);
                    bool expected = lead > 0.06d && lead < 0.18d;
                    Check(player.Health == (expected ? 100 : 80) && successes == before + (expected ? 1 : 0),
                        "Timing incoherente: dt=" + dt + ", anticipación=" + lead + ", vida=" + player.Health);
                    Check(Math.Abs(lastContact - 0.8d) < 0.00001d, "El instante de impacto deriva con FPS: " + lastContact);
                    if (expected)
                        Check(Math.Abs(parry.PressLeadSeconds - lead) < 0.00001d && observer.TraceSnapshot.Contains("anticipación ms="), "Falta medición del acierto");
                    Debug.Log("PARRY_TIMING_SAMPLE: dt=" + dt + "; anticipación=" + lead + "; vida=" + player.Health + "; acierto=" + expected);
                }
            }

            // Aísla balance de detección: con 180 ms activos, 220 ms de anticipación sí es válida.
            var duration = typeof(PrototypePlayerParry).GetField("activeDuration", BindingFlags.Instance | BindingFlags.NonPublic);
            object originalDuration = duration.GetValue(parry);
            try
            {
                duration.SetValue(parry, 0.18f);
                Reset(); Run(1f / 30f, 0.58d);
                Check(player.Health == 100 && parry.LastSucceeded, "No reproduce permisividad temporal de 180 ms");
            }
            finally { duration.SetValue(parry, originalDuration); }

            // La ventana termina dentro del frame, después del contacto: debe acertar.
            Reset(); EnemyStart(); Begin(0.626d); Step(0.79f); Step(0.04f);
            Check(player.Health == 100 && parry.LastSucceeded, "Pierde contacto válido al expirar ventana al final del frame");
            // La ventana comienza después del contacto dentro del mismo frame: no debe acertar.
            Reset(); EnemyStart(); Begin(0.746d); Step(0.79f); Step(0.04f);
            Check(player.Health == 80 && !parry.LastSucceeded, "Acepta defensa que comenzó después del contacto");
            // Un hitch atraviesa ambas ventanas: conservar el evento válido, no el estado final.
            Reset(); EnemyStart(); Begin(0.68d); Step(0.79f); Step(0.5f);
            Check(player.Health == 100 && parry.LastSucceeded, "Pierde impacto válido dentro de un frame largo");
            Reset(); EnemyStart(); Begin(0.68d); Step(0.79f);
            Invoke(parry, "AdvanceTo", 1.29d);
            Check(!(bool)Invoke(parry, "TryParryAt", 1.29d) && !attack.TryAttack() && !dodge.TryDodge(Vector2.right),
                "Una entrada nueva reemplaza la defensa antes de resolver contacto histórico");
            Invoke(enemy, "AdvanceAt", 0.5f, 1.29d); Invoke(parry, "FinalizeFeedback");
            Check(player.Health == 100 && parry.LastSucceeded && !parry.IsBusy, "No libera defensa histórica después de resolver contacto");

            foreach (float yaw in new[] { 0f, 50f, 70f, 180f })
            {
                Reset(yaw); Run(1f / 60f, 0.68d);
                Check(player.Health == (yaw <= 50f ? 100 : 80), "Cono frontal incoherente a " + yaw);
            }
            Reset(); EnemyStart(); Begin(0.68d); wall.SetActive(true); Physics.SyncTransforms(); Step(0.81f);
            Check(player.Health == 100 && !parry.LastSucceeded && !enemy.IsExposed, "Parry a través de pared");
            Reset(); EnemyStart(); Begin(0.68d);
            controller.enabled = false; player.transform.position += Vector3.right * 1.15f; controller.enabled = true;
            Physics.SyncTransforms(); Physics.Simulate(0.02f); Step(0.81f);
            Check(player.Health == 80 && !parry.LastSucceeded, "Solapamiento de cápsula concede defensa sin contacto del volumen frontal");
            Reset(); EnemyStart(); Begin(0.68d); parry.DefensiveVolume.enabled = false; Step(0.81f);
            Check(player.Health == 80 && !parry.LastSucceeded, "Concede parry sin volumen defensivo habilitado");
            parry.DefensiveVolume.enabled = true;

            Reset(); EnemyStart(); Begin(0.68d);
            controller.enabled = false; player.transform.position += Vector3.back * 1.05f; controller.enabled = true;
            Physics.SyncTransforms(); Physics.Simulate(0.02f); Step(0.81f);
            Check(player.Health == 100 && parry.LastSucceeded, "Contacto del volumen defensivo exige también golpear la cápsula");

            Reset(); EnemyStart(); Begin(0.68d); int count = successes; Step(0.81f);
            Check(player.Health == 100 && successes == count + 1 && enemy.IsExposed, "Desvío con colliders duplicados");
            Check(!attack.TryAttack() && !dodge.TryDodge(Vector2.right) && !parry.TryParry(), "Cancela recuperación exitosa");
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            Transform weapon = enemy.transform.Find("Arma enemiga");
            Check(Vector3.Distance(weapon.localPosition, new Vector3(0.65f, -0.15f, 0.5f)) < 0.001f, "Desvío oculta pose de contacto");
            Step(0.30f);
            Check(!parry.IsBusy && attack.TryAttack(), "No habilita contraataque");
            Invoke(attack, "Advance", 0.18f);
            Check(enemyHealth.Health == 75 && enemy.IsExposed, "Contraataque manual no aplica 25");
            Step(0.5f); Check(player.Health == 100 && successes == count + 1, "Golpe cancelado reaparece");
            Step(0.401f); Check(!enemy.IsExposed, "No termina apertura");
            attack.CancelAttack(); Begin(clock); Step(0.81f);
            Check(player.Health == 80, "Una defensa anterior sigue protegiendo el siguiente ataque");

            Reset(); EnemyStart(); Step(0.81f); Check(player.Health == 80, "No aplica primer golpe sin defensa");
            Begin(clock); Step(0.07f);
            Check(player.Health == 80 && !parry.LastSucceeded && !enemy.IsExposed, "Parry tardío revierte daño ya aplicado");
            Reset(); Begin(0d); int failBefore = failures; Invoke(parry, "AdvanceTo", 0.5d); Invoke(parry, "FinalizeFeedback"); Invoke(parry, "FinalizeFeedback");
            Check(failures == failBefore + 1 && parry.Result == PrototypeParryResult.Failed, "Feedback de fallo ausente o repetido");
            Reset(); Begin(0d);
            Vector3 start = player.transform.position; Quaternion rotation = player.transform.rotation;
            Invoke(movement, "AdvanceMovement", Vector2.right, 0.03f);
            Check(Mathf.Abs(player.transform.position.x - start.x) < 0.001f && player.transform.position.y < start.y
                && Quaternion.Angle(player.transform.rotation, rotation) < 0.1f, "Defensa permite caminar/girar o elimina gravedad");
            Check(!attack.TryAttack() && !dodge.TryDodge(Vector2.right), "Defensa no bloquea acciones");
            parry.CancelParry(); Check(attack.TryAttack() && !parry.TryParry(), "Parry cancela ataque");
            attack.CancelAttack(); Check(dodge.TryDodge(Vector2.right) && !parry.TryParry(), "Parry cancela esquiva");
            Reset(); Begin(0d); player.TakeDamage(100); Invoke(parry, "AdvanceTo", 0.1d);
            Check(!parry.IsBusy && !parry.TryParry(), "Muerto conserva parry");
            Reset(); EnemyStart(); Begin(0.68d); Step(0.81f); session.Restart();
            Check(!parry.IsBusy && !enemy.IsExposed && !parry.LastSucceeded && player.Health == 100 && enemyHealth.Health == 100, "R conserva defensa/apertura");
            Reset(); Begin(0d); enemyHealth.TakeDamage(100); Invoke(session, "Update");
            Check(session.HasEnded && !parry.IsBusy && !parry.TryParry(), "Victoria conserva defensa");
            Reset(); Begin(0d); parry.enabled = false; Check(!parry.IsBusy && !parry.TryParry(), "Deshabilitar conserva defensa"); parry.enabled = true;
            Check(parriedQueries == successes, "Consultas y eventos de éxito no coinciden");
            Debug.Log("PARRY_VALIDATION_SUCCEEDED: recepción frontal y volumen defensivo, exclusión, paredes, colliders duplicados, contraataque, acciones/gravedad, derrota/victoria/R y ausencia de daño diferido.");
            Debug.Log("PARRY_TIMING_VALIDATION_SUCCEEDED: barrido 30/60/120 FPS, reproducción 180 ms vs 120 ms, anticipado/tardío, fronteras dentro del frame, hitch, ataques sucesivos, contacto/presentación y feedback único.");
        }
        finally
        {
            parry.Succeeded -= succeeded; parry.Failed -= failed; PrototypeMeleeHitQuery.Completed -= sampled;
            if (floor != null) UnityEngine.Object.DestroyImmediate(floor);
            if (wall != null) UnityEngine.Object.DestroyImmediate(wall);
            if (extra != null) UnityEngine.Object.DestroyImmediate(extra);
            parry.enabled = true; parry.DefensiveVolume.enabled = true;
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
            var parry = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerParry>();
            var enemy = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyAttack>();
            parry.GetComponent<PrototypeDuelSession>().Restart();
            var controller = parry.GetComponent<CharacterController>();
            controller.enabled = false;
            parry.transform.SetPositionAndRotation(enemy.transform.position + Vector3.back * 1.4f + Vector3.up * 0.08f, Quaternion.identity);
            controller.enabled = true;
            Physics.SyncTransforms(); Physics.Simulate(0.02f);
            Camera camera = Camera.main; camera.GetComponent<PrototypeFollowCamera>().Configure(parry.transform);
            Invoke(enemy, "AdvanceAt", 0f, 0d);
            Invoke(parry, "TryParryAt", 0.68d);
            Invoke(parry, "AdvanceTo", 0.70d);
            Invoke(enemy, "AdvanceAt", 0.70f, 0.70d);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "01-Preparacion", "Logs/ParryTiming");
            Invoke(parry, "AdvanceTo", 0.76d);
            Invoke(enemy, "AdvanceAt", 0.06f, 0.76d);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "02-DefensaActiva", "Logs/ParryTiming");
            Invoke(parry, "AdvanceTo", 0.81d);
            Invoke(enemy, "AdvanceAt", 0.05f, 0.81d);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            Check(parry.LastSucceeded && enemy.IsExposed && parry.GetComponent<PrototypeHealth>().Health == 100, "Captura sin contacto/desvío real");
            PrototypeCombatVisualValidation.Capture(camera, "03-ContactoDesviado", "Logs/ParryTiming");
            Invoke(enemy, "AdvanceAt", 0.12f, 0.93d);
            Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            PrototypeCombatVisualValidation.Capture(camera, "04-ReaccionDesvio", "Logs/ParryTiming");
            enemy.CancelAttack(); Invoke(enemy.GetComponent<PrototypeAttackPresentation>(), "ApplyPose");
            parry.CancelParry(); Invoke(parry, "TryParryAt", 1d); Invoke(parry, "AdvanceTo", 1.20d); Invoke(parry, "FinalizeFeedback");
            PrototypeCombatVisualValidation.Capture(camera, "05-VentanaAgotada", "Logs/ParryTiming");
            Debug.Log("PARRY_CAPTURE_SUCCEEDED: volumen defensivo amarillo/azul, contacto desviado blanco, reacción y fallo rojo; salud 100, sin HUD OnGUI ni evaluación auditiva humana.");
        }
        finally { Physics.simulationMode = previousSimulation; UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath); }
    }

    private static object Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("PARRY_VALIDATION_FAILED: " + message);
    }
}
