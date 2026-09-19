using System;

namespace Simu1.Model
{
    /// <summary>
    /// Modelo para la simulación balística (Capa Model en MVC).
    /// Contenedor puro de datos y reglas de simulación física en C# estándar.
    /// No posee dependencias de componentes gráficos, controles visuales ni del motor de escenas.
    /// Completamente comprobable mediante pruebas unitarias puras.
    /// </summary>
    public class BallisticData
    {
        // Constantes y valores por defecto
        public const float MinAngle = 0f;
        public const float MaxAngle = 90f;
        public const float MinForce = 0f;
        public const float MinMass = 0.001f;
        public const float MinBarrelLength = 0.1f;
        public const float DefaultGravity = 9.81f;

        // Variables miembro de estado
        private float launchAngle;
        private float force;
        private float projectileMass;
        private float barrelLength;
        private float gravity;
        private float lastHorizontalDistance;
        private float lastMaxHeight;
        private float lastFlightTime;
        private float lastRelativeVelocity;
        private float lastCollisionImpulse;
        private int lastFallenPieces;
        private int lastScore;
        private bool hasImpactData;

        // Eventos desacoplados para observadores externos del modelo
        public event Action OnDataChanged;
        public event Action<float, float> OnImpactRecorded;
        public event Action OnShotReportUpdated;

        // Propiedades encapsuladas con validación estricta
        public float LaunchAngle
        {
            get => launchAngle;
            set
            {
                float clamped = Math.Max(MinAngle, Math.Min(MaxAngle, value));
                if (Math.Abs(launchAngle - clamped) > 0.0001f)
                {
                    launchAngle = clamped;
                    OnDataChanged?.Invoke();
                }
            }
        }

        public float Force
        {
            get => force;
            set
            {
                float clamped = Math.Max(MinForce, value);
                if (Math.Abs(force - clamped) > 0.0001f)
                {
                    force = clamped;
                    OnDataChanged?.Invoke();
                }
            }
        }

        public float ProjectileMass
        {
            get => projectileMass;
            set
            {
                float clamped = Math.Max(MinMass, value);
                if (Math.Abs(projectileMass - clamped) > 0.0001f)
                {
                    projectileMass = clamped;
                    OnDataChanged?.Invoke();
                }
            }
        }

        public float BarrelLength
        {
            get => barrelLength;
            set
            {
                float clamped = Math.Max(MinBarrelLength, value);
                if (Math.Abs(barrelLength - clamped) > 0.0001f)
                {
                    barrelLength = clamped;
                    OnDataChanged?.Invoke();
                }
            }
        }

        public float Gravity
        {
            get => gravity;
            set
            {
                float clamped = Math.Max(0.001f, value);
                if (Math.Abs(gravity - clamped) > 0.0001f)
                {
                    gravity = clamped;
                    OnDataChanged?.Invoke();
                }
            }
        }

        public float LastHorizontalDistance => lastHorizontalDistance;
        public float LastMaxHeight => lastMaxHeight;
        public float LastFlightTime => lastFlightTime;
        public float LastRelativeVelocity => lastRelativeVelocity;
        public float LastCollisionImpulse => lastCollisionImpulse;
        public int LastFallenPieces => lastFallenPieces;
        public int LastScore => lastScore;
        public bool HasImpactData => hasImpactData;

        /// <summary>
        /// Constructor del modelo con parámetros iniciales opcionales.
        /// </summary>
        public BallisticData(float initialAngle = 45f, float initialForce = 500f, float initialMass = 2f, float initialBarrelLength = 2f, float initialGravity = DefaultGravity)
        {
            launchAngle = Math.Max(MinAngle, Math.Min(MaxAngle, initialAngle));
            force = Math.Max(MinForce, initialForce);
            projectileMass = Math.Max(MinMass, initialMass);
            barrelLength = Math.Max(MinBarrelLength, initialBarrelLength);
            gravity = Math.Max(0.001f, initialGravity);
            lastHorizontalDistance = 0f;
            lastMaxHeight = 0f;
            lastFlightTime = 0f;
            lastRelativeVelocity = 0f;
            lastCollisionImpulse = 0f;
            lastFallenPieces = 0;
            lastScore = 0;
            hasImpactData = false;
        }

        #region Reglas de Simulación Balística y Fórmulas Físicas

        /// <summary>
        /// Calcula el impulso inicial (N*s) aplicando el Teorema de Trabajo - Energía Cinética:
        /// W = F * L = 0.5 * m * v0^2  ==>  v0 = sqrt(2 * F * L / m)
        /// Impulso J = m * v0 = sqrt(2 * m * F * L)
        /// </summary>
        public float CalculateInitialImpulse()
        {
            return (float)Math.Sqrt(2.0 * projectileMass * force * barrelLength);
        }

        /// <summary>
        /// Calcula la velocidad inicial de salida del proyectil (m/s).
        /// v0 = sqrt(2 * F * L / m)
        /// </summary>
        public float CalculateInitialVelocity()
        {
            return (float)Math.Sqrt((2.0 * force * barrelLength) / projectileMass);
        }

        /// <summary>
        /// Calcula el alcance horizontal teórico máximo (m) en ausencia de fricción del aire.
        /// R = (v0^2 * sin(2 * theta)) / g
        /// </summary>
        public float CalculateTheoreticalRange()
        {
            float v0 = CalculateInitialVelocity();
            double angleRad = launchAngle * Math.PI / 180.0;
            return (float)((v0 * v0 * Math.Sin(2.0 * angleRad)) / gravity);
        }

        /// <summary>
        /// Calcula la altura máxima teórica (m) alcanzada por el proyectil sobre el punto de disparo.
        /// H_max = (v0 * sin(theta))^2 / (2 * g)
        /// </summary>
        public float CalculateTheoreticalMaxHeight()
        {
            float v0 = CalculateInitialVelocity();
            double angleRad = launchAngle * Math.PI / 180.0;
            double v0y = v0 * Math.Sin(angleRad);
            return (float)((v0y * v0y) / (2.0 * gravity));
        }

        /// <summary>
        /// Calcula el tiempo de vuelo teórico (s) hasta impactar en el mismo plano horizontal.
        /// t_vuelo = 2 * v0 * sin(theta) / g
        /// </summary>
        public float CalculateTheoreticalFlightTime()
        {
            float v0 = CalculateInitialVelocity();
            double angleRad = launchAngle * Math.PI / 180.0;
            return (float)((2.0 * v0 * Math.Sin(angleRad)) / gravity);
        }

        /// <summary>
        /// Calcula la puntuación del disparo combinando piezas derribadas e impulso transferido.
        /// </summary>
        public int CalculateScore(int fallenPieces, float impulse)
        {
            int pieceScore = Math.Max(0, fallenPieces) * 100;
            int impulseScore = (int)Math.Round(Math.Max(0f, impulse) * 2f);
            return pieceScore + impulseScore;
        }

        /// <summary>
        /// Actualiza los resultados registrados del último impacto del proyectil con datos físicos completos.
        /// </summary>
        public void SetImpactResults(
            float horizontalDistance, 
            float maxHeight, 
            float flightTime = 0f, 
            float relativeVelocity = 0f, 
            float collisionImpulse = 0f, 
            int fallenPieces = 0)
        {
            lastHorizontalDistance = Math.Max(0f, horizontalDistance);
            lastMaxHeight = maxHeight;
            lastFlightTime = Math.Max(0f, flightTime);
            lastRelativeVelocity = Math.Max(0f, relativeVelocity);
            lastCollisionImpulse = Math.Max(0f, collisionImpulse);
            lastFallenPieces = Math.Max(0, fallenPieces);
            lastScore = CalculateScore(lastFallenPieces, lastCollisionImpulse);
            hasImpactData = true;

            OnImpactRecorded?.Invoke(lastHorizontalDistance, lastMaxHeight);
            OnShotReportUpdated?.Invoke();
            OnDataChanged?.Invoke();
        }

        /// <summary>
        /// Restablece el registro del último impacto y las métricas asociadas.
        /// </summary>
        public void ResetImpact()
        {
            lastHorizontalDistance = 0f;
            lastMaxHeight = 0f;
            lastFlightTime = 0f;
            lastRelativeVelocity = 0f;
            lastCollisionImpulse = 0f;
            lastFallenPieces = 0;
            lastScore = 0;
            hasImpactData = false;

            OnShotReportUpdated?.Invoke();
            OnDataChanged?.Invoke();
        }

        #endregion
    }
}
