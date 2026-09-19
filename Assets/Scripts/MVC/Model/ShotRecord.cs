using System;
using UnityEngine;

namespace Simu1.Model
{
    /// <summary>
    /// Registro inmutable con los parámetros de entrada y magnitudes físicas medidas en un ensayo balístico.
    /// Orientado a laboratorio y toma de datos científicos.
    /// </summary>
    public readonly struct ShotRecord
    {
        // Metadatos
        public int AttemptIndex { get; }
        public DateTime Timestamp { get; }

        // Parámetros de Entrada (Configuración Balística)
        public float Angle { get; }
        public float Force { get; }
        public float Mass { get; }
        public float BarrelLength { get; }
        public float InitialVelocity { get; }
        public float InitialImpulse { get; }

        // Magnitudes Físicas Medidas en el Impacto
        public float FlightTime { get; }
        public float HorizontalDistance { get; }
        public float MaxHeight { get; }
        public Vector3 ImpactPosition { get; }
        public float RelativeSpeed { get; }
        public float CollisionImpulse { get; }

        // Respuesta Estructural
        public int FallenPieces { get; }
        public int TotalPieces { get; }
        public int Score { get; }

        public ShotRecord(
            int attemptIndex,
            float angle,
            float force,
            float mass,
            float barrelLength,
            float flightTime,
            float horizontalDistance,
            float maxHeight,
            Vector3 impactPosition,
            float relativeSpeed,
            float collisionImpulse,
            int fallenPieces,
            int totalPieces,
            int score = 0,
            DateTime? timestamp = null)
        {
            AttemptIndex = attemptIndex;
            Angle = angle;
            Force = force;
            Mass = mass;
            BarrelLength = barrelLength;
            InitialVelocity = mass > 0.0001f ? (float)Math.Sqrt((2.0 * force * barrelLength) / mass) : 0f;
            InitialImpulse = (float)Math.Sqrt(2.0 * mass * force * barrelLength);

            FlightTime = flightTime;
            HorizontalDistance = horizontalDistance;
            MaxHeight = maxHeight;
            ImpactPosition = impactPosition;
            RelativeSpeed = relativeSpeed;
            CollisionImpulse = collisionImpulse;

            FallenPieces = fallenPieces;
            TotalPieces = totalPieces;
            Score = score;
            Timestamp = timestamp ?? DateTime.Now;
        }

        public override string ToString()
        {
            return $"[Ensayo #{AttemptIndex}] Tiempo de vuelo: {FlightTime:F2} s | Punto de impacto: ({ImpactPosition.x:F1}, {ImpactPosition.y:F1}, {ImpactPosition.z:F1}) | Vel. relativa: {RelativeSpeed:F1} m/s | Impulso de colisión: {CollisionImpulse:F1} N·s | Piezas derribadas: {FallenPieces}/{TotalPieces}";
        }
    }
}
