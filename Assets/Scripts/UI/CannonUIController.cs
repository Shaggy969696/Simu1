using System.Globalization;
using Simu1.Projectiles;
using Simu1.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Simu1.UI
{
    /// <summary>
    /// Controlador de interfaz de usuario para el simulador balístico.
    /// Sincroniza controles visuales (Slider, InputFields, Textos) con el componente Arma.
    /// Cumple con SRP (exclusivo para UI) y DIP (se comunica mediante eventos).
    /// </summary>
    public class CannonUIController : MonoBehaviour
    {
        [Header("Referencia al Cañón")]
        [Tooltip("Componente Arma que controla el cañón. Si no se asigna, se buscará automáticamente en la escena.")]
        [SerializeField] private Arma arma;

        [Header("Control de Ángulo")]
        [Tooltip("Slider para definir el ángulo de elevación (0° a 90°).")]
        [SerializeField] private Slider angleSlider;

        [Tooltip("Texto adyacente que muestra el valor numérico del ángulo seleccionado.")]
        [SerializeField] private Text angleValueText;

        [Header("Casillas de Entrada (Fuerza y Masa)")]
        [Tooltip("Casilla de texto para ingresar la fuerza en Newtons.")]
        [SerializeField] private InputField forceInputField;

        [Tooltip("Casilla de texto para ingresar la masa del proyectil en kilogramos.")]
        [SerializeField] private InputField massInputField;

        [Header("Métricas del Último Impacto")]
        [Tooltip("Texto para mostrar la distancia horizontal (plano XZ) alcanzada.")]
        [SerializeField] private Text distanceResultText;

        [Tooltip("Texto para mostrar la altura máxima (Y) alcanzada.")]
        [SerializeField] private Text heightResultText;

        private void Awake()
        {
            if (arma == null)
            {
                arma = FindFirstObjectByType<Arma>();
            }
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            if (arma == null)
            {
                Debug.LogWarning("[CannonUIController] No se encontró el componente Arma en la escena.", this);
                return;
            }

            // Configurar Slider de Ángulo
            if (angleSlider != null)
            {
                angleSlider.minValue = 0f;
                angleSlider.maxValue = 90f;
                angleSlider.value = arma.LaunchAngle;
            }
            UpdateAngleText(arma.LaunchAngle);

            // Configurar Casillas de Fuerza y Masa
            if (forceInputField != null)
            {
                forceInputField.text = arma.LaunchForce.ToString("F0", CultureInfo.InvariantCulture);
            }

            if (massInputField != null)
            {
                massInputField.text = arma.ProjectileMass.ToString("F1", CultureInfo.InvariantCulture);
            }

            // Inicializar textos de métricas de impacto
            if (distanceResultText != null)
            {
                distanceResultText.text = "Distancia XZ: --- m";
            }

            if (heightResultText != null)
            {
                heightResultText.text = "Altura Máx: --- m";
            }
        }

        private void SubscribeToEvents()
        {
            if (angleSlider != null)
            {
                angleSlider.onValueChanged.AddListener(OnAngleSliderChanged);
            }

            if (forceInputField != null)
            {
                forceInputField.onEndEdit.AddListener(OnForceInputEndEdit);
            }

            if (massInputField != null)
            {
                massInputField.onEndEdit.AddListener(OnMassInputEndEdit);
            }

            if (arma != null)
            {
                arma.OnLastShotImpact += HandleImpactResult;
            }
        }

        private void OnAngleSliderChanged(float newAngle)
        {
            if (arma != null)
            {
                arma.LaunchAngle = newAngle;
            }
            UpdateAngleText(newAngle);
        }

        private void UpdateAngleText(float angle)
        {
            if (angleValueText != null)
            {
                angleValueText.text = $"{angle:F1}°";
            }
        }

        private void OnForceInputEndEdit(string text)
        {
            if (arma == null || forceInputField == null) return;

            if (TryParseFloatFlexible(text, out float newForce) && newForce > 0f)
            {
                arma.LaunchForce = newForce;
                forceInputField.text = newForce.ToString("F0", CultureInfo.InvariantCulture);
            }
            else
            {
                // Restaurar valor previo válido
                forceInputField.text = arma.LaunchForce.ToString("F0", CultureInfo.InvariantCulture);
            }
        }

        private void OnMassInputEndEdit(string text)
        {
            if (arma == null || massInputField == null) return;

            if (TryParseFloatFlexible(text, out float newMass) && newMass > 0f)
            {
                arma.ProjectileMass = newMass;
                massInputField.text = newMass.ToString("F1", CultureInfo.InvariantCulture);
            }
            else
            {
                // Restaurar valor previo válido
                massInputField.text = arma.ProjectileMass.ToString("F1", CultureInfo.InvariantCulture);
            }
        }

        private static bool TryParseFloatFlexible(string text, out float result)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                result = 0f;
                return false;
            }

            string normalized = text.Trim().Replace(',', '.');
            return float.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out result);
        }

        private void HandleImpactResult(ProjectileImpactData impactData)
        {
            if (distanceResultText != null)
            {
                distanceResultText.text = $"Distancia XZ: {impactData.HorizontalDistance:F2} m";
            }

            if (heightResultText != null)
            {
                heightResultText.text = $"Altura Máx: {impactData.MaxHeight:F2} m";
            }
        }

        private void OnDestroy()
        {
            if (angleSlider != null)
            {
                angleSlider.onValueChanged.RemoveListener(OnAngleSliderChanged);
            }

            if (forceInputField != null)
            {
                forceInputField.onEndEdit.RemoveListener(OnForceInputEndEdit);
            }

            if (massInputField != null)
            {
                massInputField.onEndEdit.RemoveListener(OnMassInputEndEdit);
            }

            if (arma != null)
            {
                arma.OnLastShotImpact -= HandleImpactResult;
            }
        }
    }
}
