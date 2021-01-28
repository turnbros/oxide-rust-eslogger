using System;
using Oxide.Core.Plugins;

namespace Oxide.Plugins
{
    [Info("RustEventLogEntry", "RedSys", 2.0)]
    class RustEventLogEntry : RustPlugin
    {
        [PluginReference] Plugin RustEventEntity;
        [PluginReference] Plugin RustEventResident;

        [Serializable]
        public class BaseEventLogEntry
        {
            public static int GetTimestamp()
            {
                TimeSpan t = DateTime.UtcNow - new DateTime(1970, 1, 1);
                return (int)t.TotalSeconds;
            }
            public int timestamp;
            public int log_format_version = 2;
            public string event_name;
            public BaseEventLogEntry(string event_name)
            {
                timestamp = GetTimestamp();
                this.event_name = event_name;
            }
        }

        [Serializable]
        public class EntityEventLogEntry : BaseEventLogEntry
        {
            
            public RustEventEntity.Entity resident_subject;
            public RustEventEntity.Entity reporting_entity;

            public EntityEventLogEntry(string event_name, BaseEntity entity) : base(event_name)
            {
                BasePlayer player = entity?.GetComponent<BasePlayer>();
                if (player != null)
                {
                    reporting_entity = new RustEventResident.Resident(player);
                    resident_subject = new RustEventResident.Resident(player);
                }
                else
                {
                    reporting_entity = new RustEventEntity.Entity(entity);
                    resident_subject = new RustEventEntity.Entity(entity);
                }
            }
        }
    }
}