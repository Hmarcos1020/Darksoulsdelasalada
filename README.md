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
6. Verificá los `.meta` que Unity genera para los scripts, carpetas, materiales y escena. La compilación para Windows es una validación posterior del Hito 0; este incremento no configura builds.

## Responsabilidades de los scripts

- `PrototypeArenaBuilder` (`Assets/Editor/`): herramienta del editor que crea una arena de 20 × 20 metros con cuatro paredes, tres obstáculos, luz, jugador y cámara. Guarda materiales como assets y conecta las referencias automáticamente. No se ejecuta durante el juego.
- `PrototypePlayerMovement` (`Assets/Scripts/`): lee el teclado con Input System instalado y habilitado, limita la entrada diagonal y mueve un `CharacterController` según los ejes horizontales de la cámara. Aplica gravedad explícita y `Time.deltaTime`; la velocidad inicial es 5 m/s. El jugador no tiene Rigidbody ni otro collider.
- `PrototypeFollowCamera` (`Assets/Scripts/`): sigue en `LateUpdate` con `SmoothDamp`. Usa un desplazamiento fijo en ejes del mundo, sin heredar la rotación del jugador. Distancia, altura y suavizado se ajustan en el Inspector.

No se incluyen salto, combate, parry, stamina, IA, animaciones, Cinemachine ni control de cámara con mouse.

## Estado de validación de este incremento

El usuario generó la arena, ejecutó el prototipo en Unity y confirmó que puede mover al jugador. La escena, los tres materiales y los tres scripts están presentes con sus `.meta`, al igual que las carpetas nuevas. Esta prueba confirma el movimiento básico; no verifica todavía las colisiones ni el comportamiento de la cámara.

Falta comprobar colisiones contra suelo, paredes y obstáculos, velocidad diagonal, gravedad, giro suave y seguimiento y orientación de la cámara. También hay que volver a ejecutar el menú después de editar y guardar la arena para confirmar que conserva la escena existente, y probar cancelar el diálogo con una escena modificada para confirmar que no cambia la escena abierta. La compilación y prueba del ejecutable Windows siguen pendientes.
