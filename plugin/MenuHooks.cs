using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace WavesMod;

static class MenuHooks
{
    public static void InitHooks()
    {
        ArenaGameTypeID.RegisterValues();

        IL.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                // replace
                //  if (this.currentGameType == ArenaSetup.GameTypeID.Sandbox || this.currentGameType == ArenaSetup.GameTypeID.Competitive)
                // with
                //  if (
                //   this.currentGameType == ArenaSetup.GameTypeID.Sandbox ||
                //   this.currentGameType == ArenaSetup.GameTypeID.Competitive 
                //   this.currentGameType == WavesMod.GameTypeID.Waves
                //  )
                var gameTypeIdType = typeof(ExtEnum<ArenaSetup.GameTypeID>);
                var gameTypeExtEnum_opEquality = gameTypeIdType.GetMethod("op_Equality", new Type[] { gameTypeIdType, gameTypeIdType });

                ILLabel trueBranch = null;
                ILLabel falseBranch = null;

                var staticBindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

                cursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrtrue(out trueBranch),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Competitive", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 9;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch); // replace brfalse IL_050D with brtrue.s IL_0096
                //cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);

                // replace
                //  this.levelSelector = new LevelSelector(this, this.pages[0], this.currentGameType == ArenaSetup.GameTypeID.Sandbox);
                // with
                //  this.levelSelector = new LevelSelector(this, this.pages[0], this.currentGameType == ArenaSetup.GameTypeID.Sandbox || this.currentGameType == WavesMod.GameTypeID.Waves);
                cursor.GotoNext(
                    //x => x.MatchLdarg(0),
                    //x => x.MatchCall(typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType")),
                    //x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", System.Reflection.BindingFlags.Static)),
                    //x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchNewobj(typeof(Menu.LevelSelector).GetConstructor(new Type[] { typeof(Menu.Menu), typeof(Menu.MenuObject), typeof(bool) })),
                    x => x.MatchStfld(typeof(Menu.MultiplayerMenu).GetField("levelSelector"))
                );

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Or); // this.currentGameType == ArenaSetup.GameTypeID.Sandbox || this.currentGameType == WavesMod.GameTypeID.Waves

                // replace
                //  if (this.currentGameType == ArenaSetup.GameTypeID.Competitive || this.currentGameType == ArenaSetup.GameTypeID.Sandbox)
                // with
                //  if (
                //   this.currentGameType == ArenaSetup.GameTypeID.Competitive ||
                //   this.currentGameType == ArenaSetup.GameTypeID.Sandbox
                //   this.currentGameType == WavesMod.GameTypeID.Waves
                //  )
                cursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Competitive", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrtrue(out trueBranch),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 9;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch); // replace brfalse IL_050D with brtrue.s IL_0096
                //cursor.Index++;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(Menu.MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);

                //WavesMod.Instance.logger.LogInfo(il);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons failed!: " + e);
            }
        };

        /*On.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons += (On.Menu.MultiplayerMenu.orig_InitiateGameTypeSpecificButtons orig, Menu.MultiplayerMenu self) =>
        {

        };*/
    }
}