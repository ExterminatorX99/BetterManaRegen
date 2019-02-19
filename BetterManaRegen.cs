using Terraria.ModLoader;
using Terraria;
using MonoMod.RuntimeDetour.HookGen;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;

namespace BetterManaRegen
{

    class BetterManaRegen : Mod
    {
		public override void Load()
		{
			IL.Terraria.Player.UpdateManaRegen += Player_UpdateManaRegen;
		}

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
		private void Player_UpdateManaRegen(MonoMod.RuntimeDetour.HookGen.HookIL il)
		{
			HookILCursor cursor = il.At(0);
			Func<Instruction, bool>[] ilToRemove = {
					instruction => instruction.MatchLdarg(0),
					instruction => instruction.MatchLdflda<Entity>("velocity"),
					instruction => instruction.MatchLdfld<Vector2>("X"),
					instruction => instruction.MatchLdcR4(0.0f),
					instruction => instruction.Match(OpCodes.Bne_Un_S),

					instruction => instruction.MatchLdarg(0),
					instruction => instruction.MatchLdflda<Entity>("velocity"),
					instruction => instruction.MatchLdfld<Vector2>("Y"),
					instruction => instruction.MatchLdcR4(0.0f),
					instruction => instruction.Match(OpCodes.Beq_S),

					instruction => instruction.MatchLdarg(0),
					instruction => instruction.MatchLdfld<Player>("grappling"),
					instruction => instruction.MatchLdcI4(0),
					instruction => instruction.MatchLdelemI4(),
					instruction => instruction.MatchLdcI4(0),
					instruction => instruction.Match(OpCodes.Bge_S),

					instruction => instruction.MatchLdarg(0),
					instruction => instruction.MatchLdfld<Player>("manaRegenBuff"),
					instruction => instruction.Match(OpCodes.Brfalse_S)
			};
			bool found = false; //whether or not we have matched our 1 + 19 instructions

			int attemptCount = 0; //used for logging
			string loggedInstructions = "";
			while (cursor.TryGotoNext(
					// Look for when we store "manaRegen". This occurs just before our ilToRemove instructions
					instruction => instruction.MatchStfld<Player>("manaRegen")))
			{
				HookILCursor backup = cursor.Clone();
				found = true;
				for (int i = 0; i < ilToRemove.Length; i++)
				{
					// Move cursor to next instruction and see if it doesn't match
					cursor.GotoNext();
					loggedInstructions += attemptCount + ": " + cursor.Next?.OpCode.ToString() + "\n";

					
					if (!ilToRemove[i](cursor.Next))
					{
						found = false;
						break;
					}
				}
				if (!found) continue;

				// We use the backup cursor which was at the original position just before the 19 instructions
				backup.GotoNext(); 
				// remove the 19 instructions
				for(int i=0; i < ilToRemove.Length; i++)
				{
					backup.Remove();
				}
				break;

			}

			if (!found)
			{
				throw new Exception("Instructions not found; unable to patch. Sorry!\n" + loggedInstructions);
			}
		}

		
	}
}
