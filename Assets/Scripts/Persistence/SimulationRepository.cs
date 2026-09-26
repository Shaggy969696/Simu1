using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simu1.Model;
using UnityEngine;

namespace Simu1.Persistence
{
    /// <summary>
    /// Contrato abstracto para la persistencia de ensayos balísticos (Repository Pattern).
    /// Permite desacoplar el origen de datos (UGS Cloud Save, archivo local o Mock de tests) del controlador y la vista.
    /// Hereda de MonoBehaviour para facilitar su inyección y configuración en el Inspector de Unity.
    /// </summary>
    public abstract class SimulationRepository : MonoBehaviour
    {
        public event Action<SavedShotEntry> OnShotSaved;
        public event Action<List<SavedShotEntry>> OnHistoryLoaded;

        /// <summary>
        /// Guarda un ensayo de forma asíncrona en el almacenamiento persistente.
        /// </summary>
        /// <param name="shot">Datos del disparo e impacto a almacenar.</param>
        public abstract Task SaveShotAsync(SavedShotEntry shot);

        /// <summary>
        /// Carga el historial completo de ensayos almacenados de forma asíncrona.
        /// </summary>
        /// <returns>Lista con todos los registros guardados.</returns>
        public abstract Task<List<SavedShotEntry>> LoadHistoryAsync();

        protected void NotifyShotSaved(SavedShotEntry shot)
        {
            OnShotSaved?.Invoke(shot);
        }

        protected void NotifyHistoryLoaded(List<SavedShotEntry> history)
        {
            OnHistoryLoaded?.Invoke(history);
        }
    }
}
