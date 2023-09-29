using HarmonyLib;
using Mono.Security.Interface;
using Mono.Unity;
using NativeWebSocket;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;

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
            public static void SetChatChannelsToRestore() {
                //Doorstop.Entrypoint.info.WriteLine("SetChatChannels!");
            }
        }
        [HarmonyPatch(typeof(UnityTlsContext))]
        public static class UnityTlsContext_Patch {
            [HarmonyPatch(nameof(UnityTlsContext.ProcessHandshake))]
            [HarmonyPrefix]
            public static bool ProcessHandshake(UnityTlsContext __instance, ref bool __result) {
                Debugger.Break();
                var file = new StreamWriter("MyLogWTF2.txt");
                file.AutoFlush = true;
                unsafe {
                    __instance.lastException = null;
                    UnityTls.unitytls_errorstate unitytls_errorstate = UnityTls.NativeInterface.unitytls_errorstate_create();
                    UnityTls.unitytls_x509verify_result unitytls_x509verify_result = UnityTls.NativeInterface.unitytls_tlsctx_process_handshake(__instance.tlsContext, &unitytls_errorstate);
                    /*if (unitytls_errorstate.code == UnityTls.unitytls_error_code.UNITYTLS_USER_WOULD_BLOCK) {
                        __result = false;
                        return false;
                    }*/
                    if (__instance.lastException != null) {
                        throw __instance.lastException;
                    }
                    if (__instance.IsServer && unitytls_x509verify_result == (UnityTls.unitytls_x509verify_result)2147483648U) {
                        Debug.CheckAndThrow(unitytls_errorstate, "Handshake failed", AlertDescription.HandshakeFailure);
                        if (!__instance.ValidateCertificate(null, null)) {
                            throw new TlsException(AlertDescription.HandshakeFailure, "Verification failure during handshake");
                        }
                    } else {
                        Debug.CheckAndThrow(unitytls_errorstate, unitytls_x509verify_result, "Handshake failed", AlertDescription.HandshakeFailure);
                    }
                    __result = true;
                    return false;
                }
            }
        }

        [HarmonyPatch(typeof(WebSocket))]
        public static class WebSocket_Patch {
            [HarmonyPatch(nameof(WebSocket.Connect))]
            [HarmonyPrefix]
            public static bool Connect(WebSocket __instance) {
                var file = new StreamWriter("MyLogWTF.txt");
                file.AutoFlush = true;
                try {
                    __instance.m_TokenSource = new CancellationTokenSource();
                    __instance.m_CancellationToken = __instance.m_TokenSource.Token;
                    __instance.m_Socket = new System.Net.WebSockets.ClientWebSocket();
                    foreach (KeyValuePair<string, string> keyValuePair in __instance.headers) {
                        __instance.m_Socket.Options.SetRequestHeader(keyValuePair.Key, keyValuePair.Value);
                    }
                    foreach (string text in __instance.subprotocols) {
                        __instance.m_Socket.Options.AddSubProtocol(text);
                    }
                    var task = __instance.m_Socket.ConnectAsync(__instance.uri, __instance.m_CancellationToken);
                    task.Wait();
                    file.WriteLine("What?");
                    task = __instance.Receive();
                    task.Wait();
                } catch (Exception ex) {
                    file.WriteLine("Error");
                    file.WriteLine(ex.ToString());
                } finally {
                    if (__instance.m_Socket != null) {
                        __instance.m_TokenSource.Cancel();
                        __instance.m_Socket.Dispose();
                    }
                }
                return false;
            }
        }
        public static void Patch() {
            try {
                HarmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
                Doorstop.Entrypoint.info.WriteLine("Finished Patching!");
            } catch (Exception ex) {
                Doorstop.Entrypoint.error.WriteLine(ex.ToString());
            }
        }
    }
}