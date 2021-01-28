using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RustEventResidentAction", "RedSys", 2.0)]
    class RustEventResidentAction : RustPlugin
    {
        [PluginReference] Plugin RustEventEntity;
        [PluginReference] Plugin RustEventResident;

        [Serializable]
        public class GatheredItemAction
        {
            public string dispenser = "unknown";
            public string name = "unknown";
            public string category = "unknown";
            public int amount = 0;

            public GatheredItemAction() { }
            public GatheredItemAction(string dispenserName, Item item)
            {
                if (item == null) return;

                if (dispenserName != null)
                    dispenser = dispenserName;

                amount = item.amount;

                if (item.info != null)
                {
                    name = item.info.displayName.english;
                    category = item.info.category.ToString();
                }
            }
        }

        [Serializable]
        public class AggressiveAction
        {
            public RustEventEntity.Entity aggression_target = new RustEventEntity.Entity();
            public RustEventEntity.Entity aggression_initiator = new RustEventEntity.Entity();
            public string weapon_name = "unknown";
            public string weapon_prefab = "unknown";
            public string material_name = "unknown";

            public bool did_gather = false;
            public bool did_hit = false;
            public bool is_projectile = false;
            public bool is_headshot = false;
            public bool is_attack = false;

            public string damage_name = "unknown";
            public string damage_type = "unknown";
            public int damage_delt = 0;
            public float projectile_distance = 0;

            public AggressiveAction() { }
            public AggressiveAction(HitInfo info)
            {

                if (info == null) return;

                // Get the source of the aggression
                if (info.Initiator != null)
                {
                    if (info.InitiatorPlayer != null)
                        aggression_initiator = new RustEventResident.Resident(info.InitiatorPlayer);
                    else
                        aggression_initiator = new RustEventEntity.Entity(info.Initiator);
                }


                // Get the target of the agression
                BasePlayer player = info.HitEntity?.GetComponent<BasePlayer>();
                if (player != null)
                {
                    aggression_target = new RustEventResident.Resident(player);
                }
                else if (info.HitEntity != null)
                {
                    aggression_target = new RustEventEntity.Entity(info.HitEntity);
                }

                // Get the weapons name
                if (info.Weapon.ShortPrefabName != null)
                {
                    weapon_name = info.Weapon.ShortPrefabName;
                    weapon_prefab = info.Weapon.PrefabName;
                }

                if (info.material != null)
                    material_name = info.material.name;

                if (info.hasDamage)
                {
                    damage_name = info.damageProperties.name;
                    damage_type = info.damageTypes.GetMajorityDamageType().ToString();
                    damage_delt = (int)info.damageTypes.Total();
                    is_attack = info.damageTypes.IsConsideredAnAttack();
                }

                did_gather = info.DidGather;
                is_headshot = info.isHeadshot;
                if (info.IsProjectile())
                {
                    is_projectile = info.IsProjectile();
                    projectile_distance = info.ProjectileDistance;
                }
            }
        }
    }
}
