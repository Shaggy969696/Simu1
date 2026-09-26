using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace Simu1.Services
{
    /// <summary>
    /// Servicio de inicialización para Unity Gaming Services (UGS).
    /// Ejecuta la inicialización del núcleo de servicios y autenticación anónima del usuario.
    /// Garantiza que UGS Cloud Save esté listo para su uso tanto en runtime como en pruebas.
    /// Previene llamadas concurrentes / reentrantes ("The player is already signing in").
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class UgsInitializer : MonoBehaviour
    {
        public static bool IsReady { get; private set; }
        public static string PlayerId => AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn 
            ? AuthenticationService.Instance.PlayerId 
            : null;

        public static event Action OnInitialized;
        public static event Action<string> OnInitializationFailed;

        [Header("Configuración de Arranque")]
        [Tooltip("Si está activo, inicializa UGS automáticamente durante el Awake en Play Mode.")]
        [SerializeField] private bool initializeOnAwake = true;

        private static Task<bool> activeInitializationTask;
        private static readonly object syncLock = new object();

        private void Awake()
        {
            if (initializeOnAwake && Application.isPlaying && !IsReady)
            {
                _ = InitializeAsync();
            }
        }

        /// <summary>
        /// Inicializa UGS y autentica de forma anónima al jugador de manera asíncrona.
        /// Si ya hay una inicialización en curso, retorna la misma tarea para evitar condiciones de carrera.
        /// </summary>
        /// <returns>True si la inicialización y autenticación fueron exitosas.</returns>
        public static Task<bool> InitializeAsync()
        {
            if (IsReady)
            {
                return Task.FromResult(true);
            }

            lock (syncLock)
            {
                if (activeInitializationTask != null)
                {
                    return activeInitializationTask;
                }

                activeInitializationTask = RunInitializationAsync();
                return activeInitializationTask;
            }
        }

        private static async Task<bool> RunInitializationAsync()
        {
            if (IsReady)
            {
                return true;
            }

            if (!Application.isPlaying)
            {
                Debug.LogWarning("[UgsInitializer] Unity Gaming Services solo puede inicializarse en Play Mode.");
                return false;
            }

            try
            {
                if (UnityServices.State == ServicesInitializationState.Uninitialized)
                {
                    Debug.Log("[UgsInitializer] Inicializando UnityServices...");
                    await UnityServices.InitializeAsync();
                }

                if (AuthenticationService.Instance != null && !AuthenticationService.Instance.IsSignedIn)
                {
                    Debug.Log("[UgsInitializer] Iniciando sesión anónima en UGS Authentication...");
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();
                }

                IsReady = true;
                Debug.Log($"[UgsInitializer] UGS inicializado y autenticado con éxito. Player ID: {PlayerId}");
                OnInitialized?.Invoke();
                return true;
            }
            catch (Exception ex)
            {
                // Si la excepción indica que ya había un proceso de sign-in en curso, esperamos a que concluya
                if (ex.Message.IndexOf("already signing in", StringComparison.OrdinalIgnoreCase) >= 0 && AuthenticationService.Instance != null)
                {
                    Debug.Log("[UgsInitializer] Sign-in en curso detectado, sincronizando estado...");
                    float waitTime = 0f;
                    while (!AuthenticationService.Instance.IsSignedIn && waitTime < 5f)
                    {
                        await Task.Delay(100);
                        waitTime += 0.1f;
                    }

                    if (AuthenticationService.Instance.IsSignedIn)
                    {
                        IsReady = true;
                        Debug.Log($"[UgsInitializer] Sincronización exitosa tras espera. Player ID: {PlayerId}");
                        OnInitialized?.Invoke();
                        return true;
                    }
                }

                Debug.LogError($"[UgsInitializer] Fallo al inicializar UGS: {ex.Message}");
                OnInitializationFailed?.Invoke(ex.Message);
                return false;
            }
            finally
            {
                lock (syncLock)
                {
                    activeInitializationTask = null;
                }
            }
        }

        /// <summary>
        /// Permite restablecer el estado en caso de reinicio de sesión o tests.
        /// </summary>
        public static void ResetStateForTesting()
        {
            lock (syncLock)
            {
                IsReady = false;
                activeInitializationTask = null;
            }
        }
    }
}
