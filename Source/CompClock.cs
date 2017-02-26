using Verse;

namespace MoreTimeInfo
{
    public class CompClock : ThingComp
    {
        public CompProperties_Clock clock
        {
            get
            {
                return (CompProperties_Clock)props;
            }
        }
        public int clockAccuracy
        {
            get
            {
                return clock.clockAccuracyInt;
            }
        }
        public override void PostSpawnSetup()
        {
            base.PostSpawnSetup();
        }
        public override void CompTick()
        {
            base.CompTick();
            DateReadoutAdvanced.update();
        }
    }
}
