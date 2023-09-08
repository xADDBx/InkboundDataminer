using HarmonyLib;
using InkboundDataminer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ShinyShoe;
using ShinyShoe.Ares;
using ShinyShoe.Ares.SharedSOs;
using ShinyShoe.SharedDataLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Doorstop {
    class Entrypoint {
        public static StreamWriter error;
        public static StreamWriter info;
        public static void Start() {
            Task.Run(runnable);
        }
        public static void runnable() {
            var p = "Miner" + Path.DirectorySeparatorChar;
            error = new StreamWriter(p + "error.log");
            info = new StreamWriter(p + "info.log");
            var harmony = new Harmony("ADDB.InkboundDataminer");
            while (true) {
                try {
                    var original = typeof(StatCategoryVisualData).GetMethod(nameof(StatCategoryVisualData.GetStatCategories));
                    if (original != null) {
                        var prefix = typeof(StatCategoryVisualDataPatch).GetMethod(nameof(StatCategoryVisualDataPatch.Prefix));
                        harmony.Patch(original, new HarmonyMethod(prefix));
                        break;
                    } else {
                        info.WriteLine($"{new DateTimeOffset(DateTime.UtcNow)}: Patch Target not found, delaying.");
                        Thread.Sleep(100);
                    }
                } catch (Exception e) {
                    error.Write(e.ToString());
                    error.WriteLine();
                    error.Flush();
                    break;
                }
            }
            error.Close();
            info.Close();
        }
        public static class StatCategoryVisualDataPatch {
            public static void Prefix(StatCategoryVisualData __instance) {
                foreach (var stat in __instance.statCategories) {
                    stat.onlyShowInHud = false;
                    stat.hideEntriesIfZero = false;
                }
            }
        }
    }
}