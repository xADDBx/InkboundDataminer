using ShinyShoe;
using ShinyShoe.Ares;
using ShinyShoe.SharedDataLoader;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace Doorstop {
    class Entrypoint {
        public static StreamWriter error;
        public static StreamWriter info;
        public static void Main() {
            Task.Run(() => {
                var ret = Task.Run(runnable);
                ret.Wait();
                error.WriteLine(ret.Exception.ToString());
            });
        }
        public static void Start() {
            var p = "Miner" + Path.DirectorySeparatorChar;
            error = new StreamWriter(p + "error.log");
            info = new StreamWriter(p + "info.log");
            error.AutoFlush = true;
            info.AutoFlush = true;
            try {
                Main();
            } catch (Exception ex) {
                error.WriteLine(ex.ToString());
            }
        }
        public static void runnable() {
            var p = "Miner" + Path.DirectorySeparatorChar;
            try {
                InkboundDataminer.Patches.Patch();
                /*// Delaying stuff until game loads.
                // Very elegant if I do say so myself.
                while (ClientApp.Inst?._applicationState?.AssetLibrary == null) {
                    Thread.Sleep(500);
                }
                var assetLib = ClientApp.Inst._applicationState.AssetLibrary;
                while (true) {
                    try {
                        assetLib.GetOrLoadGlobalGameData();
                        break;
                    } catch {
                        Thread.Sleep(500);
                    }
                }
                // Game should've started most systems here
                dumpSeasonRewards(assetLib);
                //dumpWorldClient(p);
                dumpDifficulties();
                //dumpAssetLibs(p);*/
                info.WriteLine("Finished Dumping everything");
            } catch (Exception e) {
                error.Write(e.ToString());
                error.WriteLine();
            }
            error.Close();
            info.Close();
        }
        public static void dumpDifficulties() {
            ComponentsUtil.GetCompsFromPool<LoadWorldSystem.Components>(ClientApp.Inst._applicationState, out var comps);
            var ggd = comps.globalGameData;

            var difficultyLog = new StreamWriter("Miner" + Path.DirectorySeparatorChar + "difficulty.log");
            difficultyLog.AutoFlush = true;
            while (ggd.rankedDifficultyData.difficultyLevelData[3].TierNumber <= 0) {
                info.WriteLine("Delay because difficulties are null");
                Thread.Sleep(200);
            }
            difficultyLog.WriteLine("-----------------------------------");
            difficultyLog.WriteLine("difficultyLevelData");
            difficultyLog.WriteLine("-----------------------------------");
            foreach (var tier in ggd.rankedDifficultyData.difficultyLevelData) {
                difficultyLog.WriteLine(tier.name + ": " + tier.TierNumber);
                difficultyLog.WriteLine("Mutators:");
                var whitespace = "        ";
                foreach (var mutator in tier.availableBookMutators.stageMutators) {
                    difficultyLog.WriteLine(whitespace + $"{mutator.name}: {mutator.glyphsGainedOnSelection} Glyphs, {mutator.runCurrencyGainedOnSelection}");
                }
                difficultyLog.WriteLine(tier.ToString());
                difficultyLog.WriteLine("-----------------------------------");
            }
            difficultyLog.WriteLine("-----------------------------------");
            difficultyLog.WriteLine("difficultyScalingData");
            difficultyLog.WriteLine("-----------------------------------");
            foreach (var tier in ggd.rankedRunConfigurationData.difficultyScalingData) {
                difficultyLog.WriteLine(tier.tier + ":");
                foreach (var statusses in tier.statusEffectsToApplyToEnemies) {
                    var whitespace = "        ";
                    difficultyLog.WriteLine(whitespace + statusses.statusEffectData?.name);
                }
                difficultyLog.WriteLine(tier.ToString());
                difficultyLog.WriteLine("-----------------------------------");
            }
        }
        public static void dumpSeasonRewards(AssetLibrary assetLib) {
            var seasonLog = new StreamWriter("Miner" + Path.DirectorySeparatorChar + "season.log");
            seasonLog.AutoFlush = true;
            foreach (var season in SeasonHelper.GetAllSeasonDatas(assetLib)) {
                seasonLog.WriteLine("-----------------------------------");
                seasonLog.WriteLine(season.Name);
                seasonLog.WriteLine("-----------------------------------");
                seasonLog.WriteLine("Victory Board Rewards");
                void dumpSeasonReward(ShinyShoe.Ares.SharedSOs.SeasonRewardData r) {
                    if (r == null) return;
                    var whitespace = "        ";
                    var currencyPrefix = (r.premiumCurrencyCode == 2001) ? "Shinies" : $"Currency {r.premiumCurrencyCode}";
                    if (r.currencyAmount > 0)
                        seasonLog.WriteLine(whitespace + currencyPrefix + r.currencyAmount);
                    if (r.cosmeticData != null)
                        seasonLog.WriteLine(whitespace + r.cosmeticData);
                    if (r.cosmeticBundleData != null)
                        seasonLog.WriteLine(whitespace + r.cosmeticBundleData);
                    if (r.trinketCurrencyAmount > 0)
                        seasonLog.WriteLine(whitespace + r.trinketCurrencyAmount);
                    if (r.equipmentData != null)
                        seasonLog.WriteLine(whitespace + r.equipmentData);
                }
                foreach (var reward in season.seasonAchievementRewardListData.seasonRewardsForAchievementLevel) {
                    seasonLog.WriteLine($"Level: {reward.level}: ");
                    dumpSeasonReward(reward.reward);
                }
                seasonLog.WriteLine("Battlepass Rewards");
                foreach (var reward in season.seasonRewardListData.seasonRewardsForLevel) {
                    seasonLog.WriteLine($"Level: {reward.level}: ");
                    seasonLog.WriteLine("Free:");
                    dumpSeasonReward(reward.freeReward);
                    seasonLog.WriteLine("Premium:");
                    dumpSeasonReward(reward.premiumReward);
                }
                seasonLog.WriteLine("Progress Rewards");
                var itemUnlockRewardListData = assetLib.GetOrLoadGlobalGameData().GetItemUnlockRewardListData();
                ComponentsUtil.GetCompsFromPool<LogbookScreen.Components>(ClientApp.Inst._applicationState, out var comps);
                for (int i = 0; i < itemUnlockRewardListData.ItemUnlockRewards.Count; i++) {
                    var seasonItemUnlockRewardsForLevel = itemUnlockRewardListData.ItemUnlockRewards[i];
                    var reward = seasonItemUnlockRewardsForLevel.GetReward();
                    if (reward != null) {
                        seasonLog.WriteLine($"Level: {seasonItemUnlockRewardsForLevel.levelUpCount}");
                        dumpSeasonReward(reward);
                    }
                }
            }
        }/*
        public static void dumpWorldClient(string p) {
            string pa = p + "WorldClient" + Path.DirectorySeparatorChar;
            if (Directory.Exists(pa)) {
                info.WriteLine("Skipped Dumping WorldClient as directory already exists");
                return;
            }
            info.WriteLine("Start Dumping WorldClient");
            string json;
            var settings = new JsonSerializerSettings() {
                ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
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
            var pa = p + "C" + Path.DirectorySeparatorChar;
            AssetLibrary assetLib = new AssetLibraryClientStandalone();
            assetLib.Initialize();
            assetLib.LoadAll();
            dumpAssetLib(pa, assetLib);
            info.WriteLine("Finished Dumping ClientStandalone AssetLib");
    }
    public static void dumpAssetLib(string pa, AssetLibrary assetLib, bool shortDump = true) {
        if (shortDump) {
            createFileAt(pa + "!!ShortDump.txt", "This dump is created with shortDump set to true, so all Graphs are skipped");
        }
        var settings = new JsonSerializerSettings() {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = new AllFieldsContractResolver()
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
                    }
                    //if (!ex.ToString().Contains("UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationHandle")) {
                    error.Write(ex.ToString());
                    error.WriteLine();
                    error.Flush();
                    //}
                }
            }
        }
    }*/
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
        public static IList createList(Type myType) {
            Type genericListType = typeof(List<>).MakeGenericType(myType);
            return (IList)Activator.CreateInstance(genericListType);
        }
    }
}