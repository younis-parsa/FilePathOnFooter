# FilePathOnFooter

[![Build VSIX](https://github.com/younis-parsa/FilePathOnFooter/actions/workflows/build.yml/badge.svg)](https://github.com/younis-parsa/FilePathOnFooter/actions/workflows/build.yml)
[![CodeQL](https://github.com/younis-parsa/FilePathOnFooter/actions/workflows/codeql.yml/badge.svg)](https://github.com/younis-parsa/FilePathOnFooter/actions/workflows/codeql.yml)
[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](LICENSE.txt)

A Visual Studio extension that shows the current document's **full file path in a real editor
bottom margin** — docked below the horizontal scroll bar, inside the editor itself.

It is not the global VS status bar (`IVsStatusbar` is never used) and it is not an adornment
overlay. It is a genuine `IWpfTextViewMargin` in the editor's `Bottom` margin container, so it
occupies real layout space, scrolls and resizes with the editor, and appears once per open
document window.

The path is rendered in a **read-only WPF `TextBox`**, so it can be selected with the mouse and
copied with <kbd>Ctrl</kbd>+<kbd>C</kbd> (or the right-click *Copy* command).

**Supported:** Visual Studio 2022 (17.x) and Visual Studio 2026 (18.x) — Community, Professional
and Enterprise, x64.

---

## How it meets the requirements

| Requirement | Where |
|---|---|
| `AsyncPackage` registration | `FilePathOnFooterPackage.cs` — `[PackageRegistration(AllowsBackgroundLoading = true)]` |
| MEF editor margin | `FilePathBottomMargin : Border, IWpfTextViewMargin` |
| MEF margin provider | `FilePathBottomMarginProvider : IWpfTextViewMarginProvider`, `[Export]` |
| Bottom margin container | `[MarginContainer(PredefinedMarginNames.Bottom)]` |
| Ordered after the scroll bar | `[Order(After = PredefinedMarginNames.HorizontalScrollBar)]` |
| Path source | `ITextDocument.FilePath`, resolved via `ITextDocumentFactoryService` |
| Selectable / copyable | read-only `TextBox` (`IsReadOnly = true`), Ctrl+C works natively |
| No `IVsStatusbar.SetText` | not referenced anywhere |
| No adornment overlay | no `AdornmentLayerDefinition`, no `IWpfTextViewCreationListener` |
| VSIX manifest | `source.extension.vsixmanifest` — Community/Pro/Enterprise, `[17.0,19.0)`, `amd64`, `Microsoft.VisualStudio.Component.CoreEditor`, `Microsoft.VisualStudio.MefComponent` asset |

Behavioural details:

- Resolves the path from `ITextView.TextDataModel.DocumentBuffer`, so projection-backed editors
  (Razor, diff views, embedded languages) report the real on-disk file rather than the projection.
- Subscribes to `ITextDocument.FileActionOccurred`, so **Save As** and renames update the margin.
- Subscribes to `ITextDocumentFactoryService.TextDocumentCreated` / `TextDocumentDisposed`, so a
  view whose document is not yet created still picks the path up, and shows `<no file on disk>`
  for buffers with no backing file.
- Uses VS theme brushes via `SetResourceReference`, so it follows light/dark theme switches live.
- <kbd>Esc</kbd> in the margin returns focus to the editor.
- The margin is a pure MEF component: the `AsyncPackage` declares **no auto-load context**, so the
  extension adds nothing to VS startup time.

---

## Project layout

```
FilePathOnFooter/
├─ FilePathOnFooter.sln
├─ build.ps1                            build + optional install script
├─ dist/
│  └─ FilePathOnFooter.vsix             generated installer (VS 2022 + VS 2026)
└─ src/FilePathOnFooter/
   ├─ FilePathOnFooter.csproj
   ├─ FilePathOnFooterPackage.cs        AsyncPackage registration
   ├─ FilePathBottomMargin.cs           IWpfTextViewMargin implementation
   ├─ FilePathBottomMarginProvider.cs   IWpfTextViewMarginProvider MEF export
   ├─ source.extension.vsixmanifest     install targets, prerequisites, assets
   └─ Properties/AssemblyInfo.cs
```

---

## NuGet packages and references

Declared in `FilePathOnFooter.csproj`:

| Package | Version | Purpose |
|---|---|---|
| `Microsoft.VisualStudio.SDK` | `17.14.40265` | VS SDK meta-package: editor MEF contracts (`IWpfTextViewMargin`, `ITextDocument`, `MarginContainerAttribute`, `OrderAttribute`) and the shell (`AsyncPackage`). Referenced with `ExcludeAssets="runtime"` so these assemblies are **not** copied into the VSIX — Visual Studio already ships them. |
| `Microsoft.VSSDK.BuildTools` | `17.14.2142` | VSIX build targets: manifest validation and detokenisation, `.pkgdef` generation, `.vsix` container creation. Supplies `Microsoft.VsSDK.targets` from the package, so the *Visual Studio extension development* workload is **not** required to build. |

The 17.x SDK is deliberate: compiling against the **oldest** supported VS generation produces an
assembly that loads on both 17.x and 18.x. Building against an 18.x SDK would break VS 2022.

Framework references: `System`, `System.Core`, `System.Xml`, `System.Xaml`,
`System.ComponentModel.Composition` (MEF), `PresentationCore`, `PresentationFramework`,
`WindowsBase` (WPF).

Target framework: `.NET Framework 4.7.2`. Language version: C# 7.3.

---

## Building

### Prerequisites

- Visual Studio 2022 or newer, **or** just the Build Tools — `vswhere` plus MSBuild is enough.
- The VS extension development workload is *not* required; the VSIX targets come from NuGet.
- Internet access on first build, for the NuGet restore.

### Script (recommended)

From the repository root:

```powershell
.\build.ps1                          # Release build into .\dist
.\build.ps1 -Configuration Debug
.\build.ps1 -Install                 # build, then launch VSIXInstaller
```

### MSBuild directly

```powershell
$msbuild = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
              -latest -prerelease -products * -find 'MSBuild\**\Bin\MSBuild.exe'

& $msbuild src\FilePathOnFooter\FilePathOnFooter.csproj -t:Restore
& $msbuild src\FilePathOnFooter\FilePathOnFooter.csproj -t:Rebuild -p:Configuration=Release
```

Output: `src\FilePathOnFooter\bin\Release\FilePathOnFooter.vsix`.

### From inside Visual Studio

Open `FilePathOnFooter.sln`, build in Release. <kbd>F5</kbd> launches the **experimental
instance** (`devenv /rootsuffix Exp`) with the extension deployed, which is the fastest way to
iterate — the experimental instance leaves your normal VS untouched.

---

## Installing

1. **Close every Visual Studio window.** VSIXInstaller cannot write to an in-use install.
2. Double-click `dist\FilePathOnFooter.vsix`, or run:

   ```powershell
   & "$vsInstallPath\Common7\IDE\VSIXInstaller.exe" dist\FilePathOnFooter.vsix
   ```

   If both VS 2022 and VS 2026 are installed, the installer lets you pick which to install into.
3. Accept the prompt, then start Visual Studio and open any text document. The path appears along
   the bottom edge of the editor, under the horizontal scroll bar.

To uninstall: **Extensions → Manage Extensions → Installed → File Path On Footer → Uninstall**,
then restart VS.

### About the version range

`Version="[17.0,19.0)"` is inclusive of `17.0` and exclusive of `19.0`, which covers every
Visual Studio 2022 (17.x) and Visual Studio 2026 (18.x) release. The same range is used for the
`Microsoft.VisualStudio.Component.CoreEditor` prerequisite.

To restrict the extension to VS 2022 only, change those four `Version` attributes in
`source.extension.vsixmanifest` to `[17.0,18.0)` and rebuild.

---

## Troubleshooting

**The margin does not appear.**
Confirm the extension is listed under Extensions → Manage Extensions → Installed. If it is
installed but nothing renders, MEF composition probably failed; check the activity log:

```powershell
devenv /log
notepad "$env:APPDATA\Microsoft\VisualStudio\<instance-id>\ActivityLog.xml"
```

A stale MEF cache can also hide a newly installed component — delete
`%LOCALAPPDATA%\Microsoft\VisualStudio\<instance-id>\ComponentModelCache` and restart VS.

**The margin appears above the horizontal scroll bar.**
Another extension is exporting a bottom margin with a conflicting `[Order]`. Adjust the
`Order` attribute in `FilePathBottomMarginProvider.cs`.

**Install is rejected as not applicable.**
The installed VS falls outside the manifest's version range — see *About the version range*.

---

## Continuous integration and security

| Workflow | Trigger | What it does |
|---|---|---|
| `.github/workflows/build.yml` | push to `main`, manual | Builds the VSIX on `windows-latest`, verifies the archive contains the manifest, DLL, pkgdef and licence, and uploads the `.vsix` as a build artifact. |
| `.github/workflows/codeql.yml` | push to `main`, weekly, manual | CodeQL static security analysis of the C# source (`security-and-quality` query suite). Results appear under **Security → Code scanning**. |
| `.github/dependabot.yml` | weekly | Watches the NuGet packages and the GitHub Actions versions for updates and known CVEs. Major bumps of the VS SDK packages are ignored on purpose — see the comments in the file. |

Both workflows are **owner-only**: `workflow_dispatch` and pushes to `main` already require write
access, and each job additionally guards with `if: github.actor == github.repository_owner`, so
nothing executes under a fork's or an outside contributor's identity. Workflow permissions are
scoped down to `contents: read`, with `security-events: write` granted only to the CodeQL job.

---

## Licence

GNU General Public License v3.0 — see [LICENSE.txt](LICENSE.txt).

The licence text is also shipped inside the `.vsix`, since GPLv3 requires it to travel with the
binary. Each source file carries the standard GPL notice.
