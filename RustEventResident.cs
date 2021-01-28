// Requires: RustEventEntity

using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RustEventResident", "RedSys", 1.1)]
    class RustEventResident : RustPlugin
    {
        [PluginReference] Plugin RustEventEntity;

        [Serializable]
        public class Resident : RustEventEntity.Entity
        {
            public ulong user_id = 0;
            public float health = 0;
            public float heart_rate = 0;
            public bool is_building_authed = false;
            public bool is_building_blocked = false;
            public string ip_address = "0.0.0.0";
            public int port = 0;
            public string os = "unknown";
            public int seconds_connected = 0;
            public ResidentTeamMembership team = new ResidentTeamMembership();

            public Resident() { }
            public Resident(BasePlayer player) : base(player.GetEntity())
            {
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
                location = new RustEventEntity.EntityLocation(player.transform);

                ip_address = player.net.connection.ipaddress.Split(':')[0];
                port = Int32.Parse(player.net.connection.ipaddress.Split(':')[1]);
                os = player.net.connection.os;
                seconds_connected = (int)player.Connection.GetSecondsConnected();

                if (player.metabolism?.heartrate?.value != null)
                    heart_rate = player.metabolism.heartrate.value;
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

            public ResidentTeamMembership() { }
            public ResidentTeamMembership(RelationshipManager.PlayerTeam team)
            {
                if (team == null) return;

                id = team.teamID;
                name = team.teamName;
                leader = team.teamLeader;
                start_time = team.teamStartTime;
                lifetime = team.teamLifetime;
            }

        }
    }
}
