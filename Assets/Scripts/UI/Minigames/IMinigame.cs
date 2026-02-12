using System;

namespace Birdie.UI.Minigames
{
    public interface IMinigame
    {
        event Action GameClosed;
    }
}
