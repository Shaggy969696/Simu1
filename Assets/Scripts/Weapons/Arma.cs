using System;
using Simu1.Interfaces;
using Simu1.Model;
using Simu1.Pooling;
using Simu1.Projectiles;
using UnityEngine;

namespace Simu1.Weapons
{
    /// <summary>
    /// Componente principal del Cañón / Arma balística.
    /// Controla el ángulo de elevación (eje Z), fuerza en Newtons y masa del proyectil en kg.
    /// Delega los datos y las reglas físicas de cálculo a BallisticData (Capa Model en MVC).
    /// Cumple con SRP, OCP y DIP (implementa IArma y se comunica mediante eventos).
    /// </summary>
    public class Arma : MonoBehaviour, IArma
    {
        [Header("Referencias de Disparo")]
        [Tooltip("Gestor de Object Pooling para proyectiles. Si está asignado, se reutilizarán proyectiles.")]
        [SerializeField] private ProjectilePool projectilePool;

        [Tooltip("Prefab del proyectil como fallback en caso de no utilizar pool.")]
        [SerializeField] private GameObject projectile;

        [Tooltip("Punto de salida del proyectil (en la boca del cañón).")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Transform del cañón que rotará en el eje Z. Si se deja vacío, se rotará este mismo GameObject.")]
        [SerializeField] private Transform barrelTransform;

        [Header("Parámetros Físicos de Disparo")]
        [Tooltip("Ángulo de elevación en grados (0° = horizontal hacia +X, 90° = vertical hacia +Y).")]
        [Range(0f, 90f)]
        [SerializeField] private float launchAngle = 45f;

        [Tooltip("Fuerza constante ejercida por los gases en el cañón (en Newtons).")]
        [SerializeField] private float force = 500f;

        [Tooltip("Masa del proyectil en kilogramos.")]
        [SerializeField] private float projectileMass = 2.0f;

        [Tooltip("Longitud del tubo del cañón en metros a lo largo de la cual actúa la fuerza.")]
        [SerializeField] private float barrelLength = 2.0f;

        // Instancia del Modelo (Capa Model en MVC)
        private BallisticData model;

        // Evento desacoplado para notificar métricas de impacto a la UI (DIP)
        public event Action<ProjectileImpactData> OnLastShotImpact;

        public BallisticData Model => model ?? EnsureModelInitialized();

        // Propiedades de la interfaz IArma delegadas en el Modelo
        public float LaunchAngle
        {
            get => Model.LaunchAngle;
            set
            {
                Model.LaunchAngle = value;
                launchAngle = Model.LaunchAngle;
                ApplyRotation();
            }
        }

        public float LaunchForce
        {
            get => Model.Force;
            set
            {
                Model.Force = value;
                force = Model.Force;
            }
        }

        public float ProjectileMass
        {
            get => Model.ProjectileMass;
            set
            {
                Model.ProjectileMass = value;
                projectileMass = Model.ProjectileMass;
            }
        }

        public float BarrelLength
        {
            get => Model.BarrelLength;
            set
            {
                Model.BarrelLength = value;
                barrelLength = Model.BarrelLength;
            }
        }

        private void Awake()
        {
            EnsureModelInitialized();
            InitializeReferences();
            ApplyRotation();
        }

        private BallisticData EnsureModelInitialized()
        {
            if (model == null)
            {
                model = new BallisticData(
                    initialAngle: launchAngle,
                    initialForce: force,
                    initialMass: projectileMass,
                    initialBarrelLength: barrelLength
                );
            }
            return model;
        }

        private void OnValidate()
        {
            if (model != null)
            {
                model.LaunchAngle = launchAngle;
                model.Force = force;
                model.ProjectileMass = projectileMass;
                model.BarrelLength = barrelLength;
            }
            ApplyRotation();
        }

        private void Reset()
        {
            InitializeReferences();
            ApplyRotation();
        }

        private void InitializeReferences()
        {
            if (barrelTransform == null)
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

        private void Update()
        {
            if (IsFireInputPressed())
            {
                Shoot();
            }
        }

        /// <summary>
        /// Comprueba si se ha pulsado la tecla de disparo (Barra Espaciadora),
        /// compatible con Input System Package e Input Manager clásico.
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
        /// Aplica la rotación del cañón alrededor del eje Z según el ángulo seleccionado.
        /// Convención: 0° = horizontal hacia +X, 90° = vertical hacia +Y.
        /// </summary>
        public void ApplyRotation()
        {
            Transform target = barrelTransform != null ? barrelTransform : transform;
            if (target != null)
            {
                float zAngle = -(90f - LaunchAngle);
                target.localRotation = Quaternion.Euler(0f, 0f, zAngle);
            }
        }

        /// <summary>
        /// Establece el ángulo de disparo y actualiza la rotación inmediatamente.
        /// </summary>
        public void SetAngle(float angleInDegrees)
        {
            LaunchAngle = angleInDegrees;
        }

        /// <summary>
        /// Delega el cálculo del impulso inicial al Modelo (BallisticData).
        /// </summary>
        public float CalculateInitialImpulse()
        {
            return Model.CalculateInitialImpulse();
        }

        /// <summary>
        /// Ejecuta el disparo de un proyectil aprovechando el Object Pool si está disponible.
        /// </summary>
        public void Shoot()
        {
            Vector3 fireDirection = spawnPoint != null ? spawnPoint.up : (barrelTransform != null ? barrelTransform.up : transform.up);
            Vector3 firePosition = spawnPoint != null ? spawnPoint.position : transform.position;
            Quaternion fireRotation = spawnPoint != null ? spawnPoint.rotation : transform.rotation;

            float impulse = CalculateInitialImpulse();
            float mass = Model.ProjectileMass;

            // Prioridad 1: Obtener instancia desde el Object Pool
            if (projectilePool != null)
            {
                BallisticProjectile pooledProjectile = projectilePool.Get();
                if (pooledProjectile != null)
                {
                    pooledProjectile.transform.SetParent(null);
                    pooledProjectile.transform.SetPositionAndRotation(firePosition, fireRotation);
                    pooledProjectile.transform.localScale = Vector3.one;
                    
                    // Escuchar el impacto para notificar a la UI y actualizar el Modelo
                    void HandleImpact(ProjectileImpactData impactData)
                    {
                        pooledProjectile.OnImpactDetected -= HandleImpact;
                        Model.SetImpactResults(impactData.HorizontalDistance, impactData.MaxHeight);
                        OnLastShotImpact?.Invoke(impactData);
                    }
                    pooledProjectile.OnImpactDetected += HandleImpact;

                    pooledProjectile.Launch(fireDirection, impulse, mass);
                    return;
                }
            }

            // Prioridad 2: Fallback instanciando el Prefab directamente en la raíz del mundo
            if (projectile != null)
            {
                GameObject newProjectile = Instantiate(projectile, firePosition, fireRotation, null);
                newProjectile.transform.localScale = Vector3.one;

                if (newProjectile.TryGetComponent<ProjectileBase>(out var projBase))
                {
                    void HandleImpact(ProjectileImpactData impactData)
                    {
                        projBase.OnImpactDetected -= HandleImpact;
                        Model.SetImpactResults(impactData.HorizontalDistance, impactData.MaxHeight);
                        OnLastShotImpact?.Invoke(impactData);
                    }
                    projBase.OnImpactDetected += HandleImpact;

                    projBase.Launch(fireDirection, impulse, mass);
                }
                else if (newProjectile.TryGetComponent<ILaunchable>(out var launchable))
                {
                    launchable.Launch(fireDirection, impulse, mass);
                }
                else if (newProjectile.TryGetComponent<Rigidbody>(out var rb))
                {
                    rb.mass = mass;
                    rb.AddForce(fireDirection.normalized * impulse, ForceMode.Impulse);
                }
            }
            else
            {
                Debug.LogWarning("[Arma] No se ha configurado ni un ProjectilePool ni un Prefab de proyectil.", this);
            }
        }
    }
}
