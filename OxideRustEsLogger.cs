using System;
using Newtonsoft.Json;
using ConVar;
using Oxide.Core.Libraries;
using UnityEngine;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("OxideRustEsLogger", "RedSys", 4.0)]
    [Description("Logs player actions.")]
    class OxideRustEsLogger : RustPlugin
    {

        void CreateLogEntry(string fileName, object eventObject) {
            try {
                LogToFile(fileName, JsonConvert.SerializeObject(eventObject), this);
            } catch (Exception error) {
                LogToFile("error.log", $"[{DateTime.Now}] ERROR - {error.Message} - {error.StackTrace}", this);
            }
        }

        void Loaded()
        {
            timer.Every(3f, () =>
            {
                var mapSize = TerrainMeta.Size.x;
                var initialColliders = (int)(((mapSize * mapSize) / 1000000) * 1500);
                int colliders = UnityEngine.Object.FindObjectsOfType<Collider>().Count(x => x.enabled);
                CreateLogEntry("on_server_event.log", new ServerEventLogEntry(BasePlayer.activePlayerList.Count, Performance.current, colliders));
            });
        }
        [Serializable]
        public class ServerEventLogEntry : BaseEventLogEntry
        {
            public int player_count = 0;
            public int fps = 0;
            public int colliders = 0;
            public long load_balancer_tasks = 0;
            public long memory_allocations = 0;
            public long memory_collections = 0;
            public long memory_usage_system = 0;
            public int ping = 0;

            public ServerEventLogEntry() : base("ServerEvent"){}
            public ServerEventLogEntry(int playerCount, Performance.Tick performance, int colliders) : base("ServerEvent")
            {
                player_count = playerCount;
                fps = performance.frameRate;
                this.colliders = colliders;
                load_balancer_tasks = performance.loadBalancerTasks;
                memory_allocations = performance.memoryAllocations;
                memory_collections = performance.memoryCollections;
                memory_usage_system = performance.memoryUsageSystem;
                ping = performance.ping;
            }
        }

        /******************************************************
         ** Multi Resident Event Serializable Object Classes **
         ******************************************************/


        // On Player Attack
        // Useful for modifying an attack before it goes out hitInfo.HitEntity should be the
        void OnPlayerAttack(BasePlayer attacker, HitInfo info)
        {
            CreateLogEntry("on_player_attack.log", new ResidentAttack(attacker, info));
        }
        [Serializable]
        public class ResidentAttack : EntityEventLogEntry
        {
            public AggressiveAction aggressive_act;
            public ResidentAttack(BasePlayer player, HitInfo info) : base("OnPlayerAttack", player)
            {
                if (info == null) aggressive_act = new AggressiveAction();
                else aggressive_act = new AggressiveAction(info);
            }
        }


        // On Player Death
        // Called when the player is about to die. HitInfo may be null sometimes
        object OnPlayerDeath(BasePlayer player, HitInfo info)
        {
            CreateLogEntry("on_player_death.log", new ResidentDead(player, info));
            return null;
        }
        [Serializable]
        public class ResidentDead : EntityEventLogEntry
        {
            public AggressiveAction aggressive_act;
            public ResidentDead(BasePlayer player, HitInfo info) : base("OnPlayerDeath", player)
            {
                if(info == null) aggressive_act = new AggressiveAction();
                else aggressive_act = new AggressiveAction(info);
            }
        }


        // On Loot Player
        // Called when the player starts looting another player
        void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            CreateLogEntry("on_player_death.log", new ResidentLooted(player, target));
        }
        [Serializable]
        public class ResidentLooted : EntityEventLogEntry
        {
            public Resident looted_resident;
            public ResidentLooted(BasePlayer player, BasePlayer target) : base("OnLootPlayer", player)
            {
                looted_resident = new Resident(target);
            }
        }


        /*******************************************************
         ** Single Resident Event Serializable Object Classes **
         *******************************************************/


        // On Player Connected
        // Called after the player object is created, but before the player has spawned
        void OnPlayerConnected(BasePlayer player)
        {
            CreateLogEntry("on_player_connect.log", new ResidentConnected(player));
        }
        [Serializable]
        public class ResidentConnected : EntityEventLogEntry
        {
            public ResidentConnected(BasePlayer player) : base("OnPlayerConnected", player){}
        }


        // On Player Disconnected
        // Called after the player has disconnected from the server
        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            CreateLogEntry("on_player_disconnect.log", new ResidentDisconnected(player, reason));
        }
        [Serializable]
        public class ResidentDisconnected : EntityEventLogEntry
        {
            public string reason;
            public ResidentDisconnected(BasePlayer player, string reason) : base("OnPlayerDisconnected", player)
            {
                this.reason = reason;
            }
        }

        
        // On Player Chat
        // Called when the player sends chat to the server
        object OnPlayerChat(BasePlayer player, string message, Chat.ChatChannel channel)
        {
            CreateLogEntry("on_player_chat.log", new ResidentChatMessage(player, message, channel.ToString()));
            return null;
        }
        [Serializable]
        public class ResidentChatMessage : EntityEventLogEntry
        {
            public string message;
            public string channel;
            public ResidentChatMessage(BasePlayer player, string message, string channel) : base("OnPlayerChat", player)
            {
                this.message = message;
                this.channel = channel;
            }
        }

        // On Collectible Pickup
        // Called when the player collects an item
        object OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            CreateLogEntry("on_player_gather.log", new ResidentGatheredItem(player, entity?.name, item));
            return null;
        }

        // On Growable Gathered
        // Called before the player receives an item from gathering a growable entity
        void OnGrowableGathered(GrowableEntity plant, Item item, BasePlayer player) {
            CreateLogEntry("on_player_gather.log", new ResidentGatheredItem(player, plant?.name, item));
        }

        // On Dispenser Gather
        // Called before the player is given items from a resource
        object OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item) {
            CreateLogEntry("on_player_gather.log", new ResidentGatheredItem(entity, dispenser?.name, item));
            return null; // Always return null!
        }

        // On Dispenser Bonus
        // Called before the player is given a bonus item for gathering
        object OnDispenserBonus(ResourceDispenser dispenser, BasePlayer player, Item item) {
            CreateLogEntry("on_player_gather.log", new ResidentGatheredItem(player, dispenser?.name, item));
            return null; // Always return null!
        }

        [Serializable]
        public class ResidentGatheredItem : EntityEventLogEntry
        {
            public GatheredItemAction gathered_item_action;
            public ResidentGatheredItem(BaseEntity entity, string dispenserName, Item item) : base("OnGather", entity){   
                    gathered_item_action = new GatheredItemAction(dispenserName, item);
            }
        }

        /*****************************************
         ** Generic Serializable Object Classes **
         *****************************************/

        [Serializable]
        public class BaseEventLogEntry {
            public static int GetTimestamp() {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return (int)t.TotalSeconds;
            }
            public int timestamp;
            public int log_format_version = 2;
            public string event_name;
            public BaseEventLogEntry(string event_name) {
                timestamp = GetTimestamp();
                this.event_name = event_name;
            }
        }

        [Serializable]
        public class EntityEventLogEntry : BaseEventLogEntry {

            public Entity resident_subject;
            public Entity reporting_entity;

            public EntityEventLogEntry(string event_name, BaseEntity entity) : base(event_name) {
                BasePlayer player = entity?.GetComponent<BasePlayer>();
                if (player != null) {
                    reporting_entity = new Resident(player);
                    resident_subject = new Resident(player);
                } else {
                    reporting_entity = new Entity(entity);
                    resident_subject = new Entity(entity);
                }
            }
        }

        [Serializable]
        public class Entity
        {
            // Name and ID stuff
            public ulong owner_id = 0;
            public string name = "unknown";
            public string type = "unknown";

            // Prefab Info
            public uint prefab_id = 0;
            public string prefab_name = "unknown";
            public string prefab_path = "unknown";

            public bool is_npc = true;
            public EntityLocation location = new EntityLocation();

            public Entity() { }
            public Entity(BaseEntity entity)
            {
                if (entity == null) return;

                owner_id = entity.OwnerID;
                name = entity.name;
                type = entity.GetType()?.Name;
                if (type == null) type = "unknown";

                prefab_id = entity.prefabID;
                prefab_name = entity.ShortPrefabName;
                prefab_path = entity.PrefabName;

                location = new EntityLocation(entity.transform);
                is_npc = entity.IsNpc;
            }
        }

        [Serializable]
        public class Resident : Entity {
            public ulong user_id = 0;
            public float health = 0;
            public float heart_rate = 0;
            public bool is_building_authed = false;
            public bool is_building_blocked = false;
            public ResidentTeamMembership team = new ResidentTeamMembership();

            public Resident() {}
            public Resident(BasePlayer player) : base(player.GetEntity()) {
                if (player == null) return;

                user_id = player.userID;
                name = player.displayName;
                prefab_path = player.PrefabName;
                prefab_id = player.prefabID;
                prefab_name = player.ShortPrefabName;
                is_npc = player.IsNpc;
                health = player.health;

                is_building_authed = player.IsBuildingAuthed();
                is_building_blocked = player.IsBuildingBlocked();
                team = new ResidentTeamMembership(player.Team);
                location = new EntityLocation(player.transform);

                if (player.metabolism != null)
                    if (player.metabolism.heartrate != null)
                            heart_rate = player.metabolism.heartrate.value;
            }
        }

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

                if (item.info != null) {
                    name = item.info.displayName.english;
                    category = item.info.category.ToString();
                }
            }
        }

        [Serializable]
        public class AggressiveAction
        {
            public Entity aggression_target = new Entity();
            public Entity aggression_initiator = new Entity();
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
                        aggression_initiator = new Resident(info.InitiatorPlayer);
                    else
                        aggression_initiator = new Entity(info.Initiator);
                }


                // Get the target of the agression
                BasePlayer player = info.HitEntity?.GetComponent<BasePlayer>();
                if (player != null) {
                    aggression_target = new Resident(player);
                } else if(info.HitEntity != null) {
                    aggression_target = new Entity(info.HitEntity);
                }

                // Get the weapons name
                if (info.Weapon.ShortPrefabName != null) {
                    weapon_name = info.Weapon.ShortPrefabName;
                    weapon_prefab = info.Weapon.PrefabName;
                }

                if (info.material != null)
                    material_name = info.material.name;

                if (info.hasDamage) {
                    damage_name = info.damageProperties.name;
                    damage_type = info.damageTypes.GetMajorityDamageType().ToString();
                    damage_delt = (int)info.damageTypes.Total();
                    is_attack = info.damageTypes.IsConsideredAnAttack();
                }

                did_gather = info.DidGather;
                is_headshot = info.isHeadshot;
                if (info.IsProjectile()) {
                    is_projectile = info.IsProjectile();
                    projectile_distance = info.ProjectileDistance;
                }
            }
        }

        [Serializable]
        public class ResidentTeamMembership
        {
            public ulong id = 0;
            public string name = "unknown";
            public ulong leader = 0;
            public float start_time = 0;
            public float lifetime = 0;

            public ResidentTeamMembership(){}
            public ResidentTeamMembership(RelationshipManager.PlayerTeam team) {
                if (team == null) return;

                id = team.teamID;
                name = team.teamName;
                leader = team.teamLeader;
                start_time = team.teamStartTime;
                lifetime = team.teamLifetime;
            }

        }

        [Serializable]
        public class EntityLocation
        {
            public float x = 0;
            public float y = 0;
            public float z = 0;

            public EntityLocation(){}
            public EntityLocation(UnityEngine.Transform entityTransform) {
                if (entityTransform == null) return;

                x = entityTransform.position.x;
                y = entityTransform.position.y;
                z = entityTransform.position.z;
            }
        }
    }
}
