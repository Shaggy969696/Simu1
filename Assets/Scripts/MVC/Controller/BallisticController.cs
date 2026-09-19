using System.Globalization;
using Simu1.Interfaces;
using Simu1.Model;
using Simu1.Pooling;
using Simu1.Projectiles;
using Simu1.View;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Simu1.Controller
{
    /// <summary>
    /// Controlador para la simulación balística (Capa Controller en MVC).
    /// Actúa como el único intermediario entre el usuario, el Modelo y la Vista:
    /// - Recibe interacciones del usuario (Slider, casillas de entrada, tecla/botón de disparo).
    /// - Actualiza los valores en BallisticData (Modelo).
    /// - Notifica a BallisticView (Vista) qué información presentar.
    /// - Orquesta el lanzamiento de proyectiles y coordina los resultados de impacto.
    /// </summary>
    public class BallisticController : MonoBehaviour
    {
        [Header("Capa Vista (View)")]
        [Tooltip("Referencia a la Vista encargada de la representación gráfica y textos.")]
        [SerializeField] private BallisticView view;

        [Header("Controles de Entrada de Usuario (UI Inputs)")]
        [Tooltip("Slider para ajustar el ángulo de elevación de disparo.")]
        [SerializeField] private Slider angleSlider;

        [Tooltip("Casilla para ingresar la fuerza de disparo (uGUI InputField).")]
        [SerializeField] private InputField forceInputField;

        [Tooltip("Casilla para ingresar la masa del proyectil (uGUI InputField).")]
        [SerializeField] private InputField massInputField;

        [Tooltip("Casilla TextMeshPro para ingresar la fuerza (TMP_InputField opcional).")]
        [SerializeField] private TMP_InputField forceInputTMP;

        [Tooltip("Casilla TextMeshPro para ingresar la masa (TMP_InputField opcional).")]
        [SerializeField] private TMP_InputField massInputTMP;

        [Tooltip("Botón opcional para ejecutar el disparo.")]
        [SerializeField] private Button fireButton;

        [Header("Referencias de Disparo y Lanzamiento")]
        [Tooltip("Gestor de Object Pooling para proyectiles.")]
        [SerializeField] private ProjectilePool projectilePool;

        [Tooltip("Prefab del proyectil como fallback en caso de no utilizar pool.")]
        [SerializeField] private GameObject projectilePrefab;

        [Tooltip("Punto de salida del proyectil (en la boca del cañón).")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Transform del cañón.")]
        [SerializeField] private Transform barrelTransform;

        [Header("Configuración Inicial del Modelo")]
        [Range(0f, 90f)]
        [SerializeField] private float initialAngle = 45f;
        [SerializeField] private float initialForce = 500f;
        [SerializeField] private float initialMass = 2f;
        [SerializeField] private float barrelLength = 2f;

        // Instancia pura del Modelo (C# POCO)
        private BallisticData model;

        public BallisticData Model => model;
        public BallisticView View => view;

        private void Awake()
        {
            InitializeModel();
            ResolveReferences();
        }

        private void Start()
        {
            InitializeViewAndInputs();
            SubscribeToInputEvents();
        }

        private void InitializeModel()
        {
            model = new BallisticData(
                initialAngle: initialAngle,
                initialForce: initialForce,
                initialMass: initialMass,
                initialBarrelLength: barrelLength
            );
        }

        private void ResolveReferences()
        {
            if (view == null)
            {
                view = GetComponent<BallisticView>() ?? FindFirstObjectByType<BallisticView>();
            }

            if (barrelTransform == null && view != null)
            {
                barrelTransform = transform;
            }

            if (spawnPoint == null)
            {
                Transform childSpawn = transform.Find("spawnPoint") 
                    ?? transform.Find("SpawnPoint") 
                    ?? transform.Find("spawn_point");
                spawnPoint = childSpawn != null ? childSpawn : transform;
            }

            if (projectilePool == null)
            {
                projectilePool = GetComponent<ProjectilePool>() ?? FindFirstObjectByType<ProjectilePool>();
            }
        }

        private void InitializeViewAndInputs()
        {
            // Sincronizar Slider de Ángulo
            if (angleSlider != null)
            {
                angleSlider.minValue = BallisticData.MinAngle;
                angleSlider.maxValue = BallisticData.MaxAngle;
                angleSlider.value = model.LaunchAngle;
            }

            // Sincronizar Casillas de Fuerza
            string forceText = model.Force.ToString("F0", CultureInfo.InvariantCulture);
            if (forceInputField != null) forceInputField.text = forceText;
            if (forceInputTMP != null) forceInputTMP.text = forceText;

            // Sincronizar Casillas de Masa
            string massText = model.ProjectileMass.ToString("F1", CultureInfo.InvariantCulture);
            if (massInputField != null) massInputField.text = massText;
            if (massInputTMP != null) massInputTMP.text = massText;

            // Ordenar a la Vista que presente los datos iniciales
            if (view != null)
            {
                view.DisplayAngle(model.LaunchAngle);
                view.DisplayParameters(model.Force, model.ProjectileMass);
                view.ResetImpactDisplay();
            }
        }

        private void SubscribeToInputEvents()
        {
            if (angleSlider != null)
            {
                angleSlider.onValueChanged.AddListener(OnAngleSliderChanged);
            }

            if (forceInputField != null)
            {
                forceInputField.onEndEdit.AddListener(OnForceInputEndEdit);
            }

            if (forceInputTMP != null)
            {
                forceInputTMP.onEndEdit.AddListener(OnForceInputEndEdit);
            }

            if (massInputField != null)
            {
                massInputField.onEndEdit.AddListener(OnMassInputEndEdit);
            }

            if (massInputTMP != null)
            {
                massInputTMP.onEndEdit.AddListener(OnMassInputEndEdit);
            }

            if (fireButton != null)
            {
                fireButton.onClick.AddListener(Fire);
            }
        }

        private void Update()
        {
            if (IsFireInputPressed())
            {
                Fire();
            }
        }

        #region Manejadores de Interacción de Usuario

        /// <summary>
        /// Recibe el cambio de ángulo del slider, actualiza el Modelo y manda a la Vista a representarlo.
        /// </summary>
        public void OnAngleSliderChanged(float newAngle)
        {
            if (model == null) return;

            model.LaunchAngle = newAngle;

            if (view != null)
            {
                view.DisplayAngle(model.LaunchAngle);
            }
        }

        /// <summary>
        /// Recibe la edición de la fuerza, valida el valor, actualiza el Modelo y notifica a la Vista.
        /// </summary>
        public void OnForceInputEndEdit(string text)
        {
            if (model == null) return;

            if (TryParseFloatFlexible(text, out float newForce) && newForce >= 0f)
            {
                model.Force = newForce;
            }

            string synced = model.Force.ToString("F0", CultureInfo.InvariantCulture);
            if (forceInputField != null) forceInputField.text = synced;
            if (forceInputTMP != null) forceInputTMP.text = synced;

            if (view != null)
            {
                view.DisplayParameters(model.Force, model.ProjectileMass);
            }
        }

        /// <summary>
        /// Recibe la edición de la masa, valida el valor, actualiza el Modelo y notifica a la Vista.
        /// </summary>
        public void OnMassInputEndEdit(string text)
        {
            if (model == null) return;

            if (TryParseFloatFlexible(text, out float newMass) && newMass > 0f)
            {
                model.ProjectileMass = newMass;
            }

            string synced = model.ProjectileMass.ToString("F1", CultureInfo.InvariantCulture);
            if (massInputField != null) massInputField.text = synced;
            if (massInputTMP != null) massInputTMP.text = synced;

            if (view != null)
            {
                view.DisplayParameters(model.Force, model.ProjectileMass);
            }
        }

        #endregion

        #region Orquestación de Disparo e Impacto

        /// <summary>
        /// Comprueba si se presionó la tecla de disparo (Barra Espaciadora).
        /// Compatible con el Input System nuevo y el Input Manager legacy.
        /// </summary>
        private bool IsFireInputPressed()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                return true;
            }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Space))
            {
                return true;
            }
#endif
            return false;
        }

        /// <summary>
        /// Orquesta el proceso de disparo:
        /// 1. Consulta al Modelo el impulso y la masa.
        /// 2. Dispara el proyectil usando Object Pool o Prefab.
        /// 3. Escucha el evento de impacto para actualizar el Modelo y la Vista.
        /// </summary>
        public void Fire()
        {
            if (model == null) return;

            Vector3 fireDirection = spawnPoint != null 
                ? spawnPoint.up 
                : (barrelTransform != null ? barrelTransform.up : transform.up);
            Vector3 firePosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion fireRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            // Obtener impulso calculado por las reglas del Modelo (Trabajo - Energía Cinética)
            float impulse = model.CalculateInitialImpulse();
            float mass = model.ProjectileMass;

            // Opción 1: Obtener del ProjectilePool
            if (projectilePool != null)
            {
                BallisticProjectile pooledProjectile = projectilePool.Get();
                if (pooledProjectile != null)
                {
                    pooledProjectile.transform.SetPositionAndRotation(firePosition, fireRotation);
                    
                    void HandlePoolImpact(ProjectileImpactData impactData)
                    {
                        pooledProjectile.OnImpactDetected -= HandlePoolImpact;
                        HandleProjectileImpact(impactData);
                    }
                    pooledProjectile.OnImpactDetected += HandlePoolImpact;

                    pooledProjectile.Launch(fireDirection, impulse, mass);
                    return;
                }
            }

            // Opción 2: Fallback instanciando prefab
            if (projectilePrefab != null)
            {
                GameObject newProj = Instantiate(projectilePrefab, firePosition, fireRotation);
                if (newProj.TryGetComponent<ProjectileBase>(out var projBase))
                {
                    void HandleBaseImpact(ProjectileImpactData impactData)
                    {
                        projBase.OnImpactDetected -= HandleBaseImpact;
                        HandleProjectileImpact(impactData);
                    }
                    projBase.OnImpactDetected += HandleBaseImpact;

                    projBase.Launch(fireDirection, impulse, mass);
                }
                else if (newProj.TryGetComponent<ILaunchable>(out var launchable))
                {
                    launchable.Launch(fireDirection, impulse, mass);
                }
            }
            else
            {
                Debug.LogWarning("[BallisticController] No se asignó ni ProjectilePool ni projectilePrefab.", this);
            }
        }

        /// <summary>
        /// Procesa el impacto notificado por el proyectil:
        /// actualiza los datos en el Modelo y ordena a la Vista visualizarlos.
        /// </summary>
        public void HandleProjectileImpact(ProjectileImpactData impactData)
        {
            if (model != null)
            {
                model.SetImpactResults(impactData.HorizontalDistance, impactData.MaxHeight);
            }

            if (view != null)
            {
                view.DisplayImpactResults(impactData.HorizontalDistance, impactData.MaxHeight);
            }
        }

        #endregion

        #region Utilidades

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

            if (forceInputTMP != null)
            {
                forceInputTMP.onEndEdit.RemoveListener(OnForceInputEndEdit);
            }

            if (massInputField != null)
            {
                massInputField.onEndEdit.RemoveListener(OnMassInputEndEdit);
            }

            if (massInputTMP != null)
            {
                massInputTMP.onEndEdit.RemoveListener(OnMassInputEndEdit);
            }

            if (fireButton != null)
            {
                fireButton.onClick.RemoveListener(Fire);
            }
        }

        #endregion
    }
}
