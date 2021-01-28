using System;
using Newtonsoft.Json;
using ConVar;
using UnityEngine;
using System.Linq;

using static Oxide.Plugins.RustEventLogEntry;
using static Oxide.Plugins.RustEventResident;
using static Oxide.Plugins.RustEventServer;
using static Oxide.Plugins.RustEventResidentAction;

namespace Oxide.Plugins
{
    [Info("RustEventLogger", "RedSys", 1.0)]
    [Description("Logs Rust events")]
    class RustEsLogger : RustPlugin
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
            timer.Every(5f, () =>
            {
                var mapSize = TerrainMeta.Size.x;
                var initialColliders = (int)(((mapSize * mapSize) / 1000000) * 1500);
                int colliders = UnityEngine.Object.FindObjectsOfType<Collider>().Count(x => x.enabled);
                CreateLogEntry("on_server_event.log", new ServerEventLogEntry(Performance.current, colliders, BasePlayer.activePlayerList.Count()));
            });

            timer.Every(10f, () =>
            {
                Array.ForEach(BasePlayer.activePlayerList.ToArray(), x =>
                    CreateLogEntry("on_server_player.log", new OnlineResident(x))
                );
            });

        }
        [Serializable]
        public class OnlineResident : BaseEventLogEntry
        {
            public Resident online_resident = new Resident();
            public OnlineResident() : base("OnlineResident") {}
            public OnlineResident(BasePlayer player) : base("ServerPlayerList") {
                online_resident = new Resident(player);
            }
        }
        [Serializable]
        public class ServerEventLogEntry : BaseEventLogEntry
        {
            public ServerState server_state = new ServerState();
            public ServerEventLogEntry() : base("ServerState") {}
            public ServerEventLogEntry(Performance.Tick performance, int colliders, int playerCount) : base("ServerEvent")
            {
                server_state = new ServerState(performance, colliders, playerCount);
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
            if (player != null)
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
        void OnPlayerConnected(BasePlayer player) {
            CreateLogEntry("on_player_connect.log", new ResidentConnected(player));
        }
        [Serializable]
        public class ResidentConnected : EntityEventLogEntry {
            public ResidentConnectionEvent connection_event = new ResidentConnectionEvent();
            public ResidentConnected(BasePlayer player) : base("OnPlayerConnected", player) {
                connection_event = new ResidentConnectionEvent(true, "connected");
            }
        }


        // On Player Disconnected
        // Called after the player has disconnected from the server
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            CreateLogEntry("on_player_disconnect.log", new ResidentDisconnected(player, reason));
        }
        [Serializable]
        public class ResidentDisconnected : EntityEventLogEntry {
            public ResidentConnectionEvent connection_event = new ResidentConnectionEvent();
            public ResidentDisconnected(BasePlayer player, string reason) : base("OnPlayerDisconnected", player) {
                connection_event = new ResidentConnectionEvent(false, reason);
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
    }
}