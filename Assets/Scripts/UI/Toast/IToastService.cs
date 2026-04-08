using UnityEngine;

namespace Birdie.UI.Toast
{
    public interface IToastService
    {
        void ShowToast(string text, Transform anchor = null);
    }
}
