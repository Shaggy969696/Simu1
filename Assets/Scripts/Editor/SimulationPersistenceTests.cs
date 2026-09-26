using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Simu1.Model;
using Simu1.Persistence;
using UnityEngine;

namespace Simu1.Tests
{
    [TestFixture]
    public class SimulationPersistenceTests
    {
        [Test]
        public void SavedShotEntry_SerializationRoundTrip_MatchesValues()
        {
            var entry = new SavedShotEntry(
                attemptIndex: 1,
                angle: 45f,
                force: 500f,
                mass: 2f,
                distance: 28.5f,
                isHit: true,
                fallenPieces: 3,
                score: 350,
                flightTime: 2.1f,
                maxHeight: 9.8f,
                relativeSpeed: 14.5f,
                collisionImpulse: 38.2f,
                timestamp: "2026-09-26T12:00:00Z"
            );

            string json = JsonUtility.ToJson(entry);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var deserialized = JsonUtility.FromJson<SavedShotEntry>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(1, deserialized.attemptIndex);
            Assert.AreEqual(45f, deserialized.angle, 0.001f);
            Assert.AreEqual(500f, deserialized.force, 0.001f);
            Assert.AreEqual(2f, deserialized.mass, 0.001f);
            Assert.AreEqual(28.5f, deserialized.distance, 0.001f);
            Assert.IsTrue(deserialized.isHit);
            Assert.AreEqual(3, deserialized.fallenPieces);
            Assert.AreEqual(350, deserialized.score);
            Assert.AreEqual(2.1f, deserialized.flightTime, 0.001f);
            Assert.AreEqual(9.8f, deserialized.maxHeight, 0.001f);
            Assert.AreEqual(14.5f, deserialized.relativeSpeed, 0.001f);
            Assert.AreEqual(38.2f, deserialized.collisionImpulse, 0.001f);
            Assert.AreEqual("2026-09-26T12:00:00Z", deserialized.timestamp);
        }

        [Test]
        public void SimulationHistoryWrapper_MultipleEntries_SerializesCorrectly()
        {
            var wrapper = new SimulationHistoryWrapper();
            wrapper.shots.Add(new SavedShotEntry(1, 30f, 400f, 1.5f, 22.0f, true, 2, 200));
            wrapper.shots.Add(new SavedShotEntry(2, 45f, 600f, 2.0f, 35.5f, true, 5, 550));
            wrapper.shots.Add(new SavedShotEntry(3, 60f, 300f, 3.0f, 12.0f, false, 0, 0));

            string json = JsonUtility.ToJson(wrapper);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var deserialized = JsonUtility.FromJson<SimulationHistoryWrapper>(json);
            Assert.IsNotNull(deserialized);
            Assert.AreEqual(3, deserialized.shots.Count);
            Assert.AreEqual(1, deserialized.shots[0].attemptIndex);
            Assert.AreEqual(2, deserialized.shots[1].attemptIndex);
            Assert.AreEqual(3, deserialized.shots[2].attemptIndex);
            Assert.IsFalse(deserialized.shots[2].isHit);
            Assert.AreEqual(550, deserialized.shots[1].score);
        }

        [Test]
        public void SavedShotEntry_FromShotRecord_AccuratelyTransfersAllFields()
        {
            var testTime = new DateTime(2026, 9, 26, 10, 30, 0, DateTimeKind.Utc);
            var record = new ShotRecord(
                attemptIndex: 7,
                angle: 52.5f,
                force: 750f,
                mass: 2.5f,
                barrelLength: 2.0f,
                flightTime: 2.85f,
                horizontalDistance: 41.2f,
                maxHeight: 14.6f,
                impactPosition: new Vector3(35f, 1.2f, 21.6f),
                relativeSpeed: 19.4f,
                collisionImpulse: 55f,
                fallenPieces: 4,
                totalPieces: 13,
                score: 480,
                timestamp: testTime
            );

            var entry = SavedShotEntry.FromShotRecord(record, isHit: true);

            Assert.AreEqual(7, entry.attemptIndex);
            Assert.AreEqual(52.5f, entry.angle, 0.001f);
            Assert.AreEqual(750f, entry.force, 0.001f);
            Assert.AreEqual(2.5f, entry.mass, 0.001f);
            Assert.AreEqual(41.2f, entry.distance, 0.001f);
            Assert.IsTrue(entry.isHit);
            Assert.AreEqual(4, entry.fallenPieces);
            Assert.AreEqual(480, entry.score);
            Assert.AreEqual(2.85f, entry.flightTime, 0.001f);
            Assert.AreEqual(14.6f, entry.maxHeight, 0.001f);
            Assert.AreEqual(19.4f, entry.relativeSpeed, 0.001f);
            Assert.AreEqual(55f, entry.collisionImpulse, 0.001f);
            Assert.AreEqual(testTime.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ"), entry.timestamp);
        }

        [Test]
        public void MockSimulationRepository_SavesAndLoadsWithoutRemoteNetwork()
        {
            var go = new GameObject("MockRepoTest");
            try
            {
                var mockRepo = go.AddComponent<MockSimulationRepository>();
                bool shotSavedEventFired = false;
                bool historyLoadedEventFired = false;

                mockRepo.OnShotSaved += shot => shotSavedEventFired = true;
                mockRepo.OnHistoryLoaded += list => historyLoadedEventFired = true;

                var entry1 = new SavedShotEntry(1, 45f, 500f, 2f, 30f, true, 2, 200);
                var entry2 = new SavedShotEntry(2, 50f, 550f, 2f, 34f, true, 3, 350);

                Task saveTask1 = mockRepo.SaveShotAsync(entry1);
                saveTask1.Wait();

                Task saveTask2 = mockRepo.SaveShotAsync(entry2);
                saveTask2.Wait();

                Assert.IsTrue(shotSavedEventFired);

                Task<List<SavedShotEntry>> loadTask = mockRepo.LoadHistoryAsync();
                loadTask.Wait();

                Assert.IsTrue(historyLoadedEventFired);
                Assert.AreEqual(2, loadTask.Result.Count);
                Assert.AreEqual(1, loadTask.Result[0].attemptIndex);
                Assert.AreEqual(2, loadTask.Result[1].attemptIndex);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(go);
            }
        }

        private class MockSimulationRepository : SimulationRepository
        {
            private readonly List<SavedShotEntry> storage = new List<SavedShotEntry>();

            public override Task SaveShotAsync(SavedShotEntry shot)
            {
                storage.Add(shot);
                NotifyShotSaved(shot);
                return Task.CompletedTask;
            }

            public override Task<List<SavedShotEntry>> LoadHistoryAsync()
            {
                var copy = new List<SavedShotEntry>(storage);
                NotifyHistoryLoaded(copy);
                return Task.FromResult(copy);
            }
        }
    }
}
