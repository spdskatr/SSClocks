using System.Reflection;
using Verse;
using Harmony;

namespace SSDetours
{
    [StaticConstructorOnStartup]
    public static class DetourInjector
    {

        static DetourInjector()
        {
            LongEventHandler.QueueLongEvent(Inject, "LibraryStartup", false, null);
        }

        public static void Inject()
        {
            string Info = "SS Clocks information for modders(not an error): This mod uses harmony(https://github.com/pardeike/Harmony) patches on the following methods:\nRimWorld.DateReadout.DateONGUI() with patch type Prefix (semi-destructive)\n\n======Stack trace below======";
            HarmonyInstance harmony = HarmonyInstance.Create("com.spdskatr.moretimeinfo.detours");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log.Message(Info);
        }
    }
}