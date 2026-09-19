using UnityEngine;

namespace Simu1.Projectiles
{
    /// <summary>
    /// Estructura inmutable que encapsula los datos del impacto de un proyectil.
    /// Garantiza la integridad de datos aplicando Encapsulación y evitando efectos secundarios.
    /// </summary>
    public readonly struct ProjectileImpactData
    {
        public Vector3 LaunchPosition { get; }
        public Vector3 ImpactPosition { get; }
        public float HorizontalDistance { get; }
        public float MaxHeight { get; }
        public GameObject HitObject { get; }
        public float FlightTime { get; }
        public Vector3 RelativeVelocity { get; }
        public float RelativeSpeed => RelativeVelocity.magnitude;
        public Vector3 CollisionImpulse { get; }
        public float ImpulseMagnitude => CollisionImpulse.magnitude;

        public ProjectileImpactData(
            Vector3 launchPosition, 
            Vector3 impactPosition, 
            float horizontalDistance, 
            float maxHeight, 
            GameObject hitObject,
            float flightTime = 0f,
            Vector3 relativeVelocity = default,
            Vector3 collisionImpulse = default)
        {
            LaunchPosition = launchPosition;
            ImpactPosition = impactPosition;
            HorizontalDistance = horizontalDistance;
            MaxHeight = maxHeight;
            HitObject = hitObject;
            FlightTime = flightTime;
            RelativeVelocity = relativeVelocity;
            CollisionImpulse = collisionImpulse;
        }

        public override string ToString()
        {
            return $"[Impacto] Punto: {ImpactPosition} | Distancia XZ: {HorizontalDistance:F2} m | Altura Máx: {MaxHeight:F2} m | Tiempo: {FlightTime:F2} s | Vel. Relativa: {RelativeSpeed:F2} m/s | Impulso: {ImpulseMagnitude:F2} N·s | Objeto: {(HitObject != null ? HitObject.name : "N/A")}";
        }
    }
}
