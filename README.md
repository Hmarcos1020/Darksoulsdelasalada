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
