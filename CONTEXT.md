# Glossary

## File-based app

A single C# source file intended to be created and run directly by DotNetDo.

Generated file-based apps reference the DotNetDo package and import the `DotNetDo` namespace by default.

The initial DotNetDo API surface is intentionally tiny. Generated apps reference it to establish a stable import path for future helpers.

## App name

The extensionless local name of a file-based app. In v1, an app name resolves only to `<app-name>.cs` in the current directory.

App names are simple file stems: letters, numbers, `_`, `-`, and `.` are allowed; path separators and a `.cs` suffix are rejected.

## App arguments

Arguments after the app name in a run command are forwarded to the file-based app.

## Run command

The run command executes a file-based app through SDK file execution, equivalent to `dotnet <app-name>.cs -- <app-arguments>`. DotNetDo does not emulate file-based app support for older SDKs.

## App list

Running `do` with no arguments lists file-based apps in the current directory.

## New command

The `:new` command creates a file-based app and fails if the target file already exists.

On Unix-like systems, `:new` makes the generated file executable on a best-effort basis. Windows does not need executable bits for DotNetDo usage.

## Tool command

A command whose name starts with `:` is owned by DotNetDo. App names cannot start with `:`.

DotNetDo v1 includes only app listing, app creation with `:new`, help with `:help`, and app execution by name.

## Exec helper

A DotNetDo library helper for running an external program from a file-based app.

Exec helper commands are a single command-line string where DotNetDo parses only the program token and passes the remaining argument string to .NET process execution.
