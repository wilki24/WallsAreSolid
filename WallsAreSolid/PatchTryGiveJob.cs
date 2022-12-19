using HarmonyLib;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Reflection;
using Verse.AI;
using RimWorld;
using Verse;
using System;

namespace WallsAreSolid
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var harmony = new Harmony("com.wilki24.rimworld.mod.wallsaresolid");
            try
            {
                harmony.PatchAll();
            }
            catch (Exception ex)
            {
                var error = ex.ToString();
                Log.Error(error);
            }
        }
    }

    [HarmonyPatch(typeof(JobGiver_ExitMap), nameof(JobGiver_ExitMap.TryGiveJob))]
    internal class PatchTryGiveJob
    {
        /*
         * This is the method call that we want to modify so that it passes false instead of true, twice
          // [49 13 - 49 87]
          IL_00b9: ldarg.1      // pawn
          IL_00ba: ldloc.s      blocker
          IL_00bc: ldloc.3      // cellBefore
          IL_00bd: ldc.i4.1 <- change to ldc.i4.0
          IL_00be: ldc.i4.1 <- change to ldc.i4.0
          IL_00bf: call         class Verse.AI.Job RimWorld.DigUtility::PassBlockerJob(class Verse.Pawn, class Verse.Thing, valuetype Verse.IntVec3, bool, bool)
          IL_00c4: stloc.s      job
        */

        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, ILGenerator ilgen)
        {
            var codes = instructions.ToList();
            MethodInfo passBlockerJobMI = AccessTools.Method(typeof(DigUtility), nameof(DigUtility.PassBlockerJob));
            var found = false;

            for (int i = 0; i < codes.Count; i++)
            {
                CodeInstruction code = codes[i];

                if (code.Calls(passBlockerJobMI) &&
                    codes[i - 1].opcode == OpCodes.Ldc_I4_1
                    && codes[i - 2].opcode == OpCodes.Ldc_I4_1)
                {
                    // Change both params to false.
                    codes[i - 1].opcode = OpCodes.Ldc_I4_0;
                    codes[i - 2].opcode = OpCodes.Ldc_I4_0;
                    Log.Message("Walls are Solid - transpiler patched successfully.");
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                Log.Error("2Walls are Solid - transpiler could not find a code match. Please leave a comment on the steam workshop page.");
            }

            return codes.AsEnumerable();
        }
    }
}
