using MediatR;

namespace CodeSync.Application.Features.Dashboard.Commands.ClearFeedbackHistory;

public sealed record ClearFeedbackHistoryCommand(string UserId) : IRequest;
