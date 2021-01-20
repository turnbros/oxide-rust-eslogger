using System;
using Newtonsoft.Json;
using ConVar;

namespace Oxide.Plugins
{
    [Info("OxideRustEsLogger", "RedSys", 3.6)]
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
        public class ResidentAttack : BaseEventLogEntry
        {
            public AggressiveAction aggressive_act;
            public ResidentAttack(BasePlayer player, HitInfo info) : base("OnPlayerAttack", player)
            {
                aggressive_act = new AggressiveAction(info);
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
        public class ResidentDead : BaseEventLogEntry
        {
            public AggressiveAction aggressive_act;
            public ResidentDead(BasePlayer player, HitInfo info) : base("OnPlayerDeath", player)
            {
                aggressive_act = new AggressiveAction(info);
            }
        }


        // On Loot Player
        // Called when the player starts looting another player
        void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            CreateLogEntry("on_player_death.log", new ResidentLooted(player, target));
        }
        [Serializable]
        public class ResidentLooted : BaseEventLogEntry
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
        public class ResidentConnected : BaseEventLogEntry
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
        public class ResidentDisconnected : BaseEventLogEntry
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
        public class ResidentChatMessage : BaseEventLogEntry
        {
            public string message;
            public string channel;
            public ResidentChatMessage(BasePlayer player, string message, string channel) : base("OnPlayerChat", player)
            {
                this.message = message;
                this.channel = channel;
            }
        }


        /*****************************************
         ** Generic Serializable Object Classes **
         *****************************************/

        [Serializable]
        public class BaseEventLogEntry {
            public int timestamp;
            public int log_format_version = 1;
            public string event_name;
            public Resident resident_subject;

            public BaseEventLogEntry(string hook_name, BasePlayer player)
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                timestamp = (int)t.TotalSeconds;
                event_name = hook_name;
                resident_subject = new Resident(player);
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
                type = entity.GetType().Name;
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
                BasePlayer player = info.HitEntity.GetComponent<BasePlayer>();
                if (player != null)
                    aggression_target = new Resident(player);
                else if(info.HitEntity != null)
                    aggression_target = new Entity(info.HitEntity);


                if (info.Weapon?.ShortPrefabName != null)
                {
                    weapon_name = info.Weapon.ShortPrefabName;
                    weapon_prefab = info.Weapon.PrefabName;
                }

                if (info.material != null)
                    material_name = info.material.name;

                did_gather = info.DidGather;
                is_projectile = info.IsProjectile();
                is_headshot = info.isHeadshot;
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
