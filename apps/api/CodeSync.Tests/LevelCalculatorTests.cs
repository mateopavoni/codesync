using CodeSync.Domain;

namespace CodeSync.Tests;

public sealed class LevelCalculatorTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(2, 1)]
    [InlineData(3, 2)]
    [InlineData(6, 2)]
    [InlineData(7, 3)]
    [InlineData(14, 3)]
    [InlineData(15, 4)]
    [InlineData(24, 4)]
    [InlineData(25, 5)]
    [InlineData(100, 5)]
    public void CalculateLevel_MatchesThresholds(int completedCount, int expectedLevel) =>
        Assert.Equal(expectedLevel, CodeSync.Domain.LevelCalculator.CalculateLevel(completedCount));

    [Fact]
    public void NextLevelThreshold_AtMaxLevel_ReturnsNull() =>
        Assert.Null(LevelCalculator.NextLevelThreshold(25));

    [Fact]
    public void NextLevelThreshold_BelowCap_ReturnsUpcomingThreshold() =>
        Assert.Equal(7, LevelCalculator.NextLevelThreshold(5));

    [Fact]
    public void CurrentLevelFloor_MatchesLevelStart() =>
        Assert.Equal(15, LevelCalculator.CurrentLevelFloor(20));
}
