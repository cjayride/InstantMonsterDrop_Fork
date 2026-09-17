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
