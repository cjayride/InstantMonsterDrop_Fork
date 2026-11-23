# InstantMonsterLootDrop — 0.8.7 dedicated loot/despawn

## Goal
Fix 0.8.6 dedicated bug without adding per-frame work.

## Status
Completed (compile verified). Not git committed.

## Approach
- Schedule drop/despawn wait on each peer that loads the ragdoll (same as 0.8.4). Idle WaitForSeconds only.
- SpawnLoot / Destroy only if ZDO owner.
- 5s grace before ClaimOwnership.
- TrySpawnLoot again at despawn if loot never spawned.

## FPS
No new FixedUpdate/Update patches. HUD still idle-disabled.

## Build
`dotnet build InstantMonsterDrop.sln -c Release` succeeded (0 warnings).

**DLL:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\InstantMonsterDrop\bin\Release\net480\InstantMonsterDrop.dll` (FileVersion 0.8.7)

**Zip:** `D:\AI-Agent-Projects\cjaycraft\Cjayride.InstantMonsterDrop\dist\cjayride-InstantMonsterLootDrop-0.8.7.zip`
