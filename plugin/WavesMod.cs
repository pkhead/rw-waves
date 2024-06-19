using System;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using UnityEngine;

// allow access to private members of Rain World code
#pragma warning disable CS0618
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace WavesMod
{
    [BepInPlugin(MOD_ID, "Waves", VERSION)]
    public partial class WavesMod : BaseUnityPlugin
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

                    // TODO: initialize hooks
                }
                catch (Exception e)
                {
                    logger.LogError(e);
                }
            };
        }
    }
}