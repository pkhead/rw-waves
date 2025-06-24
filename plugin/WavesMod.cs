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
    [BepInPlugin(MOD_ID, "Arena Waves", VERSION)]
    partial class WavesMod : BaseUnityPlugin
    {
        public static WavesMod Instance;

        public const string MOD_ID = "pkhead.waves";
        public const string AUTHOR = "pkhead";
        public const string VERSION = "1.0.1";

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
                        logger.LogInfo("Dev console not found");
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
            SpriteTinter.InitHooks();
            WavesGameSession.InitHooks();
            MenuHooks.InitHooks();
            ArenaSittingHooks.InitHooks();
            ArenaUiHooks.InitHooks();
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

                    if (waveNumber <= 0)
                    {
                        throw new ArgumentException("Argument 1 is out of range", nameof(args));
                    }

                    if (game.session is WavesGameSession session)
                    {
                        session.KillAll();
                        session.wave = waveNumber - 2;
                    }
                })
                .Register();
        }
    }
}