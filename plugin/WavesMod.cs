using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using DevConsole.Commands;

// allow access to private members of Rain World code
#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace WavesMod
{
    [BepInPlugin(MOD_ID, "Waves", VERSION)]
    partial class WavesMod : BaseUnityPlugin
    {
        public static WavesMod Instance;

        public const string MOD_ID = "pkhead.waves";
        public const string AUTHOR = "pkhead";
        public const string VERSION = "0.1.0";

        private bool isInit = false;
        public BepInEx.Logging.ManualLogSource logger;

        public WavesMod()
        {
            Instance = this;
        }

        public void OnEnable()
        {
            logger = BepInEx.Logging.Logger.CreateLogSource("WavesMod");

            On.RainWorld.OnModsInit += (On.RainWorld.orig_OnModsInit orig, RainWorld self) =>
            {
                orig(self);

                try
                {
                    if (isInit) return;
                    isInit = true;

                    SpriteTinter.Reset();
                    InitHooks();

                    try
                    {
                        InitDevConsole();
                    }
                    catch (System.IO.FileNotFoundException)
                    {
                        logger.LogError("Dev console not found!");
                    }
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }

        private void InitHooks()
        {
            IL.RainWorldGame.ctor += (il) =>
            {
                try
                {
                    var cursor = new ILCursor(il);

                    // instead of startng a competitive game session, start a
                    // waves game session
                    cursor.GotoNext(
                        x => x.MatchLdarg(0),
                        x => x.MatchLdarg(0),
                        x => x.MatchNewobj(typeof(CompetitiveGameSession).GetConstructor(new Type[] { typeof(RainWorldGame) })),
                        x => x.MatchStfld(typeof(RainWorldGame).GetField("session")) 
                    );
                    cursor.Index += 2;
                    cursor.Remove();
                    cursor.Emit(OpCodes.Newobj, typeof(WavesGameSession).GetConstructor(new Type[1] { typeof(RainWorldGame) }));

                    logger.LogDebug("IL.RainWorldGame.ctor injection success!");
                }
                catch (Exception e)
                {
                    logger.LogError("Could not inject IL.RainWorldGame.ctor: " + e.ToString());
                }
            };

            SpriteTinter.InitHooks();
            WavesGameSession.InitHooks();
        }

        private void InitDevConsole()
        {
            new CommandBuilder("next_wave")
                .RunGame((game, args) =>
                {
                    if (game.session is WavesGameSession session)
                    {
                        session.KillAll();
                    }
                })
                .Register();
            
            new CommandBuilder("set_wave")
                .RunGame((game, args) =>
                {
                    if (args.Length == 0) throw new ArgumentException("Expected int for argument 1, got null", nameof(args));
                    
                    if (!int.TryParse(args[0], System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int waveNumber))
                    {
                        throw new ArgumentException("Expected int for argument 1", nameof(args));
                    }

                    if (game.session is WavesGameSession session)
                    {
                        session.KillAll();
                        session.wave = waveNumber - 1; // subtract 1, as killing all creatures will trigger the next wave
                    }
                })
                .Register();
        }
    }
}