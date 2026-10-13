using System;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

/// <summary>Valida regresión en edición y comandos en Play sin flags internos del paquete.</summary>
[InitializeOnLoad]
public static class PrototypeInputValidation
{
    private const string Pending = "PrototypeInputValidation.Pending";
    private const string ExitCode = "PrototypeInputValidation.ExitCode";
    static PrototypeInputValidation() => EditorApplication.playModeStateChanged += OnPlayModeChanged;

    [MenuItem("Tools/Soulslike/Validar proyecto y entrada en Play")]
    public static void Validate()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrototypeProjectValidation.Validate();
        StartInputValidation();
    }

    public static void ValidateAndBuild()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        PrototypeProjectValidation.ValidateAndBuildFromSceneList();
        StartInputValidation();
    }

    private static void StartInputValidation()
    {
        SessionState.SetBool(Pending, true);
        SessionState.SetInt(ExitCode, 1);
        EditorApplication.EnterPlaymode();
    }

    private static void OnPlayModeChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(Pending, false)) return;
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            // Espera que terminen los callbacks de entrada en Play del propio Input System.
            EditorApplication.delayCall += RunChecks;
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            int exitCode = SessionState.GetInt(ExitCode, 1);
            SessionState.SetBool(Pending, false);
            if (Application.isBatchMode) EditorApplication.Exit(exitCode);
        }
    }

    private static void RunChecks()
    {
        if (!SessionState.GetBool(Pending, false) || !EditorApplication.isPlaying) return;
        try
        {
            var enemyDefense = UnityEngine.Object.FindFirstObjectByType<PrototypeEnemyParry>();
            bool previousEnabled = enemyDefense != null && enemyDefense.enabled;
            try
            {
                // Regresión de los incrementos anteriores con su rival original; no cambia la escena guardada.
                if (enemyDefense != null) enemyDefense.enabled = false;
                CheckCommands();
                if (UnityEngine.Object.FindFirstObjectByType<PrototypeCombatDiagnostics>() != null)
                    PrototypeDiagnosticsValidation.Validate();
                if (UnityEngine.Object.FindFirstObjectByType<PrototypePlayerParry>() != null)
                    PrototypeParryValidation.Validate();
                if (UnityEngine.Object.FindFirstObjectByType<PrototypeStamina>() != null)
                    PrototypeStaminaValidation.Validate();
            }
            finally { if (enemyDefense != null) enemyDefense.enabled = previousEnabled; }
            if (previousEnabled) PrototypeEnemyParryValidation.Validate();
            SessionState.SetInt(ExitCode, 0);
            Debug.Log("INPUT_VALIDATION_SUCCEEDED: Play, mapa clonado, WASD/flechas combinados, diagonal, E/clic, Espacio/R, mantener sin repetir y exclusión de acciones.");
        }
        catch (Exception exception) { Debug.LogException(exception); }
        finally { EditorApplication.ExitPlaymode(); }
    }

    private static void CheckCommands()
    {
        var player = UnityEngine.Object.FindFirstObjectByType<PrototypePlayerInput>();
        var dodge = player.GetComponent<PrototypePlayerDodge>();
        var parry = player.GetComponent<PrototypePlayerParry>();
        var movement = player.GetComponent<PrototypePlayerMovement>();
        var attack = player.GetComponent<PrototypePlayerAttack>();
        var session = player.GetComponent<PrototypeDuelSession>();
        var runtime = (InputActionAsset)typeof(PrototypePlayerInput).GetField("runtimeActions", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(player);
        var previousDevices = runtime.devices;
        var previousKeyboard = Keyboard.current;
        var previousMouse = Mouse.current;
        var previousFocusBehavior = InputSystem.settings.editorInputBehaviorInPlayMode;
        var previousBackgroundBehavior = InputSystem.settings.backgroundBehavior;
        Keyboard keyboard = null;
        Mouse mouse = null;
        try
        {
            // Solo durante esta prueba en Play; permite procesar dispositivos virtuales en batch.
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            keyboard = InputSystem.AddDevice<Keyboard>();
            mouse = InputSystem.AddDevice<Mouse>();
            runtime.devices = new InputDevice[] { keyboard, mouse };
            session.Restart();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.W, Key.RightArrow, Key.Space, Key.E));
            InputSystem.Update();
            Check(player.Move.x > 0.7f && player.Move.y > 0.7f && player.Move.magnitude <= 1.001f && player.DodgePressed && player.AttackPressed,
                "Composite o pulsación incorrectos: Move=" + player.Move + ", Espacio=" + player.DodgePressed);
            Invoke(dodge, "Update");
            Invoke(attack, "Update");
            Check(dodge.IsBusy && !attack.IsAttacking, "Conflicto simultáneo no prioriza esquiva");
            InputSystem.Update();
            Check(!player.DodgePressed && !player.AttackPressed, "Mantener botones genera nueva pulsación");
            Invoke(movement, "AdvanceMovement", Vector2.zero, 0.5f);
            Invoke(dodge, "Update");
            Invoke(attack, "Update");
            Check(!dodge.IsBusy && !attack.IsAttacking, "Entrada mantenida o rechazada se encola");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R, Key.LeftArrow));
            InputSystem.Update();
            Check(player.RestartPressed && player.Move.x < -0.99f, "R/flechas no funcionan");
            Invoke(session, "Update");
            Check(!session.HasEnded && !dodge.IsBusy && !attack.IsAttacking, "R no restaura acciones");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Left));
            InputSystem.Update();
            Check(player.AttackPressed, "Clic no activa ataque");
            Invoke(attack, "Update");
            Check(attack.IsAttacking && !dodge.TryDodge(Vector2.right), "Clic no inicia o ataque permite esquiva");
            InputSystem.QueueStateEvent(mouse, new MouseState());
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
            InputSystem.Update();
            Invoke(dodge, "Update");
            Check(!dodge.IsBusy, "Espacio cancela ataque");
            attack.CancelAttack();
            InputSystem.Update();
            Invoke(dodge, "Update");
            Check(!dodge.IsBusy, "Esquiva rechazada queda en cola");
            var observer = player.GetComponent<PrototypeCombatDiagnostics>();
            if (observer != null)
            {
                float originalScale = Time.timeScale;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F3, Key.F4));
                InputSystem.Update();
                Check(player.DiagnosticsPressed && player.SlowMotionPressed, "F3/F4 sin binding");
                Invoke(observer, "Update");
                Check(observer.IsVisible && observer.SlowMotion && Mathf.Abs(Time.timeScale - originalScale * 0.2f) < 0.0001f, "No activa diagnóstico/lento");
                InputSystem.Update();
                Invoke(observer, "Update");
                Check(observer.IsVisible && observer.SlowMotion, "Mantener F3/F4 repite");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F3, Key.F4));
                InputSystem.Update();
                Invoke(observer, "Update");
                Check(!observer.IsVisible && !observer.SlowMotion && Mathf.Abs(Time.timeScale - originalScale) < 0.0001f, "F4 no restaura tiempo");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.F5));
                InputSystem.Update();
                Check(player.ExportTracePressed, "F5 sin binding");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
            }
            if (parry != null)
            {
                session.Restart();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q, Key.Space, Key.E));
                InputSystem.Update();
                Check(player.ParryPressed && player.AttackPressed && player.DodgePressed, "Q no comparte frame con otras entradas");
                Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
                Check(parry.IsBusy && !dodge.IsBusy && !attack.IsAttacking, "No prioriza parry ante pulsaciones simultáneas");
                InputSystem.Update();
                Check(!player.ParryPressed, "Mantener Q repite parry");
                Invoke(parry, "Advance", 1f);
                Invoke(parry, "FinalizeFeedback");
                Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
                Check(!parry.IsBusy && !dodge.IsBusy && !attack.IsAttacking, "Entrada mantenida queda en cola");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState().WithButton(MouseButton.Right));
                InputSystem.Update();
                Check(player.ParryPressed && !player.AttackPressed, "Clic derecho no activa solo parry");
                Invoke(parry, "Update");
                Check(parry.IsBusy, "Clic derecho no inicia parry");
                InputSystem.QueueStateEvent(mouse, new MouseState());
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.R));
                InputSystem.Update(); Invoke(session, "Update");
                Check(!parry.IsBusy && !parry.LastSucceeded, "R por Input Actions no limpia parry");
                Check(attack.TryAttack(), "No inicia ataque para rechazar parry");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q));
                InputSystem.Update(); Invoke(parry, "Update");
                Check(!parry.IsBusy, "Q cancela ataque");
                attack.CancelAttack(); InputSystem.Update(); Invoke(parry, "Update");
                Check(!parry.IsBusy, "Q rechazada se encola");
            }
            player.enabled = false;
            player.enabled = true;
            session.Restart();
            Check(attack.TryAttack(), "No prepara recuperación por desvío");
            Invoke(attack, "Advance", 0.18f);
            double deflectTime = Time.timeAsDouble;
            Check(attack.ReceiveParry(deflectTime), "No acepta resultado de desvío para validar entrada");
            InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E, Key.Space, Key.Q)); InputSystem.Update();
            Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
            Check(attack.IsStaggered && !dodge.IsBusy && !parry.IsBusy, "Pulsación cancela desvío");
            Invoke(attack, "AdvanceAt", 0.61f, deflectTime + 0.61d); InputSystem.Update();
            Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
            Check(!attack.IsAttacking && !dodge.IsBusy && !parry.IsBusy, "Entrada rechazada durante desvío se ejecuta después");
            player.enabled = false;
            var stamina = player.GetComponent<PrototypeStamina>();
            if (stamina != null)
            {
                player.enabled = true;
                session.Restart();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.QueueStateEvent(mouse, new MouseState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q, Key.Space, Key.E)); InputSystem.Update();
                Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
                Check(parry.IsBusy && Mathf.Abs(stamina.Current - 85f) < 0.001f, "Pulsaciones simultáneas pagan más de una acción");
                session.Restart();
                for (int i = 0; i < 3; i++) Check(stamina.TrySpend(PrototypeStaminaAction.Dodge), "No prepara agotamiento");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Q, Key.Space, Key.E)); InputSystem.Update();
                Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
                Check(!parry.IsBusy && !dodge.IsBusy && !attack.IsAttacking && Mathf.Abs(stamina.Current - 10f) < 0.001f,
                    "Input Actions inicia acción sin energía");
                Invoke(stamina, "Advance", 4.5f); InputSystem.Update();
                Invoke(parry, "Update"); Invoke(dodge, "Update"); Invoke(attack, "Update");
                Check(!parry.IsBusy && !dodge.IsBusy && !attack.IsAttacking && Mathf.Abs(stamina.Current - 100f) < 0.001f,
                    "Mantener o recuperar energía ejecuta una pulsación rechazada");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState()); InputSystem.Update();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.E)); InputSystem.Update(); Invoke(attack, "Update");
                Check(attack.IsAttacking && Mathf.Abs(stamina.Current - 80f) < 0.001f, "Nueva pulsación tras recuperar no paga ataque");
                player.enabled = false;
            }
            Check(player.Move == Vector2.zero && !player.AttackPressed && !player.DodgePressed && !player.ParryPressed && !player.RestartPressed,
                "Componente deshabilitado conserva comandos");
            player.enabled = true;
        }
        finally
        {
            InputSystem.settings.editorInputBehaviorInPlayMode = previousFocusBehavior;
            InputSystem.settings.backgroundBehavior = previousBackgroundBehavior;
            runtime.devices = previousDevices;
            if (keyboard != null && keyboard.added) InputSystem.RemoveDevice(keyboard);
            if (mouse != null && mouse.added) InputSystem.RemoveDevice(mouse);
            if (previousKeyboard != null && previousKeyboard.added) previousKeyboard.MakeCurrent();
            if (previousMouse != null && previousMouse.added) previousMouse.MakeCurrent();
            session.Restart();
        }
    }

    private static void Invoke(object component, string method, params object[] arguments)
        => component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException("INPUT_VALIDATION_FAILED: " + message);
    }
}
