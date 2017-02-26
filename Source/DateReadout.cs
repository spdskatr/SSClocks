using Harmony;
using RimWorld;
using RimWorld.Planet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace MoreTimeInfo
{
    //The messiest class in RimWorld history.
    [HarmonyPatch (typeof(DateReadout)), HarmonyPatch ("DateOnGUI"), HarmonyPatch (new Type[] { typeof(Rect) })]
    public static class DateReadoutAdvanced
    {
        private static string dateString;

        private static int dateStringDay;

        private static Season dateStringSeason;

        private static int dateStringYear;
        
        private static readonly List<string> fastHourStrings = new List<string>();

        public static Rect DateRect;

        private static readonly List<string> minuteString = new List<string>();

        public static int minute;

        public static int second;

        public static float x = 0;

        private static int index;

        public static string hrtime;

        public static int clockAccuracy = -1;

        public static string currentMin;

        /// <summary>
        /// This must be called every tick. See CompClock.CompTick().
        /// </summary>
        public static void update()
        {
            if (fastHourStrings.Count != 24 || minuteString.Count != 60) Reset();
            minute = (int)Math.Floor((decimal)(Find.TickManager.TicksAbs) / 2500 * 60) % 60;
            second = (int)Math.Floor((decimal)(Find.TickManager.TicksAbs) / 2500 * 3600) % 60;
            index = GenDate.HourInt(Find.TickManager.TicksAbs, x);
            currentMin = (clockAccuracy > 0) ? minuteString[minute] + ":" + minuteString[second] : minuteString[minute] ;
        }

        /// <summary>
        /// Resets all timekeeping lists.
        /// </summary>
        public static void Reset()
        {
            dateString = null;
            dateStringDay = -1;
            dateStringSeason = Season.Undefined;
            dateStringYear = -1;
            fastHourStrings.Clear();
            minuteString.Clear();
            fastHourStrings.Add( 12 + ":");
            for (int i = 1; i < 12; i++)
            {
                fastHourStrings.Add(i + ":");
            }
            fastHourStrings.Add(12 + ":");
            for (int i = 1; i < 12; i++)
            {
                fastHourStrings.Add(i + ":");
            }
            for (int i = 0; i < 60; i++)
            {
                string istr;
                if (i < 10) istr = "0" + i.ToString();
                else istr = i.ToString();
                minuteString.Add(istr);
            }
        }
        
        /// <summary>
        /// Returns all powered buildings with the clock comp within the specified map and also records their clock accuracy. 
        /// </summary>
        public static List<Building> GetClocks(Map map)
        {
            List<Building> buildings = new List<Building>();
            if (map == null) return buildings;
            int clockaccuracy = -1;
            foreach (Building b in map.listerBuildings.allBuildingsColonist)
            {
                if (b.GetComp<CompClock>() != null)
                {
                    if (b.GetComp<CompPowerTrader>().PowerOn)
                    {
                        foreach (IntVec3 p in GenAdj.CellsOccupiedBy(b))
                        {
                            if (!map.areaManager.Home[p])
                            {
                                continue;
                            }
                            buildings.Add(b);
                            clockaccuracy = Mathf.Max(b.GetComp<CompClock>().clockAccuracy, clockaccuracy);
                        }

                    }
                }
            }
            clockAccuracy = clockaccuracy;
            return buildings;
        }

        /// <summary>
        /// Patched method. Only destructive if there are clocks on the map.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Rect dateRect)
        {
            Reset();
            if (GetClocks(Find.VisibleMap).Count() == 0) return true; //use original method
            if (clockAccuracy < 0)
            {
                Log.Warning("Found clock but clockAccuracy not set. Setting to Analog.");
                clockAccuracy = 0;
            }
            DateRect = dateRect;
            if (WorldRendererUtility.WorldRenderedNow && Find.WorldSelector.selectedTile >= 0)
            {
                x = Find.WorldGrid.LongLatOf(Find.WorldSelector.selectedTile).x;
            }
            else if (WorldRendererUtility.WorldRenderedNow && Find.WorldSelector.NumSelectedObjects > 0)
            {
                x = Find.WorldGrid.LongLatOf(Find.WorldSelector.FirstSelectedObject.Tile).x;
            }
            else
            {
                if (Find.VisibleMap == null)
                {
                    return false;
                }
                x = Find.WorldGrid.LongLatOf(Find.VisibleMap.Tile).x;
            }
            int index = GenDate.HourInt(Find.TickManager.TicksAbs, x);
            DateReadoutAdvanced.index = index;
            int num = GenDate.DayOfMonth(Find.TickManager.TicksAbs, x);
            Season season = GenDate.Season(Find.TickManager.TicksAbs, x);
            int num2 = GenDate.Year(Find.TickManager.TicksAbs, x);
            if (Mouse.IsOver(dateRect))
            {
                Widgets.DrawHighlight(dateRect);
            }
            GUI.BeginGroup(dateRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperRight;
            Rect rect = dateRect.AtZero();
            rect.xMax -= 7f;
            hrtime = (index < 12) ? "amannotation".Translate(fastHourStrings[index].ToString() + currentMin) : "pmannotation".Translate(fastHourStrings[index].ToString() + currentMin);
            Widgets.Label(rect, hrtime);
            rect.yMin += 26f;
            if (num != dateStringDay || season != dateStringSeason || num2 != dateStringYear)
            {
                dateString = GenDate.DateReadoutStringAt(Find.TickManager.TicksAbs, x);
                dateStringDay = num;
                dateStringSeason = season;
                dateStringYear = num2;
            }
            Widgets.Label(rect, dateString);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("DateReadoutTip".Translate(GenDate.DaysPassed, 15, season.Label()));
            if(clockAccuracy > 0)stringBuilder.AppendLine("TicksAbsOnGUI".Translate(Find.TickManager.TicksGame));
            TooltipHandler.TipRegion(dateRect, new TipSignal(() => stringBuilder.ToString(), 86423));
            return false; //Original method disabled
        }
    }
}
