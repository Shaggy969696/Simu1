# SIMU1 - Reglas y Directrices de Desarrollo (Unity 6)

Guía técnica y de arquitectura para el desarrollo y asistencia en el proyecto **Simu1** (Simulador Balístico y de Física en Unity).

---

## 1. Entorno de Desarrollo y Dependencias

* **Motor:** Unity 6 (`6000.3.12f1`)
* **Render Pipeline:** Universal Render Pipeline (URP `17.3.0`)
* **Sistema de Entrada:** New Input System (`com.unity.inputsystem` 1.19.0) — *No utilizar el sistema legacy `Input.GetKeyDown`*.
* **Navegación / IA:** Unity AI Navigation (`com.unity.ai.navigation` 2.0.11)
* **UI:** TextMeshPro (`TextMeshProUGUI`, `TMP_InputField`) combinado con componentes estándar de uGUI (`Slider`, `Button`).
* **Framework de Pruebas:** Unity Test Framework (`com.unity.test-framework` 1.6.0).

---

## 2. Arquitectura de Software

El proyecto sigue una arquitectura estricta orientada a desacoplamiento, rendimiento y testeo:

### 2.1 Patrón MVC (Model - View - Controller)
* **Modelo (`Simu1.Model`):**
  * Clases C# puras (POCO), **sin heredar de `MonoBehaviour`** y sin dependencias gráficas directas (`UnityEngine` mínimo o nulo).
  * Contiene las fórmulas físicas, datos de simulación y validación de rangos (`Math.Clamp`, límites balísticos).
  * Comunica cambios de estado exclusivamente mediante eventos C# estándar (`event Action`, `event Action<float, float>`).
  * Debe ser 100% testeable mediante pruebas unitarias sin requerir el ciclo de vida de escenas de Unity.
* **Vista (`Simu1.View`):**
  * Hereda de `MonoBehaviour`.
  * Se encarga de la presentación visual: textos de UI, animaciones, sliders informativos, efectos y renderizado.
  * No toma decisiones de negocio ni modifica directamente los datos del modelo.
* **Controlador (`Simu1.Controller`):**
  * Hereda de `MonoBehaviour`.
  * Único intermediario: escucha la UI / Input del usuario, actualiza el Modelo (`BallisticData`) y le indica a la Vista qué reflejar. Orquesta el disparo y coordina con el sistema de pooling.

### 2.2 Object Pooling (`Simu1.Pooling`)
* **Cero asignaciones en caliente:** Prohibido instanciar (`Instantiate`) o destruir (`Destroy`) proyectiles en tiempo de ejecución durante la simulación.
* Todo proyectil (`ProjectileBase`, `BallisticProjectile`) debe ser solicitado y devuelto a través de `ProjectilePool`.
* Las clases poolables deben implementar la lógica de reset en su activación (`OnEnable` / método de inicialización).

### 2.3 Namespaces y Organización
Todo código nuevo debe ubicarse dentro del namespace raíz `Simu1`:
* `Simu1.Model`
* `Simu1.View`
* `Simu1.Controller`
* `Simu1.Pooling`
* `Simu1.Projectiles`
* `Simu1.Interfaces`
* `Simu1.Editor` (exclusivo para herramientas y ventanas del editor en `Assets/Scripts/Editor`)

---

## 3. Integridad de Unity y Serialización Segura

### 3.1 Manejo de Archivos `.meta` y GUIDs
* **Nunca eliminar, mover o renombrar un archivo de código o recurso sin gestionar su archivo `.meta` correspondiente.**
* Al crear nuevos scripts, tener en cuenta que Unity generará su GUID en el `.meta`.
* No alterar manualmente los GUIDs de assets ya existentes para no romper referencias en `Assets/Scenes/SampleScene.unity` ni en `Assets/Prefab/SimuladorUI.prefab`.

### 3.2 Convenciones de Serialización e Inspector
* **Campos inspeccionables:** Usar siempre `[SerializeField] private` en lugar de campos `public`.
* **Renombrado seguro:** Si se renombra un campo serializado existente, **es obligatorio** usar `[FormerlySerializedAs("nombreAnterior")]` de `UnityEngine.Serialization` para evitar perder los valores o referencias asignadas en Prefabs e inspectores.
* **Atributos de usabilidad:** Añadir `[Header("...")]`, `[Tooltip("...")]`, y validadores como `[Min(...)]` o `[Range(...)]` en los campos serializados.

---

## 4. Diagnóstico, Logs y Depuración

* Los logs y errores de compilación del Editor se escriben en tiempo real en:
  `%LOCALAPPDATA%\Unity\Editor\Editor.log`
* Ante cualquier error o excepción reportada, se puede consultar directamente el archivo de log para diagnosticar trazas de error completas.
* Mantener el código limpio de advertencias (warnings CS0219, CS0414, CS0168, etc.).
