using CodeSync.Domain;

namespace CodeSync.Tests;

public sealed class LevelCalculatorTests
{
    [Theory]
    [InlineData(0, 1)]
    [InlineData(99, 1)]
    [InlineData(100, 2)]
    [InlineData(249, 2)]
    [InlineData(250, 3)]
    [InlineData(499, 3)]
    [InlineData(500, 4)]
    [InlineData(899, 4)]
    [InlineData(900, 5)]
    [InlineData(5000, 5)]
    public void CalculateLevel_MatchesThresholds(int xp, int expectedLevel) =>
        Assert.Equal(expectedLevel, CodeSync.Domain.LevelCalculator.CalculateLevel(xp));

    [Fact]
    public void NextLevelThreshold_AtMaxLevel_ReturnsNull() =>
        Assert.Null(LevelCalculator.NextLevelThreshold(900));

    [Fact]
    public void NextLevelThreshold_BelowCap_ReturnsUpcomingThreshold() =>
        Assert.Equal(250, LevelCalculator.NextLevelThreshold(150));

    [Fact]
    public void CurrentLevelFloor_MatchesLevelStart() =>
        Assert.Equal(500, LevelCalculator.CurrentLevelFloor(600));
}
