using Terraria.ModLoader;
using Terraria;
using Harmony;
using System.Collections.Generic;
using System.Reflection.Emit;
using System.Reflection;
using System.IO;

namespace BetterManaRegen
{

    class BetterManaRegen : Mod
    {
        public static string ConfigPath = Path.Combine(Main.SavePath, "Mod Configs", "bettermanaregen.json");

        static BetterManaRegen()
        {
            var harmony = HarmonyInstance.Create("io.github.rypofalem.tmods.bettermanaregen");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(Player))]
    [HarmonyPatch("UpdateManaRegen")]
    public static class Player_UpdateManaRegen_Patcher
    {

        // We're looking for these instructions and we want to delete all but the first one
        // IL_0114: stfld     int32 Terraria.Player::manaRegen
        //
        // IL_0119: ldarg.0
        // IL_011A: ldflda    valuetype [Microsoft.Xna.Framework]Microsoft.Xna.Framework.Vector2 Terraria.Entity::velocity
        // IL_011F: ldfld     float32 [Microsoft.Xna.Framework]Microsoft.Xna.Framework.Vector2::X
        // IL_0124: ldc.r4    0.0
        // IL_0129: bne.un.s  IL_013D
        //
        // IL_012B: ldarg.0
        // IL_012C: ldflda    valuetype [Microsoft.Xna.Framework]Microsoft.Xna.Framework.Vector2 Terraria.Entity::velocity
        // IL_0131: ldfld     float32 [Microsoft.Xna.Framework]Microsoft.Xna.Framework.Vector2::Y
        // IL_0136: ldc.r4    0.0
        // IL_013B: beq.s     IL_0150
        //
        // IL_013D: ldarg.0
        // IL_013E: ldfld     int32[] Terraria.Player::grappling
        // IL_0143: ldc.i4.0
        // IL_0144: ldelem.i4
        // IL_0145: ldc.i4.0
        // IL_0146: bge.s     IL_0150
        //
        // IL_0148: ldarg.0
        // IL_0149: ldfld     bool Terraria.Player::manaRegenBuff
        // IL_014E: brfalse.s IL_0165
        static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var code = new List<CodeInstruction>(instructions);

            // look for three instructions and set deleteStart to the index of the second one
            int deleteStart = -1;
            int findCount = 0;
            for (int i = 0; i < code.Count - 2; i++) // -2 since we will be checking i + 2
            {
                if (code[i].opcode == OpCodes.Stfld && code[i].operand.Equals(typeof(Player).GetRuntimeField("manaRegen"))
                    && code[i + 1].opcode == OpCodes.Ldarg_0
                    && code[i + 2].opcode == OpCodes.Ldflda && code[i + 2].operand.Equals(typeof(Entity).GetRuntimeField("velocity"))
                    ) {
                    deleteStart = i + 1;
                    findCount++;
                }
            }

            // we were not able to find the instructions or we matched multiple instructions
            // either way, something went wrong and we won't modify any code today
            if (findCount != 1) return code;


            // remove the next 19 instructions
            for (int i = 0; i < 19; i++) {
                code.RemoveAt(deleteStart);
            }
                

            return code;
        }
    }
}
