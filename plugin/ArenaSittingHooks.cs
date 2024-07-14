// did joar mean to spell it "ArenaSetting"???
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

namespace WavesMod;

static class ArenaSittingHooks
{
    public class ArenaSittingExtras
    {
        public int currentWave = 0;
        public int totalTime = 0;
        public List<int> playerLives; // a life value of 0 means the player is on their last life. if dead, they will not respawn.

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
                }

                extras.totalTime += max;
            }

            orig(self, session);
        };

        // for the round results screen, replace the header text
        // to show "WAVE #" instead of "ROUND # - etc"
        On.Menu.ArenaOverlay.ctor += (
            On.Menu.ArenaOverlay.orig_ctor orig, Menu.ArenaOverlay self,
            ProcessManager manager, ArenaSitting ArenaSitting, List<ArenaSitting.ArenaPlayer> result
        ) =>
        {
            orig(self, manager, ArenaSitting, result);

            if (ArenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves && cwt.TryGetValue(ArenaSitting, out var extras))
            {
                var wavesSession = (manager.currentMainLoop as RainWorldGame).GetArenaGameSession as WavesGameSession;

                bool canContinue = false;
                for (int i = 0; i < extras.playerLives.Count; i++)
                {
                    if (extras.playerLives[i] > 0)
                    {
                        canContinue = true;
                        break;
                    }
                }

                self.headingLabel.text = 
                    canContinue ? string.Concat("WAVE ", wavesSession.wave+1)
                                : string.Concat("WAVE ", wavesSession.wave+1, " - GAME OVER");
            }
        };

        // final multiplayer results screen
        On.Menu.MultiplayerResults.ctor += (
            On.Menu.MultiplayerResults.orig_ctor orig, Menu.MultiplayerResults self,
            ProcessManager manager
        ) =>
        {
            orig(self, manager);

            if (self.ArenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves && cwt.TryGetValue(self.ArenaSitting, out var extraData))
            {
                // allocate space for wave and time info
                // TODO: position better
                self.topMiddle.y -= 50f;

                var pos = new Vector2(self.topMiddle.x, self.headingLabel.pos.y + 20f);

                var timeInSecs = extraData.totalTime / 40;
                var timeInMins = timeInSecs / 60;

                var timeStr = $"~ {timeInMins.ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}:{(timeInSecs%60).ToString(CultureInfo.InvariantCulture).PadLeft(2, '0')}";
                var timeSymbol = new Menu.PlayerResultBox.SymbolAndLabel(
                    menu: self,
                    owner: self.pages[0],
                    pos: pos + new Vector2(Menu.Remix.MixedUI.LabelTest.GetWidth(timeStr, false) / -2f, -30f),
                    symbolName: "Multiplayer_Time",
                    initialLabel: timeStr,
                    labelXOffset: -39f
                );

                string wavesStr = "~ " + (extraData.currentWave+1).ToString(CultureInfo.InvariantCulture);
                var wavesSymbol = new Menu.PlayerResultBox.SymbolAndLabel(
                    menu: self,
                    owner: self.pages[0],
                    pos: pos + new Vector2(Menu.Remix.MixedUI.LabelTest.GetWidth(wavesStr, false) / -2f, -65f),
                    symbolName: "Multiplayer_Star",
                    initialLabel: wavesStr,
                    labelXOffset: -39f
                );

                self.pages[0].subObjects.Add(timeSymbol);
                self.pages[0].subObjects.Add(wavesSymbol);
            }
        };

        // for final player results, don't show wins and deaths
        // since these would all be the same for every player...
        // also you can't "win" in waves mode anyway.
        On.Menu.FinalResultbox.ctor += (
            On.Menu.FinalResultbox.orig_ctor orig, Menu.FinalResultbox self,
            Menu.MultiplayerResults resultPage, Menu.MenuObject owner, ArenaSitting.ArenaPlayer player, int index
        ) =>
        {
            orig(self, resultPage, owner, player, index);

            if (resultPage.ArenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves)
            {
                // just move them offscreen lol
                self.winsSymbol.pos.x = -10000f;
                self.deathsSymbol.pos.x = -10000f;
                self.scoreSymbol.pos.x = -10000f;

                // only kills are relevant
                self.killsSymbol.pos.x = 135f;
                
                // TODO: try to prevent kill trophy overlap with text
            }
        };
    }

    public static bool TryGetData(ArenaSitting arenaSitting, out ArenaSittingExtras data)
        => cwt.TryGetValue(arenaSitting, out data);
}