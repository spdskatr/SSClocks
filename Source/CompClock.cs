using Verse;

namespace MoreTimeInfo
{
    public class CompClock : ThingComp
    {
        public CompProperties_Clock Clock
        {
            get
            {
                return (CompProperties_Clock)props;
            }
        }
        public int ClockAccuracy
        {
            get
            {
                return Clock.ClockAccuracyInt;
            }
        }
        public override void CompTick()
        {
            base.CompTick();
            DateReadoutAdvanced.Update();
        }
    }
}
