using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Simu1.Model;
using Simu1.Services;
using Unity.Services.CloudSave;
using UnityEngine;

namespace Simu1.Persistence
{
    /// <summary>
    /// Implementación del repositorio de persistencia balística utilizando UGS Cloud Save (Player Data).
    /// Permite almacenar y consultar el historial de disparos y ensayos acumulados en la nube.
    /// Asegura un guardado atómico sin sobreescribir sesiones previas y una consulta no bloqueante.
    /// </summary>
    public class UgsSimulationRepository : SimulationRepository
    {
        [Header("Configuración de UGS Cloud Save")]
        [Tooltip("Clave utilizada para almacenar la lista de ensayos en Cloud Save (Player Data).")]
        [SerializeField] private string historyKey = "simulation_history";

        [Tooltip("Si está activo, carga el historial remoto en segundo plano al arrancar.")]
        [SerializeField] private bool preloadOnStart = true;

        private readonly List<SavedShotEntry> cachedHistory = new List<SavedShotEntry>();
        private bool hasFetchedRemoteHistory;
        private readonly object cacheLock = new object();

        public string HistoryKey => historyKey;
        public IReadOnlyList<SavedShotEntry> CachedHistory => cachedHistory;

        private void Start()
        {
            if (preloadOnStart && Application.isPlaying)
            {
                _ = PreloadRemoteHistoryAsync();
            }
        }

        private async Task PreloadRemoteHistoryAsync()
        {
            try
            {
                await LoadHistoryAsync();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[UgsSimulationRepository] Pre-carga de historial remoto no completada: {ex.Message}");
            }
        }

        /// <summary>
        /// Guarda un disparo en UGS Cloud Save de manera asíncrona.
        /// Si es el primer guardado de la sesión, descarga el historial existente para acumular los nuevos datos.
        /// </summary>
        public override async Task SaveShotAsync(SavedShotEntry shot)
        {
            if (shot == null) return;

            // Asegurar que UGS esté inicializado y autenticado
            if (!UgsInitializer.IsReady)
            {
                bool initOk = await UgsInitializer.InitializeAsync();
                if (!initOk)
                {
                    Debug.LogWarning("[UgsSimulationRepository] UGS no está listo. El disparo se conservará solo en memoria local.");
                    lock (cacheLock)
                    {
                        cachedHistory.Add(shot);
                    }
                    NotifyShotSaved(shot);
                    return;
                }
            }

            // Si aún no hemos traído el historial remoto previo, lo sincronizamos primero para no sobrescribirlo
            if (!hasFetchedRemoteHistory)
            {
                await LoadHistoryAsync();
            }

            lock (cacheLock)
            {
                cachedHistory.Add(shot);
            }

            try
            {
                SimulationHistoryWrapper wrapper;
                lock (cacheLock)
                {
                    wrapper = new SimulationHistoryWrapper(new List<SavedShotEntry>(cachedHistory));
                }

                // Guardar directamente el objeto wrapper para que UGS Cloud Save lo almacene
                // como un objeto JSON nativo formateado y estructurado en el Unity Dashboard,
                // en lugar de una cadena de texto plana entrecomillada con barras invertidas (\" \").
                var dataToSave = new Dictionary<string, object>
                {
                    { historyKey, wrapper }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
                Debug.Log($"[UgsSimulationRepository] Disparo #{shot.attemptIndex} guardado en UGS Cloud Save como JSON estructurado (Total en nube: {wrapper.shots.Count}).");
                NotifyShotSaved(shot);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UgsSimulationRepository] Error al guardar el disparo en Cloud Save: {ex.Message}");
            }
        }

        /// <summary>
        /// Consulta y descarga el historial de ensayos desde UGS Cloud Save.
        /// </summary>
        public override async Task<List<SavedShotEntry>> LoadHistoryAsync()
        {
            if (!UgsInitializer.IsReady)
            {
                bool initOk = await UgsInitializer.InitializeAsync();
                if (!initOk)
                {
                    Debug.LogWarning("[UgsSimulationRepository] No se pudo inicializar UGS para cargar datos. Retornando caché local.");
                    lock (cacheLock)
                    {
                        return new List<SavedShotEntry>(cachedHistory);
                    }
                }
            }

            try
            {
                var keysToLoad = new HashSet<string> { historyKey };
                var loadedData = await CloudSaveService.Instance.Data.Player.LoadAsync(keysToLoad);

                if (loadedData != null && loadedData.TryGetValue(historyKey, out var item) && item?.Value != null)
                {
                    SimulationHistoryWrapper wrapper = null;

                    // 1. Intento de deserialización directa mediante Newtonsoft en CloudSave SDK
                    try
                    {
                        wrapper = item.Value.GetAs<SimulationHistoryWrapper>();
                    }
                    catch
                    {
                        // Fallback
                    }

                    // 1.1 Si fue guardado directamente como array JSON
                    if (wrapper == null || wrapper.shots == null)
                    {
                        try
                        {
                            var shotList = item.Value.GetAs<List<SavedShotEntry>>();
                            if (shotList != null)
                            {
                                wrapper = new SimulationHistoryWrapper(shotList);
                            }
                        }
                        catch
                        {
                            // Fallback a string
                        }
                    }

                    // 2. Si no devolvió wrapper, intentar como string plano con JsonUtility (formato legacy)
                    if (wrapper == null || wrapper.shots == null)
                    {
                        string rawJson = item.Value.GetAsString();
                        if (!string.IsNullOrEmpty(rawJson))
                        {
                            // Si el string viene serializado dos veces (cadena JSON entrecomillada)
                            if (rawJson.StartsWith("\"") && rawJson.EndsWith("\""))
                            {
                                try
                                {
                                    rawJson = JsonUtility.FromJson<string>(rawJson);
                                }
                                catch
                                {
                                    // Ignorar si falla el unwrap
                                }
                            }

                            wrapper = JsonUtility.FromJson<SimulationHistoryWrapper>(rawJson);
                        }
                    }

                    if (wrapper != null && wrapper.shots != null)
                    {
                        lock (cacheLock)
                        {
                            cachedHistory.Clear();
                            cachedHistory.AddRange(wrapper.shots);
                            hasFetchedRemoteHistory = true;
                        }

                        Debug.Log($"[UgsSimulationRepository] Historial recuperado de UGS Cloud Save: {cachedHistory.Count} entradas.");
                        NotifyHistoryLoaded(new List<SavedShotEntry>(cachedHistory));
                        return new List<SavedShotEntry>(cachedHistory);
                    }
                }

                // Si la clave no existía aún en el servidor (primer uso del jugador)
                lock (cacheLock)
                {
                    hasFetchedRemoteHistory = true;
                    NotifyHistoryLoaded(new List<SavedShotEntry>(cachedHistory));
                    return new List<SavedShotEntry>(cachedHistory);
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UgsSimulationRepository] Error al consultar datos en Cloud Save: {ex.Message}");
                lock (cacheLock)
                {
                    return new List<SavedShotEntry>(cachedHistory);
                }
            }
        }

        /// <summary>
        /// Borra el historial guardado en la nube (útil para reiniciar ensayos o tests).
        /// </summary>
        public async Task ClearRemoteHistoryAsync()
        {
            lock (cacheLock)
            {
                cachedHistory.Clear();
            }

            if (!UgsInitializer.IsReady)
            {
                await UgsInitializer.InitializeAsync();
            }

            try
            {
                var emptyWrapper = new SimulationHistoryWrapper();
                var dataToSave = new Dictionary<string, object>
                {
                    { historyKey, emptyWrapper }
                };

                await CloudSaveService.Instance.Data.Player.SaveAsync(dataToSave);
                Debug.Log("[UgsSimulationRepository] Historial remoto vaciado con éxito en UGS Cloud Save.");
                NotifyHistoryLoaded(new List<SavedShotEntry>());
            }
            catch (Exception ex)
            {
                Debug.LogError($"[UgsSimulationRepository] Error al limpiar historial en Cloud Save: {ex.Message}");
            }
        }
    }
}
