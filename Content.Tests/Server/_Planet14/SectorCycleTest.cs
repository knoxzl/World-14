using System;
using System.IO;
using Content.Server._Planet14.Sector;
using NUnit.Framework;
using Robust.Shared.ContentPack;

namespace Content.Tests.Server._Planet14;

[TestFixture]
public sealed class SectorCycleTest
{
    private static readonly DateTimeOffset Origin = new(2029, 10, 29, 0, 0, 0, TimeSpan.Zero);

    [TestCase(0, 1, BlackSpacePhase.Unstable, SectorPhase.Survival)]
    [TestCase(3, 4, BlackSpacePhase.Traversable, SectorPhase.Survival)]
    [TestCase(10, 11, BlackSpacePhase.Traversable, SectorPhase.Survival)]
    [TestCase(11, 12, BlackSpacePhase.OneWay, SectorPhase.Survival)]
    [TestCase(12, 13, BlackSpacePhase.OneWay, SectorPhase.CapacityReview)]
    [TestCase(13, 14, BlackSpacePhase.OneWay, SectorPhase.ShieldCharging)]
    [TestCase(14, 15, BlackSpacePhase.OneWay, SectorPhase.LastDay)]
    [TestCase(15, 15, BlackSpacePhase.Consumed, SectorPhase.Collapsed)]
    [TestCase(100, 15, BlackSpacePhase.Consumed, SectorPhase.Collapsed)]
    public void DayBoundaries(int elapsedDays, int day, BlackSpacePhase blackSpace, SectorPhase phase)
    {
        var cycle = new SectorCycle(Origin, 86400);
        var snapshot = cycle.GetSnapshot(Origin.AddDays(elapsedDays));
        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Day, Is.EqualTo(day));
            Assert.That(snapshot.BlackSpace, Is.EqualTo(blackSpace));
            Assert.That(snapshot.Phase, Is.EqualTo(phase));
            Assert.That(snapshot.UntilCollapse, Is.GreaterThanOrEqualTo(TimeSpan.Zero));
        });
    }

    [TestCase(3, BlackSpacePhase.Unstable, BlackSpacePhase.Traversable)]
    [TestCase(11, BlackSpacePhase.Traversable, BlackSpacePhase.OneWay)]
    [TestCase(15, BlackSpacePhase.OneWay, BlackSpacePhase.Consumed)]
    public void BlackSpaceChangesAtDeadline(int elapsedDays, BlackSpacePhase before, BlackSpacePhase after)
    {
        var cycle = new SectorCycle(Origin, 86400);
        var deadline = Origin.AddDays(elapsedDays);
        Assert.That(cycle.GetSnapshot(deadline.AddTicks(-1)).BlackSpace, Is.EqualTo(before));
        Assert.That(cycle.GetSnapshot(deadline).BlackSpace, Is.EqualTo(after));
    }

    [TestCase(0, SectorPhase.CapacityReview, SectorPhase.ShieldCharging)]
    [TestCase(120, SectorPhase.ShieldCharging, SectorPhase.Boarding)]
    [TestCase(300, SectorPhase.Boarding, SectorPhase.Rush)]
    [TestCase(315, SectorPhase.Rush, SectorPhase.FinalFiveMinutes)]
    [TestCase(320, SectorPhase.FinalFiveMinutes, SectorPhase.Departed)]
    public void EvacuationDeadlines(int minutesSinceArrival, SectorPhase before, SectorPhase after)
    {
        var cycle = new SectorCycle(Origin, 86400);
        var deadline = Origin.AddDays(13).AddMinutes(minutesSinceArrival);
        Assert.That(cycle.GetSnapshot(deadline.AddTicks(-1)).Phase, Is.EqualTo(before));
        Assert.That(cycle.GetSnapshot(deadline).Phase, Is.EqualTo(after));
    }

    [TestCase(60)]
    [TestCase(3600)]
    [TestCase(86400)]
    public void AcceleratedClockPreservesSchedule(int dayLength)
    {
        var cycle = new SectorCycle(Origin, dayLength);
        var arrival = Origin.AddSeconds(13L * dayLength);
        var boarding = arrival.AddTicks(TimeSpan.FromSeconds(dayLength).Ticks / 12);
        var rush = boarding.AddTicks(TimeSpan.FromSeconds(dayLength).Ticks / 8);
        Assert.Multiple(() =>
        {
            Assert.That(cycle.GetSnapshot(Origin.AddSeconds(3L * dayLength)).BlackSpace,
                Is.EqualTo(BlackSpacePhase.Traversable));
            Assert.That(cycle.GetSnapshot(arrival).Phase, Is.EqualTo(SectorPhase.ShieldCharging));
            Assert.That(cycle.GetSnapshot(boarding).Phase, Is.EqualTo(SectorPhase.Boarding));
            Assert.That(cycle.GetSnapshot(rush).Phase, Is.EqualTo(SectorPhase.Rush));
            Assert.That(cycle.GetSnapshot(Origin.AddSeconds(15L * dayLength)).UntilCollapse,
                Is.EqualTo(TimeSpan.Zero));
        });
    }

    [Test]
    public void RestartReconcilesDirectlyToCurrentPhase()
    {
        IWritableDirProvider storage = new VirtualWritableDirProvider();
        SectorCycleStore.Create(storage, new SectorCycle(Origin, 86400));

        // Loading after an outage derives the current state without resuming an old timer.
        var restored = SectorCycleStore.Load(storage)!;
        var snapshot = restored.GetSnapshot(Origin.AddDays(13).AddHours(5).AddMinutes(17));
        Assert.That(snapshot.Phase, Is.EqualTo(SectorPhase.FinalFiveMinutes));
        Assert.That(snapshot.Day, Is.EqualTo(14));
    }

    [Test]
    public void PulseIsEveryFourMinutesAndStopsAdvancingAtCollapse()
    {
        var cycle = new SectorCycle(Origin, 86400);
        Assert.That(cycle.GetSnapshot(Origin.AddMinutes(4).AddTicks(-1)).PulseNumber, Is.Zero);
        Assert.That(cycle.GetSnapshot(Origin.AddMinutes(4)).PulseNumber, Is.EqualTo(1));
        Assert.That(cycle.GetSnapshot(Origin.AddMinutes(8)).PulseNumber, Is.EqualTo(2));
        Assert.That(cycle.GetSnapshot(Origin.AddDays(20)).PulseNumber,
            Is.EqualTo(cycle.GetSnapshot(Origin.AddDays(15)).PulseNumber));
    }

    [Test]
    public void AcceleratedPulsesDoNotAccumulateRoundingDrift()
    {
        var cycle = new SectorCycle(Origin, 60);
        var oneDay = TimeSpan.FromMinutes(1);
        Assert.That(cycle.GetSnapshot(Origin.Add(oneDay).AddTicks(-1)).PulseNumber, Is.EqualTo(359));
        Assert.That(cycle.GetSnapshot(Origin.Add(oneDay)).PulseNumber, Is.EqualTo(360));
    }

    [Test]
    public void TimeBeforeOriginIsClampedToDayOne()
    {
        var cycle = new SectorCycle(Origin, 86400);
        Assert.That(cycle.GetSnapshot(Origin.AddHours(-1)), Is.EqualTo(cycle.GetSnapshot(Origin)));
    }

    [Test]
    public void OffsetsReferToTheSameUtcInstant()
    {
        var cycle = new SectorCycle(Origin.ToOffset(TimeSpan.FromHours(2)), 86400);
        Assert.That(cycle.StartedAtUtc.Offset, Is.EqualTo(TimeSpan.Zero));
        Assert.That(cycle.GetSnapshot(Origin.AddDays(3)).Day, Is.EqualTo(4));
    }

    [TestCase(-1)]
    [TestCase(0)]
    [TestCase(59)]
    [TestCase(86401)]
    [TestCase(int.MaxValue)]
    public void InvalidDayLengthIsRejected(int dayLength)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SectorCycle(Origin, dayLength));
    }

    [Test]
    public void MissingOriginIsRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new SectorCycle(default, 86400));
    }

    [Test]
    public void PersistenceRoundTripKeepsOriginAndSpeed()
    {
        IWritableDirProvider storage = new VirtualWritableDirProvider();
        Assert.That(SectorCycleStore.Load(storage), Is.Null);
        var original = new SectorCycle(Origin, 60);
        SectorCycleStore.Create(storage, original);
        var restored = SectorCycleStore.Load(storage)!;
        Assert.That(restored.StartedAtUtc, Is.EqualTo(original.StartedAtUtc));
        Assert.That(restored.DayLengthSeconds, Is.EqualTo(60));
        Assert.That(restored.GetSnapshot(Origin.AddMinutes(14)),
            Is.EqualTo(original.GetSnapshot(Origin.AddMinutes(14))));
    }

    [Test]
    public void ExistingCycleCannotBeOverwritten()
    {
        IWritableDirProvider storage = new VirtualWritableDirProvider();
        SectorCycleStore.Create(storage, new SectorCycle(Origin, 86400));
        Assert.Throws<IOException>(() => SectorCycleStore.Create(storage, new SectorCycle(Origin.AddDays(1), 60)));
        Assert.That(SectorCycleStore.Load(storage)!.StartedAtUtc, Is.EqualTo(Origin));
    }

    [TestCase("null")]
    [TestCase("{}")]
    [TestCase("{\"StartedAtUtc\":\"2029-10-29T00:00:00Z\",\"DayLengthSeconds\":0}")]
    [TestCase("not json")]
    public void InvalidSaveIsReportedAndPreserved(string contents)
    {
        IWritableDirProvider storage = new VirtualWritableDirProvider();
        storage.CreateDir(SectorCycleStore.StatePath.Directory);
        using (var writer = storage.OpenWriteText(SectorCycleStore.StatePath))
        {
            writer.Write(contents);
        }

        Assert.That(() => SectorCycleStore.Load(storage), Throws.Exception);
        using var reader = storage.OpenText(SectorCycleStore.StatePath);
        Assert.That(reader.ReadToEnd(), Is.EqualTo(contents));
        Assert.Throws<IOException>(() => SectorCycleStore.Create(storage, new SectorCycle(Origin, 86400)));
    }
}
