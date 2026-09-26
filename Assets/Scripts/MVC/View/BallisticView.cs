using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Simu1.Model;

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

        [Header("Panel Reporte de Tiro")]
        [Tooltip("Contenedor GameObject de la ventana modal o panel de Reporte de Tiro.")]
        [SerializeField] private GameObject shotReportPanel;

        [Tooltip("Texto para la puntuación en el Reporte de Tiro.")]
        [SerializeField] private TMP_Text reportScoreTextTMP;

        [Tooltip("Texto con el desglose físico completo del Reporte de Tiro.")]
        [SerializeField] private TMP_Text reportDetailsTextTMP;

        [Tooltip("Texto legacy para la puntuación.")]
        [SerializeField] private Text reportScoreTextLegacy;

        [Tooltip("Texto legacy para los detalles del reporte.")]
        [SerializeField] private Text reportDetailsTextLegacy;

        [Header("Panel Historial Persistido (UGS Cloud Save)")]
        [Tooltip("Contenedor GameObject de la ventana modal de Historial UGS.")]
        [SerializeField] private GameObject cloudHistoryPanel;

        [Tooltip("Texto TextMeshPro para el encabezado del Historial UGS.")]
        [SerializeField] private TMP_Text cloudHistoryTitleTMP;

        [Tooltip("Texto TextMeshPro con los registros recuperados de UGS.")]
        [SerializeField] private TMP_Text cloudHistoryDetailsTMP;

        [Tooltip("Texto legacy para el encabezado del Historial UGS.")]
        [SerializeField] private Text cloudHistoryTitleLegacy;

        [Tooltip("Texto legacy con los registros recuperados de UGS.")]
        [SerializeField] private Text cloudHistoryDetailsLegacy;

        [Tooltip("Botón para cerrar la ventana modal de Historial UGS.")]
        [SerializeField] private Button closeCloudHistoryButton;

        private void Awake()
        {
            if (barrelTransform == null)
            {
                barrelTransform = transform;
            }

            if (closeCloudHistoryButton != null)
            {
                closeCloudHistoryButton.onClick.AddListener(HideCloudHistory);
            }
        }

        private void OnDestroy()
        {
            if (closeCloudHistoryButton != null)
            {
                closeCloudHistoryButton.onClick.RemoveListener(HideCloudHistory);
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
        /// Muestra el panel modal o textos detallados con el "Reporte de Tiro" y puntuación final.
        /// </summary>
        public void DisplayShotReport(
            int score, 
            float flightTime, 
            Vector3 impactPoint, 
            float relativeSpeed, 
            float collisionImpulse, 
            int fallenPieces, 
            int totalPieces)
        {
            if (shotReportPanel != null)
            {
                shotReportPanel.SetActive(true);
            }

            string scoreFormatted = $"PUNTUACIÓN: {score:N0}";
            SetText(reportScoreTextTMP, reportScoreTextLegacy, scoreFormatted);

            string details = string.Format(
                CultureInfo.InvariantCulture,
                "Tiempo de vuelo: {0:F2} s\n" +
                "Punto de impacto: ({1:F1}, {2:F1}, {3:F1})\n" +
                "Velocidad relativa: {4:F1} m/s\n" +
                "Impulso de colisión: {5:F1} N·s\n" +
                "Piezas derribadas: {6} / {7}",
                flightTime,
                impactPoint.x, impactPoint.y, impactPoint.z,
                relativeSpeed,
                collisionImpulse,
                fallenPieces,
                totalPieces
            );

            SetText(reportDetailsTextTMP, reportDetailsTextLegacy, details);

            Debug.Log($"[Reporte de Tiro] {scoreFormatted} | {details.Replace("\n", " | ")}");
        }

        /// <summary>
        /// Muestra el historial técnico acumulativo de ensayos balísticos en el panel modal (simulador de toma de datos).
        /// </summary>
        public void DisplayShotHistory(IReadOnlyList<ShotRecord> history, int totalCumulativeScore)
        {
            if (shotReportPanel != null)
            {
                shotReportPanel.SetActive(true);
            }

            int count = history != null ? history.Count : 0;
            string headerFormatted = count == 1 
                ? "REGISTRO TÉCNICO: 1 ENSAYO REGISTRADO"
                : $"REGISTRO TÉCNICO: {count} ENSAYOS REGISTRADOS";

            SetText(reportScoreTextTMP, reportScoreTextLegacy, headerFormatted);

            if (history == null || history.Count == 0)
            {
                SetText(reportDetailsTextTMP, reportDetailsTextLegacy, "Sin ensayos registrados en la sesión actual.\nRealiza un disparo para iniciar la adquisición de datos.");
                return;
            }

            var sb = new StringBuilder();
            // Mostrar los ensayos en orden inverso (el más reciente arriba de todo)
            for (int i = history.Count - 1; i >= 0; i--)
            {
                var r = history[i];
                string isLatestTag = (i == history.Count - 1) ? " [ÚLTIMO ENSAYO]" : "";

                sb.AppendLine($"<size=17><b>=== ENSAYO #{r.AttemptIndex}{isLatestTag} ===</b></size>");
                sb.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "  • <b>Tiempo de vuelo:</b> {0:F2} s\n" +
                    "  • <b>Punto de impacto:</b> ({1:F1}, {2:F1}, {3:F1})\n" +
                    "  • <b>Velocidad relativa:</b> {4:F1} m/s\n" +
                    "  • <b>Impulso de colisión:</b> {5:F1} N·s\n" +
                    "  • <b>Piezas derribadas:</b> {6} / {7}",
                    r.FlightTime,
                    r.ImpactPosition.x, r.ImpactPosition.y, r.ImpactPosition.z,
                    r.RelativeSpeed,
                    r.CollisionImpulse,
                    r.FallenPieces,
                    r.TotalPieces
                ));

                if (i > 0)
                {
                    sb.AppendLine("\n────────────────────────────────────────\n");
                }
            }

            string fullDetails = sb.ToString().TrimEnd();
            SetText(reportDetailsTextTMP, reportDetailsTextLegacy, fullDetails);

            Debug.Log($"[Telemetría Balística] {headerFormatted} | Ensayos mostrados: {count}");
        }

        /// <summary>
        /// Oculta el panel de reporte de tiro.
        /// </summary>
        public void HideShotReport()
        {
            if (shotReportPanel != null)
            {
                shotReportPanel.SetActive(false);
            }
        }

        /// <summary>
        /// Muestra el estado de carga mientras se consultan los datos en UGS Cloud Save.
        /// </summary>
        public void ShowCloudHistoryLoading()
        {
            if (cloudHistoryPanel != null)
            {
                cloudHistoryPanel.SetActive(true);
            }

            SetText(cloudHistoryTitleTMP, cloudHistoryTitleLegacy, "CONSULTANDO UGS CLOUD SAVE...");
            SetText(cloudHistoryDetailsTMP, cloudHistoryDetailsLegacy, "Descargando registros balísticos desde Unity Gaming Services...\nPor favor espera un momento.");
        }

        /// <summary>
        /// Muestra la lista de ensayos recuperados desde UGS Cloud Save en el panel modal.
        /// </summary>
        public void DisplayCloudHistory(List<SavedShotEntry> entries)
        {
            if (cloudHistoryPanel != null)
            {
                cloudHistoryPanel.SetActive(true);
            }

            int count = entries != null ? entries.Count : 0;
            string title = count == 1
                ? "HISTORIAL UGS: 1 ENSAYO GUARDADO"
                : $"HISTORIAL UGS: {count} ENSAYOS GUARDADOS";

            SetText(cloudHistoryTitleTMP, cloudHistoryTitleLegacy, title);

            if (entries == null || entries.Count == 0)
            {
                SetText(cloudHistoryDetailsTMP, cloudHistoryDetailsLegacy, "No se encontraron ensayos guardados en UGS Cloud Save.\nRealiza disparos en el simulador para persistir datos en la nube.");
                return;
            }

            var sb = new StringBuilder();
            // Mostrar del más reciente al más antiguo
            for (int i = entries.Count - 1; i >= 0; i--)
            {
                var entry = entries[i];
                string formattedDate = entry.timestamp;
                if (System.DateTime.TryParse(entry.timestamp, out var dt))
                {
                    formattedDate = dt.ToLocalTime().ToString("dd/MM/yyyy HH:mm:ss");
                }

                string hitStatus = entry.isHit ? "<color=#2ecc71>IMPACTO EXITOSO</color>" : "<color=#e74c3c>FALLO</color>";

                sb.AppendLine($"<size=17><b>=== ENSAYO #{entry.attemptIndex} ===</b></size>");
                sb.AppendLine($"  • <b>Fecha / Hora:</b> {formattedDate}");
                sb.AppendLine($"  • <b>Configuración:</b> Ángulo: {entry.angle:F1}° | Fuerza: {entry.force:F0} N | Masa: {entry.mass:F1} kg");
                sb.AppendLine($"  • <b>Resultado:</b> {hitStatus} | Distancia XZ: {entry.distance:F2} m");
                sb.AppendLine($"  • <b>Estructura:</b> Piezas derribadas: {entry.fallenPieces} | Puntos: {entry.score:N0}");
                if (entry.flightTime > 0.001f || entry.maxHeight > 0.001f)
                {
                    sb.AppendLine($"  • <b>Física:</b> Tiempo vuelo: {entry.flightTime:F2} s | Altura máx: {entry.maxHeight:F2} m");
                }

                if (i > 0)
                {
                    sb.AppendLine("\n────────────────────────────────────────\n");
                }
            }

            SetText(cloudHistoryDetailsTMP, cloudHistoryDetailsLegacy, sb.ToString().TrimEnd());
            Debug.Log($"[BallisticView] Historial UGS presentado ({count} registros).");
        }

        /// <summary>
        /// Oculta el panel modal de historial UGS.
        /// </summary>
        public void HideCloudHistory()
        {
            if (cloudHistoryPanel != null)
            {
                cloudHistoryPanel.SetActive(false);
            }
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
        public void SetShotReportPanel(GameObject panel) => shotReportPanel = panel;
        public void SetReportScoreTextTMP(TMP_Text t) => reportScoreTextTMP = t;
        public void SetReportDetailsTextTMP(TMP_Text t) => reportDetailsTextTMP = t;
        public void SetReportScoreTextLegacy(Text t) => reportScoreTextLegacy = t;
        public void SetReportDetailsTextLegacy(Text t) => reportDetailsTextLegacy = t;
        public void SetCloudHistoryPanel(GameObject panel) => cloudHistoryPanel = panel;
        public void SetCloudHistoryTitleTMP(TMP_Text t) => cloudHistoryTitleTMP = t;
        public void SetCloudHistoryDetailsTMP(TMP_Text t) => cloudHistoryDetailsTMP = t;
        public void SetCloudHistoryTitleLegacy(Text t) => cloudHistoryTitleLegacy = t;
        public void SetCloudHistoryDetailsLegacy(Text t) => cloudHistoryDetailsLegacy = t;
        public void SetCloseCloudHistoryButton(Button btn) => closeCloudHistoryButton = btn;

        #endregion
    }
}
