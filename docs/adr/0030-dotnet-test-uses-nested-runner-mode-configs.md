# DotNet test uses nested runner-mode configs

`dotnet test` accepts different options depending on the repository's test runner mode, VSTest or Microsoft.Testing.Platform, which is selected by `global.json` or `DOTNET_TEST_RUNNER` rather than by the command line. `DotNet.Test` remains one command: its shared build and selection options sit at the top level, and each mode's options live in nullable nested `VSTest` and `TestingPlatform` configs. The command detects the mode as the SDK does, renders shared options such as targets for that mode, and fails before execution when a config for the other mode is set, instead of letting the test application reject unknown options. This keeps a bare `DotNet.Test` working in either kind of repository and gives options such as `Output`, which mean different things per mode, one meaning per config.

DotNetDo gets test results into CI only by configuring the test runner's own test reporters. It does not parse, convert, or publish report files, and it never enables a reporter automatically on CI, because a reporter option the test project hasn't referenced fails the run.

## Considered options

- A separate `DotNet.MtpTest` command beside `DotNet.Test`, optionally sharing a base record. Rejected so that users don't have to know their repository's runner mode just to pick an entry point.
- A single required mode-options property. Rejected because tasks using only shared options would have had to name a mode anyway.
- A DotNetDo-side publisher (`##vso[results.publish]`, GitHub step summaries rendered from TRX/JUnit). Rejected because it duplicates the runners' own reporters and pipeline tasks such as `PublishTestResults@2`, and Azure's command is undocumented.
