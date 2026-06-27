using System;
using Moq;
using Serilog;
using TreeManager.App.Services;
using TreeManager.Common.TestUtilities;
using TreeManager.Core.Abstractions.IO;
using TreeManager.Core.Abstractions.Settings;

namespace TreeManager.App.L0.Services;

public sealed class UserJournalServiceTests
{
    private readonly Mock<IFileSystemFacade> _fsMock = new();
    private readonly Mock<IRootPointerStore> _storeMock = new();
    private readonly Mock<ILogger> _logMock = new();

    #region LogAction

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LogAction_AppendsFormattedLine_Always()
    {
        //Arrange
        _storeMock.Setup(s => s.Read()).Returns(@"C:\fake\root");
        var sut = BuildSut();

        //Act
        sut.LogAction("Zapisz osobę i dodaj do drzewa", "abc12345-0000-0000-0000-000000000000");

        //Assert — AppendAllText called once with content containing action label
        _fsMock.Verify(
            fs => fs.AppendAllText(
                It.IsAny<string>(),
                It.Is<string>(c => c.Contains("Zapisz osobę i dodaj do drzewa"))),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LogAction_UsesDashLabel_WhenPersonNull()
    {
        //Arrange
        _storeMock.Setup(s => s.Read()).Returns(@"C:\fake\root");
        var sut = BuildSut();

        //Act
        sut.LogAction("Nowa osoba");

        //Assert — line contains dash as person label
        _fsMock.Verify(
            fs => fs.AppendAllText(
                It.IsAny<string>(),
                It.Is<string>(c => c.Contains("[-]"))),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LogAction_WritesToPerDayJourneyFile_Always()
    {
        //Arrange
        _storeMock.Setup(s => s.Read()).Returns(@"C:\fake\root");
        var sut = BuildSut();

        //Act
        sut.LogAction("Sprawdź spójność", "-");

        //Assert — path ends with __journey.log and contains the logs dir fragment
        _fsMock.Verify(
            fs => fs.AppendAllText(
                It.Is<string>(p => p.EndsWith("__journey.log") && p.Contains("logs")),
                It.IsAny<string>()),
            Times.Once);
    }

    [Fact]
    [Trait(TestTiers.TraitName, TestTiers.L0)]
    public void LogAction_LogsError_AndDoesNotThrow_WhenAppendFails()
    {
        //Arrange
        _storeMock.Setup(s => s.Read()).Returns(@"C:\fake\root");
        _fsMock.Setup(fs => fs.AppendAllText(It.IsAny<string>(), It.IsAny<string>()))
            .Throws(new System.IO.IOException("disk full"));
        var sut = BuildSut();

        //Act & Assert — must not throw
        var ex = Record.Exception(() => sut.LogAction("Zapisz osobę i dodaj do drzewa"));
        Assert.Null(ex);

        _logMock.Verify(
            l => l.Error(It.IsAny<Exception>(), It.IsAny<string>(), It.IsAny<string>()),
            Times.Once);
    }

    #endregion

    private UserJournalService BuildSut() =>
        new(_fsMock.Object, _storeMock.Object, _logMock.Object);
}
