using UnityEngine;

namespace Birdie.UI.Toast
{
    public interface IToastService
    {
        void ShowToast(string text, Transform anchor = null, ToastSettings settings = null, string group = null);
    }
}
