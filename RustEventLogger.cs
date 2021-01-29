using System;
using Newtonsoft.Json;
using ConVar;
using UnityEngine;
using System.Linq;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RustEventLogger", "RedSys", 2.0)]
    [Description("Logs Rust events")]
    class RustEventLogger : RustPlugin
    {

        [PluginReference] Plugin RustEventServer;
        [PluginReference] Plugin RustEventEntity;
        [PluginReference] Plugin RustEventResident;
        [PluginReference] Plugin RustEventLogEntry;
        [PluginReference] Plugin RustEventResidentAction;

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
        public class OnlineResident : RustEventLogEntry.BaseEventLogEntry
        {
            public RustEventResident.Resident online_resident = new RustEventResident.Resident();
            public OnlineResident() : base("OnlineResident") {}
            public OnlineResident(BasePlayer player) : base("ServerPlayerList") {
                online_resident = new RustEventResident.Resident(player);
            }
        }
        [Serializable]
        public class ServerEventLogEntry : RustEventLogEntry.BaseEventLogEntry
        {
            public RustEventServer.ServerState server_state = new RustEventServer.ServerState();
            public ServerEventLogEntry() : base("ServerState") {}
            public ServerEventLogEntry(Performance.Tick performance, int colliders, int playerCount) : base("ServerEvent")
            {
                server_state = new RustEventServer.ServerState(performance, colliders, playerCount);
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
        public class ResidentAttack : RustEventLogEntry.EntityEventLogEntry
        {
            public RustEventResidentAction.AggressiveAction attack;
            public ResidentAttack(BasePlayer player, HitInfo info) : base("OnPlayerAttack", player)
            {
                if (info == null) attack = new RustEventResidentAction.AggressiveAction();
                else attack = new RustEventResidentAction.AggressiveAction(info);
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
        public class ResidentDead : RustEventLogEntry.EntityEventLogEntry
        {
            public RustEventResidentAction.AggressiveAction aggressive_act;
            public ResidentDead(BasePlayer player, HitInfo info) : base("OnPlayerDeath", player)
            {
                if(info == null) aggressive_act = new RustEventResidentAction.AggressiveAction();
                else aggressive_act = new RustEventResidentAction.AggressiveAction(info);
            }
        }


        // On Loot Player
        // Called when the player starts looting another player
        void OnLootPlayer(BasePlayer player, BasePlayer target)
        {
            CreateLogEntry("on_player_death.log", new ResidentLooted(player, target));
        }
        [Serializable]
        public class ResidentLooted : RustEventLogEntry.EntityEventLogEntry
        {
            public RustEventResident.Resident looted_resident;
            public ResidentLooted(BasePlayer player, BasePlayer target) : base("OnLootPlayer", player)
            {
                looted_resident = new RustEventResident.Resident(target);
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
        public class ResidentConnected : RustEventLogEntry.EntityEventLogEntry {
            public RustEventServer.ResidentConnectionEvent connection_event = new RustEventServer.ResidentConnectionEvent();
            public ResidentConnected(BasePlayer player) : base("OnPlayerConnected", player) {
                connection_event = new RustEventServer.ResidentConnectionEvent(true, "connected");
            }
        }


        // On Player Disconnected
        // Called after the player has disconnected from the server
        void OnPlayerDisconnected(BasePlayer player, string reason) {
            CreateLogEntry("on_player_disconnect.log", new ResidentDisconnected(player, reason));
        }
        [Serializable]
        public class ResidentDisconnected : RustEventLogEntry.EntityEventLogEntry {
            public RustEventServer.ResidentConnectionEvent connection_event = new RustEventServer.ResidentConnectionEvent();
            public ResidentDisconnected(BasePlayer player, string reason) : base("OnPlayerDisconnected", player) {
                connection_event = new RustEventServer.ResidentConnectionEvent(false, reason);
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
        public class ResidentChatMessage : RustEventLogEntry.EntityEventLogEntry
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
        public class ResidentGatheredItem : RustEventLogEntry.EntityEventLogEntry
        {
            public RustEventResidentAction.GatheredItemAction gathered_item_action;
            public ResidentGatheredItem(BaseEntity entity, string dispenserName, Item item) : base("OnGather", entity){   
                    gathered_item_action = new RustEventResidentAction.GatheredItemAction(dispenserName, item);
            }
        }
    }
}