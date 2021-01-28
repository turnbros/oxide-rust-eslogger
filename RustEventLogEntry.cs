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
            public int log_format_version = 2;
            public long timestamp;
            public string event_name;
            public BaseEventLogEntry(string event_name)
            {
                timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                this.event_name = event_name;
            }
        }

        [Serializable]
        public class EntityEventLogEntry : BaseEventLogEntry
        {
            
            public RustEventEntity.Entity reporting_entity;

            public EntityEventLogEntry(string event_name, BaseEntity entity) : base(event_name)
            {
                BasePlayer player = entity?.GetComponent<BasePlayer>();
                if (player != null)
                {
                    reporting_entity = new RustEventResident.Resident(player);
                }
                else
                {
                    reporting_entity = new RustEventEntity.Entity(entity);
                }
            }
        }
    }
}