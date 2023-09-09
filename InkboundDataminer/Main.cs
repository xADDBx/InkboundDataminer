using ShinyShoe;
using ShinyShoe.Ares;
using ShinyShoe.EcsEventSystem;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
            error.AutoFlush = true;
            info.AutoFlush = true;
            try {
                // Delaying stuff until game loads.
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
                IScreenVisual vis;
                IScreenVisual vis2;
                while (true) {
                    ComponentsUtil.GetCompsFromPool<ScreenSystem.Components>(ClientApp.Inst._applicationState, out var comps);
                    IScreen sds = comps?.screens?.FindScreen(ScreenName.StatDetails);
                    IScreen sds2 = comps?.screens?.FindScreen(ScreenName.StatHud);
                    vis = sds?.GetVisual();
                    vis2 = sds2?.GetVisual();
                    if (vis != null && vis2 != null) {
                        break;
                    } else {
                        Thread.Sleep(500);
                    }
                }
                var sdsv = vis as StatDetailsScreenVisual;
                var shsv = vis2 as StatHudScreenVisual;
                // Stats show even if 0
                foreach (var statcategory in sdsv.statCategoryVisualData.statCategories) {
                    statcategory.onlyShowInHud = false;
                    statcategory.hideEntriesIfZero = false;
                }
                ComponentsUtil.GetCompsFromPool<StatDetailsScreen.Components>(ClientApp.Inst._applicationState, out var comps2);
                // ComponentsUtil.GetCompsFromPool<StatHudScreen.Components>(ClientApp.Inst._applicationState, out var comps3);
                // This needs to be called for the change to take effect properly
                addMissingStats(sdsv, comps2.networkRo.LocalEntityHandleInWorld, comps2.unitCombatDbRo, comps2.clientAssetDb, comps2.events.GetEventDB());
                // addMissingStats(shsv, comps3.networkRo.LocalEntityHandleInWorld, comps3.unitCombatDbRo, comps3.clientAssetDb, comps3.events.GetEventDB());
            } catch (Exception e) {
                error.Write(e.ToString());
                error.WriteLine();
            }
            info.WriteLine("Finished");
            error.Close();
            info.Close();
        }
        public static void addMissingStats(StatHudScreenVisual shsv, EntityHandle playerEntityHandle, UnitCombatDB.IReadonly unitCombatDbRo, ClientAssetDB clientAssetDb, EventDB eventDb) {
            foreach (var statCategory in shsv.statCategoryVisualData.GetStatCategories()) {
                foreach (StatData statData in statCategory.GetStatDatas()) {
                    try {
                        info.WriteLine($"Considering stat {statData.name}");
                        var doneStats = shsv.prefabPoolingHelper.GetInstances();
                        if (doneStats.Any(s => s.StatGuid == statData.Guid)) continue;
                        info.WriteLine($"Handling stat {statData.name}");
                        int functionalStat = unitCombatDbRo.GetFunctionalStat(playerEntityHandle, statData.Guid);
                        info.WriteLine("BeforePrefab");
                        // Produces error!
                        StatEntryUI statEntryUI = shsv.prefabPoolingHelper.InstantiateNew(shsv.parent);
                        info.WriteLine("AfterPrefab");
                        statEntryUI.Initialize(statData, playerEntityHandle, clientAssetDb, eventDb);
                        statEntryUI.SetValue((long)functionalStat);
                        info.WriteLine($"Handled stat {statData.name}");
                    } catch (Exception ex) {
                        error.WriteLine(ex.ToString());
                    }
                }
            }
            shsv.RefreshSizing();
        }

        public static void addMissingStats(StatDetailsScreenVisual sdsv, EntityHandle playerEntityHandle, UnitCombatDB.IReadonly unitCombatDbRo, ClientAssetDB clientAssetDb, EventDB eventDb) {
            var statCategories = sdsv.statCategoryVisualData.statCategories;
            for (int i = 0; i < statCategories.Count; i++) {
                StatCategoryVisualData.StatCategoryEntryData statCategoryEntryData = statCategories[i];
                if (statCategoryEntryData.OnlyShowInHud) {
                    StatCategoryUI ui = sdsv.prefabPoolingHelper.InstantiateNewCategory(sdsv.statsContainer);
                    ui.Initialize(statCategoryEntryData);
                    RectTransform childEntriesRectTransform = ui.GetChildEntriesRectTransform();
                    foreach (StatData statData in statCategoryEntryData.GetStatDatas()) {
                        int functionalStat = unitCombatDbRo.GetFunctionalStat(playerEntityHandle, statData.Guid);
                        StatEntryUI statEntryUI = sdsv.prefabPoolingHelper.InstantiateNewItem(childEntriesRectTransform);
                        StatEntryUI statEntryUI2 = statEntryUI;
                        void action(string guid) {
                            Action<string, Vector3> onStatHovered = sdsv.OnStatHovered;
                            if (onStatHovered == null) {
                                return;
                            }
                            onStatHovered(guid, ui.TooltipPos);
                        }

                        statEntryUI2.OnHover = action;
                        statEntryUI.OnUnhover = sdsv.OnStatUnhovered;
                        statEntryUI.Initialize(statData, playerEntityHandle, clientAssetDb, eventDb);
                        statEntryUI.SetValue((long)functionalStat);
                        sdsv.guidToEntryMap.Add(statData.Guid, statEntryUI);
                    }
                }
            }
            sdsv.RefreshSizing();
        }
    }
}