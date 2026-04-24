using System;
using UnityEngine.UI;

namespace Birdie.UI
{
    [Serializable]
    internal sealed class ObjectPopupMenuButtonEntry
    {
        public Button Button;
        public ObjectPopupMenuAction Action;
    }
}
