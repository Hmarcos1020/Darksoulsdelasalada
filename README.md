# Soulslike Combat Prototype

Prototipo Soulslike 3D en Unity y C#, para aprender construyendo con avances pequeños y explicaciones breves.

## Entorno

- Unity Editor: **6000.3.26f1** (Unity 6.3).
- Render pipeline: **Universal Render Pipeline (URP) 17.3.0**.

## Hito 0

Preparar una base reproducible: configurar el entorno y el control de versiones, generar una arena mínima mediante herramientas C# del editor y guardarla como escena con suelo, luz y cámara, implementar movimiento básico y generar una compilación para Windows. Combate, parry e IA quedan para hitos posteriores.

Versionar `Assets/` con sus archivos `.meta`, `Packages/` y `ProjectSettings/`. Las carpetas generadas por Unity y las compilaciones se excluyen mediante `.gitignore`.

## Generar y probar la arena

1. Abrí el proyecto con Unity **6000.3.26f1** y esperá la importación y compilación de scripts. Revisá que la consola no tenga errores.
2. Fuera del modo Play, elegí **Tools > Soulslike > Crear arena H0**. Si hay escenas con cambios sin guardar, el editor permite guardarlos o cancelar la operación.
3. La herramienta genera y guarda `Assets/Scenes/PrototypeArena.unity` y los materiales URP en `Assets/Materials/Prototype/`. Si la escena ya existe, solo la abre: no la sobrescribe ni reconstruye al entrar en Play.
4. Entrá en Play, enfocá la vista Game y mové al jugador con **WASD o flechas**. Comprobá la misma velocidad en diagonal y en línea recta, colisiones con paredes y obstáculos, gravedad y giro suave.
5. Comprobá que la cámara siga al jugador suavemente y conserve su orientación al girar. Salí de Play antes de editar y guardar ajustes.
6. Verificá los `.meta` que Unity genera para los scripts, carpetas, materiales y escena. Para compilar Windows, seguí los pasos de la sección siguiente.

## Responsabilidades de los scripts

- `PrototypeArenaBuilder` (`Assets/Editor/`): herramienta del editor que crea una arena de 20 × 20 metros con cuatro paredes, tres obstáculos, luz, jugador y cámara. Guarda materiales como assets y conecta las referencias automáticamente. No se ejecuta durante el juego.
- `PrototypePlayerMovement` (`Assets/Scripts/`): lee el teclado con Input System instalado y habilitado, limita la entrada diagonal y mueve un `CharacterController` según los ejes horizontales de la cámara. Aplica gravedad explícita y `Time.deltaTime`; la velocidad inicial es 5 m/s. El jugador no tiene Rigidbody ni otro collider.
- `PrototypeFollowCamera` (`Assets/Scripts/`): sigue en `LateUpdate` con `SmoothDamp`. Usa un desplazamiento fijo en ejes del mundo, sin heredar la rotación del jugador. Distancia, altura y suavizado se ajustan en el Inspector.

La base H0 no incluía combate. H1 agrega el primer ataque, descrito más abajo. Salto, parry, stamina, IA, animaciones de personaje con esqueleto, Cinemachine y control de cámara con mouse permanecen fuera de este incremento.

## Estado de validación de H0

El 12 de octubre de 2026 el usuario confirmó ejecución Windows, movimiento, colisiones contra paredes y obstáculos y seguimiento de cámara con orientación estable. H0 queda cerrado en su validación básica. La velocidad diagonal y los diálogos de conservación/cancelación del generador no tienen prueba manual específica registrada.

## Compilar y probar Windows H0

1. En el editor ya abierto, esperá la importación de `Assets/Editor/PrototypeWindowsBuilder.cs` y su `.meta`. Revisá que no haya errores de scripts y salí de Play.
2. Guardá los cambios de la arena. En **File > Build Profiles**, verificá que esté activa la plataforma **Windows**, arquitectura **Intel 64-bit (x86_64)**. Si está activa otra plataforma, cambiala allí y esperá la importación; la herramienta no cambia la plataforma automáticamente. Windows Build Support debe estar disponible para este editor.
3. Elegí **Tools > Soulslike > Compilar Windows H0**. La herramienta compila exclusivamente `Assets/Scenes/PrototypeArena.unity`, sin modificar la lista de escenas de Build Profiles, y escribe `Builds/Windows/SoulslikeCombatPrototype.exe`. Las compilaciones siguientes usan el mismo destino.
4. El backend se establece temporalmente en **Mono** para esta compilación y se restaura al terminar, incluso ante un fallo. No se cambian paquetes, versión de Unity ni configuración global de Git.
5. Revisá el diálogo y la consola: solo se anuncia éxito si `BuildPipeline.BuildPlayer` devuelve `BuildResult.Succeeded` y existe el ejecutable. Se informan resultado, errores, advertencias y duración; ante un fallo, un ejecutable anterior no sirve como validación.
6. Si la compilación fue exitosa, abrí el ejecutable desde Windows. Conservá junto a él la carpeta `SoulslikeCombatPrototype_Data` y los demás archivos generados; no copies únicamente el `.exe`.
7. Enfocá la ventana y probá WASD y flechas. Validá por separado velocidad diagonal, colisiones contra suelo, paredes y obstáculos, y seguimiento y orientación de la cámara. Cerrá la ventana o usá **Alt+F4** al terminar.

`Builds/` está excluido de Git. La herramienta no lanza otro editor Unity ni ejecuta automáticamente el juego. El nuevo script ya tiene su `.meta` generado por Unity. El usuario confirmó posteriormente la compilación y ejecución Windows; ver el cierre de H0 al final de este documento.

## Hito 1: primer ataque contra un objetivo inmóvil

### Objetivo y conceptos

Validar orientación, distancia y compromiso temporal antes de agregar enemigos móviles. El ataque usa una máquina de estados pequeña: reposo → preparación (0,18 s) → ventana activa (0,12 s) → recuperación (0,35 s). Un clic inicia una acción; mantener el botón o pulsarlo durante una acción no encola otra.

Durante el ataque se bloquean desplazamiento horizontal y giro, mientras el CharacterController sigue aplicando gravedad. La ejecución del ataque precede al movimiento para que ambos componentes observen el mismo estado en ese fotograma.

### Preparar y jugar

1. Abrí la arena y elegí **Tools > Soulslike > Preparar primer ataque H1**, fuera de Play. Agrega componentes y un objetivo; conserva la arena existente. Repetir el menú no duplica estos elementos.
2. Entrá en Play. Movete con WASD/flechas y acercate al objetivo verde mirando hacia él. **Clic izquierdo o E** inicia el ataque.
3. La marca frontal representa la proyección del volumen de impacto sobre el suelo: amarillo en preparación, rojo durante la ventana activa y azul en recuperación. El volumen tiene altura; se puede inspeccionar seleccionando al jugador con Gizmos habilitados.
4. La UI muestra fase y vida del objetivo. Cada golpe aplica 25 de daño; cuatro golpes lo dejan en gris con 0/100. El objetivo permanece como obstáculo y no recibe más daño. **R** restaura su vida.
5. Para Windows, usá el menú existente **Compilar Windows H0**: compila la misma arena, ahora con el ejercicio H1.

### Responsabilidades

- `PrototypePlayerAttack`: entrada, fases temporales y consulta física frontal. Deduplica objetivos durante cada golpe y verifica obstáculos entre jugador y contacto. Los tiempos, el daño y el volumen son configurables en el Inspector.
- `PrototypeTrainingTarget`: vida, destello blanco al recibir daño, estado de derrota y reinicio. Usa MaterialPropertyBlock sin modificar los materiales compartidos.
- `PrototypeCombatHUD`: controles y estado visible para el ejercicio. UI provisional con OnGUI.
- `PrototypeCombatBuilder`: agrega y guarda el ejercicio desde el editor sin reconstruir la arena.
- `PrototypeCombatValidation`: verifica fases y consultas físicas en una escena temporal sin guardar. Separa el tiempo de la lectura de teclado para simular distintos pasos; no simula interacción real ni renderizado.

### Criterios de aceptación y validación

- A corta distancia y mirando al objetivo: un solo daño por golpe, después de la preparación.
- Fuera del alcance, mirando hacia atrás o con un obstáculo interpuesto: ningún daño.
- Durante preparación y recuperación: no acepta otro ataque; no se puede mover ni girar durante toda la acción.
- Cuatro golpes válidos: 0/100; R permite repetir el ejercicio.
- El menú de preparación se puede repetir sin duplicar componentes ni objetivos.
- **Tools > Soulslike > Validar lógica de ataque H1** verifica spam, deduplicación con varios colliders, 30/120 FPS, un fotograma largo, alcance, dirección, obstáculos, derrota, reinicio y desactivación.

Riesgo principal: el alcance y los tiempos necesitan ajuste jugando. La detección muestrea un volumen frontal durante la ventana activa; no sigue una espada animada. Próximos sistemas como stamina, esquiva, parry e IA permanecen pendientes.

### Validación de H0 confirmada por el usuario

El 12 de octubre de 2026 el usuario confirmó ejecución Windows, movimiento con WASD/flechas, colisiones con paredes y obstáculos y seguimiento de cámara con orientación estable. Se cierra la validación básica de H0. Las pruebas específicas de los diálogos del generador siguen sin confirmación.

### Estado de validación de H1

El 12 de octubre de 2026 Unity 6000.3.26f1 importó y compiló los scripts. Se guardó la arena y se verificó que repetir su preparación no duplica ataques ni objetivos. Las pruebas de lógica y consultas físicas terminaron con H1_VALIDATION_SUCCEEDED y sin errores ni aserciones. La build Windows terminó con éxito: 0 errores y 486 advertencias. El log contiene advertencias de shaders Sentis del paquete com.unity.ai.inference ya instalado; no se modificaron paquetes. El ejecutable base puede conservar su fecha original en una build incremental: Assembly-CSharp.dll y level0 se regeneraron con el ataque y la arena H1. Logs locales: Logs/H1Validation.log y Logs/H1WindowsBuild.log. El 12 de octubre de 2026 el usuario confirmó que pasaron las siete pruebas manuales del ejecutable: un golpe (100 a 75), botón sostenido sin repetición, fuera de alcance sin daño, dirección incorrecta sin daño, bloqueo del movimiento durante el ataque, derrota tras cuatro golpes y reinicio con R. H1 queda cerrado. El usuario señaló que la falta de animaciones dificulta comprobar visualmente el ataque; mejorar su legibilidad queda como candidato para el próximo incremento. La prueba manual no incluye colocar un obstáculo entre jugador y objetivo: ese caso se verificó con la prueba automatizada.

## Incremento: lectura visual del ataque H1

Objetivo: reconocer preparación, golpe y recuperación, y distinguir un impacto válido de un ataque fallido. Se conserva el combate de H1: 0,18 s de preparación, 0,12 s de ventana activa, 0,35 s de recuperación y 25 de daño.

### Decisión técnica y conceptos

Se eligió animación procedural de una espada hecha con cubos. Evita depender de modelos, esqueletos y clips externos en este ejercicio. `PrototypePlayerAttack.PhaseProgress` expone el progreso normalizado (0 a 1) calculado con su temporizador existente. `PrototypeAttackPresentation` lee fase y progreso en LateUpdate, interpola la posición local del pivote y su rotación mediante Quaternion.Slerp y suaviza el movimiento con Mathf.SmoothStep.

El pivote es un Transform hijo del jugador. Sus coordenadas locales permiten que la espada acompañe al jugador al desplazarse y girar. Se asigna una pose calculada en cada fotograma, para evitar que se acumulen rotaciones o desplazamientos entre ataques.

`PrototypeTrainingTarget` emite eventos C# locales de daño recibido y reinicio. `PrototypeHitPresentation` escucha esos eventos: muestra un destello geométrico y un número flotante con el daño efectivo, se limpia con R y cancela las suscripciones al desactivarse. El daño se decide antes de notificar a la presentación.

La revisión de capturas detectó que el material Jugador heredado usaba VR/SpatialMapping/Occlusion, sin soporte para _BaseColor. Se crearon materiales URP/Lit para el jugador y el objetivo, y se asignó un material compatible al indicador. El material anterior se conserva como asset; no se modificaron paquetes. Los colores ahora permiten distinguir jugador naranja, objetivo verde y fases amarillo/rojo/azul. La espada y el destello no tienen colliders. La detección sigue usando el volumen frontal de H1; el recorrido del filo es una representación aproximada y no define contactos físicos. La marca sobre el suelo y la pérdida de vida siguen indicando cuándo y dónde puede causar daño el ataque. Un destello sobre el objetivo confirma que el daño fue aplicado.

### Preparar y probar

1. Fuera de Play, **Tools > Soulslike > Preparar lectura visual H1** agrega el arma, los materiales y la señal de impacto. Repetirlo no duplica componentes ni figuras.
2. En Play o en la build Windows, mirá al jugador: debe llevar una espada provisional. Usá E/clic para ver carga, barrido y regreso a la postura inicial.
3. Cerca del objetivo y mirando hacia él: un golpe produce destello sobre el objetivo y **-25**, además de la vida 100 a 75.
4. Fuera de alcance o mirando en otra dirección: la espada se mueve, pero el objetivo no muestra destello ni número de daño.
5. Repetí cuatro golpes y reiniciá con R: las poses no deben acumularse y la señal anterior debe desaparecer al reiniciar. Mantener E produce un solo ataque.
6. Caminá y girá: el arma debe acompañar al jugador. Durante el ataque continúa el bloqueo de movimiento y giro de H1.

### Validación

`PrototypeCombatVisualValidation` vuelve a ejecutar las pruebas de combate y verifica poses por fase, recuperación sin deriva, animación sin alterar daño, señal de impacto/reinicio/expiración y ausencia de colliders en la geometría visual. Las capturas de fases, cuando el renderizado está disponible, se guardan en Logs/H1Visual y no incluyen la UI OnGUI. No sustituyen la prueba manual de la animación en tiempo real.

Estado al 12 de octubre de 2026: Unity 6000.3.26f1 importó y compiló los scripts. Pasaron la regresión H1 y las pruebas de presentación, progreso, señales de daño efectivo, expiración/reinicio y suscripciones. Repetir la preparación no duplica los componentes visuales; arma y señal no tienen colliders. Se generaron e inspeccionaron capturas de reposo, preparación, impacto y recuperación en Logs/H1Visual. La build Windows terminó con éxito: 0 errores y 486 advertencias (el log incluye shaders Sentis del paquete ya instalado). Log: Logs/H1VisualValidationBuild.log. El 12 de octubre de 2026 el usuario confirmó funcionamiento correcto en tiempo real: animación sin daño fuera de alcance, destello y número -25 en golpes válidos, retorno a la postura inicial tras repetir ataques, reinicio con R y acompañamiento del arma al caminar/girar. El incremento de lectura visual queda cerrado. La espada continúa representando de forma aproximada la acción; el volumen frontal de H1 sigue determinando los impactos.

Referencias para profundizar: [Quaternion.Slerp](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Quaternion.Slerp.html), [Mathf.SmoothStep](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Mathf.SmoothStep.html) y [LateUpdate](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/MonoBehaviour.LateUpdate.html), documentación de Unity 6.3.

## Estabilización: escena de build y regresión repetible (H01)

La auditoría detectó que la lista global habilitaba SampleScene, mientras el builder H0 compilaba la arena mediante una ruta explícita. Se corrige ProjectSettings/EditorBuildSettings.asset para que PrototypeArena sea la única escena habilitada, con el GUID de su .meta. SampleScene se conserva como asset y queda fuera de la compilación global.

En Unity 6.3, EditorBuildSettings.globalScenes representa la lista global y EditorBuildSettings.scenes refleja la del perfil activo. Un perfil personalizado puede reemplazar la lista global. En File > Build Profiles, revisá Scene List: PrototypeArena debe ser la única habilitada, en índice 0. Si habilitás Override Global Scene List, mantené esa misma arena en la lista del perfil.

### Objetivo y aceptación

- Las listas global y del perfil activo incluyen únicamente la arena habilitada, con GUID válido.
- La regresión de combate y presentación existente pasa sin regenerar la arena ni cambiar parámetros de gameplay.
- Una build que lee EditorBuildSettings.scenes termina con éxito y usa PrototypeArena.
- Los cambios no agregan paquetes ni modifican la versión del motor.

### Repetir la regresión

Desde PowerShell, ubicado en la raíz del proyecto y con Unity cerrado:

```powershell
.\Tools\Validate-Prototype.ps1
```

Comprueba configuración y ejecuta las pruebas existentes. No genera build ni capturas. El log queda en Logs/ProjectValidation.log. El runner evita iniciar otra instancia cuando Unity está abierto o el lockfile está en uso. Si queda un lockfile residual de una ejecución fallida, verifica acceso exclusivo sin borrarlo y deja que Unity compruebe el proyecto. Una excepción, salida de Unity distinta de cero o ausencia de marcadores de éxito hace fallar el comando.

Para ejecutar lo anterior y además probar el camino que usa la lista del perfil activo:

```powershell
.\Tools\Validate-Prototype.ps1 -BuildFromSceneList
```

Requiere Windows x86_64 activo y soporte de compilación. Usa Mono temporalmente y restaura el backend al terminar. Log: Logs/ProjectValidationBuild.log. Destino separado: Builds/WindowsSceneList/SoulslikeCombatPrototype.exe. El builder H0 y su destino habitual se conservan.

En el editor también podés ejecutar Tools > Soulslike > Validar proyecto y regresión, fuera de Play. El validador permite guardar/cancelar cambios y abre la arena guardada antes de crear las escenas temporales: Unity batch puede iniciar con una escena sin nombre, que impide la validación aditiva. Las pruebas usan escenas temporales de edición y pasos temporales controlados: no prueban el teclado real, el ciclo de PlayMode ni el renderizado. Son una regresión automatizada previa a Unity Test Framework/CI.

### Prueba manual breve de la build de lista

Abrí el ejecutable de WindowsSceneList junto con todos sus archivos generados. Debe mostrar la arena, jugador naranja, objetivo verde, arma y HUD. Probá movimiento/colisiones, un golpe cercano (100 a 75), un golpe fuera de alcance sin daño y R para reiniciar. Esta comprobación distingue configuración y compilación correctas de ejecución jugable verificada.

### Riesgos registrados de la auditoría

H02 (daño ligado al maniquí) se abordará al introducir jugador/enemigo con vida. H03 (arreglos en consultas físicas) requiere medir antes de optimizar. H04 conserva el volumen frontal como regla de impacto y la espada como representación aproximada. H05 se evaluará antes de ampliar entradas de combate. H06 espera una segunda variante de ataque. H07 requiere un incremento separado para revisar dependencias y advertencias. No se introducen ahora esas refactorizaciones.

Estado al 12 de octubre de 2026: Unity 6000.3.26f1 importó los cambios y confirmó las listas global y activa con la arena y su GUID correcto. Pasaron la regresión H1 y la regresión visual. El runner con -BuildFromSceneList terminó correctamente; el log registró PrototypeArena como entrada y SCENE_LIST_BUILD_SUCCEEDED: 0 errores, 486 advertencias y 16,21 s de build. Se verificaron el ejecutable, level0 y la referencia a Assets/Scenes/PrototypeArena.unity en globalgamemanagers. La comprobación final dejó ProjectSettings/ProjectSettings.asset sin cambios: el backend se configura solo cuando es necesario. El usuario confirmó el 12 de octubre de 2026 que la build de lista arranca la arena correcta y permite movimiento, ataque y reinicio. H01 queda cerrado con validación automatizada y manual.

Referencias: [EditorBuildSettings.globalScenes](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorBuildSettings-globalScenes.html) y [EditorBuildSettings.scenes](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/EditorBuildSettings-scenes.html), Unity 6.3.
