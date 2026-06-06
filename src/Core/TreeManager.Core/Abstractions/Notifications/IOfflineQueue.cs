using System.Collections.Generic;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.Core.Abstractions.Notifications;

public interface IOfflineQueue
{
    void Enqueue(QueuedMessage message);
    IReadOnlyList<QueuedMessage> List();
    void Remove(string id);
}
