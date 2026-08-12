using CodeSync.Application.Common.Interfaces;
using CodeSync.Application.Features.Dashboard.Commands.ClearFeedbackHistory;
using Moq;

namespace CodeSync.Tests.Dashboard;

public sealed class ClearFeedbackHistoryHandlerTests
{
    [Fact]
    public async Task Handle_DeletesAllFeedbackForTheRequestingUser()
    {
        var feedbacks = new Mock<IFeedbackRepository>();
        var handler = new ClearFeedbackHistoryHandler(feedbacks.Object);

        await handler.Handle(new ClearFeedbackHistoryCommand("u-1"), CancellationToken.None);

        feedbacks.Verify(f => f.DeleteAllByUserIdAsync("u-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}
