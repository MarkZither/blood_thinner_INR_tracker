# T009-013 Test Coverage Plan

Status: draft
Related task: T009-013
Branch: `009-bug-fix-editing`

Purpose
- Define a concrete, actionable plan to reach the repository's test coverage goals for the work in `T009-*` (feature 009).
- Provide local commands, CI snippets, and templates so contributors can measure, reproduce, and enforce coverage.

Goals
- Primary goal: Achieve >= 90% line coverage for the code changed by this feature (audit, INR flow, DB wiring) across the affected projects. This is a scoped requirement focused on the modified assemblies, not necessarily the entire solution.
- Secondary goals:
  - Improve branch/condition coverage where it matters for security and audit logic.
  - Harden flaky tests and provide guidance to run integration scenarios deterministically (Testcontainers / Docker).
  - Add CI gating that fails when coverage for targeted projects falls below thresholds.

Scope
- Projects to include in the coverage scope for T009 (examples):
  - `src/BloodThinnerTracker.Api` (API surface + controllers)
  - `src/BloodThinnerTracker.Data.Shared` (shared EF models, interceptors)
  - `src/BloodThinnerTracker.Data.SQLite` and `src/BloodThinnerTracker.Data.SqlServer` (provider-specific wiring)
  - `src/BloodThinnerTracker.Web` (important UI logic such as mapping PublicId) — focus on BUnit for component-level tests where logical
  - Tests projects under `tests/` will be the measurement entry points

Acceptance Criteria
- A baseline coverage report exists and is checked into the feature spec (artifact or summary table).
- For the modified assemblies, coverage is >= 90% lines (or agreed per-project threshold).
- CI pipeline runs coverage on PRs that change the targeted projects and fails if thresholds aren't met.
- Flaky tests reduced to < 1% failures on CI and documented with mitigation steps.

Plan — high level tasks
1. Measure baseline coverage (T009-013.1)
2. Define per-project thresholds and finalize targets (T009-013.2)
3. Add test cases to cover identified gaps (T009-013.4)
4. Add CI coverage collection & gating (T009-013.3)
5. Local verification & flaky-test mitigation (T009-013.5)

Detailed Steps

1) Measure baseline coverage (quick, local)
- Install ReportGenerator (dotnet tool) if not present:

```powershell
dotnet tool install --global dotnet-reportgenerator-globaltool --version 5.*
# May need to restart your shell or use the full path to the tool
```

- Run coverage for a test project (example: API tests):

```powershell
# Run per-test-project with coverlet via msbuild integration
dotnet test tests\BloodThinnerTracker.Api.Tests\BloodThinnerTracker.Api.Tests.csproj `
  -c Debug `
  /p:CollectCoverage=true `
  /p:CoverletOutput=./coverage/` `
  /p:CoverletOutputFormat=opencover `
  /p:Exclude="[xunit.*]*,[*]Migrations.*"

# Generate an HTML report
reportgenerator -reports:tests\BloodThinnerTracker.Api.Tests\coverage\coverage.opencover.xml -targetdir:tests\BloodThinnerTracker.Api.Tests\coverage\report
```

- Repeat for each test project. Keep the reports and summarize percentages in the spec.

2) Define per-project thresholds
- For this feature, propose thresholds:
  - `BloodThinnerTracker.Data.Shared`: 95% (critical: audit + DB logic)
  - `BloodThinnerTracker.Api`: 90% (controllers + validation)
  - `BloodThinnerTracker.Web`: 80% (UI is lower priority; use BUnit where applicable)
- Document thresholds in `test-coverage-plan.md` and in the CI workflow so PRs can be validated automatically.

3) Add tests to close gaps
- Prioritize tests that exercise:
  - AuditInterceptor: create/edit/delete flows (happy path + failure rollback)
  - Soft-delete behavior and global query filters
  - EnsureDatabaseAsync and migration-lock helper behavior (integration tests using Testcontainers)
  - Controller validation and authorization paths (403/400 cases)
- Use test patterns:
  - Unit tests for pure logic (interceptor serialization, JSON snapshots)
  - Integration tests (SQLite in-memory with OpenConnection/EnsureCreated for schema) for EF wiring
  - Containerized integration tests for full Postgres/SQL Server behaviour (locks and migrations)
  - BUnit tests for Blazor components that were modified
- Add tests incrementally; each PR should increase coverage for the impacted assemblies.

4) CI integration (GitHub Actions example)
- Add a workflow `/.github/workflows/coverage.yml` that:
  - Runs on PRs that touch targeted projects
  - Restores, builds, runs tests with coverage collection
  - Merges coverage results and generates an HTML report artifact
  - Fails the job if thresholds are not met

Example workflow snippet (skeleton):

```yaml
name: Coverage
on:
  pull_request:
    paths:
      - 'src/BloodThinnerTracker.Api/**'
      - 'src/BloodThinnerTracker.Data.Shared/**'
      - 'src/BloodThinnerTracker.Data.SQLite/**'
      - 'tests/**'

jobs:
  coverage:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v4
      - name: Setup .NET
        uses: actions/setup-dotnet@v4
        with:
          dotnet-version: '10.0.x'
      - name: Restore
        run: dotnet restore
      - name: Run tests with coverage
        run: |
          dotnet test tests/BloodThinnerTracker.Api.Tests/BloodThinnerTracker.Api.Tests.csproj \
            -c Debug /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=coverage/api/ /p:Exclude="[xunit.*]*,[*]Migrations.*"

          dotnet test tests/BloodThinnerTracker.Web.Tests/BloodThinnerTracker.Web.Tests.csproj \
            -c Debug /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=coverage/web/ /p:Exclude="[xunit.*]*,[*]Migrations.*"

      - name: Merge reports and publish HTML
        run: |
          reportgenerator -reports:coverage/**/coverage.opencover.xml -targetdir:coverage/report -reporttypes:Html
      - name: Upload coverage report
        uses: actions/upload-artifact@v4
        with:
          name: coverage-report
          path: coverage/report
      - name: Enforce thresholds
        run: |
          # Example using coverlet's threshold enforcement on a critical project
          dotnet test tests/BloodThinnerTracker.Api.Tests/BloodThinnerTracker.Api.Tests.csproj /p:CollectCoverage=true /p:CoverletOutput=coverage/api/ /p:CoverletOutputFormat=opencover /p:Threshold=90 /p:ThresholdType=line /p:ThresholdStat=total
```

Notes:
- The last `dotnet test` step runs with `/p:Threshold` parameters to cause `dotnet test` to fail when coverage is below the threshold (requires `coverlet.msbuild` to be referenced by the test project). Ensure test projects reference `coverlet.msbuild` where enforcement is desired.
- Alternative: use a coverage badge and a policy that PRs must include a coverage report artifact and reviewers check the numbers manually. Automated enforcement is more reliable with thresholds.

5) Local verification & flaky tests
- Flaky integration tests: prefer Testcontainers and explicit container lifecycle. Use `StartFresh` isolated DB per test or per-class and deterministic seeding.
- For EF tests using SQLite: open a shared in-memory connection and call `context.Database.EnsureCreated()` before the test to ensure migrations/schema are present.
- Use `dotnet test --filter TestName` for focused runs.

Run settings (optional)
- Use a `.runsettings` to configure diagnostics and exclude auto-generated code from coverage.

Example `coverage.runsettings` minimal:

```xml
<?xml version="1.0" encoding="utf-8"?>
<RunSettings>
  <DataCollectionRunSettings>
    <DataCollectors>
      <DataCollector friendlyName="XPlat Code Coverage">
        <Configuration>
          <Format>opencover</Format>
        </Configuration>
      </DataCollector>
    </DataCollectors>
  </DataCollectionRunSettings>
  <RunConfiguration>
    <ResultsDirectory>TestResults</ResultsDirectory>
  </RunConfiguration>
</RunSettings>
```

Useful local commands summary

```powershell
# Per-test project coverage and HTML report
dotnet test tests\BloodThinnerTracker.Api.Tests\BloodThinnerTracker.Api.Tests.csproj -c Debug /p:CollectCoverage=true /p:CoverletOutputFormat=opencover /p:CoverletOutput=./coverage/
reportgenerator -reports:tests\BloodThinnerTracker.Api.Tests\coverage\coverage.opencover.xml -targetdir:tests\BloodThinnerTracker.Api.Tests\coverage\report

# Combine all coverage reports
reportgenerator -reports:tests\**\coverage\coverage.opencover.xml -targetdir:coverage\combined-report -reporttypes:Html
```

Prioritization & test backlog
- Triage changed files and identify functions with lowest coverage and highest risk (security, DB, audit).
- Create focused tickets (or GitHub issues) for each high-priority test to add:
  - `Unit: AuditInterceptor - Before/After snapshots` (1-2 tests)
  - `Integration: Soft-delete + global filter` (1 test)
  - `Integration: EnsureDatabaseAsync + migration lock` (containerized test)
  - `BUnit: INR edit form behaviour` (1-3 tests)

PR Checklist (when adding tests)
- [ ] Include which project(s) the new tests exercise
- [ ] Add/update coverage report and expected delta
- [ ] If flaky, mark with `[Flaky]` and add mitigation notes
- [ ] Link to the T009-013 plan entry for traceability

Notes and risks
- Targets are intentionally high; consider incremental enforcement: start with reporting only, then add gating for critical projects.
- Coverage is a quality metric but not a sole measure of reliability: pair coverage with targeted integration tests and manual verification for migration locking behavior.

Next steps I can take (pick one):
- Run baseline coverage for the whole solution and produce a summary table of per-project coverage.
- Create a GitHub Actions workflow `coverage.yml` implementing the snippet above and push it to the branch (requires a PR).
- Create the prioritized test backlog issues in `specs/009-bug-fix-editing/tests/` as individual markdown files.

If you want me to run the baseline coverage now, tell me and I'll collect per-project numbers and attach the report to the spec.

## Baseline Results (2025-11-18)

- **Report files generated:** `coverage/report` (HTML) and `coverage/report-summary` (text). Open `coverage/report/index.html` to view the merged HTML report.
- **Observed result:** Coverage files were produced by the VSTest XPlat collector (`coverage.cobertura.xml`) but contain no covered assemblies (the merged summary reports 0 assemblies / 0 covered lines). The generated report exists but shows no coverage data.
- **Likely cause:** Tests were executed with the built-in XPlat collector but the produced coverage payloads do not include assembly instrumentation for this repo configuration. Common fixes:
  - Add `coverlet.msbuild` (or `coverlet.collector`) to test projects so `/p:CollectCoverage=true` and `/p:CoverletOutputFormat=opencover` emit usable OpenCover XML.
  - Or run `dotnet test` with coverlet parameters per-project (e.g. `/p:CollectCoverage=true /p:CoverletOutput=./coverage/ /p:CoverletOutputFormat=opencover`) and ensure the test project references `coverlet.msbuild`.
- **Next steps recommended:**
  1. Add `Coverlet.MSBuild` package to the test projects where enforcement or reporting is desired.
  2. Re-run per-project `dotnet test` with coverage parameters and verify `coverage/coverage.opencover.xml` (or `coverage.cobertura.xml`) contains assembly elements.
  3. Re-generate the merged HTML/Text reports and paste the `Summary.txt` contents into this spec as the baseline.

If you want, I can make the minimal test-project changes (add `Coverlet.MSBuild` package references) and re-run coverage to produce a proper baseline.