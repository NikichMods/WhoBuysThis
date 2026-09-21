using System;
using System.Collections;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace WhoBuysThis
{
    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "nikich.gyk.whobuysthis";
        public const string PluginName = "Who Buys This?";
        public const string PluginVersion = "0.1.0";

        internal static ManualLogSource Log;
        private Harmony _harmony;

        private void Awake()
        {
            Log = Logger;
            try
            {
                GameApi.Bind();
                _harmony = new Harmony(PluginGuid);

                MethodInfo started = GameApi.MainGameType.GetMethod(
                    "OnGameStartedPlaying",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                MethodInfo tooltip = GameApi.ItemDefinitionType.GetMethod(
                    "GetTooltipData",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (started == null || tooltip == null)
                    throw new MissingMethodException("Required Graveyard Keeper lifecycle/tooltip method was not found.");

                _harmony.Patch(
                    started,
                    postfix: new HarmonyMethod(typeof(RuntimePatches).GetMethod(
                        nameof(RuntimePatches.OnGameStartedPlayingPostfix),
                        BindingFlags.Static | BindingFlags.Public)));

                _harmony.Patch(
                    tooltip,
                    postfix: new HarmonyMethod(typeof(RuntimePatches).GetMethod(
                        nameof(RuntimePatches.GetTooltipDataPostfix),
                        BindingFlags.Static | BindingFlags.Public)));

                Log.LogInfo(PluginName + " " + PluginVersion + " loaded.");
            }
            catch (Exception ex)
            {
                Log.LogError("Who Buys This? failed to initialize: " + ex);
            }
        }

        private void OnDestroy()
        {
            if (_harmony != null)
                _harmony.UnpatchSelf();
        }
    }

    internal static class RuntimePatches
    {
        public static void OnGameStartedPlayingPostfix()
        {
            try
            {
                BuyerIndex.Build();
            }
            catch (Exception ex)
            {
                BuyerIndex.Clear();
                Plugin.Log.LogError("Buyer index build failed; tooltip extension disabled for this load: " + ex);
            }
        }

        public static void GetTooltipDataPostfix(object __instance, bool full_detail, IList __result)
        {
            if (!full_detail || __instance == null || __result == null)
                return;

            try
            {
                BuyerIndex.AppendTooltip(__instance, __result);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError("Tooltip buyer rendering failed: " + ex);
            }
        }
    }
}
