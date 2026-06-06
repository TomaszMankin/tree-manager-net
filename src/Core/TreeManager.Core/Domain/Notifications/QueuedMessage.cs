using System;

namespace TreeManager.Core.Domain.Notifications;

public sealed class QueuedMessage
{
    public string Id { get; init; }
    public string SubjectText { get; init; }
    public string BodyText { get; init; }
    public DateTime CreatedUtc { get; init; }
}
