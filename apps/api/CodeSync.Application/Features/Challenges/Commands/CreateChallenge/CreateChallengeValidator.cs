using FluentValidation;

namespace CodeSync.Application.Features.Challenges.Commands.CreateChallenge;

internal sealed class CreateChallengeValidator : AbstractValidator<CreateChallengeCommand>
{
    public CreateChallengeValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).NotEmpty().MaximumLength(2000);
        RuleFor(x => x.FunctionName).NotEmpty().Matches(@"^[a-zA-Z_]\w*$")
            .WithMessage("FunctionName must be a valid identifier.");
        RuleFor(x => x.SolutionTemplate).NotEmpty();
        RuleFor(x => x.TestCases).NotEmpty()
            .WithMessage("At least one test case is required.");
        RuleForEach(x => x.TestCases).ChildRules(tc =>
        {
            tc.RuleFor(t => t.Args).NotNull();
            tc.RuleFor(t => t.ExpectedOutput).NotNull();
        });
    }
}
