using HarmonyLib;
using Mono.Btls;
using Mono.Net.Security;
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
        [HarmonyPatch(typeof(Mono.Net.Security.MonoTlsProviderFactory))]
        public static class MonoTlsProviderFactory_Patch {
            [HarmonyPatch(nameof(Mono.Net.Security.MonoTlsProviderFactory.CreateDefaultProviderImpl))]
            [HarmonyPrefix]
            public static bool CreateDefaultProviderImpl(ref MobileTlsProvider __result) {
                var file = new StreamWriter("MyLogWTF.txt");
                file.AutoFlush = true;
                file.WriteLine("Start!");
                try {
                    string text = Environment.GetEnvironmentVariable("MONO_TLS_PROVIDER");
                    if (string.IsNullOrEmpty(text)) {
                        text = "default";
                    }
                    if (!(text == "default") && !(text == "legacy")) {
                        if (!(text == "btls")) {
                            if (!(text == "unitytls")) {
                                __result = Mono.Net.Security.MonoTlsProviderFactory.LookupProvider(text, true);
                                file.WriteLine("Done");
                                file.Close();
                                return false;
                            }
                            goto IL_6E;
                        }
                    } else {
                        if (UnityTls.IsSupported) {
                            goto IL_6E;
                        }
                        if (!Mono.Net.Security.MonoTlsProviderFactory.IsBtlsSupported()) {
                            throw new NotSupportedException("TLS Support not available.");
                        }
                    }
                    __result = new MonoBtlsProvider();
                    file.WriteLine("Done");
                    file.Close();
                    return false;
IL_6E:
                    __result = new UnityTlsProvider();
                    file.WriteLine("Done");
                    file.Close();
                    return false;
                } catch (Exception ex) {
                    file.WriteLine(ex.ToString());
                }
                file.WriteLine("Done");
                file.Close();
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