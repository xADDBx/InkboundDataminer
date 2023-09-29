using HarmonyLib;
using NativeWebSocket;
using ShinyShoe;
using System;
using System.Collections.Generic;

namespace InkboundDataminer {
    public static class Patches {
        public static Harmony HarmonyInstance => new Harmony("ADDB.Test");
        /*
        [HarmonyPatch(typeof(StatCategoryVisualData))]
        public static class StatCategoryVisualData_Patch {
            [HarmonyPatch(nameof(StatCategoryVisualData.GetStatCategories))]
            [HarmonyPrefix]
            public static bool GetStatCategories(StatCategoryVisualData __instance, ref IReadOnlyList<StatCategoryEntryData> __result) {
                foreach (var stat in __instance.statCategories) {
                    stat.hideEntriesIfZero = false;
                    stat.onlyShowInHud = false;
                }
                __result = __instance.statCategories;
                return false;
            }
        }
        */
        [HarmonyPatch(typeof(MessagingSystem.State))]
        public static class MessagingSystem_State_Patch {
            [HarmonyPatch(nameof(MessagingSystem.State.SetChatChannelsToRestore))]
            [HarmonyPrefix]
            public static void SetChatChannelsToRestore(MessagingSystem.State __instance, IReadOnlyDictionary<MessagingSystem.ChatLogBufferType, string> currentChatChannels) {
                Log.Info(LogGroups.Networking, string.Format("Saving websocket channels to restore - count: 666{0}", currentChatChannels.Count), LogOptions.None);
                try {
                    Doorstop.Entrypoint.info.WriteLine("SetChatChannels!");
                } catch (Exception ex) {
                    Log.Info(LogGroups.Networking, "Why no Patch Channels");
                    Log.Info(LogGroups.Networking, ex.ToString());
                }
            }
        }
        [HarmonyPatch(typeof(WebSocket))]
        public static class WebSocket_Patch {
            [HarmonyPatch(nameof(WebSocket.Connect))]
            [HarmonyPrefix]
            public static void Connect(WebSocket __instance) {
                try {
                    Doorstop.Entrypoint.info.WriteLine("Try Connect!");
                    foreach (KeyValuePair<string, string> keyValuePair in __instance.headers) {
                        Doorstop.Entrypoint.info.WriteLine(keyValuePair.Key + ", " + keyValuePair.Value);
                    }
                    foreach (string text in __instance.subprotocols) {
                        Doorstop.Entrypoint.info.WriteLine(text);
                    }
                } catch (Exception ex) {
                    Log.Info(LogGroups.Networking, "Why no Patch");
                    Log.Info(LogGroups.Networking, ex.ToString());
                }
            }
        }
        public static void Patch() {
            try {
                Harmony.DEBUG = true;
                HarmonyInstance.PatchAll();
                Doorstop.Entrypoint.info.WriteLine("Finished Patching!");
            } catch (Exception ex) {
                Doorstop.Entrypoint.error.WriteLine(ex.ToString());
            }
        }
    }
}
