using System.Threading.Tasks;

namespace TreeManager.Core.Abstractions.Notifications;

public interface IEmailEscalator
{
    Task SendAsync(string subject, string body);
}
