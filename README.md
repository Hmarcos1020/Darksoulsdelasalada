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
- `PrototypePlayerMovement` (`Assets/Scripts/`): consume dirección lógica desde `PrototypePlayerInput`, limita la entrada diagonal y mueve un `CharacterController` según los ejes horizontales de la cámara. Combina caminar/esquivar y gravedad en una única llamada a Move; la velocidad al caminar es 5 m/s. El jugador no tiene Rigidbody ni otro collider.
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
- `PrototypeTrainingTarget`: presentación del color y destello del objetivo. En el primer duelo se extrajo su vida, daño y eventos a `PrototypeHealth`, compartido con el jugador. Usa MaterialPropertyBlock sin modificar materiales compartidos.
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

`PrototypeHealth` emite eventos C# locales de daño recibido y reinicio (extraídos desde `PrototypeTrainingTarget` al introducir el primer duelo). `PrototypeHitPresentation` escucha esos eventos: muestra un destello geométrico y un número flotante con el daño efectivo, se limpia con R y cancela las suscripciones al desactivarse. El daño se decide antes de notificar a la presentación.

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

## Primer duelo: enemigo inmóvil y vida del jugador

Objetivo: completar un bucle pequeño de acercarse, leer una amenaza, apartarse, aprovechar recuperación y ganar/perder. El enemigo permanece en su posición; activa un ataque cuando el jugador está a 2,2 m o menos. Prepara durante 0,8 s, golpea durante 0,15 s y se recupera durante 1,1 s. Fija la dirección al iniciar la preparación; caminar hacia un costado o alejarse permite evitar el impacto. Cada golpe válido causa 20 de daño al jugador.

Ambos personajes tienen 100 de vida. El ataque del jugador conserva 25 de daño y sus tiempos de H1. Cuatro golpes derrotan al enemigo; cinco golpes enemigos derrotan al jugador. Un personaje derrotado deja de atacar. El jugador derrotado deja de desplazarse horizontalmente y de girar; se mantiene la gravedad.

### Conceptos y responsabilidades

- `PrototypeHealth` administra vida, daño efectivo y eventos locales para ambos personajes. Resuelve H02 con composición: recibir daño ya no exige el tipo de maniquí. No se agregan atributos, resistencias ni jerarquías de herencia.
- `PrototypeTrainingTarget` conserva la presentación de color/destello del objetivo y el reinicio local de H1 cuando no tiene ataque enemigo; delega salud al nuevo componente.
- `PrototypeMeleeHitQuery` reutiliza volumen frontal, deduplicación por golpe y comprobación de obstáculos. Se conservan las consultas que generan arreglos; H03 sigue pendiente de medición.
- `PrototypeAttackPhase` es la enumeración común. Las dos máquinas de estados conservan sus responsabilidades de entrada y decisiones; ambas exponen progreso para la animación procedural.
- `PrototypeEnemyAttack` decide cuándo iniciar, fija orientación y administra sus fases. Su recuperación deja una oportunidad para atacar; recibir daño no interrumpe un ataque salvo que cause derrota.
- `PrototypeAttackPresentation` puede representar jugador o enemigo mediante referencias explícitas; no aplica daño. La espada del enemigo es rosa y su preparación se anuncia con área violeta, impacto rojo y recuperación celeste.
- `PrototypeHitPresentation` escucha la salud de cualquiera de los dos; el jugador muestra -20 al recibir un golpe y el enemigo -25. La HUD muestra ambas vidas, fases, victoria o derrota.
- `PrototypeDuelSession` procesa R para cancelar acciones, restaurar vidas y posiciones, reiniciar gravedad y ajustar la cámara al jugador.

### Preparación y regresión

Fuera de Play, Tools > Soulslike > Preparar duelo inmóvil agrega las referencias y figuras necesarias a la arena existente. Repetir el menú no duplica enemigo, salud ni sesión. El spawn del jugador queda fuera de la distancia de activación. No hay persecución, esquiva, stamina, parry ni aprendizaje de patrones en este incremento.

El runner Tools/Validate-Prototype.ps1 conserva la regresión H1 y visual. Si la arena contiene el enemigo, también ejecuta PrototypeDuelValidation: activación por distancia, tiempo de aviso, daño único, dirección fija, evasión caminando, obstáculos, 30/120 FPS, derrota de ambos, bloqueo del movimiento y reinicio. Son pruebas de edición con tiempo controlado, previas a suites PlayMode y CI.

### Criterios de aceptación y prueba manual

1. Al arrancar, HUD con ambas vidas 100/100; enemigo en espera y arma rosa.
2. Acercarse y quedarse quieto: área violeta, golpe y vida del jugador 100 a 80, con -20. Mantenerse cerca permite repetir hasta derrota; entonces no puede moverse ni atacar.
3. Acercarse y apartarse durante la preparación: el enemigo golpea en la dirección inicial y no causa daño fuera de su volumen.
4. Atacar cerca: enemigo 100 a 75; cuatro golpes válidos causan victoria y detienen su ofensiva.
5. R durante preparación, tras victoria o tras derrota: restaura las dos vidas y posiciones, limpia señales y cancela golpes pendientes. Permite volver a moverse y atacar.

Riesgos a evaluar jugando: legibilidad del área violeta respecto del área del jugador, tiempo de reacción y correspondencia aproximada entre espada y volumen frontal. Ajustaremos parámetros después de probar el duelo antes de sumar otra acción defensiva.

Estado al 12 de octubre de 2026: scripts importados/compilados y arena de duelo guardada; repetir la preparación no duplicó enemigo, sesión ni salud. Pasaron la regresión H1, la visual y DUEL_VALIDATION_SUCCEEDED. La build desde la lista terminó con 0 errores y 486 advertencias, en 54,04 s; log Logs/DuelValidationBuild.log. El ejecutable inició durante ocho segundos en modo headless sin excepciones de scripts (Logs/DuelPlayerSmoke.log); esta comprobación no valida gráficos ni controles. Se capturaron e inspeccionaron aviso, impacto y recuperación (Logs/Duel y Logs/DuelCapture.log), sin UI OnGUI. Los cambios de las capturas se descartaron sin guardar. El 12 de octubre de 2026 el usuario confirmó margen suficiente para reaccionar al aviso y una ventana aprovechable durante recuperación. También confirmó victoria tras cuatro golpes, derrota tras cinco golpes enemigos con bloqueo de controles y reinicio completo con R. El incremento del primer duelo queda cerrado con validación automatizada, arranque y prueba manual.

### Observación de balance de la primera prueba

El usuario observó que el enemigo es más lento que el jugador y permite atacar y retirarse durante la preparación, además de la oportunidad durante recuperación. Considera válido el comportamiento para la dificultad de este prototipo. Se conservan los tiempos actuales: ataque del jugador de 0,65 s en total; enemigo con 0,8 s de preparación, 0,15 s de impacto y 1,1 s de recuperación. Esta aceptación valida el ejercicio inicial, no demuestra un balance definitivo. Reevaluar las oportunidades de ataque cuando se incorpore la primera acción defensiva, antes de aumentar dificultad o agregar variantes.


## Primera esquiva: reposicionamiento y controles lógicos

Objetivo: evitar un golpe mediante desplazamiento, conservando el compromiso temporal del combate. **Espacio** inicia una esquiva de **3 m en 0,25 s**, seguida de **0,20 s de recuperación**. Con WASD/flechas sigue la dirección relativa a la cámara, capturada al iniciar; sin dirección retrocede respecto de la orientación del jugador y conserva esa orientación. Durante desplazamiento y recuperación no se puede caminar, girar, atacar ni iniciar otra esquiva. Las paredes y obstáculos pueden acortar la distancia: no se recupera después el tramo bloqueado.

Esta versión no concede invulnerabilidad y todavía no usa stamina. Si el personaje permanece dentro del volumen del golpe, recibe daño incluso durante la esquiva. Ataque y esquiva se excluyen durante todas sus fases; no se encolan pulsaciones rechazadas y mantener el botón no repite la acción. Si E/clic y Espacio se pulsan en el mismo fotograma estando libre, tiene prioridad la esquiva. La muerte o el final del duelo cancelan la esquiva; R cancela acciones y restaura vida, posición, orientación y gravedad.

### Responsabilidades y conceptos

- `PrototypeCombat.inputactions`: mapa pequeño con Move, Attack, Dodge y Restart. Usa el Input System **1.20.0 ya instalado**, sin paquetes nuevos. Conserva WASD, flechas, E, clic y R; agrega Espacio. El asset de entrada de la plantilla permanece disponible.
- `PrototypePlayerInput`: adapta dispositivos a comandos lógicos. Clona el asset por jugador, habilita/deshabilita su mapa y expone la dirección y pulsaciones nuevas. Los sistemas de juego no deciden qué tecla usar.
- `PrototypePlayerDodge`: captura dirección y administra desplazamiento/recuperación. No mueve directamente el collider ni modifica salud. Señal celeste bajo el jugador al desplazarse y tenue durante recuperación; la cápsula todavía no tiene una animación de rodar.
- `PrototypePlayerMovement`: es el único componente que llama a `CharacterController.Move`. Combina caminar o desplazamiento de esquiva con gravedad; las colisiones físicas resuelven la posición final. Consume el tiempo de desplazamiento de cada frame, incluyendo pasos que cruzan fases, para conservar la distancia a 30/120 FPS. Si acaba recuperación dentro de un frame, caminar se reanuda en el siguiente.
- `PrototypePlayerAttack` y `PrototypeDuelSession`: consumen comandos lógicos y cancelan/bloquean acciones según sus estados. Se mantienen los tiempos del enemigo aceptados en el duelo anterior.

### Preparar, validar y probar

La arena guardada ya incluye la esquiva. Fuera de Play, **Tools > Soulslike > Preparar primera esquiva** permite reproducir la configuración; repetirlo no duplica componentes ni agrega colliders a la señal visual.

Con Unity y el juego cerrados, `powershell -ExecutionPolicy Bypass -File .\Tools\Validate-Prototype.ps1` comprueba la escena, regresión H1/visual, duelo y esquiva, y luego entra/sale de Play automáticamente para comprobar Input Actions con teclado/mouse virtuales. `-BuildFromSceneList` también compila Windows. El runner debe confirmar los marcadores de las suites y el resultado del proceso. Dentro del editor, **Tools > Soulslike > Validar proyecto y entrada en Play** ejecuta el mismo flujo sin cerrar Unity. Guardá tus cambios antes de correrlo; las posiciones y dispositivos de prueba se restauran y no se guardan los cambios de Play.

`PrototypeDodgeValidation` usa una escena temporal y un CharacterController real: distancia a 30/120 FPS y en un paso largo, dirección de cámara, retroceso, recuperación, exclusión con ataque, pared, gravedad, cancelación por derrota/victoria y reinicio. También compara dos golpes reales: esquivar lateralmente en espacio libre evita el golpe; una pared que impide salir del volumen permite recibirlo. `PrototypeInputValidation` prueba el mapa importado dentro de Play, pulsaciones simultáneas, teclas alternativas combinadas, diagonal, E/clic, R y ausencia de repetición/cola. La prueba ajusta temporalmente el foco de entrada para batch y lo restaura; no usa flags internos del paquete.

La build actual de prueba está en `Builds/WindowsSceneList/SoulslikeCombatPrototype.exe`; conservar sus archivos y carpeta Data juntos.

Protocolo manual de aceptación (aprobado por el usuario; observaciones al final):

1. Con la ventana enfocada, pulsar Espacio con dirección y sin dirección. Comprobar dirección, retroceso y las dos señales; mantener Espacio debe producir una sola acción.
2. Esquivar contra una pared y un cubo: debe detenerse sin atravesarlos ni desplazarse después para compensar.
3. Durante ataque, intentar esquivar; durante esquiva/recuperación, intentar atacar. No debe cancelar ni ejecutar después la pulsación rechazada.
4. Acercarse al enemigo, esperar su aviso violeta y esquivar lateralmente. En espacio libre se puede evitar daño; si un obstáculo impide salir del área, se puede recibir el golpe.
5. Usar R durante la esquiva y tras derrota: restaura el duelo y permite repetir acciones. Confirmar también que la victoria sigue requiriendo cuatro golpes y la derrota cinco golpes enemigos.

La opinión del jugador sobre distancia, recuperación y sincronización queda registrada en el cierre al final. La aceptación funcional no demuestra el balance definitivo de esta nueva acción.

Referencias: [InputAction](https://docs.unity3d.com/Packages/com.unity.inputsystem@1.20/api/UnityEngine.InputSystem.InputAction.html) y [CharacterController.Move, Unity 6.3](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/CharacterController.Move.html). Las firmas se comprueban con el paquete local 1.20.0; la documentación web de la familia 1.20 puede mostrar una revisión posterior.

Estado al 12 de octubre de 2026: escena guardada y preparación repetida sin duplicados. El runner completo con -BuildFromSceneList confirmó H1, visual, duelo, esquiva e Input Actions en Play, con salida 0 (Logs/ProjectValidationBuild.log). La build de Windows terminó con 0 errores y 486 advertencias en 11,18 s. En las pruebas de edición se avanza Physics.Simulate para publicar las poses nativas del CharacterController antes de consultar golpes; el modo se restaura en finally. El runner conserva el archivo original de ajustes físicos, incluso si Unity actualiza su formato al tocar el modo de simulación. Se capturaron e inspeccionaron las señales celeste y tenue (Logs/Dodge y Logs/DodgeCapture.log); las capturas no incluyen UI OnGUI y sus posiciones se descartaron sin guardar. Se observó durante ocho segundos el arranque headless del ejecutable previo con el mismo código de juego sin excepciones de scripts (Logs/DodgePlayerSmoke.log); no valida gráficos ni controles humanos. El usuario aprobó posteriormente la prueba jugable; ver el cierre y las observaciones de sincronización a continuación.


### Cierre de la prueba manual y observaciones

El 12 de octubre de 2026 el usuario aprobó la implementación y las pruebas jugables de la primera esquiva. Considera coherente la recuperación de 0,20 s y posiblemente excesiva la distancia de 3 m. Se conservan los valores probados para cerrar este incremento; reducir la distancia será un ajuste de balance separado, que deberá volver a probarse.

El usuario observó que algunas esquivas al último momento reciben daño y sospecha un desfase entre animación e impacto, pero no pudo identificar el fotograma ni confirmar un error. No se registra esa sincronización como validada. Sin invulnerabilidad, iniciar la acción no garantiza evitar un golpe: hay que salir físicamente del volumen antes de la consulta de daño.

La inspección del código encuentra dos factores concretos que deben distinguirse en una reproducción:

- `PrototypePlayerDodge` procesa la pulsación en Update con orden -120; `PrototypeEnemyAttack` comprueba impactos con orden -90; `PrototypePlayerMovement` aplica el desplazamiento con orden predeterminado 0. El golpe puede consultar la posición anterior al desplazamiento de ese mismo fotograma. Eso describe el orden actual; no demuestra que haya causado el caso observado.
- El daño se consulta al entrar en Active y durante esa ventana, usando una caja frontal fija. `PrototypeAttackPresentation` representa en LateUpdate el recorrido de la espada durante Active. Ambos leen la misma fase, pero la caja no sigue el filo: puede haber daño cuando la espada apenas empieza el recorrido. La representación aproximada ya era una limitación del prototipo; falta comprobar su legibilidad en esta situación.

La versión de esquiva queda aprobada con estas observaciones abiertas. Stamina sigue pendiente para limitar la repetición de acciones; no se agregó en este cierre.

### Próximo hito: comprobar sincronización de esquiva e impacto

Objetivo: reproducir y explicar una esquiva tardía antes de modificar reglas de daño o añadir stamina. Conceptos: orden de Update/LateUpdate, posición que consulta la física, tiempo de cada fase y diferencia entre volumen de daño y representación visual.

Alcance propuesto: una herramienta de diagnóstico del combate con tiempo reducido, visualización del volumen activo y registro del fotograma/posición al iniciar esquiva y aplicar daño. Debe permitir comparar un intento con salida del volumen y otro que permanece dentro. Dificultad principal: distinguir lectura visual, orden de ejecución y tiempo físico; las pruebas anteriores con pasos controlados no reprodujeron el orden completo de un frame real.

Criterios de aceptación: poder identificar qué posición y fase determinaron cada golpe; reproducir los dos casos a 30 y 120 FPS; comprobar que el volumen y la señal visible comunican la misma ventana. Si se confirma una inconsistencia, corregirla con una prueba de regresión y nueva validación jugable. Después se decidirá el ajuste de distancia y se introducirá stamina con costos de ataque/esquiva y regeneración, en su propio incremento.


## Dirección del combate: parry para jugador y enemigo

El 12 de octubre de 2026 el usuario confirmó que el parry debe formar parte del combate de **ambos** personajes. Es una mecánica central del proyecto. No requiere implementar primero un bloqueo tradicional: bloquear y hacer parry pueden ser acciones distintas. El primer parry se implementará con una ventana temporal y un resultado observable contra un golpe definido; las reglas de recepción deberán resolver el parry antes de descontar vida.

La ruta incremental queda: diagnóstico/sincronización → primer parry del jugador → integración de stamina y ajustes de combate → primer parry del enemigo → reconocimiento de ofensivas repetidas del «Duelista». Los dos parrys deben tener validación jugable propia. La adaptación del Duelista se mantiene como ampliación posterior, cuando existan decisiones ofensivas suficientes para que reconocer repeticiones tenga sentido. Este incremento no implementa todavía parry, guardia ni stamina.

## Diagnóstico del combate: volumen, posiciones y registro

Objetivo: distinguir una esquiva que permanece dentro del alcance, una pulsación rechazada y un impacto consultado antes del movimiento del mismo fotograma. También permite comparar la ventana activa con la representación de la espada. Se conservan distancia, tiempos, daño y orden de ejecución de la versión de esquiva aprobada.

### Usar la herramienta

La arena guardada incluye `PrototypeCombatDiagnostics`. Se inicia oculto y con velocidad normal. **Tools > Soulslike > Preparar diagnóstico de combate**, fuera de Play, reproduce sus referencias sin reconstruir el duelo; repetir el menú no duplica el componente.

- **F3** muestra/oculta el diagnóstico y habilita el registro. El contorno amarillo representa el volumen del futuro golpe durante preparación; rojo durante Active; celeste durante recuperación. Solo Active causa daño. El volumen 3D usa los mismos parámetros que la consulta física; la espada conserva su representación aproximada.
- **F4** alterna tiempo normal y 0,2x. Ajusta también el paso físico para conservar su frecuencia en tiempo real. Al salir del modo lento o deshabilitar el componente se restauran exactamente los valores previos de `timeScale` y `fixedDeltaTime`. Ocultar con F3 conserva el modo de velocidad elegido; se puede volver a normal con F4.
- Al recibir daño enemigo con F3 activo, se conserva el volumen de esa consulta en **naranja** y una **cruz blanca** en la posición del jugador en el momento del daño. La cruz es una referencia de posición, no la forma del collider. La HUD compara esa posición con la posición al cerrar el frame, e indica fase/progreso de la espada.
- **R** restaura el duelo y limpia registro y marcas históricas. Mantiene visible el diagnóstico y el modo lento para repetir la prueba.
- **F5**, con F3 activo, exporta los últimos 128 eventos a un archivo `.log` nuevo. La HUD muestra el nombre y la consola/Player.log la ruta completa con `COMBAT_TRACE_EXPORTED`. En este proyecto Windows, la carpeta habitual es `%USERPROFILE%\AppData\LocalLow\DefaultCompany\SoulslikeCombatPrototype\CombatDiagnostics`. No modifica un registro previo ni envía archivos por red.

El registro contiene fotograma, tiempo de juego y real, esquiva aceptada/rechazada, daño efectivo, posición del jugador y volumen consultado. En LateUpdate agrega la posición final, fase/progreso enemigo y orientación local de su espada, después de actualizar la presentación. Las líneas con el mismo `f=` pertenecen al mismo frame. Si la fase cambia dentro de un paso largo, la pose final puede corresponder a una fase posterior a la que produjo el daño; se conservan ambas observaciones.

### Responsabilidades y límites

`PrototypeMeleeHitQuery` publica un snapshot al terminar cada consulta, con volumen y daño efectivo. `PrototypePlayerDodge` publica intentos aceptados/rechazados. `PrototypeCombatDiagnostics` observa esos eventos y los de vida, dibuja contornos sin colliders, conserva un registro circular y controla únicamente el modo de tiempo de diagnóstico. `PrototypePlayerInput` agrega Diagnostics, SlowMotion y ExportTrace al mapa existente; mantiene las acciones y GUID anteriores.

La herramienta explica la regla actual: el enemigo consulta daño antes de que el motor aplique desplazamiento en ese frame; pulsar Espacio no concede invulnerabilidad. No determina por sí sola si esa regla es la más justa ni prueba cuál fue el caso reportado por el usuario. Para decidir un cambio se necesita una reproducción concreta. La caja fija tampoco demuestra contacto del filo: esa diferencia continúa siendo una limitación visual conocida.

### Validación y prueba jugable

El runner `Tools/Validate-Prototype.ps1` conserva las suites previas y exige `DIAGNOSTICS_VALIDATION_SUCCEEDED`. Dentro de Play, la prueba de entrada verifica F3/F4/F5 y que mantener una tecla no repita un toggle. `PrototypeDiagnosticsValidation` ejecuta pasos controlados de 1/30 y 1/120 s en el orden actual: intento de esquiva → consulta enemiga → movimiento → pose → observación. Compara posición al daño y al cierre, reproduce salida temprana sin daño y esquiva bloqueada con daño, exporta un registro en Logs, verifica R, restauración de tiempos previos, suscripciones y límite de 128 eventos. Estos pasos no equivalen a una grabación de la interacción humana ni fijan los FPS reales de la aplicación.

Prueba manual para cerrar el incremento:

1. Activar F3 y F4, reiniciar con R. Acercarse al enemigo: el contorno amarillo anuncia preparación y el rojo indica la ventana activa. Comparar la espada con el volumen.
2. Esquivar lateralmente durante preparación con espacio libre: salir del volumen permite evitar el daño. Comprobar que la acción se registra como aceptada.
3. Repetir esquivando muy tarde y, por separado, contra un obstáculo. Si se recibe daño, revisar cruz blanca, contorno naranja y las dos posiciones de la HUD. Guardar con F5 antes de usar R.
4. Intentar esquivar durante un ataque y comprobar «rechazada» en el registro exportado. Mantener F3/F4 no debe alternar repetidamente.
5. Volver a velocidad normal con F4, ocultar F3 y comprobar que el duelo conserva su comportamiento. R limpia las marcas y permite repetir.

Referencias de Unity 6.3: [Time.timeScale](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/Time-timeScale.html) y [LineRenderer](https://docs.unity3d.com/6000.3/Documentation/ScriptReference/LineRenderer.html).

Estado al 12 de octubre de 2026: preparación repetida sin duplicados. La primera prueba encontró un recurso de render creado prematuramente; se corrigió su inicialización. Los contornos usan un material URP/Unlit para que su color sea legible sin depender de la luz. La build final terminó con 0 errores y 486 advertencias en 17,83 s (Logs/DiagnosticsValidationBuild.log). En la misma ejecución se confirmaron H1, visual, duelo, esquiva, entrada en Play y DIAGNOSTICS_VALIDATION_SUCCEEDED con salida 0. Las muestras registraron daño usando x=300,000 y cierre en x=300,400 con dt=1/30, y x=300,100 con dt=1/120. Es evidencia de los pasos controlados en el orden actual, no una reproducción del intento original del usuario. El ejecutable final inició ocho segundos en headless sin excepciones de scripts (Logs/DiagnosticsPlayerSmoke.log). El usuario comprobó el diagnóstico y exportó registros humanos; se aceptó la herramienta y se observó un desfase visual de impacto, abordado en el incremento de sincronización siguiente.

Se capturaron e inspeccionaron los contornos de preparación e impacto con URP (Logs/Diagnostics y Logs/DiagnosticsCapture.log); la captura exige daño real de 20 y su registro. Estas imágenes no incluyen la HUD OnGUI. Las marcas históricas tienen profundidad y pueden quedar tapadas por el personaje hasta apartarse; las posiciones numéricas siguen disponibles en HUD/registro. Las posiciones de captura se descartaron sin guardar y se conservó el archivo de ajustes físicos.


## Corrección de la señal visual al comenzar el daño

El registro humano `combate-20261013-013522-519-b11269.log` reprodujo el problema: la esquiva se aceptó en f=6673; en f=6674 el jugador recibió 20 de daño al entrar en Active con progreso 0. La pose al cerrar ese frame tenía progreso activo 0,007 y la espada casi conservaba su orientación elevada. El jugador seguía dentro del alcance de la caja con su cápsula, así que el daño cumplía la regla física, pero la animación comunicaba el impacto demasiado tarde.

`PrototypeAttackPresentation` ahora divide Windup en dos tramos: el primer 70 % levanta el arma desde reposo; el último 30 % la acerca a una pose de contacto. Active empieza en esa pose, continúa hasta la posición final del recorrido y Recovery vuelve al reposo. SmoothStep y Slerp mantienen continuidad entre tramos y fases. Esto se aplica tanto al jugador como al enemigo.

La pose de contacto aproxima la hoja al frente y a la altura del volumen anunciado: posición local (0,65; -0,15; 0,5), orientación (5°; -20°; 0°). Es configurable en el componente. No modifica la caja ni convierte la hoja en un collider. Tampoco altera daño, duración de fases, orden de Update, distancia de esquiva, recuperación o reglas de invulnerabilidad.

### Validación de la corrección

`PrototypeImpactSyncValidation` verifica ambos atacantes en una escena temporal. Comprueba que la preparación no causa daño, que la pose anterior al impacto converge a contacto, que al comenzar Active con progreso 0 se aplica el daño esperado y la espada está en contacto, que no hay salto al recuperar y que se restaura reposo. También cruza la frontera de daño con pasos de 1/30 y 1/120 s. El runner exige `IMPACT_SYNC_VALIDATION_SUCCEEDED` junto con las suites anteriores.

El reloj de gameplay sigue siendo la autoridad: la presentación lee fase/progreso y no aplica daño. Las pruebas comparan la pose renderizable después de actualizar la presentación, no contacto físico exacto entre hoja y cápsula. Un frame largo puede atravesar varias fases; se conserva el comportamiento de daño existente y no se agrega un segundo reloj de animación.

Prueba jugable pendiente:

1. Activar F3/F4, acercarse y observar el arma rosa: se eleva, se aproxima durante amarillo y alcanza contacto cuando el volumen pasa a rojo y puede aparecer -20.
2. Repetir con el ataque del jugador: la hoja blanca debe alcanzar la pose de contacto al comenzar la ventana roja y aplicar -25.
3. Esquivar temprano en espacio libre debe seguir evitando daño. Una esquiva tardía que conserva solapamiento puede recibirlo; esta corrección visual no concede inmunidad ni cambia el orden de movimiento/impacto.
4. Comprobar recuperación, R y ciclos repetidos sin deriva. Si persiste una lectura confusa, guardar F5 antes de R para revisar el nuevo registro.

Después de aceptar la señal de impacto, el siguiente incremento será el primer parry del jugador contra este golpe definido. El parry del enemigo y la adaptación del Duelista conservan su lugar en la ruta del proyecto.

Estado al 12 de octubre de 2026: Unity importó la corrección y confirmó IMPACT_SYNC_VALIDATION_SUCCEEDED para jugador/enemigo, además de las suites H1, visual, duelo, esquiva, diagnóstico e Input Actions en Play. El runner con -BuildFromSceneList terminó con salida 0 y build Windows con 0 errores, 486 advertencias y 44,75 s (Logs/ProjectValidationBuild.log). El ejecutable inició ocho segundos en headless sin excepciones de scripts (Logs/ImpactSyncPlayerSmoke.log); esto no valida la percepción visual del usuario. La percepción de esta señal aún necesita confirmación humana detallada. Posteriormente el usuario solicitó continuar con el primer parry del jugador; la prueba de ese incremento incluye volver a observar el instante del impacto.

La inspección visual llevó a mantener el agarre de contacto hacia el costado (x local 0,65), para evitar que la aproximación quedara excesivamente oculta por las cápsulas. Se volvió a ejecutar la regresión completa y compilar esa versión; el tiempo de daño se conserva.

Se capturaron e inspeccionaron elevación, aproximación con vida 100 y primer impacto con vida 80 (Logs/ImpactSync y Logs/ImpactSyncCapture.log). La transición de pose es continua entre las dos últimas capturas y el contorno cambia de amarillo a rojo. Las imágenes no incluyen HUD OnGUI y las cápsulas todavía pueden ocultar parte de la hoja al tocarse. Las posiciones de captura se descartaron sin guardar y se conservó DynamicsManager.asset.

## Primer parry del jugador

El usuario solicitó continuar con el parry del jugador después de incorporar la sincronización de impacto. La comprobación humana detallada de esa sincronización no quedó registrada; la nueva prueba jugable deberá observar también el instante de daño y desvío.

Objetivo del incremento: anticipar un golpe frontal del enemigo inmóvil, pulsar una defensa breve y conseguir una oportunidad de contraataque. El jugador decide cuándo atacar después del desvío. El daño del contraataque sigue siendo 25.

### Reglas y controles

Q o clic derecho inicia una acción por pulsación. Mantener el botón no sostiene defensa ni repite la acción. El intento tiene 0,06 s de preparación amarilla, 0,18 s activos en azul y 0,30 s de recuperación. Al acertar se entra inmediatamente en esa recuperación con señal blanca. La placa es una señal provisional sin collider; el volumen del enemigo continúa siendo la autoridad sobre el alcance.

Durante Active se pueden desviar los golpes del `PrototypeEnemyAttack` actual dentro de un cono frontal de ±60° respecto al jugador. El atacante debe estar delante, alcanzar realmente la cápsula y tener el camino libre de obstáculos. Pulsar sin contacto no produce un acierto. En preparación, recuperación, por detrás o fuera del cono, se recibe el daño habitual si el golpe alcanza. La dirección queda fijada por la orientación al iniciar la acción; conviene mirar al enemigo antes de pulsar.

Un acierto evita los 20 de daño, cancela el golpe completo y deja al enemigo expuesto durante 1,20 s, con indicador naranja. Tras recuperar, pulsar E o clic izquierdo dentro de alcance ejecuta el contraataque normal. El enemigo retoma su preparación al terminar la apertura. No hay daño ni contraataque automático.

Ataque, esquiva y parry se excluyen durante toda su ejecución y recuperación. Caminar y girar se bloquean durante el parry; la gravedad continúa. Cuando llegan las tres pulsaciones en un mismo frame libre, el orden explícito es parry → esquiva → ataque. Una pulsación rechazada no se encola. R, derrota, victoria o deshabilitar el componente cancelan defensa; R también elimina la apertura enemiga y restablece la vida.

Estos valores son iniciales para aprender timing. Se conservan los tiempos, alcance y daño existentes del ataque y esquiva. El parry del enemigo, stamina y adaptación del Duelista siguen en los incrementos siguientes de la ruta acordada.

### Decisión técnica

Se eligió interceptar la recepción en `PrototypeMeleeHitQuery` después de comprobar solapamiento, obstáculos y daño único, antes de `TakeDamage`. Hacer inmune al jugador desde su componente de vida permitiría proteger golpes fuera del cono y mezclaría vida con decisiones defensivas. Una animación tampoco debe decidir por separado cuándo se acepta el golpe: defensa y ataque usan fases de gameplay.

`PrototypePlayerParry` administra la ventana y orientación; `PrototypeEnemyAttack.ReceiveParry` cancela su ataque y administra la apertura; el motor conserva la única llamada a `CharacterController.Move`. La integración reconoce explícitamente el ataque enemigo de este prototipo. Al incorporar parry del enemigo u otros tipos de daño habrá que revisar esa recepción compartida con casos concretos, sin añadir ahora un sistema genérico de combate.

La apertura se representa como `IsExposed` y un temporizador independiente de las fases del golpe; durante ella `Phase` es Idle, pero el enemigo está ocupado. Al cancelar dentro de una consulta se sale inmediatamente de la lista de contactos y del avance del ataque. El tiempo sobrante de un frame que cruzó Windup no se resta a la apertura recién creada: evita un golpe diferido o consumir accidentalmente la recompensa del parry.

El sistema muestrea estados discretos por frame; una pulsación que aparece después de una consulta ya ejecutada no revierte daño. Un frame muy largo que atraviesa toda la ventana defensiva puede perderla. Las señales son geometría provisional, y el arma/volumen mantienen la aproximación documentada; la validación del timing percibido corresponde a la prueba jugable.

### Preparación y diagnóstico

La escena guardada incluye un solo `PrototypePlayerParry` y una «Señal de parry» sin collider. `Tools > Soulslike > Preparar parry del jugador`, fuera de Play, reproduce la configuración sin reconstruir el duelo. El mapa existente agrega Parry con Q/clic derecho y conserva las acciones, bindings y GUID previos.

F3 habilita diagnóstico, F4 reduce el tiempo a 0,2x y F5 exporta el registro antes de R. El registro distingue PARRY aceptado/rechazado, PARRY exitoso, daño recibido con fase defensiva y consultas con `parrys=1; daño=0` (los campos no necesariamente son consecutivos). Incluye posición/frente al intentar y el estado de apertura enemiga. Al desviar, la consulta cancela el ataque antes de publicar el resultado, por lo que el enemigo aparece en Idle con apertura activa: esa fase registrada es posterior a la recepción.

### Validación y aceptación jugable

`PrototypeParryValidation` se ejecuta en Play sobre los componentes reales de la arena, en una zona temporal con suelo y obstáculo. Usa el mismo orden defensa → ataque → motor con pasos de 1/30 y 1/120 s, prueba acierto frontal, fallo temprano/tardío, cono y espalda, pared, colliders duplicados, ausencia de daño diferido, contraataque manual de 25, exclusión de acciones, gravedad, R, muerte/victoria y overflow. `PrototypeInputValidation` comprueba Q/clic derecho con dispositivos virtuales, prioridad simultánea, mantener sin repetir, rechazo sin cola y R. Finalmente restaura escena, dispositivos, tiempo y simulación; el runner conserva los bytes originales de los ajustes físicos.

Para validar jugabilidad:

1. Abrir `Assets/Scenes/PrototypeArena.unity` y entrar en Play, o ejecutar `Builds/WindowsSceneList/SoulslikeCombatPrototype.exe`. Enfocar la ventana. Acercarse y mirar al enemigo verde; usar R para empezar de nuevo.
2. Activar F3/F4 para practicar. Durante el final del aviso violeta, pulsar Q: la placa pasa de amarillo a azul. Si el impacto llega durante azul, aparece blanco/ACIERTO y naranja en el enemigo; la vida debe conservarse. El instante inicial para probar es aproximadamente 0,1 s de juego antes del golpe, no el comienzo del aviso. A 0,2x los tiempos aparentes duran cinco veces más.
3. Esperar la recuperación del jugador y pulsar E/clic izquierdo: el enemigo pierde 25 mientras sigue expuesto. Confirmar que el enemigo vuelve a preparar otro golpe después de la apertura.
4. Repetir pulsando demasiado temprano, demasiado tarde y mirando en sentido contrario. Si el golpe alcanza, deben perderse 20; el intento fallido también obliga a recuperar. Guardar F5 antes de R si el resultado parece incoherente.
5. Mantener Q/clic derecho produce una sola acción. Intentar Q durante ataque/esquiva y ataque/esquiva durante parry: las pulsaciones rechazadas no se ejecutan después. R durante un intento y durante apertura limpia ambos. Volver con F4 a velocidad normal y confirmar victoria, derrota y reinicio.

Cierre pendiente: confirmar al menos un acierto con vida intacta y contraataque, fallos por tiempo/espalda, exclusión/reinicio y legibilidad a velocidad normal. Después se revisará el balance de la ventana y se continuará con el siguiente incremento acordado.

Estado técnico: Unity confirmó PARRY_CAPTURE_SUCCEEDED y PARRY_VALIDATION_SUCCEEDED junto con H1, visual, sincronización, duelo, esquiva, diagnóstico e Input Actions en Play, con salida 0 (Logs/ParryFinalValidationBuild.log). La build de Windows terminó con 0 errores, 486 advertencias y 33,34 s. Se inspeccionaron capturas URP de preparación, ventana activa y acierto con salud 100 (Logs/Parry); muestran azul, blanco y apertura naranja, pero no incluyen HUD OnGUI. La preparación repetida dejó un solo componente y las posiciones de captura se descartaron sin guardar. La aceptación jugable del parry está pendiente.

La versión final también expresa las fases del parry en español y muestra al enemigo como Expuesto durante la apertura. El runner completo con -BuildFromSceneList aprobó nuevamente todas las suites con salida 0 y compiló Windows con 0 errores, 486 advertencias y 34,75 s (Logs/ProjectValidationBuild.log). El mismo código de combate inició ocho segundos en headless sin excepciones de scripts (Logs/ParryPlayerSmoke.log); ese arranque no comprueba gráficos ni interacción humana.

## Revisión de contacto y precisión del parry — 13 de octubre de 2026

El usuario confirmó el funcionamiento básico y pidió revisar la permisividad antes de ajustar dificultad. Se auditó la escena guardada, no solo los valores por defecto de C#: preparación defensiva 0,06 s, ventana 0,18 s, recuperación 0,30 s, cono ±60°; enemigo con 0,80 s de preparación y 0,15 s de ejecución. Los registros humanos disponibles eran anteriores al primer parry; no se usan como evidencia de esta observación nueva.

### Hallazgos y causa de la permisividad

El código anterior no concedía un acierto por pulsación ni durante Windup del enemigo. Exigía una consulta de daño en Active, solapamiento con la cápsula, orientación frontal y ausencia de obstáculos. Sin embargo, la defensa seguía activa desde 60 hasta 240 ms después de pulsar. Por eso una anticipación nominal de 220 ms podía ser un acierto correcto para esa configuración. Era una decisión temporal permisiva, no evidencia de inmunidad concedida por el botón.

Había además tres limitaciones concretas: la placa azul no tenía volumen defensivo y la cápsula servía como sustituto de contacto defensivo; se adelantaba el reloj del nuevo intento con el delta anterior a la pulsación; y se consultaba la fase defensiva final del frame, aunque el enemigo había alcanzado impacto dentro de ese intervalo. Esta última diferencia podía aceptar una defensa iniciada después del contacto o rechazar una que terminó después de un contacto válido. El adelanto del reloj altera los límites dependiendo del paso; no demuestra por sí solo una ventana más amplia para toda anticipación.

La presentación cancelaba inmediatamente la espada a reposo al desviar. No se mostraba la pose de contacto del primer frame exitoso, lo que podía dar la impresión de neutralizar un ataque antes de que llegara. Las pruebas anteriores ejercitaban fases preparadas por separado; sus casos a 30/120 FPS no barrían anticipaciones idénticas sobre una línea temporal compartida.

### Solución y parámetros

La consulta de ataque revisa primero el trigger de defensa frontal y, si no hay desvío válido, consulta daño corporal. Un trigger inactivo nunca amplía la cápsula que recibe daño. El desvío requiere contacto entre el mismo volumen de ataque que gobierna el daño y el volumen defensivo, además de tiempo, cono, obstáculos y golpe aún no consumido. El contacto del escudo puede cancelar el ataque antes de alcanzar la cápsula; un contacto lateral con la cápsula que no alcance defensa sí causa daño. Se publican las transformaciones físicas antes de consultar para mantener la posición del trigger hijo actualizada.

La defensa guarda el comienzo y fin de su ventana en segundos de juego. El enemigo comunica el instante lógico en que cruza a Active dentro del frame; durante Active, contactos nuevos se muestrean al comienzo del siguiente intervalo. La recepción compara ese instante con el intervalo defensivo [inicio, fin), no con la fase final de la pantalla. La pulsación empieza al ser procesada y no recibe tiempo de preparación anterior. El fallo por ventana agotada se notifica después de resolver los contactos del frame, evitando anunciar fallo y luego éxito para el mismo evento válido.

Primero se reproduce la configuración de 180 ms con las nuevas reglas: 220 ms de anticipación sigue siendo válido si hay contacto frontal. Esto aísla la permisividad temporal de las correcciones espaciales y de reloj. Después se propone **120 ms activos**, conservando 60 ms de preparación y 300 ms de recuperación. El intervalo nominal aceptado pasa de (60; 240) ms a (60; 180) ms antes del contacto. Los extremos reales dependen de los límites inclusivo/exclusivo del evento y la precisión numérica; las pruebas de balance usan márgenes de 5 ms, no igualdades ambiguas.

La referencia visual de este ataque es su descenso: empieza al 70 % de 0,8 s, en t=0,56 s. Con 180 ms activos, el intento más temprano podía comenzar prácticamente al inicio del descenso. Con 120 ms, el primer intento válido se desplaza hacia t=0,62 s, después de que comenzó. La nueva ventana conserva aproximadamente 3,6 frames a 30 FPS, 7,2 a 60 y 14,4 a 120. El resultado se calcula en segundos; esas cantidades describen su tamaño, no un requisito de acertar un frame concreto.

Es un valor inicial fundamentado para esta animación, no una demostración de balance óptimo ni de equivalencia con otro juego. `Windup Duration`, `Active Duration`, `Recovery Duration` y `Half Angle` siguen editables en `PrototypePlayerParry`, desde Inspector fuera de Play. La migración de la escena aplica 120 ms una sola vez al añadir el volumen; volver a preparar conserva ajustes temporales posteriores. Los cambios de Inspector durante Play se descartan al salir. Volver a 180 ms permite comparar ambos valores sin cambiar arquitectura.

El volumen frontal inicial mide 0,8 × 1,3 × 0,3 m y está centrado localmente en (0; 0,1; 0,65). Un contorno representa sus dimensiones reales; el rombo lateral indica el estado de la acción y no es un collider. Se conserva este indicador fuera de la cápsula para que sea legible cuando el personaje oculta parte del volumen frontal. No se agrega un sistema genérico de combate ni un Animator: se conserva la caja de ataque existente y el cono frontal. El tipo de ataque enemigo actual sigue siendo el único que admite parry.

### Feedback

Amarillo indica preparación, azul defensa activa, blanco desvío confirmado y rojo recuperación fallida. La HUD distingue ACIERTO y FALLO. Un acierto muestra primero la pose de contacto y luego un retroceso de 0,12 s de la espada durante la apertura; ya no vuelve inmediatamente al reposo. La presentación lee gameplay y no aplica daño.

`PrototypeParryFeedback` escucha éxito/fallo y reproduce dos sonidos sintéticos provisionales de 0,12 s: uno agudo para acierto y otro grave para fallo. Tiene volumen ajustable en Inspector y conserva una sola notificación por resultado. R y cancelaciones del duelo no producen un falso resultado. Estos tonos sirven para comprobar la lectura del evento y podrán sustituirse por recursos artísticos después.

F3/F5 registra intento, resultado, instante lógico de contacto y anticipación del acierto en milisegundos. El tiempo global de la línea sigue siendo el momento del callback; el campo `contacto t` permite distinguir el evento interpolado dentro del frame. El fallo distingue golpe recibido y ventana agotada sin desvío. Guardar F5 antes de R conserva el caso.

### Pruebas y límites

El barrido de `PrototypeParryValidation` usa el mismo impacto en t≈0,8 s y las mismas pulsaciones con pasos de 30, 60 y 120 FPS. Compara 300, 220, 185, 175, 120, 65, 55 y 20 ms antes del golpe, más 5 ms después. Con 120 ms activos, 175/120/65 ms deben acertar y el resto fallar si hay alcance. También prueba ventana que termina dentro del frame después de contacto válido, ventana que empieza después del contacto, hitch que cruza ambas, ataques sucesivos, ausencia de reversión de daño, cono/espalda, pared delante del volumen, cápsula alcanzada sin defensa, defensa alcanzada sin cápsula, colliders duplicados, contraataque, gravedad, controles, muerte/victoria/R y notificación única. Los clips deben existir y contener muestras no silenciosas.

Se conservan las suites H1, visual, sincronización de impacto, duelo, esquiva, diagnóstico y entrada real del Input System en Play. El runner exige `PARRY_TIMING_VALIDATION_SUCCEEDED` además de los marcadores anteriores. Los pasos con reloj explícito validan la regla temporal; no son mediciones de latencia de un teclado real ni fijan el FPS de la aplicación. La entrada física todavía se recibe por frame, por lo que puede tener hasta un intervalo de muestreo de latencia. El contacto espacial se consulta discretamente: no se promete detección continua del filo ni inmunidad al tunneling de objetos rápidos. El enemigo actual es inmóvil y el parry bloquea locomoción, condiciones adecuadas para aislar este incremento.

### Validación humana pendiente

1. Usar F3/F4 para reconocer elevación y descenso. Intentar durante elevación debe agotar la defensa antes del golpe; intentar durante descenso puede acertar. Revisar contorno azul, contacto de espada, blanco/apertura naranja y sonido agudo sin perder vida.
2. Intentar cuando el golpe ya llegó: perder 20 y no recuperar vida ni obtener apertura. Intentar demasiado temprano debe emitir fallo y permitir el daño posterior si se mantiene el alcance.
3. Acertar y contraatacar tras recuperar: el enemigo pierde 25. Repetir otro ciclo, comprobar que el intento anterior no protege el siguiente ataque y que mantener Q no repite defensa.
4. Volver a velocidad normal con F4 y comparar 120 ms con 180 ms desde Inspector, fuera de Play. Mantener iguales preparación, ataque y distancia; anotar intentos/aciertos y qué señal visual se usó. Validar legibilidad de éxito/fallo y volumen de los tonos.
5. Comprobar espalda, exclusión de ataque/esquiva y R. Si hay ambigüedad, exportar F5 antes de reiniciar.

El incremento técnico debe cerrarse con resultados automáticos reales y esta prueba jugable. La mejora por práctica y el balance final requieren sesiones humanas; una suite puede garantizar reglas y límites, pero no demostrar aprendizaje del jugador.

Si un frame atraviesa todo el intento, su ventana se conserva hasta resolver los contactos antes de aceptar otra acción. Una prueba verifica que una pulsación nueva no reemplace un desvío histórico válido ni se encole tras ese frame.

Estado técnico al 13 de octubre de 2026: Unity confirmó todas las suites anteriores, PARRY_VALIDATION_SUCCEEDED y PARRY_TIMING_VALIDATION_SUCCEEDED con salida 0; la build Windows terminó con 0 errores, 486 advertencias y 56,40 s (Logs/ParryTimingFinalValidationBuild.log). Se capturaron e inspeccionaron preparación, defensa, primer contacto desviado, reacción y ventana agotada con URP (Logs/ParryTiming). Las capturas no incluyen HUD OnGUI ni prueban que los tonos se escuchen adecuadamente; los clips sí se comprobaron en Play como no silenciosos. La escena se preparó repetidamente sin duplicar defensa y las posiciones de captura se descartaron sin guardar. Se conservaron paquetes, versiones y ajustes físicos. La confirmación humana del timing y del feedback a velocidad normal queda pendiente.

## Stamina del jugador — 13 de octubre de 2026

El usuario aprobó el ajuste de precisión del parry y autorizó continuar con el siguiente incremento. Se cierra funcionalmente ese ajuste con su aceptación; no se registra un experimento cuantitativo de aprendizaje ni un balance definitivo de la ventana.

Objetivo: introducir una decisión de presupuesto entre ataque, esquiva y parry. Conceptos: recurso separado de las acciones, costo atómico al aceptar entrada, recuperación temporal, feedback de rechazo y pruebas de comportamiento. El enemigo mantiene el duelo inmóvil y el parry del enemigo conserva su lugar como siguiente sistema de combate después de validar stamina.

### Valores iniciales y reglas

| Parámetro | Valor |
|---|---:|
| Máximo | 100 |
| Ataque | 20 |
| Esquiva | 30 |
| Parry | 15 |
| Regeneración | 25 por segundo de juego |
| Pausa de regeneración | 0,8 s tras dejar de actuar |

Los valores permiten cuatro ataques para derrotar al enemigo con 20 puntos restantes si no se gastan defensas, o tres esquivas consecutivas que dejan 10. Parry más contraataque requiere reservar 35: un desvío exitoso ofrece una apertura, pero no devuelve energía ni garantiza que alcance para atacar. Esta diferencia hace más económica la defensa que exige timing que la esquiva, con su riesgo correspondiente. Son hipótesis de balance del prototipo, no valores finales.

Una acción aceptada paga al iniciar, incluso si falla el contacto, es interrumpida por derrota o la esquiva queda bloqueada por una pared. No se vuelve a pagar al entrar en Active ni al acertar; cancelar no devuelve el costo. Un intento rechazado por acción ocupada, falta de energía o fin del duelo no descuenta energía. La pulsación rechazada no queda en cola y no se ejecuta cuando regenera el recurso: hay que volver a pulsar.

La regeneración se suspende durante preparación, ejecución y recuperación de las tres acciones. Después transcurre la pausa de 0,8 s; si un paso atraviesa esa pausa se regenera solo con el tiempo sobrante. Se usan segundos de juego (`deltaTime`), por lo que F4 reduce también su velocidad aparente. Intentar una acción sin energía no reinicia la pausa. Caminar y la gravedad siguen disponibles y no consumen stamina. Victoria y derrota congelan el recurso; R restaura vida, posiciones, acciones y stamina a su máximo.

El muestreo de estados del motor conserva el orden actual: stamina observa la acción antes de procesar entradas nuevas. Al terminar una acción, el primer frame observado libre empieza a consumir la pausa. La transición puede tener una diferencia de hasta un frame entre tasas de ejecución; la tasa de regeneración en reposo se valida por tiempo acumulado, no por cantidad de frames. No se cambian alcance, desplazamiento, daño, fases ni ventana de 120 ms del parry.

### Decisión técnica

`PrototypeStamina` es el único dueño de saldo, costos, pausa y regeneración. Cada acción consulta sus propias condiciones de inicio y después llama a `TrySpend`; solo si paga entra en su primera fase. Esto evita duplicar saldos o gastar energía por un botón que otra acción ya bloqueó. La prioridad simultánea parry → esquiva → ataque continúa y la exclusión impide cobrar más de una acción aceptada.

La componente se añade al jugador de la arena guardada. Los fixtures anteriores sin ella conservan sus pruebas aisladas de ataque/esquiva; la validación nueva comprueba la integración sobre los componentes reales con stamina presente. Deshabilitar una componente existente no elimina el costo ni concede acciones gratis: se rechaza el gasto. Rehabilitarla conserva el saldo, sin recarga automática.

El reinicio escucha el evento de reset de `PrototypeHealth` que ya utiliza el duelo, evitando otro lector de R. La HUD lee el recurso; el diagnóstico observa eventos de gasto y rechazo, pero no decide la disponibilidad. No se registra cada incremento de regeneración para conservar los eventos útiles del buffer de F5. No se añade un sistema genérico de estadísticas, inventario ni stamina enemiga.

### Inspector y presentación

`PrototypeStamina` permite ajustar `Maximum`, `Attack Cost`, `Dodge Cost`, `Parry Cost`, `Regeneration Per Second` y `Regeneration Delay` desde Inspector, fuera de Play. `Tools > Soulslike > Preparar stamina del jugador` añade la componente una sola vez y conserva ajustes al repetirse. Las pruebas de balance de esta versión usan los valores de la tabla; si se cambian para cerrar otro balance deberán actualizarse las expectativas numéricas correspondientes.

La HUD agrega una barra y saldo entero, junto a E/Espacio/Q y sus costos. El saldo mostrado se redondea hacia abajo para no anunciar disponibilidad que aún no existe. Verde permite todas las acciones, amarillo permite alguna y rojo indica que no alcanza para la más barata, con los costos actuales. Un mensaje indica qué acción se rechazó y cuánto requiere; desaparece al volver a ser asequible o tras 1,2 s de juego. No altera la fase ni genera una pulsación nueva. El panel de diagnóstico se desplazó para no superponerse.

Con F3 activo, F5 conserva `STAMINA gasto` con acción, costo y saldo, o `STAMINA insuficiente`. Un rechazo por exclusión de acciones sigue siendo observable en los eventos existentes de intento; no se confunde con agotamiento ni se cobra.

### Validación y prueba jugable

`PrototypeStaminaValidation` ejecuta sobre la arena en Play: costos únicos, intentos bloqueados sin gasto, agotamiento sin inicio/giro, pausa sin reiniciarse al rechazar, ausencia de regeneración durante acciones y de devolución al cancelar, cruce de pausa con sobrante, tasa con pasos de 30/60/120 FPS y frame largo, límites cero/máximo, caminar agotado, victoria/derrota/R, deshabilitación y mensaje de rechazo. El runner exige `STAMINA_VALIDATION_SUCCEEDED` junto a todas las suites previas, incluida la matriz temporal del parry.

La prueba del Input System utiliza dispositivos virtuales para comprobar cobro único ante Q/Espacio/E simultáneos, rechazo con saldo 10, mantener botones sin ejecutar después de regenerar y una pulsación nueva que sí ejecuta y paga. Estas pruebas no sustituyen la comprobación humana de la nueva HUD ni del ritmo del duelo.

Para cerrar el incremento:

1. Abrir la arena y Play, o la build `Builds/WindowsSceneList/SoulslikeCombatPrototype.exe`. R debe mostrar 100. En espacio libre, atacar una vez deja 80; esquivar desde lleno deja 70; intentar parry desde lleno deja 85, acierte o falle.
2. Pulsar Espacio tres veces, soltando y esperando cada recuperación pero sin una pausa larga entre ellas: debe quedar 10. La cuarta esquiva, ataque y parry deben rechazarse sin animación ni gasto adicional. Mantener un botón no debe activarlo cuando vuelve la energía.
3. Caminar para alejarse y esperar: tras la pausa, la barra sube a 25/s hasta 100. Repetir con F4 permite observarlo lentamente. Una nueva pulsación cuando alcanza el costo debe funcionar.
4. Reservar al menos 35 puntos, acertar el parry y contraatacar tras recuperar: entre las dos acciones se gastan 35 y se quitan 25 de vida al enemigo. Repetir sin reserva suficiente debe mostrar el rechazo del ataque, aunque el desvío haya sido válido.
5. Comprobar bloqueo entre acciones, derrota, victoria y R durante recuperación. R debe restablecer 100 y limpiar el mensaje. Si hay un gasto inesperado, activar F3 y guardar F5 antes de R.

Después de aceptar este presupuesto se podrá revisar su balance con el ritmo actual y avanzar al primer parry del enemigo. No se da por validada la dificultad final ni se agregan patrones adaptativos del Duelista en este incremento.

Estado técnico al 13 de octubre de 2026: el runner completo con -BuildFromSceneList confirmó STAMINA_VALIDATION_SUCCEEDED, las suites previas y los comandos en Play, con salida 0 (Logs/ProjectValidationBuild.log). Windows compiló con 0 errores, 486 advertencias y 62,46 s. El barrido en reposo produjo aproximadamente 50 de stamina tras 2,4 s desde saldo 10 a 30/60/120 FPS, incluyendo la pausa inicial de 0,8 s. La preparación repetida dejó una sola componente. No se modificaron paquetes ni ajustes de combate del parry aprobado. La nueva HUD, la legibilidad del mensaje y el balance requieren comprobación humana; no se afirma una inspección gráfica automática de OnGUI.

## Primer parry del enemigo — 13 de octubre de 2026

El usuario autorizó continuar después de stamina y pidió publicar el prototipo en GitHub. Este incremento añade una defensa enemiga observable y verificable. La validación humana de stamina y del nuevo duelo sigue siendo necesaria para cerrar su balance; autorizar continuar/publicar no se registra como una medición de esas pruebas.

### Secuencia inicial y respuestas

El enemigo inmóvil alterna un turno defensivo y un ataque normal mientras el jugador permanece dentro de su distancia de activación de 2,2 m. El turno defensivo tiene preparación de 0,25 s en ámbar, ventana de 0,20 s en cian y recuperación de 0,55 s en gris si no consigue desviar. Fija su orientación al iniciar la defensa. Se puede atacar antes de su ventana, durante recuperación o desde la espalda. La señal aparece en el enemigo verde y el contorno representa su volumen frontal.

Un acierto exige contacto real del volumen de ataque del jugador con el volumen defensivo enemigo dentro de la ventana temporal y del cono frontal de ±60°, sin obstáculos. Un cuerpo alcanzado fuera de la defensa puede recibir daño; la defensa puede desviar antes de que el filo aproximado alcance la cápsula. El enemigo no observa la tecla E ni conoce una pulsación futura: su secuencia es fija, no adaptativa. No se agregan persecución, nuevos ataques ni stamina enemiga.

Si desvía, el golpe del jugador se cancela completo, el enemigo conserva su vida y la señal pasa a blanco con un sonido distinto del parry del jugador. Los 20 de stamina ya gastados no se devuelven. El jugador recupera el arma durante 0,60 s: no puede caminar, girar, atacar, esquivar ni hacer parry, pero la gravedad continúa. Una entrada rechazada durante este intervalo no queda en cola.

Después del desvío, el enemigo inicia su ataque normal con los 0,80 s de aviso y los 20 de daño existentes. No aplica daño automático por el parry. La diferencia entre recuperación del jugador y preparación enemiga deja aproximadamente 0,20 s para reaccionar después de recuperar, antes del impacto. Las pruebas demuestran tanto una salida lateral con esquiva como un parry propio contra esa respuesta; la comodidad de esa oportunidad requiere validación humana a velocidad normal.

Si el jugador desvía la respuesta, el enemigo sigue quedando expuesto durante 1,20 s, y se puede contraatacar con los 25 de daño existentes. Su defensa no funciona mientras está expuesto ni durante su ataque normal. R restaura el turno inicial, vida, stamina y elimina desvío/respuesta pendiente. Victoria, derrota y deshabilitación cancelan la defensa.

Los tiempos y el cono se ajustan en `PrototypeEnemyParry`. `Deflected Recovery Duration` del `PrototypePlayerAttack` define la recuperación tras ser desviado. Los valores iniciales hacen visible la guardia y dejan una respuesta posible; no se consideran balance definitivo ni se atribuyen a reglas exactas de otro juego. La adaptación del Duelista a repeticiones queda pendiente para el siguiente incremento, cuando ambos parrys tengan aceptación jugable.

### Integración y relojes

`PrototypeEnemyParry` gobierna su ventana y señales; `PrototypeEnemyAttack` gobierna el ataque normal y consume una solicitud de respuesta al conseguir el desvío. No se ejecutan defensa y ataque al mismo tiempo. Tras la recuperación normal del ataque, se rearma el turno defensivo. El inicio de la respuesta utiliza el instante del contacto, descontando únicamente el tiempo que realmente transcurrió después de ese evento dentro del frame.

`PrototypeMeleeHitQuery` conserva una recepción común: primero revisa los triggers de defensa válidos de jugador y enemigo y después el daño corporal, ignorando triggers para daño. Sigue resolviendo orientación/obstrucción, objetivos ya alcanzados y daño único. Un desvío cancela el ataque entero, incluso con colliders adicionales. No se introduce una jerarquía genérica de estadísticas ni un framework de habilidades.

`PrototypePlayerAttack` conserva `Phase` para su ataque e incorpora recuperación por desvío como estado ocupado. Movimiento, esquiva, parry y stamina consultan esa ocupación con sus guards existentes. Un identificador del ataque evita seguir avanzando fases si la consulta lo canceló, incluso cuando un frame largo ya atravesó toda la recuperación. El daño no reaparece ni se devuelve stamina. Su entrada comienza al ser procesada: no se consume retrospectivamente el delta anterior de una pulsación nueva.

La presentación muestra primero contacto y luego retirada del arma del jugador. El enemigo muestra una postura defensiva y una transición hacia su preparación de respuesta. Los eventos de resultado alimentan `PrototypeParryFeedback` para ambos actores; no se añade una defensa de jugador al enemigo para reutilizar sonidos. El diagnóstico F3/F5 registra DEFENSA ENEMIGA iniciada, PARRY ENEMIGO exitoso, consulta desviada y ocupación del jugador. El modelo sigue usando cajas y geometría provisional, sin colisión continua del filo ni Animator.

### Validación automatizada

`PrototypeEnemyParryValidation` usa los componentes reales en Play y un reloj común para pasos de 30/60/120 FPS. Ensaya ataques con contacto anterior, dentro y posterior a la ventana; ambos últimos casos dejan dañar correctamente o desviar, según corresponda. También comprueba espalda, pared, volumen deshabilitado, defensa antes del cuerpo, colliders duplicados, ausencia de daño y refund al desviar, bloqueo/gravedad/stamina durante recuperación, respuesta que permite esquiva real o parry propio, contraataque durante exposición, alternancia fija, R/muerte y hitches sin golpe diferido.

Las regresiones previas del rival original se ejecutan con la nueva defensa deshabilitada temporalmente en Play, y se restaura en `finally`; la escena guardada sigue con defensa activa. Después se ejecuta la suite nueva con defensa activa y las interacciones de ambos parrys. Esa separación verifica los sistemas anteriores y el comportamiento nuevo; no se presentan los resultados de la antigua arena como una partida del nuevo enemigo. Input Actions también comprueba rechazo y ausencia de cola durante recuperación por desvío. El runner exige `ENEMY_PARRY_VALIDATION_SUCCEEDED` además de los marcadores anteriores.

### Prueba jugable

1. R, acercarse al verde y observar la secuencia: ámbar → cian → gris → preparación violeta de ataque. Q/clic derecho sigue siendo el parry del jugador; cian en el enemigo corresponde a su defensa.
2. Atacar de frente para que el golpe llegue durante cian: el enemigo no pierde vida, muestra blanco y suena el desvío. El arma del jugador retrocede, la HUD indica desviado y se conservan los 20 gastados. No hay daño instantáneo al jugador.
3. Esperar o cambiar de ángulo y atacar durante gris o desde la espalda: el enemigo pierde 25. Cuatro golpes efectivos siguen derrotándolo; cuatro intentos pueden incluir desvíos y no garantizan victoria.
4. Tras ser desviado, reaccionar al aviso violeta cuando termina la recuperación: esquivar para salir del volumen, o acertar un parry propio y contraatacar. Verificar también que E/Espacio/Q pulsados durante bloqueo no se ejecutan después.
5. Comprobar R durante defensa y durante desvío, victoria/derrota, costos y regeneración. Usar F3/F4 para observar y F5 antes de R si un resultado resulta confuso. Volver a velocidad normal para aceptar el ritmo y los sonidos.

Antes de añadir aprendizaje de repeticiones al Duelista, falta confirmar que la defensa, la vulnerabilidad posterior y la respuesta anunciada son legibles y que el jugador dispone de una oportunidad práctica para reaccionar.

Validación técnica: EnemyParryValidationBuild.log confirmó todas las suites y ENEMY_PARRY_VALIDATION_SUCCEEDED. Build Windows: 0 errores, 486 advertencias, 73,21 s. Capturas de defensa activa, desvío y respuesta revisadas; no incluyen HUD OnGUI. La legibilidad, los sonidos y el balance del nuevo enemigo quedan pendientes de prueba humana.
