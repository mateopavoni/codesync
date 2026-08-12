using CodeSync.Application.Common.Interfaces;
using MediatR;

namespace CodeSync.Application.Features.Dashboard.Commands.ClearFeedbackHistory;

internal sealed class ClearFeedbackHistoryHandler : IRequestHandler<ClearFeedbackHistoryCommand>
{
    private readonly IFeedbackRepository _feedbacks;

    public ClearFeedbackHistoryHandler(IFeedbackRepository feedbacks) => _feedbacks = feedbacks;

    public Task Handle(ClearFeedbackHistoryCommand request, CancellationToken cancellationToken) =>
        _feedbacks.DeleteAllByUserIdAsync(request.UserId, cancellationToken);
}
