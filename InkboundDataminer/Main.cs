using Newtonsoft.Json;
using ShinyShoe;
using ShinyShoe.Ares.Networking;
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
        public static IList createList(Type myType) {
            Type genericListType = typeof(List<>).MakeGenericType(myType);
            return (IList)Activator.CreateInstance(genericListType);
        }
        public static void runnable() {
            var p = "InkboundDataminer" + Path.DirectorySeparatorChar;
            error = new StreamWriter(p + "error.log");
            info = new StreamWriter(p + "info.log");
            try {
                int i = 0;
                while (ClientApp.Inst?._applicationState?.AssetLibrary == null) {
                    createFileAt(p + "startup.log", $"Is null 1.{i}!");
                    i++;
                    Thread.Sleep(500);
                }
                var assetLib = ClientApp.Inst._applicationState.AssetLibrary;
                GlobalGameData data;
                i = 0;
                while (true) {
                    try {
                        data = assetLib.GetOrLoadGlobalGameData();
                        break;
                    } catch {
                        createFileAt(p + "startup.log", $"Is null 2.{i}!");
                        i++;
                        Thread.Sleep(500);
                    }
                }
                var settings = new JsonSerializerSettings() {
                    _referenceLoopHandling = ReferenceLoopHandling.Ignore
                };
                string json;
                var pa = p + "Client" + Path.DirectorySeparatorChar;
                assetLib = new AssetLibraryClientStandalone();
                assetLib.Initialize();
                assetLib.LoadAll();
                dumpAssetLib(pa, assetLib);
                pa = p + "Server" + Path.DirectorySeparatorChar;
                assetLib = new AssetLibraryServerDesktop();
                assetLib.Initialize();
                assetLib.LoadAll();
                dumpAssetLib(pa, assetLib);
                json = JsonConvert.SerializeObject(ClientApp.Inst?._applicationState.WorldClient, Formatting.Indented, settings);
                createFileAt(p + "WorldClient.json", json);

            } catch (Exception e) {
                error.Write(e.ToString());
                error.WriteLine();
                error.Flush();
            }
            error.Close();
        }
        public static void dumpAssetLib(string pa, AssetLibrary assetLib, bool shortDump = true) {
            var settings = new JsonSerializerSettings() {
                _referenceLoopHandling = ReferenceLoopHandling.Ignore
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
                    var path = $"{pa}{type}{Path.DirectorySeparatorChar}{itemToName[item]}.json".Replace("ShinyShoe.Ares.SharedSOs.", "");
                    try {
                        if (item == null) continue;
                        if (shortDump) {
                            if (File.Exists(path)) continue;
                            if (path.EndsWith("Graph.json")) {
                                json = "Short Dump is activated so Graphs are not dumped.";
                                createFileAt(path, json);
                                continue;
                            }
                        }
                        json = JsonConvert.SerializeObject(Convert.ChangeType(item, type) ?? item, Formatting.Indented, settings);
                        createFileAt(path, json);
                    } catch (Exception ex) {
                        if (path.Length > 259) {
                            error.WriteLine("Filename too long!");
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