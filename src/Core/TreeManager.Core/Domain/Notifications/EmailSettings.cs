namespace TreeManager.Core.Domain.Notifications;

public sealed class EmailSettings
{
    public string Host { get; init; }
    public int Port { get; init; }
    public bool UseSsl { get; init; }
    public string FromAddress { get; init; }
    public string AppPassword { get; init; }
    public string ToAddress { get; init; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Host) &&
        !string.IsNullOrWhiteSpace(FromAddress) &&
        !string.IsNullOrWhiteSpace(AppPassword) &&
        !string.IsNullOrWhiteSpace(ToAddress) &&
        Port > 0;
}
