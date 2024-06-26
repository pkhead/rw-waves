using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Menu;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace WavesMod;

static class MenuHooks
{
    class GameTypeSetupExtras
    {
        public int attemptCount = 3;

        public GameTypeSetupExtras()
        {}
    }

    private static readonly ConditionalWeakTable<ArenaSetup.GameTypeSetup, GameTypeSetupExtras> gameTypeSetupCwt = new();

    public static int GetAttemptCount(ArenaSetup.GameTypeSetup setup)
    {
        if (gameTypeSetupCwt.TryGetValue(setup, out var extras))
            return extras.attemptCount;

        return 3;
    }

    private const string WavesModeInfoString = "Conquer waves of opponents that get more difficult as you<LINE>progress. See how long you can last!";
    public static void InitHooks()
    {
        ArenaGameTypeID.RegisterValues();

        var gameTypeIdType = typeof(ExtEnum<ArenaSetup.GameTypeID>);
        var gameTypeExtEnum_opEquality = gameTypeIdType.GetMethod("op_Equality", new Type[] { gameTypeIdType, gameTypeIdType });
        var staticBindingFlags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static;

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
                
                ILLabel trueBranch = null;
                ILLabel falseBranch = null;

                cursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrtrue(out trueBranch),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Competitive", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 9;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch); // replace brfalse IL_050D with brtrue.s IL_0096

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);

                // replace
                //  this.levelSelector = new LevelSelector(this, this.pages[0], this.currentGameType == ArenaSetup.GameTypeID.Sandbox);
                // with
                //  this.levelSelector = new LevelSelector(this, this.pages[0], this.currentGameType == ArenaSetup.GameTypeID.Sandbox || this.currentGameType == WavesMod.GameTypeID.Waves);
                cursor.GotoNext(
                    //x => x.MatchLdarg(0),
                    //x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    //x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", System.Reflection.BindingFlags.Static)),
                    //x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchNewobj(typeof(LevelSelector).GetConstructor(new Type[] { typeof(Menu.Menu), typeof(Menu.MenuObject), typeof(bool) })),
                    x => x.MatchStfld(typeof(MultiplayerMenu).GetField("levelSelector"))
                );

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(MultiplayerMenu).GetMethod("get_currentGameType"));
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
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Competitive", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrtrue(out trueBranch),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 9;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch); // replace brfalse with brtrue.s

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);

                // add code to the end of the function
                // (but before the return call, obviously)
                cursor.Index = cursor.Instrs.Count - 1;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((MultiplayerMenu self) =>
                {
                    if (self.currentGameType != ArenaGameTypeID.Waves) return;

                    self.scene.AddIllustration(new MenuIllustration(self, self.scene, "", "wavesshadow", new Vector2(-2.99f, 265.01f), true, false));
                    self.scene.AddIllustration(new MenuIllustration(self, self.scene, "", "wavestitle", new Vector2(-2.99f, 265.01f), true, false));
                    self.scene.flatIllustrations[self.scene.flatIllustrations.Count - 1].sprite.shader = self.manager.rainWorld.Shaders["MenuText"];
                });
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons failed!: " + e);
            }
        };
        
        IL.Menu.MultiplayerMenu.Update += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                ILLabel falseBranch = null;
                ILLabel trueBranch = cursor.DefineLabel();

                // replace
                //  if (this.currentGameType == ArenaSetup.GameTypeID.Sandbox)
                // with
                //  if (this.currentGameType == ArenaSetup.GameTypeID.Sandbox || this.currentGameType == WavesMod.ArenaGameTypeID.Waves)
                cursor.GotoNext(
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 4;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch);

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);

                cursor.MoveAfterLabels();
                cursor.MarkLabel(trueBranch);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.MultiplayerMenu.Update failed!: " + e);
            }
        };

        IL.Menu.MultiplayerMenu.Singal += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                ILLabel trueBranch = null;
                ILLabel falseBranch = null;

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
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Competitive", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrtrue(out trueBranch),
                    x => x.MatchLdarg(0),
                    x => x.MatchCall(typeof(MultiplayerMenu).GetMethod("get_currentGameType")),
                    x => x.MatchLdsfld(typeof(ArenaSetup.GameTypeID).GetField("Sandbox", staticBindingFlags)),
                    x => x.MatchCall(gameTypeExtEnum_opEquality),
                    x => x.MatchBrfalse(out falseBranch)
                );

                cursor.Index += 9;
                cursor.Remove();
                cursor.Emit(OpCodes.Brtrue_S, trueBranch); // replace brfalse with brtrue.s

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Call, typeof(MultiplayerMenu).GetMethod("get_currentGameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse, falseBranch);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.MultiplayerMenu.Singal failed!: " + e);
            }
        };

        string[] numberToString = new string[] { "Zero", "One", "Two", "Three", "Four", "Five", "Six", "Seven" };

        On.Menu.MultiplayerMenu.UpdateInfoText += (
            On.Menu.MultiplayerMenu.orig_UpdateInfoText orig, MultiplayerMenu self
        ) =>
        {
            if (self.selectedObject is MultipleChoiceArray.MultipleChoiceButton btn)
            {
                string text = (self.selectedObject.owner as MultipleChoiceArray).IDString;
                if (text == "ATTEMPTS")
                {
                    if (btn.index == 0)
                    {
                        return "One attempt before game over";
                    }
                    else
                    {
                        return numberToString[btn.index + 1] + " attempts before game over";
                    }
                }
            }

            return orig(self);
        };

        On.Menu.ArenaSettingsInterface.ctor += (
            On.Menu.ArenaSettingsInterface.orig_ctor orig, Menu.ArenaSettingsInterface self,
            Menu.Menu menu, MenuObject owner
        ) =>
        {
            orig(self, menu, owner);

            Vector2 vector = new Vector2(826.01f, 140.01f);
            float num = 340f;
            bool flag = menu.CurrLang != InGameTranslator.LanguageID.English && menu.CurrLang != InGameTranslator.LanguageID.Korean && menu.CurrLang != InGameTranslator.LanguageID.Chinese;

            if (self.GetGameTypeSetup.gameType == ArenaGameTypeID.Waves)
            {
                self.spearsHitCheckbox = new CheckBox(menu, self, self, vector + new Vector2(0f, 220f), 120f, menu.Translate("Spears Hit:"), "SPEARSHIT", false);
                self.subObjects.Add(self.spearsHitCheckbox);
                self.evilAICheckBox = new CheckBox(menu, self, self, vector + new Vector2(num - 24f, 220f), InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 140f : 120f, menu.Translate("Aggressive AI:"), "EVILAI", false);
                self.subObjects.Add(self.evilAICheckBox);
                self.divSprite = new FSprite("pixel", true);
                self.divSprite.anchorX = 0f;
                self.divSprite.scaleX = num;
                self.divSprite.scaleY = 2f;
                self.Container.AddChild(self.divSprite);
                self.divSprite.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey);
                self.divSpritePos = vector + new Vector2(0f, 197f);

                // attempt #
                // holy crap there are so many parameters
                MultipleChoiceArray multipleChoiceArray = new MultipleChoiceArray(
                    menu: menu,
                    owner: self,
                    reportTo: self,
                    pos: vector + new Vector2(0f, 150f),
                    text: "Attempts",
                    IDString: "ATTEMPTS",
                    textWidth: InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 140f : 120f,
                    width: num,
                    buttonsCount: 5,
                    textInBoxes: false,
                    splitText: false
                );
				self.subObjects.Add(multipleChoiceArray);
				/*for (int i = 0; i < multipleChoiceArray.buttons.Length; i++)
				{
					multipleChoiceArray.buttons[i].label.text = (i + 1).ToString() + "x";
				}*/

                return;
            }
        };

        On.Menu.ArenaSettingsInterface.GetSelected += (
            On.Menu.ArenaSettingsInterface.orig_GetSelected orig, ArenaSettingsInterface self,
            MultipleChoiceArray array
        ) =>
        {
            if (array.IDString == "ATTEMPTS")
            {
                return gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).attemptCount - 1;
            }
            else
            {
                return orig(self, array);
            }
        };

        On.Menu.ArenaSettingsInterface.SetSelected += (
            On.Menu.ArenaSettingsInterface.orig_SetSelected orig, ArenaSettingsInterface self,
            MultipleChoiceArray array, int i
        ) =>
        {
            if (array.IDString == "ATTEMPTS")
            {
                gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).attemptCount = i+1;
            }
            else
            {
                orig(self, array, i);
            }
        };

        IL.Menu.InfoWindow.ctor += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);
                var label = cursor.DefineLabel();

                // want to write some code before:
                //  string[] array = Regex.Split(text, "\r\n");
                cursor.GotoNext(
                    MoveType.AfterLabel,
                    x => x.MatchLdloc(0),
                    x => x.MatchLdstr("\r\n"), // what the hell... this isn't even from a file.
                    x => x.MatchCall(typeof(Regex).GetMethod("Split", new Type[] { typeof(string), typeof(string) })),
                    x => x.MatchStloc(1)
                );

                /*if ((menu as MultiplayerMenu).GetGameTypeSetup.gameType == ArenaGameTypeID.Waves)
                    text = Regex.Replace(WavesModeInfoString, "<LINE>", "\r\n");*/
                cursor.Emit(OpCodes.Ldarg_1); // menu variable
                cursor.Emit(OpCodes.Isinst, typeof(MultiplayerMenu));
                cursor.Emit(OpCodes.Callvirt, typeof(MultiplayerMenu).GetMethod("get_GetGameTypeSetup"));
                cursor.Emit(OpCodes.Ldfld, typeof(ArenaSetup.GameTypeSetup).GetField("gameType"));
                cursor.Emit(OpCodes.Ldsfld, typeof(ArenaGameTypeID).GetField("Waves", staticBindingFlags));
                cursor.Emit(OpCodes.Call, gameTypeExtEnum_opEquality);
                cursor.Emit(OpCodes.Brfalse_S, label);
                
                cursor.Emit(OpCodes.Ldstr, WavesModeInfoString);
                cursor.Emit(OpCodes.Ldstr, "<LINE>");
                cursor.Emit(OpCodes.Ldstr, "\r\n");
                cursor.Emit(OpCodes.Call, typeof(System.Text.RegularExpressions.Regex).GetMethod("Replace", new Type[] { typeof(string), typeof(string), typeof(string) }));
                cursor.Emit(OpCodes.Stloc_0);

                cursor.MarkLabel(label);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.InfoWindow failed! " + e);
            }
        };

        On.ArenaSetup.GameTypeSetup.InitAsGameType += (On.ArenaSetup.GameTypeSetup.orig_InitAsGameType orig, ArenaSetup.GameTypeSetup self, ArenaSetup.GameTypeID gameType) =>
        {
            orig(self, gameType);

            if (gameType == ArenaGameTypeID.Waves)
            {
                self.foodScore = 1;
                self.survivalScore = 0;
                self.spearHitScore = 1;
                self.denEntryRule = ArenaSetup.GameTypeSetup.DenEntryRule.Standard;
                self.repeatSingleLevelForever = true;
                self.savingAndLoadingSession = false;
                self.rainWhenOnePlayerLeft = false;
                self.levelItems = true;
                self.fliesSpawn = true;
                self.saveCreatures = false;

                gameTypeSetupCwt.GetOrCreateValue(self).attemptCount = 3;
            }
            else
            {
                gameTypeSetupCwt.Remove(self);
            }
        };

        IL.RainWorldGame.ctor += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                cursor.GotoNext(
                    x => x.MatchLdarg(1),
                    x => x.MatchLdfld(typeof(ProcessManager).GetField("arenaSitting")),
                    x => x.MatchBrfalse(out _)
                );

                cursor.Index += 3;
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldarg_1);
                cursor.EmitDelegate((RainWorldGame self, ProcessManager manager) =>
                {
                    if (manager.arenaSitting.gameTypeSetup.gameType == ArenaGameTypeID.Waves)
                    {
                        self.session = new WavesGameSession(self);
                    }
                });
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.RainWorldGame.ctor failed! " + e);
            }
        };
    }
}