using System;
using UnityEngine.UI;

namespace Birdie.UI
{
    [Serializable]
    internal sealed class PopupMenuButtonEntry
    {
        public Button Button;
        public PopupMenuAction Action;
    }
}
