using System;

namespace TreeManager.App.Services;

public interface ICrashReporter
{
    void Report(Exception ex, string source);
    void ReportManual(string note);
}
