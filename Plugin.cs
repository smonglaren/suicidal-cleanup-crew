using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace SelfCleanupCrew;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
public class Plugin : BaseUnityPlugin
{
    internal static new ManualLogSource Logger;
    internal static Harmony _Harmony;

    private void Awake()
    {
        // Plugin startup logic
        Logger = base.Logger;
        Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} loaded. Attempting patch...");
        _Harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        _Harmony.PatchAll();
        Logger.LogDebug("Patching done!");
    }
}

[HarmonyPatch]
public class EnemyBombThrowerHeadPatch
{
    [HarmonyTranspiler]
    [HarmonyPatch(typeof(EnemyBombThrowerHead), "ExplodeRPC")]
    public static IEnumerable<CodeInstruction> ExplodeTranspiler(IEnumerable<CodeInstruction> instructions)
    {
        var opcodes = instructions.ToList();
        for(int i = 0; i < opcodes.Count; i++) // we're modifying the opcodes list from within this function, so we can't foreach through it
        {
            if (opcodes[i].opcode == OpCodes.Stfld)
            {
                Plugin.Logger.LogDebug($"{i} Found Stfld in: {opcodes[i].operand.ToString()}");
                if (opcodes[i].operand.ToString().Contains("enemyHost"))
                {
                    Plugin.Logger.LogDebug($"{i} Attempting to patch away: {opcodes[i].ToString()}");
                    opcodes[i].opcode = OpCodes.Pop;
                    opcodes.Insert(i + 1, new CodeInstruction(OpCodes.Pop)); // because Stfld takes 2 arguments we need to pop the stack twice. words words words
                    // Gemini helped me with this. I am a husk of a human being devoid of all value. Please shoot me.
                }
            }
        }
        return opcodes.AsEnumerable();
    }
}