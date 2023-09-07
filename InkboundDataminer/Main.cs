using InkboundDataminer;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using ShinyShoe;
using ShinyShoe.Ares;
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
        public static IList createList(Type myType) {
            Type genericListType = typeof(List<>).MakeGenericType(myType);
            return (IList)Activator.CreateInstance(genericListType);
        }
        public static void runnable() {
            var p = "Miner" + Path.DirectorySeparatorChar;
            error = new StreamWriter(p + "error.log");
            info = new StreamWriter(p + "info.log");
            try {
                // Delaying stuff until game loads.
                // Very elegant if I do say so myself.
                int i = 0;
                while (ClientApp.Inst?._applicationState?.AssetLibrary == null) {
                    createFileAt(p + "startup.log", $"Waiting for game to start 1.{i}!");
                    i++;
                    Thread.Sleep(500);
                }
                var assetLib = ClientApp.Inst._applicationState.AssetLibrary;
                i = 0;
                while (true) {
                    try {
                        assetLib.GetOrLoadGlobalGameData();
                        break;
                    } catch {
                        createFileAt(p + "startup.log", $"Waiting for game to start 2.{i}!");
                        i++;
                        Thread.Sleep(500);
                    }
                }
                // Game should've started most systems here
                dumpWorldClient(p);
                dumpAssetLibs(p);
                info.WriteLine("Finished Dumping everything");
                info.Flush();
            } catch (Exception e) {
                error.Write(e.ToString());
                error.WriteLine();
                error.Flush();
            }
            error.Close();
            info.Close();
        }
        public static void dumpWorldClient(string p) {
            string pa = p + "WorldClient" + Path.DirectorySeparatorChar;
            if (Directory.Exists(pa)) {
                info.WriteLine("Skipped Dumping WorldClient as directory already exists");
                info.Flush();
                return;
            }
            info.WriteLine("Start Dumping WorldClient");
            info.Flush();
            string json;
            var settings = new JsonSerializerSettings() {
                _referenceLoopHandling = ReferenceLoopHandling.Ignore,
                ContractResolver = new AllFieldsContractResolver(),
                Converters = { new Vector3Converter() }
            };
            var wc = ClientApp.Inst?._applicationState.WorldClient;
            json = JsonConvert.SerializeObject(wc, Formatting.Indented, settings);
            createFileAt(pa + "WorldClient.json", json);
            json = JsonConvert.SerializeObject(wc.worldEngine, Formatting.Indented, settings);
            createFileAt(pa + "worldEngine.json", json);
            json = JsonConvert.SerializeObject(wc.navigation, Formatting.Indented, settings);
            createFileAt(pa + "navigation.json", json);
            json = JsonConvert.SerializeObject(wc.localEntHandle, Formatting.Indented, settings);
            createFileAt(pa + "localEntHandle.json", json);
            json = JsonConvert.SerializeObject(wc.serverWorldStateChanges, Formatting.Indented, settings);
            createFileAt(pa + "serverWorldStateChanges.json", json);
            json = JsonConvert.SerializeObject(wc.mostRecentInputStateAckByClient, Formatting.Indented, settings);
            createFileAt(pa + "mostRecentInputStateAckByClient.json", json);
            json = JsonConvert.SerializeObject(wc.mostRecentWorldStateFrameAckByClient, Formatting.Indented, settings);
            createFileAt(pa + "mostRecentWorldStateFrameAckByClient.json", json);
            json = JsonConvert.SerializeObject(wc.previousPredictedWorldState, Formatting.Indented, settings);
            createFileAt(pa + "previousPredictedWorldState.json", json);
            dumpWorldState(pa, wc.mostRecentProcessedServerWorldState, nameof(wc.mostRecentProcessedServerWorldState), settings);
            dumpWorldState(pa, wc.predictedWorldState, nameof(wc.predictedWorldState), settings);

            info.WriteLine("Finished Dumping WorldClient");
            info.Flush();
        }
        public static void dumpWorldState(string pa, WorldState ws, string name, JsonSerializerSettings settings) {
            pa += name + Path.DirectorySeparatorChar;
            foreach (var system in ws.GetSystems()) {
                string json = JsonConvert.SerializeObject(system, Formatting.Indented, settings);
                createFileAt(pa + system.ToString() + ".json", json);
            }
        }
        public static void dumpAssetLibs(string p) {
            info.WriteLine("Start Dumping ClientStandalone AssetLib");
            info.Flush();
            var pa = p + "C" + Path.DirectorySeparatorChar;
            AssetLibrary assetLib = new AssetLibraryClientStandalone();
            assetLib.Initialize();
            assetLib.LoadAll();
            dumpAssetLib(pa, assetLib);
            info.WriteLine("Finished Dumping ClientStandalone AssetLib");
            /*
            info.WriteLine("Start Dumping ServerDesktop AssetLib");
            info.Flush();
            pa = p + "D" + Path.DirectorySeparatorChar;
            assetLib = new AssetLibraryServerDesktop();
            assetLib.Initialize();
            assetLib.LoadAll();
            dumpAssetLib(pa, assetLib);
            info.WriteLine("Finished Dumping ServerDesktop AssetLib");
            info.WriteLine("Start Dumping Editor AssetLib");
            info.Flush();
            pa = p + "E" + Path.DirectorySeparatorChar;
            assetLib = new AssetLibraryEditor();
            assetLib.Initialize();
            assetLib.LoadAll();
            dumpAssetLib(pa, assetLib);
            info.WriteLine("Finished Dumping Editor AssetLib");
            info.WriteLine("Start Dumping ServerCloud AssetLib");
            info.Flush();
            pa = p + "C" + Path.DirectorySeparatorChar;
            assetLib = new AssetLibraryServerCloud();
            assetLib.Initialize();
            assetLib.LoadAll();
            dumpAssetLib(pa, assetLib);
            info.WriteLine("Finished Dumping ServerCloud AssetLib");
            */
            info.Flush();
        }
        public static void dumpAssetLib(string pa, AssetLibrary assetLib, bool shortDump = true) {
            if (shortDump) {
                createFileAt(pa + "!!ShortDump.txt", "This dump is created with shortDump set to true, so all Graphs are skipped");
            }
            var settings = new JsonSerializerSettings() {
                _referenceLoopHandling = ReferenceLoopHandling.Ignore,
                // If I uncomment this to also serialize private fields it'll crash at any items after "StageMutatorListData\\CustomStageMutators/PrologueMutators.json"
                // ContractResolver = new AllFieldsContractResolver()
            };
            string json;
            Dictionary<Type, List<object>> typeToList = new Dictionary<Type, List<object>>();
            Dictionary<object, string> itemToName = new Dictionary<object, string>();
            foreach (var asset in assetLib._assetIDToEntry.Values) {
                if (asset.asset == null) assetLib.LoadAsset(asset);
                if (asset.asset == null) continue;
                if (!typeToList.ContainsKey(asset.classType)) {
                    typeToList[asset.classType] = new List<object>();
                }
                typeToList[asset.classType].Add(asset.asset);
                itemToName[asset.asset] = asset.name ?? asset.assetID._guid ?? asset.dataId;
            }
            foreach (var type in typeToList.Keys) {
                foreach (var item in typeToList[type]) {
                    if (item == null) continue;
                    var path = $"{pa}{type}{Path.DirectorySeparatorChar}{itemToName[item]}.json".Replace("ShinyShoe.Ares.SharedSOs.", "");
                    if (shortDump) {
                        if (File.Exists(path)) continue;
                        if (path.Contains("Graph") || path.Contains("Nodes")) {
                            continue;
                        }
                    }
                    try {
                        json = JsonConvert.SerializeObject(item, type, Formatting.Indented, settings);
                        createFileAt(path, json);
                    } catch (Exception ex) {
                        if (path.Length > 259) {
                            error.WriteLine("Filename too long!");
                            error.Flush();
                        }
                        error.Write(ex.ToString());
                        error.WriteLine();
                        error.Flush();
                    }
                }
            }
        }
        public static void createFileAt(string path, string content) {
            info.WriteLine($"Creating file {path}");
            info.Flush();
            if (File.Exists(path)) {
                File.Delete(path);
            }
            var fi = new FileInfo(path);
            fi.Directory.Create();
            var wr = fi.CreateText();
            wr.Write(content);
            wr.Close();
        }
    }
}