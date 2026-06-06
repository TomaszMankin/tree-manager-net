using TreeManager.Core.Domain.Notifications;

namespace TreeManager.Core.Abstractions.Settings;

public interface IEmailSettingsStore
{
    EmailSettings Get();
}
