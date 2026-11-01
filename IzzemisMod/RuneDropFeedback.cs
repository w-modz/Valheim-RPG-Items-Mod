using System;
using System.Linq;
using HarmonyLib;
using UnityEngine;

namespace Runecarved
{
    internal static class RuneDropFeedback
    {
        private const string RunePrefabPrefix = "IzzemisMod_Rune";
        private const double FreshDropWindowSeconds = 5d;
        private static bool missingWardSoundWarningLogged;

        public static void AddGlow(GameObject prefab, Color color)
        {
            if (prefab == null)
            {
                return;
            }

            foreach (Renderer renderer in prefab.GetComponentsInChildren<Renderer>(true))
            {
                foreach (Material material in renderer.materials)
                {
                    if (material.HasProperty("_EmissionColor"))
                    {
                        material.EnableKeyword("_EMISSION");
                        material.SetColor("_EmissionColor", color * 2f);
                    }
                }
            }

            GameObject glow = new GameObject("RunecarvedRuneGlow");
            glow.transform.SetParent(prefab.transform, false);
            glow.transform.localPosition = Vector3.up * 0.5f;

            Light light = glow.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = color;
            light.intensity = 5f;
            light.range = 4f;
            light.shadows = LightShadows.None;
        }

        public static bool IsFreshRuneDrop(ItemDrop item)
        {
            if (item == null ||
                item.m_itemData == null ||
                item.m_itemData.m_dropPrefab == null ||
                !item.m_itemData.m_dropPrefab.name.StartsWith(RunePrefabPrefix, StringComparison.Ordinal))
            {
                return false;
            }

            ZNetView netView = item.GetComponent<ZNetView>();
            if (netView == null || !netView.IsValid() || ZNet.instance == null)
            {
                return false;
            }

            long spawnTime = netView.GetZDO().GetLong(ZDOVars.s_spawnTime, 0L);
            if (spawnTime <= 0L)
            {
                return false;
            }

            double age = (ZNet.instance.GetTime() - new DateTime(spawnTime)).TotalSeconds;
            if (age < 0d || age > FreshDropWindowSeconds)
            {
                return false;
            }

            PlayWardSound(item.transform.position);
            return true;
        }

        private static void PlayWardSound(Vector3 position)
        {
            GameObject wardPrefab = ZNetScene.instance != null
                ? ZNetScene.instance.GetPrefab("guard_stone")
                : null;
            PrivateArea ward = wardPrefab != null ? wardPrefab.GetComponent<PrivateArea>() : null;
            EffectList.EffectData soundEffect = ward?.m_activateEffect?.m_effectPrefabs?
                .FirstOrDefault(effect => effect != null &&
                    effect.m_enabled &&
                    effect.m_prefab != null &&
                    effect.m_prefab.GetComponentInChildren<ZSFX>(true) != null);

            if (soundEffect == null)
            {
                if (!missingWardSoundWarningLogged)
                {
                    Jotunn.Logger.LogWarning("Could not find a ward activation sound for rune drop feedback.");
                    missingWardSoundWarningLogged = true;
                }

                return;
            }

            GameObject soundInstance = UnityEngine.Object.Instantiate(
                soundEffect.m_prefab,
                position,
                Quaternion.identity);
            ZSFX[] sounds = soundInstance.GetComponentsInChildren<ZSFX>(true);
            if (sounds.Length == 0)
            {
                Jotunn.Logger.LogWarning("The ward activation effect did not contain a sound component.");
                UnityEngine.Object.Destroy(soundInstance);
                return;
            }

            DisableVisualEffects(soundInstance);
            float lifetime = 1f;
            foreach (ZSFX sound in sounds)
            {
                sound.m_playOnAwake = false;
                foreach (AudioClip clip in sound.m_audioClips)
                {
                    if (clip != null)
                    {
                        lifetime = Mathf.Max(lifetime, clip.length + 0.5f);
                    }
                }

                sound.Play();
            }

            UnityEngine.Object.Destroy(soundInstance, lifetime);
        }

        private static void DisableVisualEffects(GameObject effect)
        {
            foreach (Renderer renderer in effect.GetComponentsInChildren<Renderer>(true))
            {
                renderer.enabled = false;
            }

            foreach (ParticleSystem particles in effect.GetComponentsInChildren<ParticleSystem>(true))
            {
                particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                ParticleSystem.MainModule main = particles.main;
                main.playOnAwake = false;
            }

            foreach (Light light in effect.GetComponentsInChildren<Light>(true))
            {
                light.enabled = false;
            }

            foreach (Animator animator in effect.GetComponentsInChildren<Animator>(true))
            {
                animator.enabled = false;
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop), "Start")]
    internal static class RuneDropSoundPatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            if (__instance != null &&
                __instance.m_itemData != null &&
                __instance.m_itemData.m_dropPrefab != null &&
                RuneItems.TryGetRuneColor(__instance.m_itemData.m_dropPrefab.name, out Color color))
            {
                RuneLootBeam beam = __instance.GetComponent<RuneLootBeam>();
                if (beam == null)
                {
                    beam = __instance.gameObject.AddComponent<RuneLootBeam>();
                }

                beam.Initialize(color);
            }

            RuneDropFeedback.IsFreshRuneDrop(__instance);
        }
    }
}
