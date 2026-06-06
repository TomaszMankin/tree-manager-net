using System;
using System.Collections.Generic;
using TreeManager.Core.Domain;
using TreeManager.Core.Validation;

namespace TreeManager.App.Services;

public interface IValidationMessageFormatter
{
    IReadOnlyList<string> Format(IReadOnlyList<ValidationIssue> issues, IReadOnlyDictionary<Guid, MeFile> people);
}
