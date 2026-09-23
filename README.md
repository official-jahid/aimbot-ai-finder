# Aimbot AI Finder — Jahid Collider Finder

A Windows desktop helper for **collider / read / result hex calculations** with a modern dark-themed UI, particle background, and license-gated login. Built with **C# + WinForms on .NET Framework 4.8 (x64)** by **REGIX Studio**.

> Assembly: `JahidColliderFinder` · Solution: `Collider Finder.sln` · Company: `REGIX Studio` · License: `MIT`

## What it does

This is an **offline calculator** (no memory reading / injection). It derives one hex value from another using offsets in `offset.cs`:

```csharp
Player_Data  = 0x48
WeaponRecoil = 0x0C
WeaponOnHand = 0x54
Camera       = 0x18

baseOffset = WeaponOnHand - Camera + WeaponRecoil // 0x54 - 0x18 + 0x0C = 0x48
Jahidggg   = Player_Data + WeaponRecoil           // 0x48 + 0x0C = 0x54
```

`MainForm` (`Form1.cs`) exposes **3 cards**:

| # | Card | Inputs | Formula |
|---|------|--------|---------|
| 1 | Collider -> Result | `COLLIDER INPUT`, `TARGET VALUE` | `result = collider - value - 0x54` |
| 2 | Read -> Result | `READ VALUE HEX` (~2s delay) | `result = read + 0x48` |
| 3 | Result -> Read (`FIND READ`) | `RESULT VALUE HEX` | `read = result - 0x48` |

Invalid / negative results show `INVALID`. Hex parsing accepts `0x`-prefixed or plain hex (`ParseHex` / `ToHex` in `Form1.cs`).

### Main UI features

- Borderless custom chrome, draggable title bar, DWM shadow, live clock.
- Particle canvas (80 particles, 30ms timer) + faint grid — both toggleable.
- Glow cards (`GlowCard`, `GlowButton`, `GlowTextBox`, `GlowResultBox`), accent `#8A94F8` on `#0B0C10`.
- Sidebar (`FontAwesome.Sharp` icons): Collider Finder, Visual Settings, Profile, Logout.
- Status bar: `DEVELOPED BY REGIX STUDIO`.

## Screens / Flow

```
Program.Main -> AuthChoiceForm -> { UserPassLoginForm | LicenseLoginForm } -> MainForm <-> VisualSettingsForm / ProfileForm
```

1. **AuthChoiceForm** (`Auth/AuthChoiceForm.cs`): welcome screen, calls `AuthSession.Api.init()` once, handles `invalidver` auto-update prompt.
2. **UserPassLoginForm / LicenseLoginForm**: glow inputs + login, sets `AuthSession.IsAuthed = true` and opens `MainForm`.
3. **MainForm** (`Form1.cs`): the 3-card calculator. Re-opens auth choice if not authed.
4. **VisualSettingsForm** (`Views/VisualSettingsForm.cs`): toggles for Particles, Grid, Glow, Clock via `Views/VisualState.cs`.
5. **ProfileForm** (`Views/ProfileForm.cs`): Username, Subscription, Expires, Time Left, IP, HWID, Created, Last Login, Mode + Logout.
6. Shared theme/nav in `Views/ThemedShell.cs`.

## Tech stack

- **C# (`LangVersion: latest`, `Nullable: enable`, `AllowUnsafeBlocks: true`)** on **.NET Framework 4.8** (SDK-style project, `PlatformTarget: x64`).
- **WinForms** custom-painted controls + `FontAwesome.Sharp 5.15.4`.
- Licensing: vendored **`Auth/LicenseAuth.cs`** + `Newtonsoft.Json 13.0.4`, `System.Net.Http`, `System.Management`, `System.Web`.
- Single-file: **`Costura.Fody 6.1.0` + `Fody 6.9.3`** via `FodyWeavers.xml` (`<Costura />`).
- Misc: `fuck1.cs` (`Compat.Clamp/Sqrt`), `jahid_icon.ico`, `App.config` (.NET 4.8 runtime).

## Requirements

- Windows 10/11 x64, .NET Framework 4.8 Runtime.
- Visual Studio 2022 (v18) with `.NET desktop development` + 4.8 targeting pack.
- Internet on first run (LicenseAuth `init` / login).

## Getting started

```powershell
git clone https://github.com/official-jahid/aimbot-ai-finder.git
cd aimbot-ai-finder
start "Collider Finder.sln"
```

Build `Release x64` in VS (single file via Costura), or via CLI:

```powershell
dotnet restore "Collider Finder.sln"
dotnet build "Collider Finder.sln" -c Release
```

Output: `Collider Finder/bin/x64/Release/net48/JahidColliderFinder.exe` (dependencies embedded, no sidecar DLLs).

## Usage

1. Run `JahidColliderFinder.exe`.
2. Pick **Username + Password** or **Licence Key** and log in.
3. Fill any card and press **Find / FIND READ** to get the derived hex.
4. Sidebar: Visual Settings (particles/grid/glow/clock), Profile (subscription/expiry/HWID), Logout.

## Project structure

```
aimbot-ai-finder/
├── Collider Finder.sln
├── LICENSE / README.md / .gitignore
└── Collider Finder/
    ├── Collider Finder.csproj  # net48, WinExe, x64, Costura, FontAwesome, Json
    ├── Program.cs              # Entry -> AuthChoiceForm
    ├── Form1.cs                # MainForm: particles, 3-card UI, OnCalculate
    ├── offset.cs               # Player_Data 0x48, WeaponRecoil 0x0C, WeaponOnHand 0x54, Camera 0x18
    ├── fuck1.cs                # Compat.Clamp / Sqrt
    ├── FodyWeavers.xml / App.config / jahid_icon.ico
    ├── Properties/             # AssemblyInfo (REGIX Studio, 1.0.0.0), Resources, Settings
    ├── Auth/                   # AuthSession, AuthChoiceForm, UserPass/Login forms, LicenseAuth.cs
    └── Views/                  # ThemedShell, VisualState, VisualSettingsForm, ProfileForm
```

## Configuration

- License credentials: `Collider Finder/Auth/AuthSession.cs` (`name`, `ownerid`, `secret`, `version = "1.0"`). Hardcoded now — move to user-secrets/env vars and rotate if leaked.
- Offsets/formulas: `offset.cs` + `OnCalculate()` in `Form1.cs`.
- Auto-update: version mismatch triggers `invalidver` dialog using `app_data.downloadLink`.
- Theme: `Form1.cs` palette + `Views/ThemedShell.cs` (`BG #0B0C10`, `Accent #8A94F8`).

## License

MIT — see [LICENSE](LICENSE). Copyright (c) 2026 Jahid Ekbal Mallick.

## Credits / Disclaimer

By **Jahid Ekbal Mallick / REGIX Studio**. Licensing SDK vendored in `Auth/LicenseAuth.cs`; icons via FontAwesome.Sharp. For educational / personal workflow use — comply with any game/platform terms and safeguard keys/HWID data.

