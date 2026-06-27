using System;
using CommunityToolkit.Mvvm.Input;
using TreeManager.App.Services;

namespace TreeManager.App.Commands;

public sealed class JournalingRelayCommand : IRelayCommand
{
    private readonly IRelayCommand _inner;
    private readonly IUserJournalService _journal;
    private readonly string _label;
    private readonly Func<string> _personLabelProvider;

    public JournalingRelayCommand(
        IRelayCommand inner,
        IUserJournalService journal,
        string label,
        Func<string> personLabelProvider)
    {
        _inner = inner;
        _journal = journal;
        _label = label;
        _personLabelProvider = personLabelProvider;
    }

    public event EventHandler CanExecuteChanged
    {
        add => _inner.CanExecuteChanged += value;
        remove => _inner.CanExecuteChanged -= value;
    }

    public bool CanExecute(object parameter) => _inner.CanExecute(parameter);

    public void Execute(object parameter)
    {
        _journal.LogAction(_label, _personLabelProvider());
        _inner.Execute(parameter);
    }

    public void NotifyCanExecuteChanged() => _inner.NotifyCanExecuteChanged();
}
