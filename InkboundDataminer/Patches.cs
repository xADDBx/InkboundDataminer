//using MonoMod.RuntimeDetour;
using MonoMod;
using ShinyShoe;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using static ShinyShoe.StatCategoryVisualData;

namespace InkboundDataminer {
    public static class Patches {
        [MonoModPatch("global::ShinyShoe.StatCategoryVisualData.StatCategoryEntryData")]
        public class StatCategoryEntryDataPatch : StatCategoryEntryData {
            public static IReadOnlyList<StatCategoryEntryData> GetStatCategories() {
                foreach (var stat in __instance.statCategories) {
                    stat.hideEntriesIfZero = false;
                    stat.onlyShowInHud = false;
                }
                return __instance.statCategories;
            }
        }
        public static void Patch() {
            try {
                //var newHook = new Hook(typeof(StatCategoryVisualData).GetMethod(nameof(StatCategoryVisualData.GetStatCategories), BindingFlags.Instance | BindingFlags.Public),
                //typeof(Patches).GetMethod(nameof(Patches.GetStatCategories), BindingFlags.Static | BindingFlags.Public));
            } catch (Exception ex) {
                Doorstop.Entrypoint.error.WriteLine(ex.ToString());
            }
        }
    }
}
