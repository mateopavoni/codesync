using MediatR;

namespace CodeSync.Application.Features.Rooms.Commands.CloseRoom;

public sealed record CloseRoomCommand(string RoomId, string UserId) : IRequest;
