# Tarea Técnica: Persistencia de Resultados Balísticos con UGS Cloud Save

> **Objetivo:** Extender el simulador de balística (`Simu1`) para persistir automáticamente el resultado de cada disparo en Unity Gaming Services (UGS Cloud Save) y proporcionar una interfaz de usuario para consultar el historial de ensayos guardados.

---

## 1. Contexto y Estado del Proyecto
* **Motor:** Unity 6 (`6000.3.12f1`), Universal Render Pipeline (URP `17.3.0`).
* **Input:** New Input System (`UnityEngine.InputSystem`).
* **UI:** TextMeshPro (`TMPro`) y uGUI.
* **Paquetes UGS instalados y validados:**
  * `com.unity.services.cloudsave` (v3.4.1)
  * `com.unity.services.authentication` (v3.6.0)
  * `com.unity.services.core` (v1.15.1)
* **Enlace Cloud:** Proyecto vinculado con ID `0abfb3b0-e43b-4e9b-be34-490825ae4205` en organización `nachoseijascarp`. Autenticación anónima activa por defecto.
* **Arquitectura obligatoria:** Patrón MVC desacoplado (`Simu1.Model`, `Simu1.View`, `Simu1.Controller`), Repository Pattern para persistencia (`Simu1.Persistence` / `Simu1.Services`) y Object Pooling para proyectiles (`Simu1.Pooling`).

---

## 2. Requerimientos de la Consigna

1. **Guardado automático en UGS al finalizar cada disparo:**
   * **Ángulo de disparo** (`float Angle`).
   * **Fuerza** (`float Force`).
   * **Masa del proyectil** (`float Mass`).
   * **Resultado del impacto:** Acierto o no (`bool IsHit` o impacto en estructura) y Distancia horizontal (`float Distance`).
   * **Cantidad de objetos afectados:** Piezas derribadas (`int FallenPieces`).
   * **Metadatos adicionales recomendados:** Timestamp/fecha, índice de intento y puntaje.

2. **Visualización de Historial Persistido (UI):**
   * Botón en la interfaz (por ejemplo, en el menú o panel de controles): *"Ver Historial UGS"* o *"Resultados Guardados"*.
   * Panel / Modal que consulte los datos de UGS Cloud Save de forma asincrónica (`LoadAsync`) y presente una lista desplazable (ScrollView) o reporte estructurado con las entradas recuperadas.

---

## 3. Plan de Arquitectura e Implementación

### Paso 1: Inicializador UGS (`Simu1.Services.UgsInitializer`)
* Crear script [`Assets/Scripts/Services/UgsInitializer.cs`](file:///f:/Unity/Simu1/Assets/Scripts/Services/UgsInitializer.cs).
* Responsable de llamar a:
  ```csharp
  await UnityServices.InitializeAsync();
  if (!AuthenticationService.Instance.IsSignedIn)
  {
      await AuthenticationService.Instance.SignInAnonymouslyAsync();
  }
  ```
* Exponer una propiedad estática o evento `bool IsReady` para que el sistema sepa cuándo puede operar en la nube.

### Paso 2: Modelo de Datos para Persistencia (`Simu1.Model.SimulationRecordData`)
* Puede reutilizar o serializar la estructura ya existente [`Simu1.Model.ShotRecord`](file:///f:/Unity/Simu1/Assets/Scripts/MVC/Model/ShotRecord.cs) o una clase DTO serializable:
  ```csharp
  [System.Serializable]
  public class SavedShotEntry
  {
      public int attemptIndex;
      public string timestamp;
      public float angle;
      public float force;
      public float mass;
      public float distance;
      public bool isHit;
      public int fallenPieces;
      public int score;
  }

  [System.Serializable]
  public class SimulationHistoryWrapper
  {
      public List<SavedShotEntry> shots = new List<SavedShotEntry>();
  }
  ```

### Paso 3: Patrón Repository (`SimulationRepository` y `UgsSimulationRepository`)
* Crear la abstracción abstracta [`Assets/Scripts/Persistence/SimulationRepository.cs`](file:///f:/Unity/Simu1/Assets/Scripts/Persistence/SimulationRepository.cs):
  ```csharp
  public abstract class SimulationRepository : MonoBehaviour
  {
      public abstract Task SaveShotAsync(SavedShotEntry shot);
      public abstract Task<List<SavedShotEntry>> LoadHistoryAsync();
  }
  ```
* Crear la implementación UGS [`Assets/Scripts/Persistence/UgsSimulationRepository.cs`](file:///f:/Unity/Simu1/Assets/Scripts/Persistence/UgsSimulationRepository.cs):
  * Almacenar mediante `CloudSaveService.Instance.Data.Player.SaveAsync(...)`.
  * Utilizar una clave específica (por ejemplo `"simulation_history"` serializada a JSON o lista de registros) para acumular disparos sin sobrescribir los anteriores.

### Paso 4: Integración en `BallisticController`
* En [`Assets/Scripts/MVC/Controller/BallisticController.cs`](file:///f:/Unity/Simu1/Assets/Scripts/MVC/Controller/BallisticController.cs):
  * Añadir referencia `[SerializeField] private SimulationRepository repository;`.
  * En el método `WaitForSettlementAndReport(...)`, justo tras registrar el tiro en el modelo (`model.RecordShot(...)`), disparar en segundo plano la persistencia asíncrona:
    ```csharp
    if (repository != null)
    {
        _ = repository.SaveShotAsync(shotData);
    }
    ```
  * Exponer método `public async void OnClickShowSavedHistory()` para solicitar los datos del repositorio y mandarlos a la vista.

### Paso 5: Vista de Historial (`BallisticView` y Modal UI)
* En [`Assets/Scripts/MVC/View/BallisticView.cs`](file:///f:/Unity/Simu1/Assets/Scripts/MVC/View/BallisticView.cs):
  * Añadir método para poblar la lista de resultados guardados:
    `public void DisplayCloudHistory(List<SavedShotEntry> entries)`
  * Integrar en el Canvas de [`Assets/Scenes/SampleScene.unity`](file:///f:/Unity/Simu1/Assets/Scenes/SampleScene.unity) el botón para abrir el modal y el contenedor de tarjetas/filas con scroll.

---

## 4. Criterios de Aceptación y Validación
1. **Zero Allocations en caliente:** La persistencia remota no debe interferir con la física ni el hilo principal (`async/await` sin bloqueos).
2. **Ciclo de vida cerrado:** Disparar un proyectil -> esperar impacto -> comprobar en el Unity Dashboard (*Cloud Save > Player Data*) que la entrada aparece registrada.
3. **Persistencia probada:** Salir de *Play Mode*, volver a iniciar la simulación, presionar el botón de historial en la UI y comprobar que los datos anteriores se recuperan y listan correctamente.
4. **Respeto a SIMU1_RULES.md:** Todos los scripts bajo el namespace `Simu1.*`, campos serializados con `[SerializeField] private`, sin destruir proyectiles a mano.
