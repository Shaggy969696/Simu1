using System;
using UnityEngine;

namespace Simu1.Targets
{
    /// <summary>
    /// Componente que se añade a cada bloque o pieza móvil de una estructura objetivo.
    /// Supervisa si la pieza ha sido derribada (por desplazamiento, caída, inclinación o quiebre de Joint)
    /// y permite restablecer su posición y físicas fácilmente.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class TargetPiece : MonoBehaviour
    {
        [Header("Puntuación")]
        [Tooltip("Puntos que otorga derribar esta pieza en el reporte final.")]
        [SerializeField] private int pointValue = 100;

        [Header("Condiciones de Derribo")]
        [Tooltip("Distancia en metros desde la posición inicial para considerarse derribada.")]
        [SerializeField] private float fallDistanceThreshold = 1.0f;

        [Tooltip("Ángulo de inclinación en grados respecto a su rotación original para considerarse derribada.")]
        [SerializeField] private float tiltAngleThreshold = 40.0f;

        [Tooltip("Si la coordenada Y cae por debajo de este valor absoluto, se considera derribada.")]
        [SerializeField] private float minimumYThreshold = 0.1f;

        [Tooltip("Si es true, la ruptura de un Joint conectado a esta pieza marca automáticamente el derribo.")]
        [SerializeField] private bool toppleOnJointBreak = true;

        [Tooltip("Si es true o tiene Hinge/Spring, es una diana colgante: no se derriba por oscilar/estirarse, solo si se rompe su unión o cae al suelo.")]
        [SerializeField] private bool isSuspendedTarget = false;

        [Tooltip("Tiempo de gracia en segundos al iniciar la escena para permitir que la física se asiente sin falsos derribos.")]
        [SerializeField] private float startupGracePeriod = 0.5f;

        [Header("Estado Actual (Solo Lectura)")]
        [SerializeField] private bool isToppled;

        // Estado inicial guardado para el reinicio
        private Vector3 initialLocalPosition;
        private Quaternion initialLocalRotation;
        private Transform initialParent;
        private Rigidbody rb;
        private bool hasSavedInitialState;

        // Respaldo de Joint original para poder reconstruirlo al reiniciar si se fractura
        private struct JointBackup
        {
            public Type JointType;
            public Rigidbody ConnectedBody;
            public Vector3 Anchor;
            public Vector3 ConnectedAnchor;
            public float BreakForce;
            public float BreakTorque;
            public bool EnableCollision;
            public bool AutoConfigureConnectedAnchor;
            public bool EnablePreprocessing;
            public float MassScale;
            public float ConnectedMassScale;

            // Hinge Joint
            public Vector3 Axis;
            public bool UseLimits;
            public JointLimits Limits;
            public bool UseSpring;
            public JointSpring Spring;
            public bool UseMotor;
            public JointMotor Motor;

            // Spring Joint
            public float SpringStrength;
            public float Damper;
            public float MinDistance;
            public float MaxDistance;
            public float Tolerance;
        }

        private JointBackup originalJointConfig;
        private bool hadJoint;

        public bool IsToppled => isToppled;
        public int PointValue => pointValue;
        public bool HasSavedInitialState => hasSavedInitialState;

        public event Action<TargetPiece> OnPieceToppled;

        private void Awake()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (GetComponent<HingeJoint>() != null || GetComponent<SpringJoint>() != null)
            {
                isSuspendedTarget = true;
            }
            SaveInitialState();
        }

        private void OnValidate()
        {
            if (GetComponent<HingeJoint>() != null || GetComponent<SpringJoint>() != null)
            {
                isSuspendedTarget = true;
            }
        }

        /// <summary>
        /// Guarda la posición, rotación y parámetros de Joint iniciales.
        /// </summary>
        public void SaveInitialState()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            initialLocalPosition = transform.localPosition;
            initialLocalRotation = transform.localRotation;
            initialParent = transform.parent;

            Joint j = GetComponent<Joint>();
            if (j != null)
            {
                hadJoint = true;
                originalJointConfig = new JointBackup
                {
                    JointType = j.GetType(),
                    ConnectedBody = j.connectedBody,
                    Anchor = j.anchor,
                    ConnectedAnchor = j.connectedAnchor,
                    BreakForce = j.breakForce,
                    BreakTorque = j.breakTorque,
                    EnableCollision = j.enableCollision,
                    AutoConfigureConnectedAnchor = j.autoConfigureConnectedAnchor,
                    EnablePreprocessing = j.enablePreprocessing,
                    MassScale = j.massScale,
                    ConnectedMassScale = j.connectedMassScale
                };

                if (j is HingeJoint hj)
                {
                    originalJointConfig.Axis = hj.axis;
                    originalJointConfig.UseLimits = hj.useLimits;
                    originalJointConfig.Limits = hj.limits;
                    originalJointConfig.UseSpring = hj.useSpring;
                    originalJointConfig.Spring = hj.spring;
                    originalJointConfig.UseMotor = hj.useMotor;
                    originalJointConfig.Motor = hj.motor;
                }
                else if (j is SpringJoint sj)
                {
                    originalJointConfig.SpringStrength = sj.spring;
                    originalJointConfig.Damper = sj.damper;
                    originalJointConfig.MinDistance = sj.minDistance;
                    originalJointConfig.MaxDistance = sj.maxDistance;
                    originalJointConfig.Tolerance = sj.tolerance;
                }
            }
            hasSavedInitialState = true;
        }

        private void Update()
        {
            if (!isToppled)
            {
                CheckIfToppled();
            }
        }

        /// <summary>
        /// Evalúa si la pieza cumple cualquiera de las condiciones físicas para considerarse derribada.
        /// </summary>
        public bool CheckIfToppled()
        {
            if (isToppled) return true;

            // Esperar el periodo de gracia inicial para permitir el asentamiento gravitatorio sin falsos positivos
            if (Application.isPlaying && Time.timeSinceLevelLoad < startupGracePeriod) return false;

            // 1. Verificación por altura mínima absoluta (caída al suelo)
            if (transform.position.y < minimumYThreshold)
            {
                MarkAsToppled("Caída por debajo de la altura mínima Y");
                return true;
            }

            // Para dianas suspendidas (péndulos Hinge o dianas Spring), balancearse o estirarse es su comportamiento natural
            // Solo se consideran derribadas si su unión física se quiebra o caen al suelo
            if (isSuspendedTarget || GetComponent<HingeJoint>() != null || GetComponent<SpringJoint>() != null) return false;

            // 2. Verificación por distancia respecto a la posición inicial
            Vector3 currentPos = initialParent != null ? initialParent.TransformPoint(initialLocalPosition) : initialLocalPosition;
            float distance = Vector3.Distance(transform.position, currentPos);
            if (distance > fallDistanceThreshold)
            {
                MarkAsToppled($"Desplazamiento excedido ({distance:F2} m > {fallDistanceThreshold:F2} m)");
                return true;
            }

            // 3. Verificación por inclinación angular
            Quaternion currentRot = initialParent != null ? initialParent.rotation * initialLocalRotation : initialLocalRotation;
            float angle = Quaternion.Angle(transform.rotation, currentRot);
            if (angle > tiltAngleThreshold)
            {
                MarkAsToppled($"Inclinación angular excedida ({angle:F1}° > {tiltAngleThreshold:F1}°)");
                return true;
            }

            return false;
        }

        private void MarkAsToppled(string reason)
        {
            if (isToppled) return;

            isToppled = true;
            OnPieceToppled?.Invoke(this);
        }

        /// <summary>
        /// Se dispara automáticamente cuando un Joint conectado a este Rigidbody se rompe por fuerza excesiva.
        /// </summary>
        private void OnJointBreak(float breakForce)
        {
            if (Application.isPlaying && Time.timeSinceLevelLoad < startupGracePeriod) return;

            if (toppleOnJointBreak && !isToppled)
            {
                MarkAsToppled($"Ruptura de Joint por fuerza de {breakForce:F1} N");
            }
        }

        /// <summary>
        /// Restablece la pieza a su estado y orientación original, eliminando inercias físicas y reconstruyendo el Joint si se rompió.
        /// </summary>
        [ContextMenu("Restablecer Pieza")]
        public void ResetPiece()
        {
            if (!hasSavedInitialState)
            {
                SaveInitialState();
            }

            // Si esta pieza cuelga de un cuerpo conectado (ej. Viga_Superior) y ese cuerpo también se cayó,
            // restablecer toda la estructura coordinadamente para evitar desfases o explosiones de física.
            if (hadJoint && originalJointConfig.ConnectedBody != null && 
                originalJointConfig.ConnectedBody.TryGetComponent<TargetPiece>(out var parentPiece) && 
                parentPiece.IsToppled)
            {
                var manager = GetComponentInParent<TargetStructureManager>();
                if (manager != null)
                {
                    manager.ResetStructure();
                    return;
                }
            }

            PrepareForReset_DisconnectJoint();
            PrepareForReset_FreezeKinematic();
            PrepareForReset_Reposition();
            Physics.SyncTransforms();
            PrepareForReset_RebuildJoint();
            PrepareForReset_UnfreezeDynamic();
        }

        public void PrepareForReset_DisconnectJoint()
        {
            if (!hasSavedInitialState)
            {
                SaveInitialState();
            }

            isToppled = false;
            // Los joints intactos se conservan para evitar pérdidas de referencias y errores en runtime o editor.
            // Solo los joints que se hayan fracturado durante la simulación (GetComponent<Joint>() == null)
            // se reconstruirán en PrepareForReset_RebuildJoint().
        }

        public void PrepareForReset_FreezeKinematic()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.linearVelocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }
                rb.isKinematic = true;
            }
        }

        public void PrepareForReset_Reposition()
        {
            transform.localPosition = initialLocalPosition;
            transform.localRotation = initialLocalRotation;
        }

        public void PrepareForReset_RebuildJoint()
        {
            // Solo reconstruir si tenía un joint originalmente y se rompió por un impacto físico
            if (hadJoint && GetComponent<Joint>() == null)
            {
                RestoreJoint();
            }
        }

        public void PrepareForReset_UnfreezeDynamic()
        {
            if (rb == null) rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = false;
                rb.linearVelocity = Vector3.zero;
                rb.angularVelocity = Vector3.zero;
            }
        }

        private void RestoreJoint()
        {
            if (!hadJoint || originalJointConfig.JointType == null) return;
            if (GetComponent<Joint>() != null) return; // Si ya existe, no duplicar

            Joint newJoint = gameObject.AddComponent(originalJointConfig.JointType) as Joint;
            if (newJoint == null) return;

            // CRÍTICO: Desactivar autoConfigureConnectedAnchor PRIMERO para evitar que Unity
            // calcule connectedAnchor usando el ancla por defecto (0,0,0) produciendo desfasajes
            // de hasta 1.7 metros que destruyen la estructura con impulsos explosivos.
            newJoint.autoConfigureConnectedAnchor = false;
            newJoint.anchor = originalJointConfig.Anchor;
            newJoint.connectedAnchor = originalJointConfig.ConnectedAnchor;
            newJoint.connectedBody = originalJointConfig.ConnectedBody;
            newJoint.breakForce = originalJointConfig.BreakForce;
            newJoint.breakTorque = originalJointConfig.BreakTorque;
            newJoint.enableCollision = originalJointConfig.EnableCollision;
            newJoint.enablePreprocessing = originalJointConfig.EnablePreprocessing;
            newJoint.massScale = originalJointConfig.MassScale;
            newJoint.connectedMassScale = originalJointConfig.ConnectedMassScale;

            if (newJoint is HingeJoint hj)
            {
                hj.axis = originalJointConfig.Axis;
                hj.useLimits = originalJointConfig.UseLimits;
                hj.limits = originalJointConfig.Limits;
                hj.useSpring = originalJointConfig.UseSpring;
                hj.spring = originalJointConfig.Spring;
                hj.useMotor = originalJointConfig.UseMotor;
                hj.motor = originalJointConfig.Motor;
            }
            else if (newJoint is SpringJoint sj)
            {
                sj.spring = originalJointConfig.SpringStrength;
                sj.damper = originalJointConfig.Damper;
                sj.minDistance = originalJointConfig.MinDistance;
                sj.maxDistance = originalJointConfig.MaxDistance;
                sj.tolerance = originalJointConfig.Tolerance;
            }
        }

        [ContextMenu("Simular Derribo")]
        public void ForceTopple()
        {
            MarkAsToppled("Derribo forzado manual");
        }

        private void OnDrawGizmosSelected()
        {
            // Dibuja la esfera de tolerancia de desplazamiento en el Scene View para facilitar el ajuste manual
            Gizmos.color = isToppled ? Color.red : Color.green;
            Vector3 center = Application.isPlaying && initialParent != null 
                ? initialParent.TransformPoint(initialLocalPosition) 
                : transform.position;
            Gizmos.DrawWireSphere(center, fallDistanceThreshold);
        }
    }
}
