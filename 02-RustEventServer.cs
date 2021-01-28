using System;

namespace Oxide.Plugins
{
    [Info("RustEventServer", "RedSys", 2.0)]
    class RustEventServer : RustPlugin
    {
        [Serializable]
        public class ResidentConnectionEvent
        {
            public bool is_connected = false;
            public string event_name = "unknown";
            public string event_reason = "unknown";
            public ResidentConnectionEvent() { }
            public ResidentConnectionEvent(bool isConnected, string eventReason) {

                is_connected = isConnected;
                event_reason = eventReason;

                if (isConnected)
                    event_name = "connected";
                else
                    event_name = "disconnected";
            }
        }

        [Serializable]
        public class ChatMessage
        {
            public string channel = "unknown";
            public string message = "unknown";
            public ChatMessage() { }
            public ChatMessage(string channel, string message) {
                this.channel = channel;
                this.message = message;
            }
        }

        [Serializable]
        public class ServerState
        {
            public int player_count = 0;
            public float fps = 0;
            public int colliders = 0;
            public long load_balancer_tasks = 0;
            public long memory_allocations = 0;
            public long memory_collections = 0;
            public long memory_usage_system = 0;
            public int ping = 0;

            public ServerState() { }
            public ServerState(Performance.Tick performance, int colliders, int playerCount)
            {
                player_count = playerCount;
                fps = performance.frameRateAverage;
                this.colliders = colliders;
                load_balancer_tasks = performance.loadBalancerTasks;
                memory_allocations = performance.memoryAllocations;
                memory_collections = performance.memoryCollections;
                memory_usage_system = performance.memoryUsageSystem;
                ping = performance.ping;
            }
        }
    }
}