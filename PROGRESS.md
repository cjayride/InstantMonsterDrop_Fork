# InstantMonsterLootDrop — Valheim 1.0.21 release

## Goal
- Verify build against Valheim 1.0.21 (Steam build 26249040).
- Bump BepInEx dependency to `denikson-BepInExPack_Valheim-5.4.2360`.
- Release v0.8.3 on GitHub and package Thunderstore zip in `/dist`.

## Status
Completed.

## Changes
- `manifest.json` — version 0.8.3, BepInEx 5.4.2360
- `CHANGELOG.md` — v0.8.3 entry
- `InstantMonsterDrop.csproj` / `BepInExPlugin.cs` — 0.8.3
- `README.md` — documentation refresh (included in release)

## Build
```
dotnet build InstantMonsterDrop.sln -c Release
```
Succeeded (0 warnings, 0 errors).

**DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\InstantMonsterDrop\bin\Release\net480\InstantMonsterDrop.dll`

**Thunderstore zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\dist\cjayride-InstantMonsterLootDrop-0.8.3.zip`

## Valheim
- Install build ID: 26249040 (1.0.21)
