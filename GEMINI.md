# SIMU1 - Reglas del Proyecto (Unity 6)
> Reglas del proyecto enlazadas a [SIMU1_RULES.md](file:///f:/Unity/Simu1/SIMU1_RULES.md).

## Resumen del Proyecto
* **Nombre:** Simu1 (Simulador Balístico y Físico en Unity)
* **Motor & Gráficos:** Unity 6 (`6000.3.12f1`), Universal Render Pipeline (URP `17.3.0`)
* **Entradas:** Unity New Input System (`UnityEngine.InputSystem`)
* **UI:** TextMeshPro (`TMPro`) + uGUI

## Arquitectura
1. **MVC:**
   - `Simu1.Model`: Clases C# puras (POCO, sin `MonoBehaviour`), pruebas unitarias puras, notificación mediante eventos C# (`event Action`).
   - `Simu1.View`: `MonoBehaviour` únicamente para representación visual, textos e interfaces.
   - `Simu1.Controller`: `MonoBehaviour` orquestador (UI Input -> Model -> View).
2. **Pooling (`Simu1.Pooling`):** Prohibido instanciar/destruir proyectiles en tiempo de ejecución (`ProjectilePool`).
3. **Namespaces:** Usar `Simu1.*` en todos los archivos.

## Reglas Críticas de Unity
- **Metas:** Nunca eliminar ni modificar `.meta` sin el archivo correspondiente.
- **Serialización:** Usar `[SerializeField] private`. Si se renombra un campo serializado, usar siempre `[FormerlySerializedAs("antiguoNombre")]`.
- **Logs:** Los errores de consola del editor se pueden consultar en `%LOCALAPPDATA%\Unity\Editor\Editor.log`.
