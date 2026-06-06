using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.Infrastructure.Settings;

public sealed class EmailSettingsStore : IEmailSettingsStore
{
    private readonly string _settingsFilePath;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    public EmailSettingsStore(string settingsFilePath, IFileSystemFacade fs, ILogger log)
    {
        _settingsFilePath = settingsFilePath;
        _fs = fs;
        _log = log;
    }

    public EmailSettings Get()
    {
        if (!_fs.FileExists(_settingsFilePath))
        {
            return new EmailSettings();
        }

        try
        {
            var json = _fs.ReadAllText(_settingsFilePath);
            var root = JsonNode.Parse(json);

            if (root is not JsonObject rootObj || rootObj["email"] is not JsonObject emailObj)
            {
                return new EmailSettings();
            }

            return new EmailSettings
            {
                Host = emailObj["host"]?.GetValue<string>() ?? string.Empty,
                Port = emailObj["port"]?.GetValue<int>() ?? 0,
                UseSsl = emailObj["useSsl"]?.GetValue<bool>() ?? false,
                FromAddress = emailObj["fromAddress"]?.GetValue<string>() ?? string.Empty,
                AppPassword = emailObj["appPassword"]?.GetValue<string>() ?? string.Empty,
                ToAddress = emailObj["toAddress"]?.GetValue<string>() ?? string.Empty
            };
        }
        catch (Exception ex)
        {
            _log.Error(ex, "EmailSettingsStore: failed to read {Path}; returning not-configured", _settingsFilePath);
            return new EmailSettings();
        }
    }
}
