using TreeManager.Core.Abstractions.Persistence;
using TreeManager.Core.Abstractions.Validation;

namespace TreeManager.App.Services;

public sealed record ValidationCommandDependencies(
    ITreeConsistencyValidator Validator,
    IValidationMessageFormatter Formatter,
    IValidationReportService ReportService,
    IMeFileProcessor Processor);
