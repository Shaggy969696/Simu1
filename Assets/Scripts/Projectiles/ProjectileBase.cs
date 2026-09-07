using System;
using System.Collections;
using UnityEngine;

namespace Simu1.Projectiles
{
    /// <summary>
    /// Clase base abstracta para proyectiles.
    /// Aplica Abstracción, Encapsulación y Herencia (POO), además de SRP y OCP (SOLID).
    /// </summary>
    public abstract class ProjectileBase : MonoBehaviour, ILaunchable
    {
        [Header("Configuración de Impacto")]
        [Tooltip("Tag del suelo u objeto objetivo para registrar el impacto.")]
        [SerializeField] private string targetTag = "Ground";

        [Tooltip("Tiempo en segundos antes de desactivar el proyectil tras el impacto con el suelo.")]
        [SerializeField] private float autoDeactivateDelay = 3.0f;

        // Eventos desacoplados para observadores externos (DIP)
        public event Action<ProjectileImpactData> OnImpactDetected;
        public event Action<ProjectileBase> OnProjectileDeactivated;

        // Estado interno protegido (Encapsulación)
        protected Vector3 launchPosition;
        protected float maxHeight;
        protected bool isLaunched;
        protected bool hasImpacted;

        // Propiedades públicas de solo lectura
        public Vector3 LaunchPosition => launchPosition;
        public float MaxHeight => maxHeight;
        public bool IsLaunched => isLaunched;
        public bool HasImpacted => hasImpacted;
        public string TargetTag => targetTag;
        public float AutoDeactivateDelay => autoDeactivateDelay;

        private Coroutine deactivationCoroutine;

        protected virtual void FixedUpdate()
        {
            if (isLaunched && !hasImpacted)
            {
                if (transform.position.y > maxHeight)
                {
                    maxHeight = transform.position.y;
                }
            }
        }

        /// <summary>
        /// Método de la interfaz ILaunchable. Prepara el proyectil y delega la física a la clase derivada.
        /// </summary>
        public virtual void Launch(Vector3 direction, float force, float mass)
        {
            // Desvincular de cualquier jerarquía y asegurar escala uniforme nativa en el mundo
            transform.SetParent(null);
            transform.localScale = Vector3.one;

            ResetState();
            launchPosition = transform.position;
            maxHeight = launchPosition.y;
            isLaunched = true;
            hasImpacted = false;

            ApplyLaunchPhysics(direction, force, mass);
        }

        /// <summary>
        /// Implementación polimórfica de la física de lanzamiento (Fuerza, impulso, etc.).
        /// </summary>
        protected abstract void ApplyLaunchPhysics(Vector3 direction, float force, float mass);

        /// <summary>
        /// Reinicia el estado del proyectil para permitir su reutilización en el pool.
        /// </summary>
        public virtual void ResetState()
        {
            if (deactivationCoroutine != null)
            {
                StopCoroutine(deactivationCoroutine);
                deactivationCoroutine = null;
            }

            isLaunched = false;
            hasImpacted = false;
            maxHeight = 0f;
            launchPosition = Vector3.zero;
            transform.localScale = Vector3.one;
        }

        protected virtual void OnCollisionEnter(Collision collision)
        {
            if (!isLaunched || hasImpacted) return;

            if (collision.gameObject.CompareTag(targetTag))
            {
                hasImpacted = true;

                // Punto de contacto con el suelo
                Vector3 contactPoint = collision.contactCount > 0 
                    ? collision.GetContact(0).point 
                    : transform.position;

                // Distancia horizontal en plano XZ
                Vector2 startXZ = new Vector2(launchPosition.x, launchPosition.z);
                Vector2 impactXZ = new Vector2(contactPoint.x, contactPoint.z);
                float horizontalDistance = Vector2.Distance(startXZ, impactXZ);

                // Si alcanzó mayor altura en el frame de colisión
                if (transform.position.y > maxHeight)
                {
                    maxHeight = transform.position.y;
                }

                ProjectileImpactData impactData = new ProjectileImpactData(
                    launchPosition,
                    contactPoint,
                    horizontalDistance,
                    maxHeight,
                    collision.gameObject
                );

                // Notificar impacto
                OnImpactDetected?.Invoke(impactData);

                // Extensibilidad polimórfica post-impacto
                OnPostImpact(collision, impactData);

                // Iniciar temporizador para devolver al pool (manteniendo la física activa)
                deactivationCoroutine = StartCoroutine(DeactivateAfterDelayRoutine());
            }
        }

        /// <summary>
        /// Hook virtual para que clases derivadas reaccionen al impacto si es necesario.
        /// </summary>
        protected virtual void OnPostImpact(Collision collision, ProjectileImpactData data) { }

        private IEnumerator DeactivateAfterDelayRoutine()
        {
            yield return new WaitForSeconds(autoDeactivateDelay);
            OnProjectileDeactivated?.Invoke(this);
            gameObject.SetActive(false);
        }
    }
}
