using Simu1.Projectiles;

namespace Simu1.Interfaces
{
    /// <summary>
    /// Interfaz para desacoplar el gestor de pool del resto del sistema.
    /// Aplica el Principio de Inversión de Dependencias (DIP) y Segregación de Interfaces (ISP).
    /// </summary>
    public interface IProjectilePool
    {
        BallisticProjectile Get();
        void Release(BallisticProjectile projectile);
        void ReturnAllActive();
    }
}
