# InstantMonsterLootDrop — 0.8.4 performance pass

## Goal
- Confirm compatibility with latest Valheim.
- Improve hot-path cost: FPS, lag, load time.

## Status
Completed.

## Valheim
- Latest public patch is still **1.0.21** (Oct 28, 2026).
- Local Steam install buildid **26249040** matches.
- Harmony targets still valid: `Ragdoll.Awake`, `Ragdoll.DestroyNow`, `Ragdoll.SpawnLoot(Vector3)`, `ItemDrop.Awake`, `Player.OnSpawned`, `Character.ShowPickupMessage`, `EffectList.Create(..., ZDOID)`.

## Changes
- Removed per-`FixedUpdate` `Player.AutoPickup` prefix (was rewriting pickup range every physics tick).
- Pickup HUD component starts disabled; IMGUI `OnGUI` only runs while lines are visible.
- Cached `SpawnLoot` MethodInfo / `m_removeEffect` FieldInfo instead of Harmony Traverse per corpse.
- Cached notification config in static fields; no `Config.Save()` on every plugin load.
- Skip HUD component in batch-mode / dedicated.

## Build
`dotnet build InstantMonsterDrop.sln -c Release` — succeeded.

**DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\InstantMonsterDrop\bin\Release\net480\InstantMonsterDrop.dll`

**Zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\dist\cjayride-InstantMonsterLootDrop-0.8.4.zip`
