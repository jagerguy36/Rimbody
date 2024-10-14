using HarmonyLib;
using System;
using System.Reflection;
using Verse;
using static System.Net.Mime.MediaTypeNames;

namespace Maux36.Rimbody
{
    public class Rimbody : Mod
    {
        public static bool BodyChangeActive;
        public static bool StaminaActive;
        public static bool IndividualityLoaded;

        public Rimbody(ModContentPack content) : base(content)
        {
            if (ModsConfig.IsActive("erdelf.humanoidalienraces"))
            {
                HARCompat.Activate();
                HARCompat.Active = true;
            }

            if (ModsConfig.IsActive("mlie.syrindividuality"))
            {
                IndividualityLoaded = true;
            }
        }
    }

}
