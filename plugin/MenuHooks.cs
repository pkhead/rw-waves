using System;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using Menu;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;

namespace WavesMod;

enum WavesDifficultyOption
{
    /// <summary>
    /// Players don't lose a life if their body hadn't been eaten.
    /// </summary>
    Normal = 0,

    /// <summary>
    /// Players always lose a life when they die.
    /// </summary>
    Hard = 1,
}

static class MenuHooks
{
    public class GameTypeSetupExtras
    {
        public int wavesLives = 3;
        public WavesDifficultyOption wavesDifficulty = WavesDifficultyOption.Hard;
        public int wavesRespawnWait = 0;

        public WaveGeneratorType generatorType = WaveGeneratorType.Preset;
        public string presetName = WaveSpawnData.DefaultPresetName;

        public GameTypeSetupExtras()
        {}
    }

    class ArenaSettingsInterfaceExtras
    {
        public SelectOneButton[] respawnModes;
        public MultipleChoiceArray respawnWaitArray;
        public MenuLabel respawnModeLabel;

        public ArenaSettingsInterfaceExtras()
        {}
    }

    class MultiplayerMenuExtras
    {
        public SpawnSettingsInterface spawnSettingsInterface;
    }

    private static readonly ConditionalWeakTable<ArenaSetup.GameTypeSetup, GameTypeSetupExtras> gameTypeSetupCwt = new();
    private static readonly ConditionalWeakTable<ArenaSettingsInterface, ArenaSettingsInterfaceExtras> arenaSettingsInterfaceCwt = new();
    private static readonly ConditionalWeakTable<MultiplayerMenu, MultiplayerMenuExtras> multiplayerMenuExtras = new();

    public static GameTypeSetupExtras GetExtraSetupOptions(ArenaSetup.GameTypeSetup setup)
    {
        return gameTypeSetupCwt.GetOrCreateValue(setup);
    }

    private const string WavesModeInfoString = "Conquer never-ending waves of hungry creatures, each one more<LINE>difficult than the last. You will need to kill every hostile creature<LINE>in order to move on to the next wave. The game is over once every<LINE>player has ran out of lives. Try to see how long you can last!";
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

                    // add waves mode title
                    self.scene.AddIllustration(new MenuIllustration(self, self.scene, "", "wavesshadow", new Vector2(-2.99f, 265.01f), true, false));
                    self.scene.AddIllustration(new MenuIllustration(self, self.scene, "", "wavestitle", new Vector2(-2.99f, 265.01f), true, false));
                    self.scene.flatIllustrations[self.scene.flatIllustrations.Count - 1].sprite.shader = self.manager.rainWorld.Shaders["MenuText"];

                    // add spawn settings interface
                    var extras = multiplayerMenuExtras.GetOrCreateValue(self);
                    extras.spawnSettingsInterface = new SpawnSettingsInterface(self, self.pages[0]);
                    self.pages[0].subObjects.Add(extras.spawnSettingsInterface);
                });
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons failed!: " + e);
            }
        };

        On.Menu.MultiplayerMenu.ClearGameTypeSpecificButtons += (On.Menu.MultiplayerMenu.orig_ClearGameTypeSpecificButtons orig, MultiplayerMenu self) =>
        {
            if (multiplayerMenuExtras.TryGetValue(self, out var extras))
            {
                var uiInterface = extras.spawnSettingsInterface;
                if (uiInterface is not null)
                {
                    uiInterface.Shutdown();
                    uiInterface.RemoveSprites();
                    self.pages[0].RemoveSubObject(uiInterface);
                    extras.spawnSettingsInterface = null;
                }
            }

            orig(self);
        };

        On.Menu.MultiplayerMenu.ShutDownProcess += (On.Menu.MultiplayerMenu.orig_ShutDownProcess orig, MultiplayerMenu self) =>
        {
            if (multiplayerMenuExtras.TryGetValue(self, out var extras))
            {
                extras.spawnSettingsInterface?.Shutdown();
            }

            orig(self);
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

        string[] numberToString = new string[] { "zero", "one", "two", "three", "four", "five", "six", "seven" };

        On.Menu.MultiplayerMenu.UpdateInfoText += (
            On.Menu.MultiplayerMenu.orig_UpdateInfoText orig, MultiplayerMenu self
        ) =>
        {
            if (self.selectedObject is MultipleChoiceArray.MultipleChoiceButton btn)
            {
                string text = (self.selectedObject.owner as MultipleChoiceArray).IDString;
                if (text == "LIVES")
                {
                    if (btn.index == 0)
                    {
                        return "Each player starts with one life";
                    }
                    else
                    {
                        return $"Each player starts with {numberToString[btn.index + 1]} lives";
                    }
                }
                else if (text == "RESPAWNWAIT")
                {
                    if (btn.index == 5)
                    {
                        return "Players must wait until everyone is dead before respawning";
                    }
                    else
                    {
                        return $"Players must wait {numberToString[btn.index + 1]} {(btn.index > 0 ? "waves":"wave")} before respawning";
                    }
                }
            }
            else if (self.selectedObject is SelectOneButton selectOneButton && selectOneButton.signalText == "RESPAWNMODE")
            {
                if (selectOneButton.buttonArrayIndex == 0) // normal mode
                {
                    return "Players don't lose a life if their body was saved";
                }
                else if (selectOneButton.buttonArrayIndex == 1) // hard mod
                {
                    return "Players always lose a life upon death";
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
                var extras = arenaSettingsInterfaceCwt.GetOrCreateValue(self);

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

                // life count
                // holy crap there are so many parameters
                var choiceArrayTextWidth = InGameTranslator.LanguageID.UsesLargeFont(menu.CurrLang) ? 140f : 120f;
                MultipleChoiceArray livesChoiceArray = new MultipleChoiceArray(
                    menu: menu,
                    owner: self,
                    reportTo: self,
                    pos: vector + new Vector2(0f, 150f),
                    text: "Lives:",
                    IDString: "LIVES",
                    textWidth: choiceArrayTextWidth,
                    width: num,
                    buttonsCount: 5,
                    textInBoxes: false,
                    splitText: false
                );
				self.subObjects.Add(livesChoiceArray);

                // respawn wait
                var respawnWaitArray = extras.respawnWaitArray = new MultipleChoiceArray(
                    menu: menu,
                    owner: self,
                    reportTo: self,
                    pos: vector + new Vector2(0f, 100f),
                    text: "Respawn Wait:",
                    IDString: "RESPAWNWAIT",
                    textWidth: choiceArrayTextWidth,
                    width: num,
                    buttonsCount: 6,
                    textInBoxes: false,
                    splitText: false
                );
                self.subObjects.Add(respawnWaitArray);

                // radio buttons for respawn mode
                // wow ui code in this game is stinky
                var buttonWidth = num / 2f - 20f;

                MenuLabel respawnModeLabel;
                self.subObjects.Add(respawnModeLabel = new MenuLabel(
                    menu: menu,
                    owner: owner,
                    text: "Respawn Mode:",
                    pos: vector + new Vector2(-choiceArrayTextWidth * 1.5f, 64f),
                    size: new Vector2(choiceArrayTextWidth, 20f),
                    bigText: false
                ));
                respawnModeLabel.label.color = Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
                respawnModeLabel.label.alignment = FLabelAlignment.Left;
                respawnModeLabel.label.anchorX = 0;
                extras.respawnModeLabel = respawnModeLabel;

                extras.respawnModes = new SelectOneButton[2];
                extras.respawnModes[0] = new SelectOneButton(
                    menu: menu,
                    owner: self,
                    displayText: "EASY",
                    signalText: "RESPAWNMODE",
                    pos: vector + new Vector2(0f, 62f),
                    size: new Vector2(buttonWidth, 24f),
                    buttonArray: extras.respawnModes,
                    buttonArrayIndex: 0
                );

                extras.respawnModes[1] = new SelectOneButton(
                    menu: menu,
                    owner: self,
                    displayText: "NORMAL",
                    signalText: "RESPAWNMODE",
                    pos: vector + new Vector2(num / 2f + (num / 2f - buttonWidth) / 2f, 62f),
                    size: new Vector2(buttonWidth, 24f),
                    buttonArray: extras.respawnModes,
                    buttonArrayIndex: 1
                );

                self.subObjects.Add(extras.respawnModes[0]);
                self.subObjects.Add(extras.respawnModes[1]);

                return;
            }
        };

        On.Menu.ArenaSettingsInterface.Update += (
            On.Menu.ArenaSettingsInterface.orig_Update orig, ArenaSettingsInterface self
        ) =>
        {
            orig(self);

            if (arenaSettingsInterfaceCwt.TryGetValue(self, out var extras))
            {
                // if in single player mode, gray out options that are only applicable
                // to multiplayer mode.
                int playerCount = 0;
                for (int i = 0; i < self.GetArenaSetup.playersJoined.Length; i++)
                {
                    if (self.GetArenaSetup.playersJoined[i])
                    {
                        playerCount++;
                    }
                }

                bool singleplayerMode = playerCount < 2;
                extras.respawnWaitArray.greyedOut = singleplayerMode;

                foreach (var btn in extras.respawnModes)
                    btn.buttonBehav.greyedOut = singleplayerMode;

                extras.respawnModeLabel.label.color =
                    singleplayerMode ? Menu.Menu.MenuRGB(Menu.Menu.MenuColors.VeryDarkGrey)
                                     : Menu.Menu.MenuRGB(Menu.Menu.MenuColors.DarkGrey);
            }
        };

        On.Menu.ArenaSettingsInterface.GetSelected += (
            On.Menu.ArenaSettingsInterface.orig_GetSelected orig, ArenaSettingsInterface self,
            MultipleChoiceArray array
        ) =>
        {
            if (array.IDString == "LIVES")
            {
                return gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesLives - 1;
            }
            else if (array.IDString == "RESPAWNWAIT")
            {
                var value = gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesRespawnWait;
                return value == -1 ? 5 : value;
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
            if (array.IDString == "LIVES")
            {
                gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesLives = i+1;
            }
            else if (array.IDString == "RESPAWNWAIT")
            {
                gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesRespawnWait
                    = (i == 5) ? -1 : i;
            }
            else
            {
                orig(self, array, i);
            }
        };

        On.Menu.ArenaSettingsInterface.GetCurrentlySelectedOfSeries += (
            On.Menu.ArenaSettingsInterface.orig_GetCurrentlySelectedOfSeries orig, ArenaSettingsInterface self,
            string series
        ) =>
        {
            if (series == "RESPAWNMODE")
                return (int)gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesDifficulty;
            return orig(self, series);
        };

        On.Menu.ArenaSettingsInterface.SetCurrentlySelectedOfSeries += (
            On.Menu.ArenaSettingsInterface.orig_SetCurrentlySelectedOfSeries orig, ArenaSettingsInterface self,
            string series, int to
        ) =>
        {
            if (series == "RESPAWNMODE")
                gameTypeSetupCwt.GetOrCreateValue(self.GetGameTypeSetup).wavesDifficulty = (WavesDifficultyOption)to;
            else
                orig(self, series, to);
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
                self.savingAndLoadingSession = true;
                self.rainWhenOnePlayerLeft = false;
                self.levelItems = true;
                self.fliesSpawn = true;
                self.saveCreatures = false;

                gameTypeSetupCwt.GetOrCreateValue(self).wavesLives = 3;
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