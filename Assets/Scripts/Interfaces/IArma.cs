using System;
using Simu1.Projectiles;

namespace Simu1.Interfaces
{
    /// <summary>
    /// Contrato para el cañón/arma balística.
    /// Aplica Abstracción y el Principio de Segregación de Interfaces (ISP).
    /// </summary>
    public interface IArma
    {
        float LaunchAngle { get; set; }
        float LaunchForce { get; set; }
        float ProjectileMass { get; set; }
        float BarrelLength { get; set; }

        event Action<ProjectileImpactData> OnLastShotImpact;

        void Shoot();
        void SetAngle(float angleInDegrees);
    }
}
