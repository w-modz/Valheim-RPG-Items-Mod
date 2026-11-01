using System.Collections.Generic;
using UnityEngine;

namespace Runecarved
{
    // Equipped items are visual instances created by VisEquipment (sometimes a frame or more
    // after equipping), so emitters are synced periodically instead of at equip time.
    internal sealed class EquippedRarityParticles : MonoBehaviour
    {
        private const float SyncInterval = 0.5f;

        private readonly List<GameObject> instances = new List<GameObject>();
        private Humanoid humanoid;
        private float nextSync;

        public static void Ensure(Humanoid owner)
        {
            if (owner != null && owner.GetComponent<EquippedRarityParticles>() == null)
            {
                owner.gameObject.AddComponent<EquippedRarityParticles>().humanoid = owner;
            }
        }

        private void Update()
        {
            if (humanoid == null || Time.time < nextSync)
            {
                return;
            }

            nextSync = Time.time + SyncInterval;
            Sync();
        }

        private void Sync()
        {
            VisEquipment vis = humanoid.m_visEquipment;
            if (vis == null)
            {
                return;
            }

            SyncSlot(humanoid.m_rightItem, vis.m_rightItemInstance, vis.m_rightBackItemInstance);
            SyncSlot(humanoid.m_leftItem, vis.m_leftItemInstance, vis.m_leftBackItemInstance);
            SyncSlot(humanoid.m_helmetItem, vis.m_helmetItemInstance);
            SyncSlot(humanoid.m_chestItem, vis.m_chestItemInstances);
            SyncSlot(humanoid.m_legItem, vis.m_legItemInstances);
            SyncSlot(humanoid.m_shoulderItem, vis.m_shoulderItemInstances);
            SyncSlot(humanoid.m_utilityItem, vis.m_utilityItemInstances);
            SyncSlot(humanoid.m_trinketItem, vis.m_trinketItemInstances);
        }

        private void SyncSlot(ItemDrop.ItemData item, params GameObject[] slotInstances)
        {
            instances.Clear();
            foreach (GameObject instance in slotInstances)
            {
                if (instance != null)
                {
                    instances.Add(instance);
                }
            }

            Apply(item);
        }

        private void SyncSlot(ItemDrop.ItemData item, List<GameObject> slotInstances)
        {
            instances.Clear();
            if (slotInstances != null)
            {
                foreach (GameObject instance in slotInstances)
                {
                    if (instance != null)
                    {
                        instances.Add(instance);
                    }
                }
            }

            Apply(item);
        }

        private void Apply(ItemDrop.ItemData item)
        {
            bool hasRarity = ItemRarityVisuals.TryGetRarity(item, out ItemRarityDefinition rarity);
            foreach (GameObject instance in instances)
            {
                if (!hasRarity)
                {
                    RarityItemParticles.Remove(instance.transform);
                    continue;
                }

                Vector3 localCenter = Vector3.zero;
                float radius = 0.1f;
                bool hasBounds = false;
                Bounds bounds = default(Bounds);
                foreach (Renderer renderer in instance.GetComponentsInChildren<Renderer>())
                {
                    if (renderer is ParticleSystemRenderer || renderer is LineRenderer)
                    {
                        continue;
                    }

                    if (hasBounds)
                    {
                        bounds.Encapsulate(renderer.bounds);
                    }
                    else
                    {
                        bounds = renderer.bounds;
                        hasBounds = true;
                    }
                }

                if (hasBounds)
                {
                    localCenter = instance.transform.InverseTransformPoint(bounds.center);
                    radius = Mathf.Clamp(bounds.extents.magnitude * 0.5f, 0.08f, 0.3f);
                }

                RarityItemParticles.Ensure(instance.transform, rarity.GlowColor, localCenter, radius);
            }
        }
    }
}
