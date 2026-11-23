using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace InstantMonsterDrop
{
    public enum NotificationAnchor
    {
        BottomRight,
        BottomLeft,
        TopRight,
        TopLeft
    }

    [BepInPlugin("cjayride.InstantMonsterDrop", "Instant Monster Drop", "0.8.7")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        internal const string LootedKey = "IMD_Looted";

        private static readonly MethodInfo spawnLootMethod = AccessTools.Method(typeof(Ragdoll), "SpawnLoot", new[] { typeof(Vector3) });
        private static readonly FieldInfo removeEffectField = AccessTools.Field(typeof(Ragdoll), "m_removeEffect");
        private static readonly object[] spawnLootArgs = new object[1];

        private static BepInExPlugin context;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<float> dropDelay;
        private static ConfigEntry<float> destroyDelay;
        private static ConfigEntry<float> autoPickupDelay;
        private static ConfigEntry<float> autoPickupRange;
        private static ConfigEntry<bool> disableDespawnSound;
        private static ConfigEntry<bool> disableDespawnParticles;
        private static ConfigEntry<bool> notificationEnabled;
        private static ConfigEntry<int> notificationX;
        private static ConfigEntry<int> notificationY;
        private static ConfigEntry<int> notificationWidth;
        private static ConfigEntry<int> notificationHeight;
        private static ConfigEntry<float> notificationOpacity;
        private static ConfigEntry<float> notificationLinger;
        private static ConfigEntry<int> notificationFontSize;
        private static ConfigEntry<NotificationAnchor> notificationAnchor;
        private static ConfigEntry<bool> notificationShowBorders;
        private static ConfigEntry<bool> notificationTextOutline;
        private static ConfigEntry<Color> notificationTextColor;
        private static ConfigEntry<Color> notificationAmountColor;
        private static ConfigEntry<Color> notificationOutlineColor;
        private static bool spawningLoot;
        private static bool commandsRegistered;
        private static readonly HashSet<int> scheduledRagdolls = new HashSet<int>();

        internal static bool ModEnabled;
        internal static bool NotificationEnabled = true;
        internal static int NotificationX = 20;
        internal static int NotificationY = 20;
        internal static int NotificationWidth = 250;
        internal static int NotificationHeight = 120;
        internal static float NotificationOpacity = 0.72f;
        internal static float NotificationLinger = 8f;
        internal static int NotificationFontSize = 15;
        internal static NotificationAnchor Anchor = NotificationAnchor.BottomRight;
        internal static bool ShowBorders;
        internal static bool TextOutline = true;
        internal static Color TextColor = Color.white;
        internal static Color AmountColor = new Color(0.95f, 0.9f, 0.75f);
        internal static Color OutlineColor = Color.black;
        internal static bool DisableDespawnSound;
        internal static bool DisableDespawnParticles;
        internal static float DestroyDelaySeconds = 60f;

        private const float OwnershipClaimTimeout = 15f;
        private const float OwnerGracePeriod = 5f;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dropDelay = Config.Bind<float>("General", "DropDelay", 0.05f, "Delay before dropping loot");
            destroyDelay = Config.Bind<float>("General", "DestroyDelay", 60f, "Seconds the ragdoll stays on the ground before it is destroyed");
            autoPickupDelay = Config.Bind<float>("General", "AutoPickupDelay", 1f, "Seconds before dropped monster loot can be auto-picked up. 0 is vanilla auto-pickup timing");
            autoPickupRange = Config.Bind<float>("General", "AutoPickupRange", 2.5f, "Auto-pickup radius in meters");
            disableDespawnSound = Config.Bind<bool>("General", "DisableCorpseDespawnSound", false, "If true, mute the corpse despawn sound. Off by default.");
            disableDespawnParticles = Config.Bind<bool>("General", "DisableCorpseDespawnParticles", false, "If true, hide the corpse despawn particles. Off by default.");

            notificationEnabled = Config.Bind<bool>("Notification", "Enabled", true, "Show the stacked loot window. Disable to restore vanilla one-at-a-time pickup messages");
            notificationAnchor = Config.Bind<NotificationAnchor>("Notification", "Anchor", NotificationAnchor.BottomRight, "Corner of the screen the window is offset from");
            notificationX = Config.Bind<int>("Notification", "WindowX", 20, "Horizontal offset from the chosen corner, in pixels");
            notificationY = Config.Bind<int>("Notification", "WindowY", 20, "Vertical offset from the chosen corner, in pixels");
            notificationWidth = Config.Bind<int>("Notification", "WindowWidth", 250, "Loot window width in pixels");
            notificationHeight = Config.Bind<int>("Notification", "WindowHeight", 120, "Loot window height in pixels");
            notificationOpacity = Config.Bind<float>("Notification", "Opacity", 0.72f, "Window background opacity from 0 (invisible) to 1 (solid)");
            notificationLinger = Config.Bind<float>("Notification", "Linger", 8f, "Seconds each pickup line stays in the window");
            notificationFontSize = Config.Bind<int>("Notification", "FontSize", 15, "Font size for pickup names and amounts");
            notificationShowBorders = Config.Bind<bool>("Notification", "ShowBorders", false, "Draw the gold side bar and top line on the loot window");
            notificationTextOutline = Config.Bind<bool>("Notification", "TextOutline", true, "Draw a dark stroke around loot text so it stays readable");
            notificationTextColor = Config.Bind<Color>("Notification", "TextColor", Color.white, "Item name color");
            notificationAmountColor = Config.Bind<Color>("Notification", "AmountColor", new Color(0.95f, 0.9f, 0.75f), "Item count color");
            notificationOutlineColor = Config.Bind<Color>("Notification", "OutlineColor", Color.black, "Text outline color");

            CacheSettings();
            modEnabled.SettingChanged += OnSettingsChanged;
            dropDelay.SettingChanged += OnSettingsChanged;
            destroyDelay.SettingChanged += OnSettingsChanged;
            autoPickupRange.SettingChanged += OnPickupRangeChanged;
            disableDespawnSound.SettingChanged += OnSettingsChanged;
            disableDespawnParticles.SettingChanged += OnSettingsChanged;
            notificationEnabled.SettingChanged += OnSettingsChanged;
            notificationAnchor.SettingChanged += OnSettingsChanged;
            notificationX.SettingChanged += OnSettingsChanged;
            notificationY.SettingChanged += OnSettingsChanged;
            notificationWidth.SettingChanged += OnSettingsChanged;
            notificationHeight.SettingChanged += OnSettingsChanged;
            notificationOpacity.SettingChanged += OnSettingsChanged;
            notificationLinger.SettingChanged += OnSettingsChanged;
            notificationFontSize.SettingChanged += OnSettingsChanged;
            notificationShowBorders.SettingChanged += OnSettingsChanged;
            notificationTextOutline.SettingChanged += OnSettingsChanged;
            notificationTextColor.SettingChanged += OnSettingsChanged;
            notificationAmountColor.SettingChanged += OnSettingsChanged;
            notificationOutlineColor.SettingChanged += OnSettingsChanged;

            if (!modEnabled.Value)
                return;

            if (!Application.isBatchMode)
                gameObject.AddComponent<PickupFeed>();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        private static void OnSettingsChanged(object sender, System.EventArgs e)
        {
            CacheSettings();
        }

        private static void OnPickupRangeChanged(object sender, System.EventArgs e)
        {
            ApplyPickupRange();
        }

        private static void CacheSettings()
        {
            ModEnabled = modEnabled != null && modEnabled.Value;
            NotificationEnabled = notificationEnabled == null || notificationEnabled.Value;
            NotificationX = notificationX != null ? notificationX.Value : 20;
            NotificationY = notificationY != null ? notificationY.Value : 20;
            NotificationWidth = notificationWidth != null ? Mathf.Max(80, notificationWidth.Value) : 250;
            NotificationHeight = notificationHeight != null ? Mathf.Max(40, notificationHeight.Value) : 120;
            NotificationOpacity = notificationOpacity != null ? Mathf.Clamp01(notificationOpacity.Value) : 0.72f;
            NotificationLinger = notificationLinger != null ? Mathf.Max(0.5f, notificationLinger.Value) : 8f;
            NotificationFontSize = notificationFontSize != null ? Mathf.Clamp(notificationFontSize.Value, 8, 48) : 15;
            Anchor = notificationAnchor != null ? notificationAnchor.Value : NotificationAnchor.BottomRight;
            ShowBorders = notificationShowBorders != null && notificationShowBorders.Value;
            TextOutline = notificationTextOutline == null || notificationTextOutline.Value;
            TextColor = notificationTextColor != null ? notificationTextColor.Value : Color.white;
            AmountColor = notificationAmountColor != null ? notificationAmountColor.Value : new Color(0.95f, 0.9f, 0.75f);
            OutlineColor = notificationOutlineColor != null ? notificationOutlineColor.Value : Color.black;
            DisableDespawnSound = disableDespawnSound != null && disableDespawnSound.Value;
            DisableDespawnParticles = disableDespawnParticles != null && disableDespawnParticles.Value;
            DestroyDelaySeconds = destroyDelay != null ? Mathf.Max(0f, destroyDelay.Value) : 60f;
        }

        private static void ApplyPickupRange()
        {
            Player player = Player.m_localPlayer;
            if (player == null || !ModEnabled)
                return;
            player.m_autoPickupRange = autoPickupRange.Value;
        }

        [HarmonyPatch(typeof(Terminal), "InitTerminal")]
        static class Terminal_InitTerminal_Patch
        {
            static void Postfix()
            {
                if (commandsRegistered)
                    return;
                commandsRegistered = true;
                new Terminal.ConsoleCommand("imd_cleanup", "Remove leftover InstantMonsterDrop ragdolls", args =>
                {
                    int n = ForceCleanupRagdolls();
                    if (Console.instance != null)
                        Console.instance.Print("InstantMonsterDrop removed " + n + " ragdolls.");
                    if (Player.m_localPlayer != null)
                        Player.m_localPlayer.Message(MessageHud.MessageType.Center, "Removed " + n + " leftover corpses");
                });
            }
        }

        [HarmonyPatch(typeof(Ragdoll), "Awake")]
        static class Ragdoll_Awake_Patch
        {
            static void Postfix(Ragdoll __instance, ZNetView ___m_nview, EffectList ___m_removeEffect)
            {
                if (!ZNetScene.instance || !ModEnabled)
                    return;
                __instance.CancelInvoke("DestroyNow");
                ScheduleRagdoll(__instance, ___m_nview, ___m_removeEffect);
            }
        }

        [HarmonyPatch(typeof(Ragdoll), "DestroyNow")]
        static class Ragdoll_DestroyNow_Patch
        {
            static bool Prefix()
            {
                return !ModEnabled;
            }
        }

        [HarmonyPatch(typeof(ItemDrop), "Awake")]
        static class ItemDrop_Awake_Patch
        {
            static void Postfix(ItemDrop __instance)
            {
                if (!spawningLoot || autoPickupDelay.Value <= 0f || !__instance.m_autoPickup)
                    return;

                __instance.m_autoPickup = false;
                context.StartCoroutine(EnableAutoPickupLater(__instance, autoPickupDelay.Value));
            }
        }

        [HarmonyPatch(typeof(Player), "OnSpawned")]
        static class Player_OnSpawned_Patch
        {
            static void Postfix(Player __instance)
            {
                if (!ModEnabled || __instance != Player.m_localPlayer)
                    return;
                ApplyPickupRange();
                context.StartCoroutine(SweepLoadedRagdolls());
            }
        }

        [HarmonyPatch(typeof(Character), "ShowPickupMessage")]
        static class Character_ShowPickupMessage_Patch
        {
            static bool Prefix(Character __instance, ItemDrop.ItemData item, int amount)
            {
                if (!ModEnabled || !NotificationEnabled || __instance != Player.m_localPlayer)
                    return true;
                if (PickupFeed.Instance != null)
                    PickupFeed.Instance.Add(item, amount);
                return false;
            }
        }

        internal static int ForceCleanupRagdolls()
        {
            int count = 0;
            Ragdoll[] ragdolls = FindObjectsByType<Ragdoll>(FindObjectsSortMode.None);
            for (int i = 0; i < ragdolls.Length; i++)
            {
                Ragdoll ragdoll = ragdolls[i];
                if (ragdoll == null)
                    continue;
                ZNetView nview = ragdoll.GetComponent<ZNetView>();
                if (nview != null && nview.IsValid() && !nview.IsOwner())
                    nview.ClaimOwnership();
                if (ZNetScene.instance)
                {
                    ZNetScene.instance.Destroy(ragdoll.gameObject);
                    count++;
                }
            }
            return count;
        }

        private static void ScheduleRagdoll(Ragdoll ragdoll, ZNetView nview, EffectList removeEffect)
        {
            if (ragdoll == null)
                return;
            int id = ragdoll.GetInstanceID();
            if (!scheduledRagdolls.Add(id))
                return;
            context.StartCoroutine(DropNow(ragdoll, nview, removeEffect, id));
        }

        private static IEnumerator SweepLoadedRagdolls()
        {
            yield return new WaitForSeconds(2f);
            if (!ModEnabled)
                yield break;
            Ragdoll[] ragdolls = FindObjectsByType<Ragdoll>(FindObjectsSortMode.None);
            for (int i = 0; i < ragdolls.Length; i++)
            {
                Ragdoll ragdoll = ragdolls[i];
                if (ragdoll == null)
                    continue;
                ZNetView nview = ragdoll.GetComponent<ZNetView>();
                EffectList removeEffect = removeEffectField != null ? removeEffectField.GetValue(ragdoll) as EffectList : null;
                ScheduleRagdoll(ragdoll, nview, removeEffect);
            }
        }

        private static IEnumerator DropNow(Ragdoll ragdoll, ZNetView nview, EffectList removeEffect, int id)
        {
            float destroyAt = Time.time + DestroyDelaySeconds;
            if (dropDelay.Value >= 0)
                yield return new WaitForSeconds(dropDelay.Value);

            if (!ModEnabled)
            {
                scheduledRagdolls.Remove(id);
                yield break;
            }

            TrySpawnLoot(ragdoll, nview);

            float waitRemaining = Mathf.Max(0f, destroyAt - Time.time);
            if (waitRemaining > 0f)
                yield return new WaitForSeconds(waitRemaining);

            yield return context.StartCoroutine(DestroyNow(ragdoll, nview, removeEffect));
            scheduledRagdolls.Remove(id);
        }

        private static IEnumerator DestroyNow(Ragdoll ragdoll, ZNetView nview, EffectList m_removeEffect)
        {
            float waited = 0f;
            while (ModEnabled && ragdoll != null && nview != null && nview.IsValid() && waited < OwnershipClaimTimeout)
            {
                if (!nview.IsOwner() && waited >= OwnerGracePeriod)
                    nview.ClaimOwnership();

                if (nview.IsOwner())
                {
                    TrySpawnLoot(ragdoll, nview);
                    Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
                    PlayRemoveEffect(m_removeEffect, averageBodyPosition);
                    if (ZNetScene.instance)
                        ZNetScene.instance.Destroy(ragdoll.gameObject);
                    yield break;
                }

                yield return new WaitForSeconds(0.5f);
                waited += 0.5f;
            }
        }

        private static void PlayRemoveEffect(EffectList effect, Vector3 position)
        {
            if (effect == null)
                return;
            if (DisableDespawnSound && DisableDespawnParticles)
                return;

            GameObject[] created = effect.Create(position, Quaternion.identity, null, 1f, -1, ZDOID.None);
            if (created == null || (!DisableDespawnSound && !DisableDespawnParticles))
                return;

            for (int i = 0; i < created.Length; i++)
            {
                GameObject go = created[i];
                if (go == null)
                    continue;
                if (DisableDespawnSound)
                    Silence(go);
                if (DisableDespawnParticles)
                    StripParticles(go);
            }
        }

        private static void Silence(GameObject go)
        {
            AudioSource[] sources = go.GetComponentsInChildren<AudioSource>(true);
            for (int i = 0; i < sources.Length; i++)
            {
                AudioSource source = sources[i];
                source.Stop();
                source.mute = true;
                source.volume = 0f;
                source.enabled = false;
            }

            ZSFX[] sfx = go.GetComponentsInChildren<ZSFX>(true);
            for (int i = 0; i < sfx.Length; i++)
                sfx[i].enabled = false;
        }

        private static void StripParticles(GameObject go)
        {
            ParticleSystem[] systems = go.GetComponentsInChildren<ParticleSystem>(true);
            for (int i = 0; i < systems.Length; i++)
            {
                ParticleSystem ps = systems[i];
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystemRenderer renderer = ps.GetComponent<ParticleSystemRenderer>();
                if (renderer != null)
                    renderer.enabled = false;
            }
        }

        private static void TrySpawnLoot(Ragdoll ragdoll, ZNetView nview)
        {
            if (ragdoll == null || nview == null || !nview.IsValid() || !nview.IsOwner() || WasLooted(nview) || spawnLootMethod == null)
                return;

            Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
            spawningLoot = true;
            try
            {
                spawnLootArgs[0] = averageBodyPosition;
                spawnLootMethod.Invoke(ragdoll, spawnLootArgs);
                MarkLooted(nview);
            }
            finally
            {
                spawningLoot = false;
                spawnLootArgs[0] = null;
            }
        }

        private static bool WasLooted(ZNetView nview)
        {
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            return zdo != null && zdo.GetInt(LootedKey, 0) == 1;
        }

        private static void MarkLooted(ZNetView nview)
        {
            ZDO zdo = nview != null ? nview.GetZDO() : null;
            if (zdo != null)
                zdo.Set(LootedKey, 1);
        }

        private static IEnumerator EnableAutoPickupLater(ItemDrop drop, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (drop != null)
                drop.m_autoPickup = true;
        }
    }
}
