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

        private static Quadrum dateStringQuadrum;
        
        private static readonly List<string> fastHourStrings = new List<string>();

        public static Rect DateRect;

        private static readonly List<string> minuteString = new List<string>();

        public static int minute;

        public static int second;

        public static float x = 0;

        public static Vector2 location;

        private static int index;

        public static string hrtime;

        public static int clockAccuracy = -1;

        public static string currentMin;

        static DateReadoutAdvanced()
        {
            dateStringDay = -1;
            dateStringSeason = Season.Undefined;
            dateStringQuadrum = Quadrum.Undefined;
            dateStringYear = -1;
            fastHourStrings = new List<string>();
            Reset();
        }

        /// <summary>
        /// This must be called every tick. See CompClock.CompTick().
        /// </summary>
        public static void Update()
        {
            if (fastHourStrings.Count != 24 || minuteString.Count != 60) Reset();
            minute = (int)Math.Floor((decimal)(Find.TickManager.TicksAbs) / 2500 * 60) % 60;
            second = (int)Math.Floor((decimal)(Find.TickManager.TicksAbs) / 2500 * 3600) % 60;
            index = GenDate.HourInteger(Find.TickManager.TicksAbs, location.x);
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
                            clockaccuracy = Mathf.Max(b.GetComp<CompClock>().ClockAccuracy, clockaccuracy);
                        }

                    }
                }
            }
            clockAccuracy = clockaccuracy;
            return buildings;
        }

        public static bool PrefixOld(Rect dateRect)
        {
            if (Find.VisibleMap == null || GetClocks(Find.VisibleMap).Count() == 0) return true; //use original method
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
            int index = GenDate.HourInteger(Find.TickManager.TicksAbs, x);
            DateReadoutAdvanced.index = index;
            int num = GenDate.DayOfSeason(Find.TickManager.TicksAbs, x);
            Season season = GenDate.Season(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.VisibleMap.Tile));
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
                dateString = GenDate.DateReadoutStringAt(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.VisibleMap.Tile));
                dateStringDay = num;
                dateStringSeason = season;
                dateStringYear = num2;
            }
            Widgets.Label(rect, dateString);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendLine("DateReadoutTip".Translate(GenDate.DaysPassed, 15, season.Label(), GenDate.Quadrum(Find.TickManager.TicksAbs, Find.WorldGrid.LongLatOf(Find.VisibleMap.Tile).y)));
            if(clockAccuracy > 0)stringBuilder.AppendLine("TicksAbsOnGUI".Translate(Find.TickManager.TicksGame));
            TooltipHandler.TipRegion(dateRect, new TipSignal(() => stringBuilder.ToString(), 86423));
            return false; //Original method disabled
        }


        /// <summary>
        /// Patched method. Only destructive if there are clocks on the map.
        /// </summary>
        [HarmonyPriority(Priority.Last)]
        public static bool Prefix(Rect dateRect)
        {
            //--------------------------------------------
            if (GetClocks(Find.VisibleMap).Count() == 0) return true; //use original method
            if (clockAccuracy < 0)
            {
                Log.Warning("Found clock but clockAccuracy not set. Setting to Analog.");
                clockAccuracy = 0;
            }
            //

            //Vector2 location;
            //In event of transpiler just get local variable at end
            if (WorldRendererUtility.WorldRenderedNow && Find.WorldSelector.selectedTile >= 0)
            {
                location = Find.WorldGrid.LongLatOf(Find.WorldSelector.selectedTile);
            }
            else if (WorldRendererUtility.WorldRenderedNow && Find.WorldSelector.NumSelectedObjects > 0)
            {
                location = Find.WorldGrid.LongLatOf(Find.WorldSelector.FirstSelectedObject.Tile);
            }
            else
            {
                if (Find.VisibleMap == null)
                {
                    return false;
                }
                location = Find.WorldGrid.LongLatOf(Find.VisibleMap.Tile);
            }
            index = GenDate.HourInteger(Find.TickManager.TicksAbs, location.x);
            int num = GenDate.DayOfTwelfth(Find.TickManager.TicksAbs, location.x);
            Season season = GenDate.Season(Find.TickManager.TicksAbs, location);
            Quadrum quadrum = GenDate.Quadrum(Find.TickManager.TicksAbs, location.x);
            int num2 = GenDate.Year(Find.TickManager.TicksAbs, location.x);
            if (num != dateStringDay || season != dateStringSeason || quadrum != dateStringQuadrum || num2 != dateStringYear)
            {
                dateString = GenDate.DateReadoutStringAt(Find.TickManager.TicksAbs, location);
                dateStringDay = num;
                dateStringSeason = season;
                dateStringQuadrum = quadrum;
                dateStringYear = num2;
            }
            Text.Font = GameFont.Small;
            float num3 = Mathf.Max(Text.CalcSize(fastHourStrings[index]).x, Text.CalcSize(dateString).x + 7f);
            dateRect.xMin = dateRect.xMax - num3;
            if (Mouse.IsOver(dateRect))
            {
                Widgets.DrawHighlight(dateRect);
            }
            GUI.BeginGroup(dateRect);
            Text.Font = GameFont.Small;
            Text.Anchor = TextAnchor.UpperRight;
            Rect rect = dateRect.AtZero();
            rect.xMax -= 7f;
            //-----------------------------------------------------
            hrtime = (index < 12) ? "amannotation".Translate(fastHourStrings[index].ToString() + currentMin) : "pmannotation".Translate(fastHourStrings[index].ToString() + currentMin);
            Widgets.Label(rect, hrtime);
            //
            //Widgets.Label(rect, fastHourStrings[index]); REPLACED
            rect.yMin += 26f;
            Widgets.Label(rect, dateString);
            Text.Anchor = TextAnchor.UpperLeft;
            GUI.EndGroup();
            //Remember - this delegate in itself is its own local method (m_XXX)
            //IL code: ldftn instance string RimWorld.DateReadout/'<DateOnGUI>c__AnonStorey449'::'<>m__64B'()
            TooltipHandler.TipRegion(dateRect, new TipSignal(delegate
            {
                StringBuilder stringBuilder = new StringBuilder();
                for (int i = 0; i < 4; i++)
                {
                    Quadrum quadrum2 = (Quadrum)i;
                    stringBuilder.AppendLine(quadrum2.Label() + " - " + quadrum2.GetSeason(location.y).LabelCap());
                }
                return "DateReadoutTip".Translate(new object[]
                {
            GenDate.DaysPassed,
            15,
            season.LabelCap(),
            15,
            GenDate.Quadrum((long)GenTicks.TicksAbs, location.x).Label(),
            stringBuilder.ToString()
                })
                //-------------------------------------------
                + (clockAccuracy > 0 ? "TicksAbsOnGUI".Translate(Find.TickManager.TicksGame) : string.Empty)
                //
                ;
            }, 86423));
            return false;
        }
    }
}
