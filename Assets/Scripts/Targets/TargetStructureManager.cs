using System;
using UnityEngine;

namespace Simu1.Targets
{
    /// <summary>
    /// Administrador de la estructura de objetivos físicos.
    /// Supervisa el conjunto de piezas TargetPiece, calcula el conteo de derribos y puntuación,
    /// y provee métodos para restablecer la estructura completa para nuevos intentos.
    /// Diseñado para que cualquier usuario pueda mover, agregar o modificar piezas manualmente sin romper el sistema.
    /// </summary>
    public class TargetStructureManager : MonoBehaviour
    {
        [Header("Piezas de la Estructura")]
        [Tooltip("Lista de piezas que componen la estructura. Si está vacía, se detectan automáticamente de los hijos.")]
        [SerializeField] private TargetPiece[] pieces;

        [Header("Supervisión en Tiempo Real (Solo Lectura)")]
        [SerializeField] private int fallenCount;
        [SerializeField] private int totalScore;

        public int TotalPiecesCount => pieces != null ? pieces.Length : 0;
        public int FallenPiecesCount => GetFallenPiecesCount();
        public int CurrentScore => GetTotalScore();

        public event Action<int, int> OnStructureStateChanged;

        private void Awake()
        {
            AutoPopulatePieces();
            SubscribeToPieces();
        }

        private void OnValidate()
        {
            // Auto-detectar piezas en el editor para evitar que el usuario tenga que asignarlas manualmente
            int childCount = GetComponentsInChildren<TargetPiece>(true).Length;
            if (pieces == null || pieces.Length != childCount)
            {
                AutoPopulatePieces();
            }
        }

        /// <summary>
        /// Busca y registra automáticamente todas las piezas hijas en la jerarquía.
        /// Permite al usuario duplicar o añadir bloques sin configurar arrays a mano.
        /// </summary>
        [ContextMenu("Buscar Piezas Automáticamente")]
        public void AutoPopulatePieces()
        {
            pieces = GetComponentsInChildren<TargetPiece>(true);
            SubscribeToPieces();
        }

        private void SubscribeToPieces()
        {
            if (pieces == null) return;

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    piece.OnPieceToppled -= HandlePieceToppled;
                    piece.OnPieceToppled += HandlePieceToppled;
                }
            }
        }

        private void HandlePieceToppled(TargetPiece piece)
        {
            UpdateCounters();
            OnStructureStateChanged?.Invoke(fallenCount, totalScore);
        }

        private void UpdateCounters()
        {
            fallenCount = GetFallenPiecesCount();
            totalScore = GetTotalScore();
        }

        /// <summary>
        /// Devuelve la cantidad de piezas actualmente derribadas.
        /// </summary>
        public int GetFallenPiecesCount()
        {
            if (pieces == null) return 0;

            int count = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null && pieces[i].IsToppled)
                {
                    count++;
                }
            }
            return count;
        }

        /// <summary>
        /// Devuelve el puntaje acumulado por las piezas derribadas.
        /// </summary>
        public int GetTotalScore()
        {
            if (pieces == null) return 0;

            int score = 0;
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null && pieces[i].IsToppled)
                {
                    score += pieces[i].PointValue;
                }
            }
            return score;
        }

        /// <summary>
        /// Comprueba si todas las piezas físicas han dejado de moverse significativamente tras el impacto.
        /// </summary>
        public bool IsStructureSettled(float speedThreshold = 0.08f)
        {
            if (pieces == null) return true;

            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] == null) continue;

                // Las dianas colgantes que oscilan libremente en su joint no bloquean el reporte de tiro
                if (pieces[i].IsSuspendedTarget && !pieces[i].IsToppled) continue;

                if (pieces[i].TryGetComponent<Rigidbody>(out var rb))
                {
                    if (rb.linearVelocity.sqrMagnitude > speedThreshold * speedThreshold)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Restablece todas las piezas a su posición, rotación e integridad física inicial
        /// de forma sincronizada en fases para evitar impulsos violentos entre joints conectados.
        /// </summary>
        [ContextMenu("Restablecer Toda la Estructura")]
        public void ResetStructure()
        {
            if (pieces == null || pieces.Length == 0)
            {
                AutoPopulatePieces();
            }

            // Asegurar que todas las piezas tienen su estado inicial guardado antes de cualquier desconexión
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null && !pieces[i].HasSavedInitialState)
                {
                    pieces[i].SaveInitialState();
                }
            }

            // Fase 1: Desconectar todos los joints en todas las piezas
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null) pieces[i].PrepareForReset_DisconnectJoint();
            }

            // Fase 2: Congelar todos los Rigidbodies como cinemáticos y reiniciar velocidades
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null) pieces[i].PrepareForReset_FreezeKinematic();
            }

            // Fase 3: Reposicionar todos los transforms a sus coordenadas locales originales
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null) pieces[i].PrepareForReset_Reposition();
            }

            // Fase 4: Sincronizar transformaciones en el motor PhysX
            Physics.SyncTransforms();

            // Fase 5: Reconstruir todos los joints limpios con sus anclas originales mientras todo está inmóvil
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null) pieces[i].PrepareForReset_RebuildJoint();
            }

            // Fase 6: Descongelar y reactivar dinámica física para todas las piezas simultáneamente
            for (int i = 0; i < pieces.Length; i++)
            {
                if (pieces[i] != null) pieces[i].PrepareForReset_UnfreezeDynamic();
            }

            UpdateCounters();
            OnStructureStateChanged?.Invoke(fallenCount, totalScore);
        }

        private void OnDestroy()
        {
            if (pieces == null) return;

            foreach (var piece in pieces)
            {
                if (piece != null)
                {
                    piece.OnPieceToppled -= HandlePieceToppled;
                }
            }
        }
    }
}
