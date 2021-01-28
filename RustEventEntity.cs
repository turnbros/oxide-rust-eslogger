using System;

// Requires: RustEventResident
// Requires: RustEventResidentAction
// Requires: RustEventServer

/*
  04 RustEventResident - Unloaded
  05 RustEventResidentAction - Unloaded
  06 RustEventServer - Unloade
 */

namespace Oxide.Plugins
{
    [Info("RustEventEntity", "RedSys", 1.1)]
    class RustEventEntity : RustPlugin
    {
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
        public class EntityLocation
        {
            public float x = 0;
            public float y = 0;
            public float z = 0;

            public EntityLocation() { }
            public EntityLocation(UnityEngine.Transform entityTransform)
            {
                if (entityTransform == null) return;

                x = entityTransform.position.x;
                y = entityTransform.position.y;
                z = entityTransform.position.z;
            }
        }
    }
}