using System;
using System.Collections.Generic;

namespace Simu1.Model
{
    /// <summary>
    /// Objeto de transferencia de datos (DTO) serializable para almacenar el resultado de un ensayo balístico en UGS Cloud Save.
    /// Contiene tanto las variables de entrada configuradas como las magnitudes físicas medidas tras el impacto.
    /// Los valores de coma flotante se formatean a 2 decimales para una lectura clara y ordenada en el Unity Dashboard.
    /// </summary>
    [System.Serializable]
    public class SavedShotEntry
    {
        public int attemptIndex;
        public string timestamp;
        public float angle;
        public float force;
        public float mass;
        public float distance;
        public bool isHit;
        public int fallenPieces;
        public int score;
        public float flightTime;
        public float maxHeight;
        public float relativeSpeed;
        public float collisionImpulse;

        public SavedShotEntry()
        {
            timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        }

        public SavedShotEntry(
            int attemptIndex,
            float angle,
            float force,
            float mass,
            float distance,
            bool isHit,
            int fallenPieces,
            int score,
            float flightTime = 0f,
            float maxHeight = 0f,
            float relativeSpeed = 0f,
            float collisionImpulse = 0f,
            string timestamp = null)
        {
            this.attemptIndex = attemptIndex;
            this.timestamp = !string.IsNullOrEmpty(timestamp) ? timestamp : DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
            this.angle = (float)Math.Round(angle, 2);
            this.force = (float)Math.Round(force, 1);
            this.mass = (float)Math.Round(mass, 2);
            this.distance = (float)Math.Round(distance, 2);
            this.isHit = isHit;
            this.fallenPieces = fallenPieces;
            this.score = score;
            this.flightTime = (float)Math.Round(flightTime, 2);
            this.maxHeight = (float)Math.Round(maxHeight, 2);
            this.relativeSpeed = (float)Math.Round(relativeSpeed, 2);
            this.collisionImpulse = (float)Math.Round(collisionImpulse, 2);
        }

        /// <summary>
        /// Crea una instancia de SavedShotEntry a partir de un ShotRecord inmutable del modelo.
        /// </summary>
        public static SavedShotEntry FromShotRecord(ShotRecord record, bool isHit)
        {
            return new SavedShotEntry(
                attemptIndex: record.AttemptIndex,
                angle: record.Angle,
                force: record.Force,
                mass: record.Mass,
                distance: record.HorizontalDistance,
                isHit: isHit,
                fallenPieces: record.FallenPieces,
                score: record.Score,
                flightTime: record.FlightTime,
                maxHeight: record.MaxHeight,
                relativeSpeed: record.RelativeSpeed,
                collisionImpulse: record.CollisionImpulse,
                timestamp: record.Timestamp.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            );
        }

        public override string ToString()
        {
            string hitText = isHit ? "SÍ" : "NO";
            return $"[Ensayo #{attemptIndex} | {timestamp}] Ángulo: {angle:F1}° | Fuerza: {force:F0} N | Masa: {mass:F1} kg | Distancia: {distance:F2} m | Impacto: {hitText} | Piezas: {fallenPieces} | Puntos: {score}";
        }
    }

    /// <summary>
    /// Contenedor serializable para la lista de ensayos guardados en UGS Cloud Save.
    /// Compatible con UnityEngine.JsonUtility y Newtonsoft.Json.
    /// </summary>
    [System.Serializable]
    public class SimulationHistoryWrapper
    {
        public List<SavedShotEntry> shots = new List<SavedShotEntry>();

        public SimulationHistoryWrapper()
        {
            shots = new List<SavedShotEntry>();
        }

        public SimulationHistoryWrapper(List<SavedShotEntry> shots)
        {
            this.shots = shots ?? new List<SavedShotEntry>();
        }
    }
}
