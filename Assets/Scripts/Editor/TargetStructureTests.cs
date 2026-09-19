using NUnit.Framework;
using Simu1.Targets;
using UnityEngine;

namespace Simu1.Tests
{
    [TestFixture]
    public class TargetStructureTests
    {
        private GameObject rootGO;
        private TargetPiece piece1;
        private TargetPiece piece2;
        private TargetStructureManager manager;

        [SetUp]
        public void SetUp()
        {
            rootGO = new GameObject("TestRoot");
            manager = rootGO.AddComponent<TargetStructureManager>();

            GameObject p1GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p1GO.transform.SetParent(rootGO.transform);
            p1GO.transform.position = new Vector3(0, 1, 0);
            piece1 = p1GO.AddComponent<TargetPiece>();

            GameObject p2GO = GameObject.CreatePrimitive(PrimitiveType.Cube);
            p2GO.transform.SetParent(rootGO.transform);
            p2GO.transform.position = new Vector3(2, 1, 0);
            piece2 = p2GO.AddComponent<TargetPiece>();

            manager.AutoPopulatePieces();
        }

        [TearDown]
        public void TearDown()
        {
            if (rootGO != null)
            {
                Object.DestroyImmediate(rootGO);
            }
        }

        [Test]
        public void TargetPiece_InitialState_IsNotToppled()
        {
            Assert.IsFalse(piece1.IsToppled);
            Assert.IsFalse(piece2.IsToppled);
            Assert.AreEqual(0, manager.GetFallenPiecesCount());
            Assert.AreEqual(0, manager.GetTotalScore());
        }

        [Test]
        public void TargetPiece_ForceTopple_UpdatesStateAndManager()
        {
            bool eventFired = false;
            manager.OnStructureStateChanged += (fallen, score) => eventFired = true;

            piece1.ForceTopple();

            Assert.IsTrue(eventFired);
            Assert.IsTrue(piece1.IsToppled);
            Assert.IsFalse(piece2.IsToppled);
            Assert.AreEqual(1, manager.GetFallenPiecesCount());
            Assert.Greater(manager.GetTotalScore(), 0);
        }

        [Test]
        public void TargetStructureManager_ResetStructure_RestoresAllPieces()
        {
            piece1.ForceTopple();
            piece2.ForceTopple();

            Assert.AreEqual(2, manager.GetFallenPiecesCount());

            manager.ResetStructure();

            Assert.IsFalse(piece1.IsToppled);
            Assert.IsFalse(piece2.IsToppled);
            Assert.AreEqual(0, manager.GetFallenPiecesCount());
            Assert.AreEqual(0, manager.GetTotalScore());
        }

        [Test]
        public void TargetPiece_CheckIfToppled_DetectsFallBelowMinimumY()
        {
            // Mover la pieza al subsuelo
            piece1.transform.position = new Vector3(0, -1f, 0);

            bool toppled = piece1.CheckIfToppled();

            Assert.IsTrue(toppled);
            Assert.IsTrue(piece1.IsToppled);
        }
    }
}
