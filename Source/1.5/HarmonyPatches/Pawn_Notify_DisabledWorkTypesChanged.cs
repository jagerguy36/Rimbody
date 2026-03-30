using HarmonyLib;
using Verse;

namespace Maux36.Rimbody
{
    [HarmonyPatch(typeof(TraitSet), "GainTrait")]
    public static class TraitSet_GainTrait_Patch
    {
        static void Postfix(Pawn ___pawn)
        {
            var compPhysique = ___pawn.compPhysique();
            compPhysique?.DirtyTraitCache();
        }
    }

    [HarmonyPatch(typeof(TraitSet), "RemoveTrait")]
    public static class TraitSet_RemoveTrait_Patch
    {
        static void Postfix(Pawn ___pawn)
        {
            var compPhysique = ___pawn.compPhysique();
            compPhysique?.DirtyTraitCache();
        }
    }

    public class CharEditorPatches
    {
        [HarmonyPatch]
        public static class CE_AddTrait_Patch
        {
            public static bool Prepare()
            {
                if (ModsConfig.IsActive("void.charactereditor"))
                    return true;
                return false;
            }
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("CharacterEditor.TraitTool");
                return AccessTools.Method(type, "AddTrait");
            }
            public static void Postfix(Pawn pawn)
            {
                var compPhysique = pawn.compPhysique();
                compPhysique?.DirtyTraitCache();
            }
        }


        [HarmonyPatch]
        public static class CE_RemoveTrait_Patch
        {
            public static bool Prepare()
            {
                if (ModsConfig.IsActive("void.charactereditor"))
                    return true;
                return false;
            }
            static MethodBase TargetMethod()
            {
                var type = AccessTools.TypeByName("CharacterEditor.TraitTool");
                return AccessTools.Method(type, "RemoveTrait");
            }

            public static void Postfix(Pawn pawn, Trait t)
            {
                var compPhysique = pawn.compPhysique();
                compPhysique?.DirtyTraitCache(t.def);
            }
        }
    }
}
