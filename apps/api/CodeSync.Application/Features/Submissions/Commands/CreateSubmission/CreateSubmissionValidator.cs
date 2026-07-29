using FluentValidation;

namespace CodeSync.Application.Features.Submissions.Commands.CreateSubmission;

internal sealed class CreateSubmissionValidator : AbstractValidator<CreateSubmissionCommand>
{
    private const int MaxCodeLength = 50_000;

    public CreateSubmissionValidator()
    {
        RuleFor(x => x.ChallengeId).NotEmpty();
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Code)
            .NotEmpty()
            .MaximumLength(MaxCodeLength)
            .WithMessage($"Code must not exceed {MaxCodeLength} characters.");
    }
}
