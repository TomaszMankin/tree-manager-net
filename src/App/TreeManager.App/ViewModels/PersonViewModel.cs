using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.App.Mappers;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class PersonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    [ObservableProperty]
    private string _otherFirstNames = string.Empty;

    [ObservableProperty]
    private string _lastName = string.Empty;

    [ObservableProperty]
    private string _otherLastNames = string.Empty;

    [ObservableProperty]
    private string _maidenName = string.Empty;

    [ObservableProperty]
    private string _otherMaidenNames = string.Empty;

    [ObservableProperty]
    private bool _hasMaidenName;

    [ObservableProperty]
    private Sex _sex = Sex.Unknown;

    [ObservableProperty]
    private string _personName = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private Guid _uniqueIdentifier = Guid.Empty;

    public void Reset(MeFile meFile)
    {
        var mapped = meFile.ToViewModel();
        UniqueIdentifier = mapped.UniqueIdentifier;
        PersonName = mapped.PersonName;
        Location = mapped.Location;
        FirstName = mapped.FirstName;
        OtherFirstNames = mapped.OtherFirstNames;
        LastName = mapped.LastName;
        OtherLastNames = mapped.OtherLastNames;
        MaidenName = mapped.MaidenName;
        OtherMaidenNames = mapped.OtherMaidenNames;
        HasMaidenName = mapped.HasMaidenName;
        Sex = mapped.Sex;
    }
}
