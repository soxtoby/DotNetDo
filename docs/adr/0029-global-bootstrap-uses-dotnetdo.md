# Global bootstrap uses dotnetdo

DotNetDo publishes `dotnetdo` as its global command, matching the project and package name without punctuation. The global command initializes workspaces and manages shell completion; initialized repositories use `./do` for task and management commands. This gives up the `dotnet do` SDK shorthand but avoids the PowerShell `do` keyword and keeps the frequent workspace command local and short.
