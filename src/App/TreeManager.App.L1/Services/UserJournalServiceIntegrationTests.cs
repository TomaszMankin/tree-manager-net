using System;
using System.IO;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.Settings;
using TreeManager.Infrastructure.IO;

namespace TreeManager.App.L1.Services;

public sealed class UserJournalServiceIntegrationTests : IDisposable
{
    private readonly string _tempRoot;

    public UserJournalServiceIntegrationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempRoot);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_tempRoot, recursive: true);
        }
        catch
        {
            // best-effort cleanup
        }
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L1)]
    public void LogAction_WritesLineToJourneyFile_WhenRootSet()
    {
        //Arrange
        var storeMock = new Mock<IRootPointerStore>();
        storeMock.Setup(s => s.Read()).Returns(_tempRoot);

        var fs = new FileSystemFacade();
        var sut = new UserJournalService(fs, storeMock.Object, Log.Logger);

        //Act
        sut.LogAction("Zapisz osobę i dodaj do drzewa", "test-person-123");

        //Assert — file exists under <root>/.TreeManagerNet/logs/<date>__journey.log
        var logsDir = Path.Combine(_tempRoot, ".TreeManagerNet", "logs");
        var journalFiles = Directory.GetFiles(logsDir, "*__journey.log");
        Assert.Single(journalFiles);

        var content = File.ReadAllText(journalFiles[0]);
        Assert.Contains("Zapisz osobę i dodaj do drzewa", content);
        Assert.Contains("test-person-123", content);
    }
}
