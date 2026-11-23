# v0.8.7
- Dedicated multiplayer: loot and corpse despawn run on the corpse owner again (usually the killer's client), not only on the headless `IsServer()` process. Fixes empty corpses (nothing on the ground) and bodies that never despawned with 0.8.6.
- Peers wait 5 seconds before claiming a corpse to despawn it, so machines do not steal ownership from each other.
- If a corpse is removed and loot never spawned, spawn it then. Items already on the ground are unchanged.

# v0.8.6
- Fix `DestroyDelay`: cancel vanilla ragdoll `DestroyNow` timer, run corpse removal only on server/authority, and measure full stay time from death (respects pack `DestroyDelay = 30`)

# v0.8.5
- Optional `DisableCorpseDespawnSound` and `DisableCorpseDespawnParticles` (both default false; vanilla despawn VFX/SFX stay on unless you turn these on)

# v0.8.4
- Rechecked against Valheim 1.0.21 (Steam build 26249040); Ragdoll.SpawnLoot, DestroyNow, Player.AutoPickup, and EffectList.Create still match
- Pickup range is applied on spawn and config change instead of every physics tick
- Loot window disables itself when idle so IMGUI OnGUI is not running every frame
- Cached SpawnLoot / ragdoll field lookups; skip config rewrite on plugin load
- Dedicated / batch-mode servers skip the loot HUD component

# v0.8.3
- Verified against Valheim 1.0.21 (Release build, Steam build 26249040).
- BepInEx dependency set to `denikson-BepInExPack_Valheim-5.4.2360`.

# v0.8.2
- Loot window borders are off by default; enable ShowBorders if you want them
- Text outline plus configurable name/count/outline colors for readability
- Amount column grows with font size and window width so triple-digit stacks are not clipped
- Ragdoll cleanup retries ownership so leftover bodies are less likely to persist
- Console command `imd_cleanup` force-removes leftover corpses

# v0.8.1
- AutoPickupRange default is now 2.5 meters

# v0.8.0
- InstantMonsterLootDrop is now a loot-flow overhaul, not only an instant corpse poof
- DropDelay default 0.05, DestroyDelay default 60, AutoPickupDelay default 1, AutoPickupRange 3.5m
- Stacked loot notification window (vanilla one-at-a-time messages disabled while it is on)
- Notification size, opacity, linger, font size, corner anchor, and on/off are all configurable

# v0.7.2
- Version bump to republish on Thunderstore and Hexium (same as 0.7.1)

# v0.7.1
- Hold auto-pickup for 2 seconds on instant monster loot so drops stay visible on the ground
- New config option AutoPickupDelay (set to 0 for vanilla auto-pickup)

# v0.7.0
- Updated for Valheim 1.0 (Unity 6)
- Verified in-game on the live 1.0 client
- Build now references Unity assemblies from valheim_Data/Managed (unstripped_corlib / TextCoreModule are gone)
- BepInEx dependency updated to denikson-BepInExPack_Valheim-5.4.2350
- Ragdoll.SpawnLoot is still private; keep calling it via Harmony Traverse
- EffectList.Create now takes a ZDOID; pass ZDOID.None

# v0.6.0
- Removed terminal reload
- Added compatibility for v0.217.46

# 0.5.3
- Fixed dependency issue with Assembly file

# 0.5.2
- No changes. Just updated depedency string denikson-BepInExPack_Valheim-5.4.2202

# 0.5.0
- Updated for Mistlands
