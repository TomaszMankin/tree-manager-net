using System;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class PersonViewModel : ObservableObject
{
    [ObservableProperty]
    private string _firstName = string.Empty;

    public StringListViewModel OtherFirstNames { get; } = new();

    public string OtherFirstNamesText
    {
        get => OtherFirstNames.Serialize();
        set { OtherFirstNames.Load(value); OnPropertyChanged(); }
    }

    [ObservableProperty]
    private string _lastName = string.Empty;

    public StringListViewModel OtherLastNames { get; } = new();

    public string OtherLastNamesText
    {
        get => OtherLastNames.Serialize();
        set { OtherLastNames.Load(value); OnPropertyChanged(); }
    }

    [ObservableProperty]
    private string _maidenName = string.Empty;

    public StringListViewModel OtherMaidenNames { get; } = new();

    public string OtherMaidenNamesText
    {
        get => OtherMaidenNames.Serialize();
        set { OtherMaidenNames.Load(value); OnPropertyChanged(); }
    }

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
        UniqueIdentifier = meFile.UniqueIdentifier;
        PersonName = meFile.PersonName;
        Location = meFile.Location;
        FirstName = meFile.FirstName;
        OtherFirstNames.Load(meFile.OtherFirstNames);
        OnPropertyChanged(nameof(OtherFirstNamesText));
        LastName = meFile.LastName;
        OtherLastNames.Load(meFile.OtherLastNames);
        OnPropertyChanged(nameof(OtherLastNamesText));
        MaidenName = meFile.MaidenName;
        OtherMaidenNames.Load(meFile.OtherMaidenNames);
        OnPropertyChanged(nameof(OtherMaidenNamesText));
        HasMaidenName = meFile.HasMaidenName;
        Sex = meFile.Sex;
    }
}
