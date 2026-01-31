using Birdie.Managers;
using UnityEngine;

namespace Birdie.Debug
{
    /// <summary>
    /// Contains debug test commands that will automatically appear in the debug menu.
    /// Each method marked with [DebugCommand] will generate a button in the UI.
    /// </summary>
    public sealed class DebugTestCommands : MonoBehaviour
    {
        private const int DebugCurrencyAmount = 100;

        [DebugCommand("Add 100 Currency", "Economy")]
        public void AddTestCurrency()
        {
            if (GameManager.Instance?.EconomyManager == null)
            {
                DebugBase.Log($"[{nameof(DebugTestCommands)}] EconomyManager not available", DebugCategory.Debug);
                return;
            }

            GameManager.Instance.EconomyManager.AddGoldenSeeds(DebugCurrencyAmount);
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Added {DebugCurrencyAmount} golden seeds. Total: {GameManager.Instance.EconomyManager.GoldenSeeds}", DebugCategory.Debug);
        }
    }
}
