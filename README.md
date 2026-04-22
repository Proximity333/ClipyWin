# ClipyWin

A Windows port of [Clipy](https://github.com/Clipy/Clipy), the popular clipboard extension for macOS. Built with **WPF + .NET 10 + C#**.

Clipy is a lightweight clipboard manager that keeps a rolling history of everything you copy, lets you recall any past entry with a global shortcut, and supports reusable text snippets. ClipyWin brings the same experience to Windows 10/11.

> This project is not affiliated with the original Clipy team. It is an independent, community port of their design.

## Features

- **Clipboard history** — text, RTF, HTML, images, file drops
- **Global hotkey** — press `Ctrl+Shift+V` anywhere to pop the history menu right at your cursor
- **One-click paste** — pick an item, it gets pasted into the focused window automatically
- **Alt+click** any entry to paste as plain text (strips formatting)
- **Numeric shortcuts** — when enabled, press `1`-`9` on the open menu to paste the Nth item
- **Snippets** — group reusable texts into folders, paste with one click; XML import/export
- **Excluded apps** — disable history capture while specific apps are focused (password managers, etc.)
- **Color swatches** — hex color codes (`#FF6600`, `#0af`) show a color preview in the menu
- **Image thumbnails** — copied bitmaps show a live thumbnail in the menu
- **Configurable hotkeys** — editable in Preferences for both history and snippets menus
- **Launch at login** — native Windows Registry integration
- **Localization** — English + Türkçe, switchable in Preferences
- **Auto-update** — opt-in via [Velopack](https://velopack.io) by pointing at a release feed

## Screenshots

*(Add screenshots here once you capture them — recommended: history menu over a real app, Preferences window, Snippets editor.)*

## Install

### Option A — Pre-built release

Grab the latest installer from [Releases](../../releases). Run it and you're done.

### Option B — Build from source

Requires the **.NET 10 SDK**: https://dotnet.microsoft.com/download/dotnet/10.0

```bash
git clone https://github.com/taylaneren/ClipyWin.git
cd ClipyWin
dotnet restore
dotnet build -c Release
dotnet run --project src/ClipyWin/ClipyWin.csproj
```

The built binary lands at `src/ClipyWin/bin/Release/net10.0-windows/ClipyWin.exe`.

## Usage

| Action | Shortcut |
|---|---|
| Show clipboard history | `Ctrl+Shift+V` (default, rebindable) |
| Show snippets menu | unset by default; bind in Preferences → Shortcuts |
| Paste entry | click it |
| Paste as plain text | `Alt`+click |
| Numbered quick-paste | `1`–`9` on the open menu (enable in Preferences → Menu) |
| Open Preferences | from the history menu → *Preferences…* |
| Edit snippets | from the history menu → *Edit Snippets…* |
| Clear history | from the history menu → *Clear History* |
| Quit | from the history menu → *Quit Clipy* |

There is no tray icon by design — everything is reachable from the hotkey-summoned menu.

## Data locations

| What | Where |
|---|---|
| Settings (JSON) | `%AppData%\ClipyWin\settings.json` |
| Snippets + metadata (LiteDB) | `%AppData%\ClipyWin\clipy.db` |
| Clip payloads (per-item JSON) | `%AppData%\ClipyWin\Clips\*.json` |
| Log | `%AppData%\ClipyWin\clipy.log` |

## Architecture

ClipyWin mirrors the architecture of the original macOS app where it makes sense:

| macOS (Clipy) | Windows (ClipyWin) |
|---|---|
| `AppEnvironment` | `Environments/AppEnvironment.cs` |
| `ClipService` | `Services/ClipService.cs` (uses `WM_CLIPBOARDUPDATE`) |
| `PasteService` | `Services/PasteService.cs` (synthesised `Ctrl+V` via `SendInput`) |
| `HotKeyService` (Magnet) | `Services/HotKeyService.cs` (`RegisterHotKey`) |
| `ExcludeAppService` | `Services/ExcludeAppService.cs` (process-name match) |
| `DataCleanService` | `Services/DataCleanService.cs` |
| `MenuManager` (NSStatusItem) | `Managers/MenuManager.cs` (WPF `ContextMenu`) |
| Sparkle (updates) | `Services/UpdateService.cs` (Velopack) |
| Realm | LiteDB |
| NSKeyedArchiver `.data` | JSON `.json` per clip |
| UserDefaults | `Storage/Settings.cs` (JSON) |
| `CPYClip` | `Models/ClipEntry.cs` |
| `CPYClipData` | `Models/ClipData.cs` |
| `CPYFolder` / `CPYSnippet` | `Models/Folder.cs` / `Snippet.cs` |

## Tech stack

- **.NET 10** + **WPF** (Windows-only target: `net10.0-windows`)
- **LiteDB** for metadata, folders, snippets
- **Velopack** for app packaging and updates
- P/Invoke for `RegisterHotKey`, `SendInput`, `WM_CLIPBOARDUPDATE`, `AttachThreadInput`

## Development

Core project layout:

```
src/ClipyWin/
├── Environments/     # AppEnvironment (DI root)
├── Services/         # Clip, Paste, HotKey, Exclude, DataClean, Update
├── Managers/         # MenuManager (history + snippets menus)
├── Models/           # ClipEntry, ClipData, Folder, Snippet
├── Storage/          # Settings (JSON), ClipyDb (LiteDB)
├── Interop/          # NativeMethods, GlobalHotKey, InputSimulator
├── Utilities/        # Log, Loc, AppPaths, HotKeyCombo, LoginItem
└── Views/            # PreferencesWindow, SnippetsWindow
```

Entry point is [`Program.cs`](src/ClipyWin/Program.cs), which runs `VelopackApp.Build().Run()` first (to handle install/update hooks) before starting the WPF application.

## Contributing

Issues and pull requests are welcome. If you're adding a feature that already exists in macOS Clipy, it's helpful to link to the corresponding source file so reviewers can compare behaviour.

## Credits

- **[Clipy](https://github.com/Clipy/Clipy)** — the original macOS app by the Clipy Project (MIT licensed), which inspired this port and whose UX this project follows closely.
- **[ClipMenu](https://www.clipmenu.com)** — the earlier macOS clipboard manager that Clipy itself was inspired by.

## License

MIT, consistent with the upstream Clipy project. See [LICENSE](LICENSE).
