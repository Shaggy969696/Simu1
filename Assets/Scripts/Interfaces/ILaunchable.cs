using UnityEngine;

namespace Simu1.Projectiles
{
    /// <summary>
    /// Contrato para cualquier proyectil u objeto que pueda ser lanzado con dirección, fuerza y masa.
    /// Cumple con el Principio de Segregación de Interfaces (ISP) y Abstracción.
    /// </summary>
    public interface ILaunchable
    {
        void Launch(Vector3 direction, float force, float mass);
    }
}
