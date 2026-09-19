using NUnit.Framework;
using Simu1.Model;

namespace Simu1.Tests
{
    [TestFixture]
    public class BallisticDataTests
    {
        [Test]
        public void Constructor_InitializesWithDefaultValues()
        {
            var data = new BallisticData(45f, 500f, 2f, 2f);

            Assert.AreEqual(45f, data.LaunchAngle, 0.001f);
            Assert.AreEqual(500f, data.Force, 0.001f);
            Assert.AreEqual(2f, data.ProjectileMass, 0.001f);
            Assert.AreEqual(2f, data.BarrelLength, 0.001f);
            Assert.AreEqual(0f, data.LastHorizontalDistance);
            Assert.AreEqual(0f, data.LastFlightTime);
            Assert.AreEqual(0f, data.LastRelativeVelocity);
            Assert.AreEqual(0f, data.LastCollisionImpulse);
            Assert.AreEqual(0, data.LastFallenPieces);
            Assert.AreEqual(0, data.LastScore);
            Assert.IsFalse(data.HasImpactData);
        }

        [Test]
        public void SetImpactResults_FullMetrics_UpdatesStateAndTriggersEvents()
        {
            var data = new BallisticData();
            bool impactRecordedFired = false;
            bool shotReportUpdatedFired = false;
            bool dataChangedFired = false;

            data.OnImpactRecorded += (dist, height) => impactRecordedFired = true;
            data.OnShotReportUpdated += () => shotReportUpdatedFired = true;
            data.OnDataChanged += () => dataChangedFired = true;

            data.SetImpactResults(
                horizontalDistance: 35.5f,
                maxHeight: 12.3f,
                flightTime: 2.15f,
                relativeVelocity: 18.4f,
                collisionImpulse: 45.2f,
                fallenPieces: 3
            );

            Assert.IsTrue(impactRecordedFired);
            Assert.IsTrue(shotReportUpdatedFired);
            Assert.IsTrue(dataChangedFired);
            Assert.IsTrue(data.HasImpactData);

            Assert.AreEqual(35.5f, data.LastHorizontalDistance, 0.001f);
            Assert.AreEqual(12.3f, data.LastMaxHeight, 0.001f);
            Assert.AreEqual(2.15f, data.LastFlightTime, 0.001f);
            Assert.AreEqual(18.4f, data.LastRelativeVelocity, 0.001f);
            Assert.AreEqual(45.2f, data.LastCollisionImpulse, 0.001f);
            Assert.AreEqual(3, data.LastFallenPieces);

            // Score = 3 * 100 + round(45.2 * 2) = 300 + 90 = 390
            Assert.AreEqual(390, data.LastScore);
        }

        [Test]
        public void CalculateScore_CalculatesCorrectly()
        {
            var data = new BallisticData();

            int score0 = data.CalculateScore(0, 0f);
            Assert.AreEqual(0, score0);

            int score1 = data.CalculateScore(2, 50f);
            // 2 * 100 + 50 * 2 = 200 + 100 = 300
            Assert.AreEqual(300, score1);
        }

        [Test]
        public void ResetImpact_ClearsAllReportMetrics()
        {
            var data = new BallisticData();
            data.SetImpactResults(30f, 10f, 2f, 15f, 40f, 2);

            Assert.IsTrue(data.HasImpactData);
            Assert.Greater(data.LastScore, 0);

            data.ResetImpact();

            Assert.IsFalse(data.HasImpactData);
            Assert.AreEqual(0f, data.LastHorizontalDistance);
            Assert.AreEqual(0f, data.LastMaxHeight);
            Assert.AreEqual(0f, data.LastFlightTime);
            Assert.AreEqual(0f, data.LastRelativeVelocity);
            Assert.AreEqual(0f, data.LastCollisionImpulse);
            Assert.AreEqual(0, data.LastFallenPieces);
            Assert.AreEqual(0, data.LastScore);
        }
    }
}
