using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
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

    [BepInPlugin("cjayride.InstantMonsterDrop", "Instant Monster Drop", "0.8.0")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<float> dropDelay;
        private static ConfigEntry<float> destroyDelay;
        private static ConfigEntry<float> autoPickupDelay;
        private static ConfigEntry<float> autoPickupRange;
        private static ConfigEntry<bool> notificationEnabled;
        private static ConfigEntry<int> notificationX;
        private static ConfigEntry<int> notificationY;
        private static ConfigEntry<int> notificationWidth;
        private static ConfigEntry<int> notificationHeight;
        private static ConfigEntry<float> notificationOpacity;
        private static ConfigEntry<float> notificationLinger;
        private static ConfigEntry<int> notificationFontSize;
        private static ConfigEntry<NotificationAnchor> notificationAnchor;
        private static bool spawningLoot;

        internal static bool ModEnabled => modEnabled != null && modEnabled.Value;
        internal static bool NotificationEnabled => notificationEnabled == null || notificationEnabled.Value;
        internal static int NotificationX => notificationX != null ? notificationX.Value : 20;
        internal static int NotificationY => notificationY != null ? notificationY.Value : 20;
        internal static int NotificationWidth => notificationWidth != null ? Mathf.Max(80, notificationWidth.Value) : 250;
        internal static int NotificationHeight => notificationHeight != null ? Mathf.Max(40, notificationHeight.Value) : 120;
        internal static float NotificationOpacity => notificationOpacity != null ? Mathf.Clamp01(notificationOpacity.Value) : 0.72f;
        internal static float NotificationLinger => notificationLinger != null ? Mathf.Max(0.5f, notificationLinger.Value) : 8f;
        internal static int NotificationFontSize => notificationFontSize != null ? Mathf.Clamp(notificationFontSize.Value, 8, 48) : 15;
        internal static NotificationAnchor Anchor => notificationAnchor != null ? notificationAnchor.Value : NotificationAnchor.BottomRight;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dropDelay = Config.Bind<float>("General", "DropDelay", 0.05f, "Delay before dropping loot");
            destroyDelay = Config.Bind<float>("General", "DestroyDelay", 60f, "Seconds the ragdoll stays on the ground before it is destroyed");
            autoPickupDelay = Config.Bind<float>("General", "AutoPickupDelay", 1f, "Seconds before dropped monster loot can be auto-picked up. 0 is vanilla auto-pickup timing");
            autoPickupRange = Config.Bind<float>("General", "AutoPickupRange", 3.5f, "Auto-pickup radius in meters");

            notificationEnabled = Config.Bind<bool>("Notification", "Enabled", true, "Show the stacked loot window. Disable to restore vanilla one-at-a-time pickup messages");
            notificationAnchor = Config.Bind<NotificationAnchor>("Notification", "Anchor", NotificationAnchor.BottomRight, "Corner of the screen the window is offset from");
            notificationX = Config.Bind<int>("Notification", "WindowX", 20, "Horizontal offset from the chosen corner, in pixels");
            notificationY = Config.Bind<int>("Notification", "WindowY", 20, "Vertical offset from the chosen corner, in pixels");
            notificationWidth = Config.Bind<int>("Notification", "WindowWidth", 250, "Loot window width in pixels");
            notificationHeight = Config.Bind<int>("Notification", "WindowHeight", 120, "Loot window height in pixels");
            notificationOpacity = Config.Bind<float>("Notification", "Opacity", 0.72f, "Window background opacity from 0 (invisible) to 1 (solid)");
            notificationLinger = Config.Bind<float>("Notification", "Linger", 8f, "Seconds each pickup line stays in the window");
            notificationFontSize = Config.Bind<int>("Notification", "FontSize", 15, "Font size for pickup names and amounts");
            Config.Save();
            if (!modEnabled.Value)
                return;

            gameObject.AddComponent<PickupFeed>();
            Harmony.CreateAndPatchAll(Assembly.GetExecutingAssembly(), null);
        }

        [HarmonyPatch(typeof(Ragdoll), "Awake")]
        static class Ragdoll_Awake_Patch
        {
            static void Postfix(Ragdoll __instance, ZNetView ___m_nview, EffectList ___m_removeEffect)
            {
                if (!ZNetScene.instance)
                    return;
                context.StartCoroutine(DropNow(__instance, ___m_nview, ___m_removeEffect));
            }
        }

        [HarmonyPatch(typeof(Ragdoll), "DestroyNow")]
        static class Ragdoll_DestroyNow_Patch
        {
            static bool Prefix()
            {
                return !modEnabled.Value;
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

        [HarmonyPatch(typeof(Player), "AutoPickup")]
        static class Player_AutoPickup_Patch
        {
            static void Prefix(Player __instance)
            {
                if (!modEnabled.Value || __instance != Player.m_localPlayer)
                    return;
                __instance.m_autoPickupRange = autoPickupRange.Value;
            }
        }

        [HarmonyPatch(typeof(Character), "ShowPickupMessage")]
        static class Character_ShowPickupMessage_Patch
        {
            static bool Prefix(Character __instance, ItemDrop.ItemData item, int amount)
            {
                if (!modEnabled.Value || !NotificationEnabled || __instance != Player.m_localPlayer)
                    return true;
                if (PickupFeed.Instance != null)
                    PickupFeed.Instance.Add(item, amount);
                return false;
            }
        }

        private static IEnumerator DropNow(Ragdoll ragdoll, ZNetView nview, EffectList removeEffect)
        {
            if (dropDelay.Value < 0)
            {
                context.StartCoroutine(DestroyNow(ragdoll, nview, removeEffect));
                yield break;
            }

            yield return new WaitForSeconds(dropDelay.Value);

            if (!modEnabled.Value)
                yield break;

            if (ragdoll == null || nview == null || !nview.IsValid() || !nview.IsOwner())
                yield break;

            Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
            spawningLoot = true;
            try
            {
                Traverse.Create(ragdoll).Method("SpawnLoot", new object[] { averageBodyPosition }).GetValue();
            }
            finally
            {
                spawningLoot = false;
            }
            context.StartCoroutine(DestroyNow(ragdoll, nview, removeEffect));
        }

        private static IEnumerator DestroyNow(Ragdoll ragdoll, ZNetView nview, EffectList m_removeEffect)
        {
            yield return new WaitForSeconds(Mathf.Max(destroyDelay.Value - dropDelay.Value, 0));

            if (!modEnabled.Value)
                yield break;

            if (ragdoll == null || nview == null || !nview.IsValid() || !nview.IsOwner())
                yield break;

            Vector3 averageBodyPosition = ragdoll.GetAverageBodyPosition();
            if (m_removeEffect != null)
                m_removeEffect.Create(averageBodyPosition, Quaternion.identity, null, 1f, -1, ZDOID.None);

            if (ZNetScene.instance)
                ZNetScene.instance.Destroy(ragdoll.gameObject);
        }

        private static IEnumerator EnableAutoPickupLater(ItemDrop drop, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (drop != null)
                drop.m_autoPickup = true;
        }
    }
}
