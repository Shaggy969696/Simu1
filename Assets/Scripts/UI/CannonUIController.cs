using System.Globalization;
using Simu1.Controller;
using Simu1.Model;
using Simu1.Projectiles;
using Simu1.View;
using Simu1.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace Simu1.UI
{
    /// <summary>
    /// Adaptador de interfaz de usuario para el simulador balístico.
    /// Conecta los elementos gráficos con la arquitectura MVC (BallisticController, BallisticView y BallisticData).
    /// Garantiza compatibilidad retroactiva total con la escena y prefabs existentes.
    /// </summary>
    public class CannonUIController : MonoBehaviour
    {
        [Header("Componentes MVC")]
        [Tooltip("Controlador MVC principal.")]
        [SerializeField] private BallisticController controller;

        [Tooltip("Vista MVC principal.")]
        [SerializeField] private BallisticView view;

        [Header("Referencia al Cañón (Legacy)")]
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

        public BallisticController Controller => controller;
        public BallisticView View => view;

        private void Awake()
        {
            if (arma == null)
            {
                arma = FindFirstObjectByType<Arma>();
            }

            SetupMVCComponents();
        }

        private void SetupMVCComponents()
        {
            // Asegurar que la Vista esté presente y configurada
            if (view == null)
            {
                view = GetComponent<BallisticView>() ?? gameObject.AddComponent<BallisticView>();
            }

            if (view != null)
            {
                if (angleValueText != null) view.SetAngleTextLegacy(angleValueText);
                if (distanceResultText != null) view.SetDistanceTextLegacy(distanceResultText);
                if (heightResultText != null) view.SetHeightTextLegacy(heightResultText);
                if (arma != null) view.SetBarrelTransform(arma.transform);
            }
        }

        private void Start()
        {
            InitializeUI();
            SubscribeToEvents();
        }

        private void InitializeUI()
        {
            if (arma == null && controller == null)
            {
                Debug.LogWarning("[CannonUIController] No se encontró el componente Arma ni BallisticController en la escena.", this);
                return;
            }

            float currentAngle = arma != null ? arma.LaunchAngle : 45f;
            float currentForce = arma != null ? arma.LaunchForce : 500f;
            float currentMass = arma != null ? arma.ProjectileMass : 2f;

            // Configurar Slider de Ángulo
            if (angleSlider != null)
            {
                angleSlider.minValue = BallisticData.MinAngle;
                angleSlider.maxValue = BallisticData.MaxAngle;
                angleSlider.value = currentAngle;
            }

            // Configurar Casillas de Fuerza y Masa
            if (forceInputField != null)
            {
                forceInputField.text = currentForce.ToString("F0", CultureInfo.InvariantCulture);
            }

            if (massInputField != null)
            {
                massInputField.text = currentMass.ToString("F1", CultureInfo.InvariantCulture);
            }

            // Actualizar vista
            if (view != null)
            {
                view.DisplayAngle(currentAngle);
                view.DisplayParameters(currentForce, currentMass);
                view.ResetImpactDisplay();
            }
            else
            {
                UpdateAngleText(currentAngle);
                if (distanceResultText != null) distanceResultText.text = "Distancia XZ: --- m";
                if (heightResultText != null) heightResultText.text = "Altura Máx: --- m";
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

            if (view != null)
            {
                view.DisplayAngle(newAngle);
            }
            else
            {
                UpdateAngleText(newAngle);
            }
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
            if (TryParseFloatFlexible(text, out float newForce) && newForce >= 0f)
            {
                if (arma != null) arma.LaunchForce = newForce;
                if (forceInputField != null) forceInputField.text = newForce.ToString("F0", CultureInfo.InvariantCulture);
            }
            else if (arma != null && forceInputField != null)
            {
                forceInputField.text = arma.LaunchForce.ToString("F0", CultureInfo.InvariantCulture);
            }
        }

        private void OnMassInputEndEdit(string text)
        {
            if (TryParseFloatFlexible(text, out float newMass) && newMass > 0f)
            {
                if (arma != null) arma.ProjectileMass = newMass;
                if (massInputField != null) massInputField.text = newMass.ToString("F1", CultureInfo.InvariantCulture);
            }
            else if (arma != null && massInputField != null)
            {
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
            if (view != null)
            {
                view.DisplayImpactResults(impactData.HorizontalDistance, impactData.MaxHeight);
            }
            else
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
