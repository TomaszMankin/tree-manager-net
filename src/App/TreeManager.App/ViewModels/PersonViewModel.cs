using System;
using System.ComponentModel.DataAnnotations;
using CommunityToolkit.Mvvm.ComponentModel;
using TreeManager.Core.Domain;

namespace TreeManager.App.ViewModels;

public sealed partial class PersonViewModel : ObservableValidator
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
    [NotifyDataErrorInfo]
    [CustomValidation(typeof(PersonViewModel), nameof(ValidateSex))]
    private Sex _sex = Sex.Unknown;

    [ObservableProperty]
    private string _personName = string.Empty;

    [ObservableProperty]
    private string _location = string.Empty;

    [ObservableProperty]
    private Guid _uniqueIdentifier = Guid.Empty;

    public static ValidationResult ValidateSex(Sex value, ValidationContext context)
    {
        return value == Sex.Unknown
            ? new ValidationResult("Płeć jest wymagana")
            : ValidationResult.Success;
    }

    public void ValidateAll() => ValidateAllProperties();
}
