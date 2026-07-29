using CodeSync.Domain.Enums;
using MediatR;

namespace CodeSync.Application.Features.Challenges.Commands.CreateChallenge;

public sealed record CreateChallengeCommand(
    string Title,
    string Description,
    DifficultyLevel Difficulty,
    ProgrammingLanguage Language,
    string FunctionName,
    string SolutionTemplate,
    IReadOnlyList<CreateTestCaseDto> TestCases) : IRequest<string>;

public sealed record CreateTestCaseDto(
    string Args,
    string ExpectedOutput,
    bool IsVisible = true);
