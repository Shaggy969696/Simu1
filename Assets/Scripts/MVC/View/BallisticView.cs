using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simu1.View
{
    /// <summary>
    /// Vista para la simulación balística (Capa View en MVC).
    /// Maneja toda la presentación visual y la actualización de elementos de UI.
    /// 
    /// Restricciones estrictas:
    /// - Muestra los datos que le indica el Controlador pero NO toma decisiones de simulación.
    /// - No contiene lógica física ni fórmulas de cálculo balístico.
    /// - No modifica el Modelo; la Vista es estrictamente pasiva.
    /// - Soporta campos TextMeshPro (TMP_Text) y campos Text legacy de uGUI como fallback.
    /// </summary>
    public class BallisticView : MonoBehaviour
    {
        [Header("Presentación 3D del Cañón")]
        [Tooltip("Transform del cañón que rotará en el eje Z para representar la elevación visual.")]
        [SerializeField] private Transform barrelTransform;

        [Header("Textos de Visualización TextMeshPro (TMP)")]
        [Tooltip("Campo de texto TextMeshPro para mostrar el ángulo actual en grados.")]
        [SerializeField] private TMP_Text angleTextTMP;

        [Tooltip("Campo de texto TextMeshPro para mostrar la distancia horizontal de impacto.")]
        [SerializeField] private TMP_Text distanceResultTextTMP;

        [Tooltip("Campo de texto TextMeshPro para mostrar la altura máxima alcanzada.")]
        [SerializeField] private TMP_Text heightResultTextTMP;

        [Tooltip("Campo de texto TextMeshPro opcional para mostrar la fuerza de disparo.")]
        [SerializeField] private TMP_Text forceTextTMP;

        [Tooltip("Campo de texto TextMeshPro opcional para mostrar la masa del proyectil.")]
        [SerializeField] private TMP_Text massTextTMP;

        [Header("Textos de Visualización Legacy (uGUI)")]
        [Tooltip("Texto UI legacy como fallback si no se utiliza TextMeshPro.")]
        [SerializeField] private Text angleTextLegacy;

        [Tooltip("Texto UI legacy de distancia como fallback.")]
        [SerializeField] private Text distanceResultTextLegacy;

        [Tooltip("Texto UI legacy de altura como fallback.")]
        [SerializeField] private Text heightResultTextLegacy;

        [Tooltip("Texto UI legacy de fuerza como fallback.")]
        [SerializeField] private Text forceTextLegacy;

        [Tooltip("Texto UI legacy de masa como fallback.")]
        [SerializeField] private Text massTextLegacy;

        private void Awake()
        {
            if (barrelTransform == null)
            {
                barrelTransform = transform;
            }
        }

        #region Métodos de Presentación Visual y UI

        /// <summary>
        /// Actualiza la visualización del ángulo tanto en la UI (TextMeshPro/Text) como en la orientación 3D del cañón.
        /// </summary>
        public void DisplayAngle(float angleDegrees)
        {
            string formatted = $"{angleDegrees:F1}°";
            SetText(angleTextTMP, angleTextLegacy, formatted);
            SetBarrelRotation(angleDegrees);
        }

        /// <summary>
        /// Actualiza la orientación visual del cañón alrededor del eje Z.
        /// Convención: 0° = horizontal hacia +X, 90° = vertical hacia +Y.
        /// </summary>
        public void SetBarrelRotation(float angleDegrees)
        {
            if (barrelTransform != null)
            {
                float zAngle = -(90f - angleDegrees);
                barrelTransform.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            }
        }

        /// <summary>
        /// Actualiza la visualización de los parámetros físicos de fuerza y masa en la interfaz.
        /// </summary>
        public void DisplayParameters(float force, float mass)
        {
            if (forceTextTMP != null || forceTextLegacy != null)
            {
                SetText(forceTextTMP, forceTextLegacy, $"{force:F0} N");
            }

            if (massTextTMP != null || massTextLegacy != null)
            {
                SetText(massTextTMP, massTextLegacy, $"{mass:F1} kg");
            }
        }

        /// <summary>
        /// Actualiza los campos de texto con los resultados del último impacto registrado.
        /// </summary>
        public void DisplayImpactResults(float horizontalDistance, float maxHeight)
        {
            string distString = $"Distancia XZ: {horizontalDistance:F2} m";
            string heightString = $"Altura Máx: {maxHeight:F2} m";

            SetText(distanceResultTextTMP, distanceResultTextLegacy, distString);
            SetText(heightResultTextTMP, heightResultTextLegacy, heightString);
        }

        /// <summary>
        /// Restablece los textos de resultados de impacto a su estado vacío inicial.
        /// </summary>
        public void ResetImpactDisplay()
        {
            SetText(distanceResultTextTMP, distanceResultTextLegacy, "Distancia XZ: --- m");
            SetText(heightResultTextTMP, heightResultTextLegacy, "Altura Máx: --- m");
        }

        /// <summary>
        /// Helper privado para actualizar texto priorizando TextMeshPro sobre Text legacy.
        /// </summary>
        private static void SetText(TMP_Text tmpField, Text legacyField, string content)
        {
            if (tmpField != null)
            {
                tmpField.text = content;
            }
            if (legacyField != null)
            {
                legacyField.text = content;
            }
        }

        #endregion

        #region Asignación en Tiempo de Ejecución o Editor

        public void SetBarrelTransform(Transform t) => barrelTransform = t;
        public void SetAngleTextTMP(TMP_Text t) => angleTextTMP = t;
        public void SetDistanceTextTMP(TMP_Text t) => distanceResultTextTMP = t;
        public void SetHeightTextTMP(TMP_Text t) => heightResultTextTMP = t;
        public void SetAngleTextLegacy(Text t) => angleTextLegacy = t;
        public void SetDistanceTextLegacy(Text t) => distanceResultTextLegacy = t;
        public void SetHeightTextLegacy(Text t) => heightResultTextLegacy = t;

        #endregion
    }
}
