using UnityEngine;
using System.Collections.Generic;
using System.Globalization;
namespace WavesMod;

static class ArenaUiHooks
{
    public static void InitHooks()
    {
        // for the round results screen, replace the header text
        // to show "WAVE #" instead of "ROUND # - etc"
        On.Menu.ArenaOverlay.ctor += (
            On.Menu.ArenaOverlay.orig_ctor orig, Menu.ArenaOverlay self,
            ProcessManager manager, ArenaSitting ArenaSitting, List<ArenaSitting.ArenaPlayer> result
        ) =>
        {
            orig(self, manager, ArenaSitting, result);

            if (ArenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves && ArenaSittingHooks.TryGetData(ArenaSitting, out var extras))
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

            if (self.ArenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves && ArenaSittingHooks.TryGetData(self.ArenaSitting, out var extraData))
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

        On.HUD.PlayerSpecificMultiplayerHud.ctor += (
            On.HUD.PlayerSpecificMultiplayerHud.orig_ctor orig, HUD.PlayerSpecificMultiplayerHud self,
            HUD.HUD hud, ArenaGameSession session, AbstractCreature abstractPlayer
        ) =>
        {
            orig(self, hud, session, abstractPlayer);

            if (session is WavesGameSession)
            {
                self.parts.Add(new HudLifeTracker(self));
            }
        };
    }
}