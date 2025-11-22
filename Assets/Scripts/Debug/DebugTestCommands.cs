using UnityEngine;

namespace Birdie.Debug
{
    /// <summary>
    /// Contains debug test commands that will automatically appear in the debug menu.
    /// Each method marked with [DebugCommand] will generate a button in the UI.
    /// </summary>
    public sealed class DebugTestCommands : MonoBehaviour
    {
        [DebugCommand("Spawn Test Bird", "Birds")]
        public void SpawnTestBird()
        {
            // TODO: Implement bird spawning test
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Spawn Test Bird - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Clear All Birds", "Birds")]
        public void ClearAllBirds()
        {
            // TODO: Implement clear all birds functionality
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Clear All Birds - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Add Currency", "Economy")]
        public void AddTestCurrency()
        {
            // TODO: Implement currency addition test
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Add Currency - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Reset Save Data", "Save System")]
        public void ResetSaveData()
        {
            // TODO: Implement save data reset
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Reset Save Data - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Toggle FPS Display", "Performance")]
        public void ToggleFpsDisplay()
        {
            // TODO: Implement FPS counter toggle
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Toggle FPS Display - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Test Friendship Level Up", "Friendship")]
        public void TestFriendshipLevelUp()
        {
            // TODO: Implement friendship level up test
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Test Friendship Level Up - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Reload Scene", "General")]
        public void ReloadCurrentScene()
        {
            // TODO: Implement scene reload
            DebugBase.Log($"[{nameof(DebugTestCommands)}] Reload Scene - Not yet implemented", DebugCategory.Debug);
        }

        [DebugCommand("Print System Info", "General")]
        public void PrintSystemInfo()
        {
            // TODO: Implement system info display
            DebugBase.Log($"[{nameof(DebugTestCommands)}] System Info - Not yet implemented", DebugCategory.Debug);
        }

        // Example of a static debug command
        [DebugCommand("Test Static Command", "General")]
        public static void TestStaticCommand()
        {
            UnityEngine.Debug.Log("[DebugTestCommands] This is a static test command!");
        }
    }
}
