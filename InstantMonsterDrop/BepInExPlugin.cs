using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace InstantMonsterDrop
{
    [BepInPlugin("cjayride.InstantMonsterDrop", "Instant Monster Drop", "0.7.1")]
    public class BepInExPlugin : BaseUnityPlugin
    {
        private static BepInExPlugin context;
        private static ConfigEntry<bool> modEnabled;
        private static ConfigEntry<float> dropDelay;
        private static ConfigEntry<float> destroyDelay;
        private static ConfigEntry<float> autoPickupDelay;
        private static bool spawningLoot;

        private void Awake()
        {
            context = this;
            modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            dropDelay = Config.Bind<float>("General", "DropDelay", 0.01f, "Delay before dropping loot");
            destroyDelay = Config.Bind<float>("General", "DestroyDelay", 0.05f, "Delay before destroying ragdoll");
            autoPickupDelay = Config.Bind<float>("General", "AutoPickupDelay", 2f, "Seconds before dropped monster loot can be auto-picked up. Set to 0 for vanilla auto-pickup.");
            Config.Save();
            if (!modEnabled.Value)
                return;

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
