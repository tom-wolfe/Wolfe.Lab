using Wolfe.Lab.Domain;
using Wolfe.Lab.Domain.Backups;

namespace Wolfe.Lab.Tests.Domain.Backups;

public class RetentionPolicyTests
{
    [Fact]
    public void Create_KeepsWhatItIsTold() =>
        RetentionPolicy.Create(SnapshotCount.From(7), SnapshotCount.From(0), SnapshotCount.From(12)).Value.ShouldNotBeNull().Monthly.ShouldBe(SnapshotCount.From(12));

    [Fact]
    public void Create_RefusesOneThatKeepsNothing() =>
        RetentionPolicy.Create(SnapshotCount.From(0), SnapshotCount.From(0), SnapshotCount.From(0)).Errors.ShouldNotBeNull().ShouldHaveSingleItem();

    [Fact]
    public void SnapshotCount_RefusesFewerThanNone() =>
        SnapshotCount.TryFrom(-1).IsSuccess.ShouldBeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(101)]
    public void Percentage_RefusesWhatIsNoShare(int percent) =>
        Percentage.TryFrom(percent).IsSuccess.ShouldBeFalse();
}
