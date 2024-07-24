// did joar mean to spell it "ArenaSetting"???
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;
using System.Text.RegularExpressions;

namespace WavesMod;

static class ArenaSittingHooks
{
    public class ArenaSittingExtras
    {
        public int currentWave = 0;
        public int totalTime = 0;
        public List<int> playerLives; // a life value of 0 means the player is on their last life. if dead, they will not respawn.
        public List<int> playerRespawnWait;

        public int startingLives = 3;
        public WavesDifficultyOption difficulty = WavesDifficultyOption.Hard;
        public int respawnWait = 0;

        public ArenaSittingExtras()
        {}
    }

    private static readonly ConditionalWeakTable<ArenaSitting, ArenaSittingExtras> cwt = new();

    public static void InitHooks()
    {
        // initialize ArenaSitting extra data
        On.ArenaSitting.ctor += (
            On.ArenaSitting.orig_ctor orig, ArenaSitting self,
            ArenaSetup.GameTypeSetup gameTypeSetup, MultiplayerUnlocks multiplayerUnlocks) =>
        {
            orig(self, gameTypeSetup, multiplayerUnlocks);
            
            if (gameTypeSetup.gameType == ArenaGameTypeID.Waves)
            {
                var data = new ArenaSittingExtras();
                cwt.Add(self, data);

                var setupData = MenuHooks.GetExtraSetupOptions(gameTypeSetup);
                data.startingLives = setupData.wavesLives;
                data.difficulty = setupData.wavesDifficulty;
                data.respawnWait = setupData.wavesRespawnWait;
                data.playerLives = new();
                data.playerRespawnWait = new();
            }
        };

        On.ArenaSitting.AddPlayer += (
            On.ArenaSitting.orig_AddPlayer orig, ArenaSitting self,
            int playerNumber
        ) =>
        {
            orig(self, playerNumber);

            if (self.gameTypeSetup.gameType == ArenaGameTypeID.Waves)
            {
                var data = cwt.GetOrCreateValue(self);
                while (data.playerLives.Count <= playerNumber) data.playerLives.Add(0);
                data.playerLives[playerNumber] = data.startingLives;

                while (data.playerRespawnWait.Count <= playerNumber) data.playerRespawnWait.Add(int.MaxValue);
                if (data.respawnWait >= 0) data.playerRespawnWait[playerNumber] = data.respawnWait;

                WavesMod.Instance.logger.LogInfo($"Player {playerNumber} lives: {data.startingLives}");
            }
        };

        On.ArenaSitting.AddPlayerWithClass += (
            On.ArenaSitting.orig_AddPlayerWithClass orig, ArenaSitting self,
            int playerNumber, SlugcatStats.Name playerClass
        ) =>
        {
            orig(self, playerNumber, playerClass);

            if (self.gameTypeSetup.gameType == ArenaGameTypeID.Waves)
            {
                var data = cwt.GetOrCreateValue(self);
                while (data.playerLives.Count <= playerNumber) data.playerLives.Add(0);
                data.playerLives[playerNumber] = data.startingLives;

                while (data.playerRespawnWait.Count <= playerNumber) data.playerRespawnWait.Add(int.MaxValue);
                if (data.respawnWait >= 0) data.playerRespawnWait[playerNumber] = data.respawnWait;

                WavesMod.Instance.logger.LogInfo($"Player {playerNumber} lives: {data.startingLives}");
            }
        };

        // limit amount of retries players will have
        IL.ArenaSitting.NextLevel += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                /*
                find: this.currentLevel++;

                append:
                if (this.gameTypeSetup.gameType == ArenaGameTypeID.Waves)
                {
                    manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);
                    return;
                }
                */
                cursor.GotoNext(
                    MoveType.After,
                    x => x.MatchLdarg(0),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld(typeof(ArenaSitting).GetField("currentLevel")),
                    x => x.MatchLdcI4(1),
                    x => x.MatchAdd(),
                    x => x.MatchStfld(typeof(ArenaSitting).GetField("currentLevel"))
                );

                var label = cursor.DefineLabel();
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate((ArenaSitting self, ProcessManager manager) =>
                {
                    if (self.gameTypeSetup.gameType == ArenaGameTypeID.Waves && cwt.TryGetValue(self, out var extras))
                    {
                        if (manager.currentMainLoop is RainWorldGame rwGame && rwGame.GetArenaGameSession is WavesGameSession wavesSession)
                        {
                            extras.currentWave = wavesSession.wave;

                            // check if any players have any lives left
                            // if not, game over man...
                            bool canContinue = false;
                            for (int i = 0; i < extras.playerLives.Count; i++)
                            {
                                if (extras.playerLives[i] > 0)
                                {
                                    canContinue = true;
                                    break;
                                }
                            }

                            if (!canContinue)
                            {
                                manager.RequestMainProcessSwitch(ProcessManager.ProcessID.MultiplayerResults);
                                return true;
                            }
                        }
                    }

                    return false;
                });
                cursor.Emit(OpCodes.Brfalse, label);
                cursor.Emit(OpCodes.Ret);
                cursor.MarkLabel(label);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.ArenaSitting.NextLevel failed! " + e);
            }
        };

        // keep track of total time
        // also make sure nobody really "wins"
        // by setting player.winner = false for every player
        On.ArenaSitting.SessionEnded += (
            On.ArenaSitting.orig_SessionEnded orig, ArenaSitting self,
            ArenaGameSession session
        ) =>
        {
            if (session is WavesGameSession wavesSession && cwt.TryGetValue(self, out var extras))
            {
                int max = 0;

                foreach (var player in self.players)
                {
                    if (player.timeAlive > max)
                        max = player.timeAlive;
                    
                    player.winner = false;

                    // if a player was permadead at the start of the round,
                    // i don't want it to increase the death amount since
                    // they were already dead.
                    // on that condition, the player's abstractCreature is
                    // never created, so to check if the player was permadead
                    // at the start of the round, i simply check if it's null.
                    if (!player.alive)
                    {
                        var creature = session.Players.Find(x => (x.state as PlayerState).playerNumber == player.playerNumber);
                        if (creature is null)
                        {
                            Debug.Log($"Player #{player.playerNumber} never spawned in. Don't increase death #");
                            player.deaths--;
                        }
                    }
                }

                extras.totalTime += max;
            }

            orig(self, session);
        };

        // session saving
        IL.ArenaSitting.SaveToFile += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld(typeof(RainWorld).GetField("options")),
                    x => x.MatchLdloc(0),
                    x => x.MatchCallvirt(typeof(Options).GetMethod("SaveArenaSitting", new Type[] { typeof(string) }))
                );

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloca_S, (byte)0);
                cursor.EmitDelegate((ArenaSitting self, ref string text) =>
                {
                    if (self.gameTypeSetup.gameType == ArenaGameTypeID.Waves && cwt.TryGetValue(self, out var extras))
                    {
                        WavesMod.Instance.logger.LogInfo("Save arena session");
                        text += "WAVES<ssB>";
                        text += $"currentWave<sbpB>{extras.currentWave}<sbpA>";
                        text += $"totalTime<sbpB>{extras.totalTime}<sbpA>";
                        text += $"difficulty<sbpB>{extras.difficulty}<sbpA>";
                        text += $"respawnWait<sbpB>{extras.respawnWait}<sbpA>";

                        text += "playerLives<sbpB>";
                        for (int i = 0; i < extras.playerLives.Count; i++)
                        {
                            if (i > 0) text += "<sbpC>";
                            text += extras.playerLives[i];
                        }
                        text += "<sbpA>";

                        text += "playerRespawnWaits<sbpB>";
                        for (int i = 0; i < extras.playerRespawnWait.Count; i++)
                        {
                            if (i > 0) text += "<sbpC>";
                            text += extras.playerRespawnWait[i];
                        }
                        text += "<sbpA>";

                        text += "<ssA>";
                    }

                    WavesMod.Instance.logger.LogInfo("save: " + text);
                });
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.ArenaSitting.SaveToFile failed! " + e);
            }
        };

        // session loading
        IL.ArenaSitting.LoadFromFile += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                // add code to the end of the function
                // (but before the return call, obviously)
                cursor.Index = cursor.Instrs.Count - 1;

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc_0);
                cursor.EmitDelegate((ArenaSitting self, string[] ssaSplit) =>
                {
                    if (!cwt.TryGetValue(self, out var extras)) return;

                    foreach (var ssa in ssaSplit)
                    {
                        var ssbSplit = Regex.Split(ssa, "<ssB>");
                        if (ssbSplit.Length >= 2 && ssbSplit[0] == "WAVES")
                        {
                            var sbpASplit = Regex.Split(ssbSplit[1], "<sbpA>");
                            foreach (var sbpA in sbpASplit)
                            {
                                var sbpBSplit = Regex.Split(sbpA, "<sbpB>");
                                if (sbpBSplit.Length < 2) continue;
                                
                                switch (sbpBSplit[0])
                                {
                                    case "currentWave":
                                        extras.currentWave = int.Parse(sbpBSplit[1], CultureInfo.InvariantCulture);
                                        break;
                                    
                                    case "totalTime":
                                        extras.totalTime = int.Parse(sbpBSplit[1], CultureInfo.InvariantCulture);
                                        break;
                                    
                                    case "difficulty":
                                        extras.difficulty = (WavesDifficultyOption) Enum.Parse(typeof(WavesDifficultyOption), sbpBSplit[1]);
                                        break;
                                    
                                    case "respawnWait":
                                        extras.respawnWait = int.Parse(sbpBSplit[1], CultureInfo.InvariantCulture);
                                        break;
                                    
                                    case "playerLives":
                                    {
                                        var sbcSplit = Regex.Split(sbpBSplit[1], "<sbpC>");
                                        extras.playerLives.Clear();
                                        for (int i = 0; i < sbcSplit.Length; i++)
                                        {
                                            extras.playerLives.Add(int.Parse(sbcSplit[i], CultureInfo.InvariantCulture));
                                        }
                                        break;
                                    }
                                    
                                    case "playerRespawnWaits":
                                    {
                                        var sbcSplit = Regex.Split(sbpBSplit[1], "<sbpC>");
                                        extras.playerRespawnWait.Clear();
                                        for (int i = 0; i < sbcSplit.Length; i++)
                                        {
                                            extras.playerRespawnWait.Add(int.Parse(sbcSplit[i], CultureInfo.InvariantCulture));
                                        }
                                        break;
                                    }
                                    
                                    default:
                                        WavesMod.Instance.logger.LogWarning($"Unrecognized save string option {sbpBSplit[0]}");
                                        break;
                                }
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.ArenaSitting.LoadFromFile failed! " + e);
            }
        };
    }

    public static bool TryGetData(ArenaSitting arenaSitting, out ArenaSittingExtras data)
        => cwt.TryGetValue(arenaSitting, out data);
}