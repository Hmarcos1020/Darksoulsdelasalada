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

No se incluyen salto, combate, parry, stamina, IA, animaciones, Cinemachine ni control de cámara con mouse.

## Estado de validación de este incremento

El usuario generó la arena, ejecutó el prototipo en Unity y confirmó que puede mover al jugador. La escena, los tres materiales y los tres scripts están presentes con sus `.meta`, al igual que las carpetas nuevas. Esta prueba confirma el movimiento básico; no verifica todavía las colisiones ni el comportamiento de la cámara.

Falta comprobar colisiones contra suelo, paredes y obstáculos, velocidad diagonal, gravedad, giro suave y seguimiento y orientación de la cámara. También hay que volver a ejecutar el menú después de editar y guardar la arena para confirmar que conserva la escena existente, y probar cancelar el diálogo con una escena modificada para confirmar que no cambia la escena abierta. La compilación y prueba del ejecutable Windows siguen pendientes.

## Compilar y probar Windows H0

1. En el editor ya abierto, esperá la importación de `Assets/Editor/PrototypeWindowsBuilder.cs` y su `.meta`. Revisá que no haya errores de scripts y salí de Play.
2. Guardá los cambios de la arena. En **File > Build Profiles**, verificá que esté activa la plataforma **Windows**, arquitectura **Intel 64-bit (x86_64)**. Si está activa otra plataforma, cambiala allí y esperá la importación; la herramienta no cambia la plataforma automáticamente. Windows Build Support debe estar disponible para este editor.
3. Elegí **Tools > Soulslike > Compilar Windows H0**. La herramienta compila exclusivamente `Assets/Scenes/PrototypeArena.unity`, sin modificar la lista de escenas de Build Profiles, y escribe `Builds/Windows/SoulslikeCombatPrototype.exe`. Las compilaciones siguientes usan el mismo destino.
4. El backend se establece temporalmente en **Mono** para esta compilación y se restaura al terminar, incluso ante un fallo. No se cambian paquetes, versión de Unity ni configuración global de Git.
5. Revisá el diálogo y la consola: solo se anuncia éxito si `BuildPipeline.BuildPlayer` devuelve `BuildResult.Succeeded` y existe el ejecutable. Se informan resultado, errores, advertencias y duración; ante un fallo, un ejecutable anterior no sirve como validación.
6. Si la compilación fue exitosa, abrí el ejecutable desde Windows. Conservá junto a él la carpeta `SoulslikeCombatPrototype_Data` y los demás archivos generados; no copies únicamente el `.exe`.
7. Enfocá la ventana y probá WASD y flechas. Validá por separado velocidad diagonal, colisiones contra suelo, paredes y obstáculos, y seguimiento y orientación de la cámara. Cerrá la ventana o usá **Alt+F4** al terminar.

`Builds/` está excluido de Git. La herramienta no lanza otro editor Unity ni ejecuta automáticamente el juego. El nuevo script ya tiene su `.meta` generado por Unity. Aún falta confirmar el resultado del menú en Unity y probar el ejecutable: no se afirma que la compilación Windows haya sido exitosa.
