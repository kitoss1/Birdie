using System;
using System.Collections.Generic;

namespace Birdie.Save
{
    [Serializable]
    public class TrashItemSaveEntry
    {
        public int prefabIndex;
        public float positionX;
        public float positionY;
        public float rotation;
    }

    [Serializable]
    public class WindowsillSaveData
    {
        public List<TrashItemSaveEntry> activeTrash = new List<TrashItemSaveEntry>();
        public long lastSpawnTimestamp;

        public bool IsValid() => activeTrash != null;
    }
}
