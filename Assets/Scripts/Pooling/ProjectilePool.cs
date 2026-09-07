using Simu1.Interfaces;
using Simu1.Projectiles;
using UnityEngine;
using UnityEngine.Pool;

namespace Simu1.Pooling
{
    /// <summary>
    /// Gestor de Object Pooling para proyectiles balísticos.
    /// Utiliza UnityEngine.Pool.ObjectPool para maximizar el rendimiento y evitar recolección de basura.
    /// Cumple con SRP (responsabilidad única de reciclaje) y DIP (implementa IProjectilePool).
    /// </summary>
    public class ProjectilePool : MonoBehaviour, IProjectilePool
    {
        [Header("Configuración del Prefab")]
        [Tooltip("Prefab del proyectil que implementa BallisticProjectile.")]
        [SerializeField] private BallisticProjectile projectilePrefab;

        [Header("Parámetros del Pool")]
        [Tooltip("Cantidad inicial de proyectiles pre-instanciados.")]
        [SerializeField] private int defaultCapacity = 10;

        [Tooltip("Cantidad máxima de proyectiles permitidos en el pool.")]
        [SerializeField] private int maxPoolSize = 30;

        [Tooltip("Contenedor opcional en la jerarquía para mantener organizados los proyectiles.")]
        [SerializeField] private Transform poolContainer;

        private ObjectPool<BallisticProjectile> pool;

        public int DefaultCapacity => defaultCapacity;
        public int MaxPoolSize => maxPoolSize;
        public int ActiveCount => pool != null ? pool.CountActive : 0;
        public int InactiveCount => pool != null ? pool.CountInactive : 0;

        private void Awake()
        {
            InitializePool();
        }

        public void SetPrefab(BallisticProjectile prefab)
        {
            projectilePrefab = prefab;
            if (pool == null)
            {
                InitializePool();
            }
        }

        private void InitializePool()
        {
            if (projectilePrefab == null)
            {
                Debug.LogWarning("[ProjectilePool] No se ha asignado el prefab del proyectil en el Inspector.", this);
                return;
            }

            if (poolContainer == null)
            {
                // Crear un contenedor independiente en la raíz de la escena para evitar emparentar con el cañón
                GameObject containerGO = new GameObject("[Pool_Proyectiles]");
                poolContainer = containerGO.transform;
            }

            pool = new ObjectPool<BallisticProjectile>(
                createFunc: CreateProjectile,
                actionOnGet: OnTakeFromPool,
                actionOnRelease: OnReturnedToPool,
                actionOnDestroy: OnDestroyPoolObject,
                collectionCheck: true,
                defaultCapacity: defaultCapacity,
                maxSize: maxPoolSize
            );
        }

        private BallisticProjectile CreateProjectile()
        {
            BallisticProjectile instance = Instantiate(projectilePrefab, poolContainer);
            instance.gameObject.SetActive(false);

            // Desacoplamiento: el pool se suscribe al evento de desactivación del proyectil
            instance.OnProjectileDeactivated += HandleProjectileDeactivated;

            return instance;
        }

        private void OnTakeFromPool(BallisticProjectile projectile)
        {
            // Desvincular de la jerarquía al salir al mundo y asegurar escala esférica original
            projectile.transform.SetParent(null);
            projectile.transform.localScale = Vector3.one;
            projectile.gameObject.SetActive(true);
        }

        private void OnReturnedToPool(BallisticProjectile projectile)
        {
            projectile.ResetState();
            projectile.transform.localScale = Vector3.one;
            projectile.gameObject.SetActive(false);

            if (poolContainer != null)
            {
                projectile.transform.SetParent(poolContainer, false);
            }
        }

        private void OnDestroyPoolObject(BallisticProjectile projectile)
        {
            if (projectile != null)
            {
                projectile.OnProjectileDeactivated -= HandleProjectileDeactivated;
                Destroy(projectile.gameObject);
            }
        }

        private void HandleProjectileDeactivated(ProjectileBase projectile)
        {
            if (projectile is BallisticProjectile ballisticProj)
            {
                Release(ballisticProj);
            }
        }

        /// <summary>
        /// Obtiene un proyectil disponible del pool.
        /// </summary>
        public BallisticProjectile Get()
        {
            if (pool == null)
            {
                InitializePool();
            }

            return pool.Get();
        }

        /// <summary>
        /// Retorna un proyectil al pool para su reutilización.
        /// </summary>
        public void Release(BallisticProjectile projectile)
        {
            if (pool != null && projectile != null && projectile.gameObject.activeSelf)
            {
                pool.Release(projectile);
            }
        }

        private void OnDestroy()
        {
            pool?.Clear();
        }
    }
}
