---
layout: landing
title: DotNetDo
description: A repository task runner built on modern .NET. Build tasks are plain C# files that run the same way on your laptop and in CI.
---

<div class="landing">

<section class="hero">
  <p class="eyebrow">A repository task runner built on modern .NET</p>
  <h1>Your build script is just <span>C#</span></h1>
  <p class="lede">Every repository accumulates a pile of YAML, shell, and copy-pasted pipeline steps that nobody can run locally and nobody wants to change. DotNetDo moves it into the language you already ship, where the compiler checks it, your IDE understands it, and it runs the same on your laptop and in CI.</p>
  <div class="hero-actions">
    <a class="button primary" href="getting-started.md">Get started</a>
    <a class="button" href="guides/task-orchestration.md">Read the guides</a>
    <a class="text-link" href="https://github.com/soxtoby/DotNetDo">GitHub</a>
  </div>
</section>

<section class="showcase">

<div class="showcase-copy">

## A whole build task in one file

<p class="section-lede">DotNetDo is built on .NET 10 file-based apps. A shebang, a package reference, and the file is a runnable task. No project, no scaffolding, no build step in front of your build.</p>

<ul class="showcase-notes">
  <li>Parameters are declared in code and exposed on the command line</li>
  <li>Tool commands are records, so options are discoverable and type-checked</li>
  <li><code>./do build</code> runs it anywhere; add <code>--version-suffix beta.1</code> when you need it</li>
</ul>

</div>

<div class="showcase-code">

```csharp
#!/usr/bin/env dotnet
#:package DotNetDo.Core@0.7.0
using DotNetDo;
using static DotNetDo.Tools;

[assembly: TaskDescription("Build, test, and package the solution.")]

var versionSuffix = Do.Param("version-suffix");

await DotNet.Test;

await (DotNet.Pack with
{
    VersionSuffix = versionSuffix,
    Output = Do.RootDirectory / "artifacts",
});
```

</div>

</section>

<section class="pillars">

## Why write automation in C#

<p class="section-lede">Build logic is real logic. It deserves the same tools as the rest of your code.</p>

<div class="pillar-grid">
  <div class="pillar">
    <span class="marker">01</span>
    <h3>Code, not configuration</h3>
    <p>Autocomplete finds the options, the compiler catches the typos, and rename refactoring reaches your build. Nothing important hides inside an unchecked string.</p>
  </div>
  <div class="pillar">
    <span class="marker">02</span>
    <h3>Modern .NET, no ceremony</h3>
    <p>File-based apps mean a task is a single <code>.cs</code> file. Nothing to scaffold, nothing to keep in sync, nothing standing between an idea and running it.</p>
  </div>
  <div class="pillar">
    <span class="marker">03</span>
    <h3>Batteries included</h3>
    <p>Typed commands wrap the tools you already run, like <code>dotnet</code>, <code>git</code>, and <code>npm</code>. Paths, secrets, and logging come in the box. Fill in properties instead of stitching together a command line.</p>
  </div>
  <div class="pillar">
    <span class="marker">04</span>
    <h3>The same script everywhere</h3>
    <p>What you debug on your laptop is what CI runs. Paths are cross-platform by construction, and the CI provider is detected for you, so the pipeline stays a one-liner.</p>
  </div>
</div>

</section>

<section class="steps">

## From nothing to a working build

<div class="step-list">
  <div class="step">
    <span class="count">STEP 01</span>
    <p>Install the global tool. DotNetDo needs the .NET 10 SDK.</p>
    <code>dotnet tool install --global DotNetDo</code>
  </div>
  <div class="step">
    <span class="count">STEP 02</span>
    <p>Set up the workspace and its local launchers.</p>
    <code>dotnetdo :init</code>
  </div>
  <div class="step">
    <span class="count">STEP 03</span>
    <p>Run a task by name, with completion in your shell.</p>
    <code>./do build</code>
  </div>
</div>

</section>

<section class="closing">

## Let's <span>do</span> it

<p class="section-lede">Start with a single task and move the rest of the pipeline over as you go. Nothing has to migrate at once.</p>

  <div class="hero-actions">
    <a class="button primary" href="getting-started.md">Get started</a>
    <a class="button" href="reference/index.md">API reference</a>
  </div>

</section>

</div>
