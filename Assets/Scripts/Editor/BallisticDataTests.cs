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

        [Test]
        public void RecordShot_AddsEntryToHistoryAndCalculatesCumulativeScore()
        {
            var data = new BallisticData(initialAngle: 45f, initialForce: 500f, initialMass: 2f);
            bool historyUpdatedFired = false;
            data.OnShotHistoryUpdated += () => historyUpdatedFired = true;

            var rec1 = data.RecordShot(
                horizontalDistance: 30f,
                maxHeight: 10f,
                flightTime: 1.5f,
                relativeVelocity: 15f,
                collisionImpulse: 40f,
                fallenPieces: 2,
                totalPieces: 13
            );

            Assert.IsTrue(historyUpdatedFired);
            Assert.AreEqual(1, data.TotalAttemptsCount);
            Assert.AreEqual(1, rec1.AttemptIndex);
            Assert.AreEqual(45f, rec1.Angle);
            Assert.AreEqual(500f, rec1.Force);
            Assert.AreEqual(2f, rec1.Mass);
            Assert.AreEqual(1.5f, rec1.FlightTime);
            Assert.AreEqual(30f, rec1.HorizontalDistance, 0.001f);
            Assert.AreEqual(10f, rec1.MaxHeight, 0.001f);
            Assert.Greater(rec1.InitialVelocity, 0f);
            Assert.Greater(rec1.InitialImpulse, 0f);
            Assert.AreEqual(2, rec1.FallenPieces);
            Assert.AreEqual(13, rec1.TotalPieces);

            // Second shot
            data.LaunchAngle = 35f;
            data.Force = 700f;
            var rec2 = data.RecordShot(
                horizontalDistance: 35f,
                maxHeight: 8f,
                flightTime: 1.2f,
                relativeVelocity: 20f,
                collisionImpulse: 60f,
                fallenPieces: 4,
                totalPieces: 13
            );

            Assert.AreEqual(2, data.TotalAttemptsCount);
            Assert.AreEqual(2, rec2.AttemptIndex);
            Assert.AreEqual(35f, rec2.Angle);
            Assert.AreEqual(700f, rec2.Force);
            Assert.AreEqual(rec1.Score + rec2.Score, data.TotalCumulativeScore);
            Assert.AreEqual(rec2.Score, data.LastShotRecord.Value.Score);
        }

        [Test]
        public void ResetImpact_PreservesShotHistory()
        {
            var data = new BallisticData();
            data.RecordShot(25f, 8f, 1.2f, 10f, 30f, 2, 13);
            data.RecordShot(30f, 9f, 1.4f, 12f, 40f, 3, 13);

            Assert.AreEqual(2, data.TotalAttemptsCount);
            int cumulativeBefore = data.TotalCumulativeScore;
            Assert.Greater(cumulativeBefore, 0);

            // Simulating ResetAttempt after a shot
            data.ResetImpact();

            // Last shot transient metrics are cleared
            Assert.IsFalse(data.HasImpactData);
            Assert.AreEqual(0f, data.LastFlightTime);

            // But cumulative history is preserved
            Assert.AreEqual(2, data.TotalAttemptsCount);
            Assert.AreEqual(cumulativeBefore, data.TotalCumulativeScore);
        }

        [Test]
        public void ClearHistory_EmptiesHistoryAndResetsScore()
        {
            var data = new BallisticData();
            data.RecordShot(25f, 8f, 1.2f, 10f, 30f, 2, 13);
            Assert.AreEqual(1, data.TotalAttemptsCount);

            data.ClearHistory();

            Assert.AreEqual(0, data.TotalAttemptsCount);
            Assert.AreEqual(0, data.TotalCumulativeScore);
            Assert.IsFalse(data.HasImpactData);
            Assert.IsNull(data.LastShotRecord);
        }
    }
}
