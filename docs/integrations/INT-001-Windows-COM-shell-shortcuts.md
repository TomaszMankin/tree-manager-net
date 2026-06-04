# INT-001 — Windows COM shell shortcut creation

## Integration
Windows Shell COM API via CsWin32 P/Invoke source generator.

## Why not WScript.Shell
`WScript.Shell.CreateShortcut()` corrupts non-ASCII characters in target paths. Polish diacritics (ę, ń, ł, ó, ś, ż, ą) become mojibake because the VBScript bridge converts strings through the system ANSI codepage (cp1250/cp1252).

## Why CsWin32 + IShellLinkW
`IShellLinkW` is the Unicode-native COM interface for shell link creation. CsWin32 generates strongly-typed, compile-time-verified P/Invoke wrappers. Target paths are passed as `PCWSTR` (16-bit wide-character pointers) with no codepage conversion. Integration tests confirm diacritics survive a full roundtrip (write + resolve).

## NativeMethods.txt
`src/Infrastructure/TreeManager.Infrastructure/NativeMethods.txt` is the CsWin32 source generator input. Each line names a Windows API for which CsWin32 emits interop code at build time.

Current entries:

| Entry | Purpose |
|---|---|
| `IShellLinkW` | Read/write shell link properties (target path, description, working dir) |
| `IPersistFile` | Persist the in-memory link object to a `.lnk` file on disk |
| `CoCreateInstance` | Instantiate the `ShellLink` COM coclass |
| `CoInitializeEx` | Initialise the COM apartment on the calling thread |
| `CoUninitialize` | Release the COM apartment (called only when we initialised it) |

Do not add entries to NativeMethods.txt without verifying the generated code compiles; some Windows SDK symbols require additional SDK package references.

## TFM requirement
Projects that reference CsWin32-generated types must target `net10.0-windows`. The plain `net10.0` TFM does not expose `Windows.Win32` namespaces. This cascades to all test projects that have a ProjectReference to the Infrastructure library.
