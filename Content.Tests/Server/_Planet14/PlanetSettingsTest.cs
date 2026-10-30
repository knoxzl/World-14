using System;
using System.IO;
using Content.Server._Planet14.Sector;
using Content.Shared._Planet14.Planets;
using NUnit.Framework;
using Robust.Shared.ContentPack;

namespace Content.Tests.Server._Planet14;

[TestFixture]
public sealed class PlanetSettingsTest
{
    [TestCase(0, 0.08f, 3f, 1.25f, false)]
    [TestCase(-1, 0.08f, 3f, 1.25f, false)]
    [TestCase(1, 0.08f, 3f, 1.25f, true)]
    [TestCase(1800, -1f, 3f, 1.25f, false)]
    [TestCase(1800, 2f, 1f, 1.25f, false)]
    [TestCase(1800, 0.08f, 3f, -1f, false)]
    [TestCase(1800, 0f, 0f, 0f, true)]
    [TestCase(86400, 0.08f, 3f, 1.25f, true)]
    public void DayNightSettingsRejectInvalidCurves(int seconds, float min, float max, float clip, bool valid)
    {
        var settings = new PlanetDayNightSettings
        {
            Duration = TimeSpan.FromSeconds(seconds),
            MinLightLevel = min,
            MaxLightLevel = max,
            ClipLight = clip,
        };
        Assert.That(settings.IsValid, Is.EqualTo(valid));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    public void NonFiniteLightingIsRejected(float value)
    {
        Assert.That(new PlanetDayNightSettings { MaxLightLevel = value }.IsValid, Is.False);
    }

    [Test]
    public void DailyAnnouncementsAreNotRepeatedAfterReload()
    {
        IWritableDirProvider storage = new VirtualWritableDirProvider();
        var origin = DateTimeOffset.UtcNow;
        Assert.That(SectorDayAnnouncementStore.TryMarkSent(storage, origin, 1), Is.True);
        Assert.That(SectorDayAnnouncementStore.TryMarkSent(storage, origin, 1), Is.False);
        Assert.That(SectorDayAnnouncementStore.TryMarkSent(storage, origin, 2), Is.True);
        Assert.That(SectorDayAnnouncementStore.TryMarkSent(storage, origin, 2), Is.False);
        Assert.That(SectorDayAnnouncementStore.TryMarkSent(storage, origin.AddDays(15), 1), Is.True);
    }

    [TestCase(0)]
    [TestCase(16)]
    public void InvalidAnnouncementDaysAreRejected(int day)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            SectorDayAnnouncementStore.TryMarkSent(new VirtualWritableDirProvider(), DateTimeOffset.UtcNow, day));
    }

    [Test]
    public void StorageFailuresAreNotMistakenForAlreadyAnnounced()
    {
        var storage = new Moq.Mock<IWritableDirProvider>();
        storage.Setup(x => x.CreateDir(Moq.It.IsAny<Robust.Shared.Utility.ResPath>()))
            .Throws(new IOException("Unavailable storage"));
        Assert.Throws<IOException>(() =>
            SectorDayAnnouncementStore.TryMarkSent(storage.Object, DateTimeOffset.UtcNow, 1));
    }
}
