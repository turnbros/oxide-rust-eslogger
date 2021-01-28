using System;

namespace Oxide.Plugins
{
    [Info("RustEventLogEntry", "RedSys", 1.1)]
    class RustEventLogEntry : RustPlugin
    {
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
            public Entity resident_subject;
            public Entity reporting_entity;

            public EntityEventLogEntry(string event_name, BaseEntity entity) : base(event_name)
            {
                BasePlayer player = entity?.GetComponent<BasePlayer>();
                if (player != null)
                {
                    reporting_entity = new Resident(player);
                    resident_subject = new Resident(player);
                }
                else
                {
                    reporting_entity = new Entity(entity);
                    resident_subject = new Entity(entity);
                }
            }
        }
    }
}