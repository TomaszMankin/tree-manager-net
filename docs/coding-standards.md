# Coding Standards — tree-manager-net

Agent instruction: Read this file in full before writing or reviewing any code in this repo. These rules are enforced at every PR and repeat violations are PR-blockers.

---

## Test naming

3-segment pattern: `MethodOrClass_ReturnsOrDoesOrThrows_WhenCondition`

Examples:
- `ParsePartialDate_ReturnsPartialDate_WhenSerializedStringIsValid`
- `Serialize_EmitsPolishWord_WhenSexIsMale`
- `ReadMeFile_Throws_WhenPathIsNull`

After writing a test, re-read the name. If it could apply to a different test body, rename it.

---

## AAA comments

Add `//Arrange`, `//Act`, `//Assert` to every test body with 2+ lines.

Exceptions:
- Single-expression tests (`Assert.Throws<X>(() => expr)` alone) — skip AAA
- 2-liner with Arrange + Assert.Throws still needs AAA

Do NOT add redundant sub-comments under `//Assert` that restate what the assertion checks.

---

## Test structure

- `_sut` created in constructor, not per-test
- Cleanup via `IDisposable` / `Dispose()` — NOT per-test try/finally
- Private helper methods go at the **bottom** of the test class, after all `[Fact]`/`[Theory]` methods
- Use `#region` to group test sections by method-under-test
- Magic strings → named `private const string` fields (e.g. `WireMale = "Mężczyzna"`)
- Foreach loops inside tests = antipattern → use `[Theory][InlineData]` instead

---

## Test fixture factories

When a second test class needs to build the same domain object (e.g. `MeFile`), extract a shared factory/builder to a shared test-helpers project in the same sprint pass. Do not defer.

---

## Test data — no real personal data

Never use the repo owner's real surname in test fixtures, const strings, or test method names. Use fictitious names only.

---

## What NOT to test

- Thin pass-through wrappers (e.g. `FileSystemFacade` → `System.IO`) — trust the BCL
- Test the class that HAS logic; mock `IFileSystemFacade` in processor/service tests
- No L1 tests unless the class has real filesystem/network/integration logic beyond pass-through

---

## Exception logging — FIRM RULE

Caught exceptions ALWAYS log at `Error` level, never `Warning`.

```csharp
catch (Exception ex)
{
    _log.Error(ex, "Description of what failed");
}
```

Before committing: grep all catch blocks in changed files for `.Warning(`. Replace any `.Warning(` inside a catch with `.Error(`.

---

## Nullable

`<Nullable>disable</Nullable>` is set in `Directory.Build.props`. Never write `null!` or `(T)null!`. Assign `string input = null` and check manually. No `?` annotations needed anywhere.

---

## Mocking library

Use **Moq** in all L0 test projects. Never use NSubstitute.

```csharp
var mock = new Mock<IInterface>();
mock.Setup(x => x.Method(It.IsAny<string>())).Returns("value");
var sut = new Sut(mock.Object);
mock.Verify(x => x.Method("expected"), Times.Once());
```

---

## No Polglish identifiers

ALL code identifiers must be English: class names, interfaces, methods, fields, properties, local variables, const names.

Polish is only allowed in:
- String literals visible to the elderly user (UI text)
- JSON key names that are persisted data (changing them breaks files)
- Path constants that match real filesystem paths

Translation table:
- Drzewo → FolderTree
- Lista → List
- Osoba → Person
- Rody → Lineage

Grep all changed files for `drzewo`, `rody`, `osoba`, `lista` as identifiers before committing.

---

## Source code comments

### Banned (PR-blocker)

- ADR references in comments: `// See ADR-012`, `// per ADR-021`
- Issue/PR numbers: `// #13`, `// issue #36`, `// PR #35 feedback`
- Foreign repo names: `py-tree-manager`, `familytree`, `bloodline`
- Sprint references: `// added in sprint-12`, `// TODO sprint-15`
- `// TODO` / `// FIXME` / `// HACK` leftover from development

### No self-explanatory comments

Do NOT comment method calls, obvious assignments, standard LINQ, property getters, constructors.

Comment ONLY when the WHY is non-obvious: a hidden constraint, a subtle invariant, a specific bug workaround, COM/Win32 interop, an algorithm not obvious from the code.

If a comment could be deleted and replaced by reading the method name + parameters, delete it.

### Allowed

- `#region` grouping labels — these are structure, not comments
- Numbered orchestrator steps `// 1. Do this`, `// 2. Then that` — these are the spec for orchestration logic
- COM/Win32/P-Invoke interop rationale
- Non-obvious serializer option explanation

---

## Long methods — preferred decomposition

```csharp
public void LongMethod(/* params */)
{
    // 1. Do this first
    var result = DoingThisFirst(params);
    SomethingElse(result, params);

    // 2. Then do this
    DoSecondThing(params);

    // 3. Then that
    DoThirdThing(params);
}
```

Each numbered step = one private method. The public method reads as a numbered list of product-level steps.

---

## Extension methods

- Prefer `this T input` extension over static helper on a utility class
- Extensions on external types (e.g. `string`) go in their own file: `StringExtensions.cs`
- Data classes should not have static `Parse` methods — use string extension methods instead

---

## Feature name references in comments and strings

When referring to named folders like `'Drzewo'` or `'Rody'` in comments or docs, wrap in single quotes to signal it is a specific named artifact, not a Polish identifier.

---

## PR format

Always read `.github/PULL_REQUEST_TEMPLATE.md` before creating a PR.

- Title: `[Feature|Fix|Refactor|Docs|Chore] [#N] Short title`
- Body sections: `Closes #N`, `## What`, `## Links`, `## Checklist`
- `Closes #N` is required — enables automatic issue close on merge
- Do NOT use: Conventional Commits headers, Summary/Architecture Notes/Test Plan sections

---

## Spec vs reality

When the issue spec contradicts actual data files, parity with data wins. Document the override in an ADR. Do not block the sprint on it.

---

## ADR content rules

ADRs must NOT contain implementation details: no field names, method names, class names, interface names, or converter logic.

Decision bullets stay at "what" and "why" level. Implementation belongs in code, not in the ADR.

Forbidden in ADR text: class names (`CrashReporter`), interface names (`Serilog.Core.ILogEventSink`), sprint references, issue numbers in body text (only in the `Issue:` frontmatter field).

---

## Pipeline artifacts — never commit

Files under `.pipeline/`, `3-repo/pr-*.md`, and any other working files belong under `.pipeline/` or `.claude/` (both gitignored). Never commit them to the main repo.
