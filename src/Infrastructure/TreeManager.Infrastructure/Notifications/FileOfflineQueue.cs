using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Serilog;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Notifications;
using TreeManager.Core.Domain.Notifications;

namespace TreeManager.Infrastructure.Notifications;

public sealed class FileOfflineQueue : IOfflineQueue
{
    private const string RuntimeFolderName = ".TreeManager";
    private const string QueueFolderName = "offline_queue";

    private readonly string _rootPath;
    private readonly IFileSystemFacade _fs;
    private readonly ILogger _log;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };

    public FileOfflineQueue(string rootPath, IFileSystemFacade fs, ILogger log)
    {
        _rootPath = rootPath;
        _fs = fs;
        _log = log;
    }

    public void Enqueue(QueuedMessage message)
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            _log.Debug("FileOfflineQueue.Enqueue: root path is empty; skipping enqueue for {Id}", message.Id);
            return;
        }

        var dir = QueueDirectory();
        _fs.CreateDirectory(dir);

        var filePath = Path.Combine(dir, $"{message.Id}.json");
        var json = JsonSerializer.Serialize(message, SerializerOptions);
        _fs.WriteAllText(filePath, json);
    }

    public IReadOnlyList<QueuedMessage> List()
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return Array.Empty<QueuedMessage>();
        }

        var dir = QueueDirectory();

        if (!_fs.DirectoryExists(dir))
        {
            return Array.Empty<QueuedMessage>();
        }

        var results = new List<QueuedMessage>();
        foreach (var filePath in _fs.EnumerateFiles(dir, "*.json"))
        {
            try
            {
                var json = _fs.ReadAllText(filePath);
                var message = JsonSerializer.Deserialize<QueuedMessage>(json, SerializerOptions);
                if (message != null)
                {
                    results.Add(message);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "FileOfflineQueue.List: failed to deserialize {FilePath}; skipping", filePath);
            }
        }

        return results;
    }

    public void Remove(string id)
    {
        if (string.IsNullOrWhiteSpace(_rootPath))
        {
            return;
        }

        var filePath = Path.Combine(QueueDirectory(), $"{id}.json");
        if (_fs.FileExists(filePath))
        {
            _fs.DeleteFile(filePath);
        }
    }

    private string QueueDirectory()
        => Path.Combine(_rootPath, RuntimeFolderName, QueueFolderName);
}
