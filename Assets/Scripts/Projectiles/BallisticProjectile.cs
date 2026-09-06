using UnityEngine;

namespace Simu1.Projectiles
{
    /// <summary>
    /// Proyectil que utiliza el motor físico de Unity (Rigidbody) para su desplazamiento balístico.
    /// Emplea ContinuousDynamic e Interpolate para colisiones confiables y movimiento fluido.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class BallisticProjectile : ProjectileBase
    {
        private Rigidbody rb;

        private void Awake()
        {
            InitializeRigidbody();
        }

        private void Reset()
        {
            InitializeRigidbody();
        }

        private void InitializeRigidbody()
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (rb != null)
            {
                rb.useGravity = true;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }

        public override void ResetState()
        {
            base.ResetState();

            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            if (rb != null)
            {
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        protected override void ApplyLaunchPhysics(Vector3 direction, float force, float mass)
        {
            if (rb == null)
            {
                rb = GetComponent<Rigidbody>();
            }

            // Asignar masa solicitada por el usuario
            rb.mass = Mathf.Max(0.001f, mass);

            // Asegurar que no arrastre inercia previa
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            // Aplicar el impulso inicial según la dirección y fuerza
            rb.AddForce(direction.normalized * force, ForceMode.Impulse);
        }

        protected override void OnPostImpact(Collision collision, ProjectileImpactData data)
        {
            // El proyectil conserva sus físicas activas, rodando o rebotando según su Rigidbody/Material.
            Debug.Log($"[Simulador Balístico] {data}");
        }
    }
}
