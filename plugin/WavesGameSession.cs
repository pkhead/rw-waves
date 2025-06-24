using System;
using System.Collections.Generic;
using System.Reflection;
using ArenaBehaviors;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RWCustom;
using System.Linq;
namespace WavesMod;
using Random = UnityEngine.Random;

class WavesGameSession : ArenaGameSession
{
    // if all tracked creatures are dead, initiate the next wave
    private readonly List<AbstractCreature> trackedCreatures;
    private readonly HashSet<AbstractCreature> permaDeadPlayers;
    private WavesCreatureSpawner creatureSpawner = null;
    private readonly List<(HUD.PlayerSpecificMultiplayerHud hud, AbstractCreature newCreature)> stalePlayerHuds = new();
    private readonly WaveSpawnData.WaveData[] waveData;

    public int wave = -1;
    private int nextWaveTimer = -1;
    private readonly int initialTotalTime = 0;

    // grace period, after which exiting the session will decrease the
    // player's life count...
    public int gracePeriodTicker = 0;

    public WavesGameSession(RainWorldGame game) : base(game)
    {
        waveData = WaveSpawnData.Read();

        rainCycleTimeInMinutes = 0;
        trackedCreatures = new();
        permaDeadPlayers = new();
        
        arenaSitting.gameTypeSetup.levelItems = true;
        arenaSitting.gameTypeSetup.spearHitScore = 0;
        arenaSitting.gameTypeSetup.foodScore = 0;
        arenaSitting.gameTypeSetup.rainWhenOnePlayerLeft = false;

        // i think this will make award one point for killing any type of creature?
        /*for (int i = 0; i < arenaSitting.gameTypeSetup.killScores.Length; i++)
        {
            arenaSitting.gameTypeSetup.killScores[i] = 1;
        }*/

        if (ArenaSittingHooks.TryGetData(arenaSitting, out var extras))
        {
            wave = extras.currentWave - 1;
            initialTotalTime = extras.totalTime;
        }

        if (noRain is null)
            AddBehavior(new NoRain(this));

        if (respawnFlies is null)
            AddBehavior(new RespawnFlies(this));
        
        AddBehavior(new StartBump(this));
    }

    public override void Initiate()
    {
        Debug.Log("Initiate waves game session!");

        SpawnPlayers(room, null);
        base.Initiate();
        AddHUD();
        AnnounceWave();
    }

    public override void SpawnCreatures()
    {
        base.SpawnCreatures();
        NewWave();
    }

    private void AnnounceWave()
    {
        game.cameras[0].hud?.textPrompt?.AddMessage("Wave " + (wave+1), 0, 240, false, false);
    }

    private void SpawnPlayer(int playerIndex)
    {
        var playerList = arenaSitting.players;

        int exits = this.game.world.GetAbstractRoom(0).exits;
        int[] array = new int[exits];
        
        int num2 = UnityEngine.Random.Range(0, exits);
		float num3 = float.MinValue;
		for (int m = 0; m < exits; m++)
		{
			float num4 = UnityEngine.Random.value - (float)array[m] * 1000f;
			IntVector2 startTile = room.ShortcutLeadingToNode(m).StartTile;
			for (int n = 0; n < exits; n++)
			{
				if (n != m && array[n] > 0)
				{
					num4 += Mathf.Clamp(startTile.FloatDist(room.ShortcutLeadingToNode(n).StartTile), 8f, 17f) * UnityEngine.Random.value;
				}
			}
			if (num4 > num3)
			{
				num2 = m;
				num3 = num4;
			}
		}
		array[num2]++;
		AbstractCreature abstractCreature = new AbstractCreature(
            world: this.game.world,
            creatureTemplate: StaticWorld.GetCreatureTemplate("Slugcat"),
            realizedCreature: null,
            pos: new WorldCoordinate(0, -1, -1, -1),
            ID: new EntityID(-1, playerList[playerIndex].playerNumber)
        );
		if (ModManager.MSC && playerIndex == 0)
		{
			this.game.cameras[0].followAbstractCreature = abstractCreature;
		}
		if (this.chMeta != null)
		{
			abstractCreature.state = new PlayerState(abstractCreature, playerList[playerIndex].playerNumber, this.characterStats_Mplayer[0].name, false);
		}
		else
		{
			abstractCreature.state = new PlayerState(abstractCreature, playerList[playerIndex].playerNumber, new SlugcatStats.Name(ExtEnum<SlugcatStats.Name>.values.GetEntry(playerList[playerIndex].playerNumber), false), false);
		}
		abstractCreature.Realize();
		ShortcutHandler.ShortCutVessel shortCutVessel = new ShortcutHandler.ShortCutVessel(new IntVector2(-1, -1), abstractCreature.realizedCreature, this.game.world.GetAbstractRoom(0), 0);
		shortCutVessel.entranceNode = num2;
		shortCutVessel.room = this.game.world.GetAbstractRoom(0);
		abstractCreature.pos.room = this.game.world.offScreenDen.index;
		this.game.shortcuts.betweenRoomsWaitingLobby.Add(shortCutVessel);

        Debug.Log($"pos: {abstractCreature.realizedCreature.mainBodyChunk.pos.x}, {abstractCreature.realizedCreature.mainBodyChunk.pos.y}");

        // replace old player AbstractCreature in the Players list with the new one
        AbstractCreature oldCreature = null;
        for (int i = 0; i < Players.Count; i++)
        {
            if (Players[i].state is PlayerState state && state.playerNumber == playerList[playerIndex].playerNumber)
            {
                oldCreature = Players[i];
                Players[i] = abstractCreature;
                break;
            }
        }

        if (oldCreature is null)
        {
            Debug.Log("Player spawned in but was not previously in the session. How??");
            AddPlayer(abstractCreature);

            var hud = game.cameras[0].hud;
            var playerSpecificMultiplayerHud = new HUD.PlayerSpecificMultiplayerHud(hud, this, abstractCreature);
            hud.AddPart(playerSpecificMultiplayerHud);
            if (ModManager.MSC)
            {
                hud.AddPart(new MoreSlugcats.AmmoMeter(hud, playerSpecificMultiplayerHud, hud.fContainers[1]));
            }
        }
        else
        {
            // update hud tracker
            foreach (var hud in game.cameras[0].hud.parts.Where(x => x is HUD.PlayerSpecificMultiplayerHud))
            {
                var playerSpecificHud = hud as HUD.PlayerSpecificMultiplayerHud;

                if (playerSpecificHud.abstractPlayer == oldCreature)
                {
                    Debug.Log("Update tracked creature for HUD on the next frame");
                    stalePlayerHuds.Add((playerSpecificHud, abstractCreature));
                }
            }
        }
    }

    private void PlayerPermaDie(AbstractCreature player)
    {
        if (!Players.Contains(player)) return;
        Debug.Log("player permadie");
        permaDeadPlayers.Add(player);
    }

    private void NewWave()
    {
        wave++;

        Debug.Log("Spawn wave " + wave);

        gracePeriodTicker = 40 * 10;

        // respawn previously dead players
        // if player count is 0, players might not have spawned yet
        if (Players.Count > 0 && ArenaSittingHooks.TryGetData(arenaSitting, out var sittingData))
        {
            for (int i = 0; i < arenaSitting.players.Count; i++)
            {
                var playerNumber = arenaSitting.players[i].playerNumber;

                // check that player isn't dead
                var playerCreature = Players.Find(p => (p.state as PlayerState).playerNumber == playerNumber);
                if (playerCreature is null)
                {
                    Debug.Log($"could not find creature for player with number {playerNumber}");
                    continue;
                }

                if (playerCreature.state.alive) continue;
                if (sittingData.playerRespawnWait[i] > 0)
                {
                    sittingData.playerRespawnWait[i]--;
                    continue;
                }
                
                // respawn the player if they have enough lives
                // if on normal mode and player isn't perma-dead,
                // they get an extra chance
                if (sittingData.difficulty == WavesDifficultyOption.Normal && !permaDeadPlayers.Contains(playerCreature))
                    sittingData.playerLives[playerNumber]++;
                
                if (sittingData.playerLives[playerNumber] >= 1)
                {
                    sittingData.playerLives[playerNumber]--;

                    // do dissolve animation for old player corpse
                    if (playerCreature.realizedCreature?.room is not null)
                        playerCreature.realizedCreature.room.AddObject(new DespawnAnimation(playerCreature.realizedCreature));
                    
                    permaDeadPlayers.Remove(playerCreature);
                    SpawnPlayer(i);
                    sittingData.playerRespawnWait[i] = sittingData.respawnWait;
                }
            }
        }

        var abstractRoom = game.world.GetAbstractRoom(0);
        
        // spawn creatures
        var spawnData = waveData[Math.Min(wave, waveData.Length - 1)];

        int creaturesRemaining = Random.Range(spawnData.minCreatures, spawnData.maxCreatures+1);
        List<CreatureTemplate.Type> spawnList = new();

        // spawn creatures not tagged with Random first
        bool hasSkyExit = abstractRoom.AnySkyAccess;
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (creaturesRemaining == 0) continue;

            // NoSkyExit modifier
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.NoSkyExit) && hasSkyExit)
                continue;
            
            // don't spawn in sky creatures if there is no sky exit
            if (WavesCreatureSpawner.IsSkyCreature(spawnData.spawns[i].template) && !hasSkyExit)
                continue;
            
            if (!spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.RandomSpawn))
            {
                spawnList.Add(spawnData.spawns[i].template);
                creaturesRemaining--;
            }
        }

        // then for the remainder of the creatures randomly choose a creature
        // with the Random tag and spawn it
        // first, collect a list of creatures tagged with RandomSpawn
        List<CreatureTemplate.Type> randomSpawns = new();
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            // NoSkyExit modifier
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.NoSkyExit) && hasSkyExit)
                continue;

            // don't spawn in sky creatures if there is no sky exit
            if (WavesCreatureSpawner.IsSkyCreature(spawnData.spawns[i].template) && !hasSkyExit)
                continue;
            
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.RandomSpawn))
                randomSpawns.Add(spawnData.spawns[i].template);
        }

        // if no creatures were tagged with RandomSpawn, then just randomly select
        // all creatures
        if (randomSpawns.Count == 0)
        {
            for (int i = 0; i < spawnData.spawns.Length; i++)
            {
                randomSpawns.Add(spawnData.spawns[i].template);
            }   
        }

        while (creaturesRemaining > 0)
        {
            spawnList.Add(randomSpawns[Random.Range(0, randomSpawns.Count)]);
            creaturesRemaining--;
        }
        
        creatureSpawner = new WavesCreatureSpawner(abstractRoom, spawnList.ToArray(), OnSpawn);

        if (wave > 0)
            SaveSession(false);
    }

    private void SaveSession(bool quit)
    {
        if (GameTypeSetup.savingAndLoadingSession && ArenaSittingHooks.TryGetData(arenaSitting, out var extras))
        {
            extras.currentWave = wave;

            if (quit)
            {
                extras.totalTime = initialTotalTime;

                // if the players leave 10 seconds after the wave starts,
                // it decreases their karma, so they can't cheese it.
                // (just like in story mode!)
                // it seems that when reloading, the life count decreases by one
                // (presumably because it has to spawn the player?)
                // so i just add one to the life count if the player leaves during the grace period.
                if (gracePeriodTicker > 0)
                {
                    WavesMod.Instance.logger.LogInfo("Grace period");

                    for (int i = 0; i < extras.playerLives.Count; i++)
                    {
                        if (extras.playerLives[i] > 0) extras.playerLives[i]++;
                    }

                }
            }
            else
            {
                int maxTimeAlive = 0;
                foreach (var p in arenaSitting.players)
                {
                    if (p.timeAlive > maxTimeAlive)
                        maxTimeAlive = p.timeAlive;
                }

                extras.totalTime = initialTotalTime + maxTimeAlive;
            }

            /*if (gracePeriodTicker > 0)
            {
                for (int i = 0; i < extras.playerLives.Count; i++)
                {
                    if (extras.playerLives[i] >= 0) extras.playerLives[i]++;
                }
            }*/

            arenaSitting.SaveToFile(game.rainWorld);
        }
    }

    public override bool ShouldSessionEnd()
    {
        return thisFrameActivePlayers == 0;
    }

    private void OnSpawn(AbstractCreature creature)
    {
        trackedCreatures.Add(creature);
    }

    private bool IsCreatureFriendly(Creature creature)
    {
        return creature is Lizard liz && liz.AI.friendTracker.friend is Player;
    }

    private int _dbgLastFriendCount = 0;

    public override void Update()
    {
        base.Update();

        if (game.paused) return;
        if (gracePeriodTicker > 0) gracePeriodTicker--;

        if (creatureSpawner is not null && creatureSpawner.Update())
            creatureSpawner = null;
        
        // update stale player huds
        // i don't really know exactly when the mainBodyChunk position stops being (0, 0)
        // all i know is that i don't want the revive bump circle to be centered at that position
        for (int i = stalePlayerHuds.Count - 1; i >= 0; i--)
        {
            var (hud, creature) = stalePlayerHuds[i];

            var realized = creature.realizedCreature;
            if (realized is not null && !(realized.mainBodyChunk.pos.x == 0 && realized.mainBodyChunk.pos.y == 0))
            {
                hud.abstractPlayer = creature;
                stalePlayerHuds.RemoveAt(i);
            }
        }

        if (nextWaveTimer > 0)
        {
            if (--nextWaveTimer == 0 && thisFrameActivePlayers > 0)
            {
                NewWave();
                AnnounceWave();
                room.PlaySound(SoundID.UI_Multiplayer_Game_Start, 0f, 1f, 1f);
            }
        }
        else
        {
            bool noCreaturesRemaining = true;
            var friendCount = 0;

            for (int i = trackedCreatures.Count - 1; i >= 0; i--)
            {
                var creature = trackedCreatures[i];
                var realized = creature.realizedCreature;

                if (creature.state.alive)
                {
                    // if a lizard is a friend of the player, the player doesn't have to kill them
                    // in order for the next wave to continue
                    // but don't remove them from tracked creatures list in case they no longer like the player
                    // afterwards
                    if (realized is not null && IsCreatureFriendly(realized))
                    {
                        friendCount++;
                    }
                    
                    // if a creature wants to stay in den, stop tracking them and delete them
                    // ideally this behavior would be minimized but IDK how to code
                    // rather not deal with a softlock
                    else if (creature.InDen && (creature.WantToStayInDenUntilEndOfCycle() || creature.remainInDenCounter == -1))
                    {
                        Debug.Log("Creature wants to stay in den forever... remove");
                        creature.Room.RemoveEntity(creature);
                        creature.Destroy();
                        trackedCreatures.RemoveAt(i);
                    }
                    else
                    {
                        noCreaturesRemaining = false;
                    }
                }
                else if (creature.realizedCreature?.room is not null)
                {
                    creature.realizedCreature.room.AddObject(new DespawnAnimation(creature.realizedCreature));
                    trackedCreatures.RemoveAt(i);
                } else {
                    Debug.Log("creature died but is not realized or in a room. Destroy!");
                    creature.Room.RemoveEntity(creature);
                    creature.Destroy();
                    trackedCreatures.RemoveAt(i);
                }
            }

            if (_dbgLastFriendCount != friendCount)
            {
                _dbgLastFriendCount = friendCount;
                Debug.Log("Friend count changed to " + friendCount);
            }

            if (noCreaturesRemaining)
            {
                nextWaveTimer = 120;
            }
        }
    }

    public override void ProcessShutDown()
    {
        base.ProcessShutDown();
        
        WavesMod.Instance.logger.LogInfo("Quit session");
        if (wave > 0)
            SaveSession(true);
    }

    public void KillAll()
    {
        for (int i = trackedCreatures.Count - 1; i >= 0; i--)
        {
            var creature = trackedCreatures[i];
            if (creature.realizedCreature is not null && IsCreatureFriendly(creature.realizedCreature))
                continue;

            if (creature.realizedCreature is not null)
            {
                creature.realizedCreature?.Die();
            }
            else
            {
                // creature is not realized, probably still in its den or something.
                // just delete the creature.
                creature.Room.RemoveEntity(creature);
                creature.Destroy();
                trackedCreatures.RemoveAt(i);
            }
        }
    }

    /// <summary>
    /// Initialize hooks for creature/arena behavior during
    /// a Waves game session
    /// </summary>
    public static void InitHooks()
    {
        // this is the slugcat's personalized hell
        On.ArenaBehaviors.ExitManager.ExitsOpen += (On.ArenaBehaviors.ExitManager.orig_ExitsOpen orig, ExitManager self) =>
        {
            if (self.gameSession is WavesGameSession)
                return false;
            
            return orig(self);
        };
        
        // make sure creatures don't escape to their dens when injured
        // (basically, same as the mmf option, except not dependent on mmf)        
        On.LizardAI.WantToStayInDenUntilEndOfCycle += (On.LizardAI.orig_WantToStayInDenUntilEndOfCycle orig, LizardAI self) =>
        {
            if (self.creature.Room.world.game.session is WavesGameSession)
                return false;
            
            return orig(self);
        };

        On.InjuryTracker.Utility += (On.InjuryTracker.orig_Utility orig, InjuryTracker self) =>
        {
            if (self.AI.creature.Room.world.game.session is WavesGameSession) return 0f;
            return orig(self);
        };

        On.LizardAI.LizardInjuryTracker.Utility += (On.LizardAI.LizardInjuryTracker.orig_Utility orig, LizardAI.LizardInjuryTracker self) =>
        {
            if (self.AI.creature.Room.world.game.session is WavesGameSession) return 0f;
            return orig(self);
        };

        // make it so lizards are friendly to every player if they are tamed.
        // this enables that code path normally only enabled by the jolly co-op Friendly Lizards option.
        // TODO: crap i think etiquette is to provide own implementation of this if the user does not have
        // the DLC. Fix this before the Coppers come!s
        IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                // source:  if (ModManager.CoopAvailable && Custom.rainWorld.options.friendlyLizards)
                // desired: if (this.creature.Room.world.game.session is WavesGameSession || (Custom.rainWorldModManager.CoopAvailable && Custom.rainWorld.options.friendlyLizards))
                var branchLabel = cursor.DefineLabel();

                cursor.GotoNext(MoveType.Before,
                    x => x.MatchLdsfld(typeof(ModManager).GetField("CoopAvailable", BindingFlags.Public | BindingFlags.Static)),
                    x => x.MatchBrfalse(out _),
                    x => x.MatchLdsfld(typeof(RWCustom.Custom).GetField("rainWorld", BindingFlags.Public | BindingFlags.Static)),
                    x => x.MatchLdfld(typeof(RainWorld).GetField("options")),
                    x => x.MatchLdfld(typeof(Options).GetField("friendlyLizards")),
                    x => x.MatchBrfalse(out _)
                );

                cursor.Emit(OpCodes.Ldarg_0);
                cursor.EmitDelegate((LizardAI self) =>
                {
                    return self.creature.Room.world.game.session is WavesGameSession;
                });
                cursor.Emit(OpCodes.Brtrue, branchLabel);
                cursor.Index += 6;
                cursor.MarkLabel(branchLabel);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship failed! " + e);
            }
        };

        On.VultureAI.DisencouragedTracker.Utility += (On.VultureAI.DisencouragedTracker.orig_Utility orig, VultureAI.DisencouragedTracker self) =>
        {
            if (self.AI.creature.Room.world.game.session is WavesGameSession) return 0f;
            return orig(self);
        };

        On.VultureAI.Update += (On.VultureAI.orig_Update orig, VultureAI self) =>
        {
            Debug.Log(self.behavior);
            orig(self);
        };

        On.AbstractCreature.WantToStayInDenUntilEndOfCycle += (On.AbstractCreature.orig_WantToStayInDenUntilEndOfCycle orig, AbstractCreature self) =>
        {
            if (self.world.game.session is WavesGameSession) return orig(self);
            return false;
        };

        // IL.VultureAI.Update += (il) =>
        // {
        //     try
        //     {
        //         var cursor = new ILCursor(il);

        //         cursor.GotoNext(MoveType.Before,
        //             x => x.MatchLdarg(0),
        //             x => x.MatchLdsfld(typeof(VultureAI.Behavior).GetField("Disencouraged", BindingFlags.Public | BindingFlags.Static)),
        //             x => x.MatchStfld(typeof(VultureAI).GetField("behavior"))
        //         );

        //         cursor.EmitDelegate(() => {
        //             Debug.Log("set disencouraged!!! no!!");
        //         });
        //     }
        //     catch (Exception e)
        //     {
        //         WavesMod.Instance.logger.LogError("IL.VultureAI.Update failed! " + e);
        //     }
        // };

        // flies turn into spears when grabbed
        On.Fly.Update += (On.Fly.orig_Update orig, Fly self, bool eu) =>
        {
            orig(self, eu);
            if (self.room is null) return;

            var game = self.room.game;

            if (game.session is not WavesGameSession)
                return;
            
            Player player = null;
            Creature.Grasp grasp = null;

            foreach (var g in self.grabbedBy)
            {
                if (g.grabber is Player p)
                {
                    player = p;
                    grasp = g;
                    break;
                }
            }
            
            if (player is null || grasp is null) return;

            Debug.Log("fly was grabbed");

            // replace the grasp with a spear grasp
            var spear = new AbstractSpear(game.world, null, self.abstractCreature.pos, game.GetNewID(), false);
            spear.RealizeInRoom();
            self.room.PlaySound(SoundID.Fly_Caught, self.bodyChunks[0].pos);

            grasp.Release();

            if (player.CanIPickThisUp(spear.realizedObject))
            {
                // if hands are full...
                if (
                    (player.grasps[0] != null && player.grasps[1] != null) ||    
                    (player.grasps[0] != null && player.Grabability(spear.realizedObject) >= Player.ObjectGrabability.BigOneHand) ||
                    (player.grasps[1] != null && player.Grabability(spear.realizedObject) >= Player.ObjectGrabability.BigOneHand)
                )
                {
                    if (player.CanPutSpearToBack && !player.spearOnBack.HasASpear)
                    {
                        self.room.PlaySound(SoundID.Slugcat_Switch_Hands_Init, player.mainBodyChunk);
                        player.spearOnBack.SpearToBack(spear.realizedObject as Spear);
                    }
                }
                else
                {
                    player.SlugcatGrab(spear.realizedObject, player.FreeHand());
                }
            }

            for (int i = 0; i < 8; i++)
            {
                self.room.AddObject(new ParticleEffects.FlyTransformParticle(self.mainBodyChunk.pos));
            }

            self.Destroy();
        };

        // perma-die player when they are destroyed
        // this occurs when player falls off room and when player is eaten by rot or worm grass
        // does not occur when a player is dragged into a den, sky or ground 
        On.Player.Destroy += (On.Player.orig_Destroy orig, Player self) =>
        {
            orig(self);

            if (self.room.game.session is WavesGameSession wavesSesh)
            {
                wavesSesh.PlayerPermaDie(self.abstractCreature);
            }
        };

        On.Vulture.AccessSkyGate += (
            On.Vulture.orig_AccessSkyGate orig, Vulture self,
            WorldCoordinate start, WorldCoordinate dest
        ) =>
        {
            orig(self, start, dest);
            if (self.room.game.session is WavesGameSession wavesSesh)
            {
                foreach (var grasp in self.grasps)
                {
                    if (grasp?.grabbed is Player player)
                    {
                        wavesSesh.PlayerPermaDie(player.abstractCreature);
                    }
                }
            }
        };

        On.AbstractCreature.IsEnteringDen += (
            On.AbstractCreature.orig_IsEnteringDen orig, AbstractCreature self,
            WorldCoordinate den
        ) =>
        {
            if (self.world.game.session is WavesGameSession wavesSesh)
            {
                if (self.creatureTemplate.quantified && self.creatureTemplate.type == CreatureTemplate.Type.Fly)
                {
                    orig(self, den);
                    return;
                }

                for (int i = self.stuckObjects.Count - 1; i >= 0; i--)
                {
                    if (i < self.stuckObjects.Count && self.stuckObjects[i] is AbstractPhysicalObject.CreatureGripStick && self.stuckObjects[i].A == self)
                    {
                        if (self.stuckObjects[i].B is AbstractCreature)
                        {
                            if ((self.stuckObjects[i].B as AbstractCreature).creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                            {
                                wavesSesh.PlayerPermaDie(self.stuckObjects[i].B as AbstractCreature);
                            }
                        }
                    }
                }

                orig(self, den);
                return;
            }

            orig(self, den);
        };

        // change whether or not a player spawns on a new session
        IL.ArenaGameSession.SpawnPlayers += (il) =>
        {
            try
            {
                var cursor = new ILCursor(il);

                // Go to:
                // for (int l = 0; l < arenaPlayerList.Count; l++)
                //                 ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^^
                // and store label here so we can jump there for a continue
                // as well as the label back to the start of the loop so we
                // can inject logic there.
                var forLoopContinue = cursor.DefineLabel();
                ILLabel forLoopStart = null;
                cursor.GotoNext(
                    // for (int l = 0; l < arenaPlayerList.Count; l++)
                    //                                            ^^^
                    x => x.MatchLdloc(8),
                    x => x.MatchLdcI4(1),
                    x => x.MatchAdd(),
                    x => x.MatchStloc(8),
                    
                    // for (int l = 0; l < arenaPlayerList.Count; l++)
                    //                 ^^^^^^^^^^^^^^^^^^^^^^^^^
                    x => x.MatchLdloc(8),
                    x => x.MatchLdloc(0),
                    x => x.MatchCallvirt(typeof(List<ArenaSitting.ArenaPlayer>).GetMethod("get_Count")),
                    x => x.MatchBlt(out forLoopStart)
                );
                cursor.MarkLabel(forLoopContinue);

                // go to the start of the loop
                // and make it so that if the injected logic determines that the player should not spawn,
                // "continue" the for loop to the next index
                cursor.GotoLabel(forLoopStart, MoveType.AfterLabel, false);
                cursor.Emit(OpCodes.Ldarg_0);
                cursor.Emit(OpCodes.Ldloc, 8);
                cursor.Emit(OpCodes.Ldloc, 0);
                cursor.EmitDelegate((ArenaGameSession self, int playerIndex, List<ArenaSitting.ArenaPlayer> playerList) =>
                {
                    if (self.game.session is not WavesGameSession)
                        return true;

                    // if the given player has at least one life, consume it to spawn the player
                    if (ArenaSittingHooks.TryGetData(self.arenaSitting, out var data))
                    {
                        // wat...
                        if (playerIndex >= data.playerLives.Count) return false;

                        var playerNumber = playerList[playerIndex].playerNumber;
                        if (data.playerLives[playerNumber] >= 1)
                        {
                            data.playerLives[playerNumber]--;
                            return true;
                        }
                        
                        // player ran out of lives, will not respawn
                        playerList[playerIndex].alive = false;
                        return false;
                    }

                    return true;
                });
                cursor.Emit(OpCodes.Brfalse, forLoopContinue);
            }
            catch (Exception e)
            {
                WavesMod.Instance.logger.LogError("IL.ArenaGameSession.SpawnPlayers failed! " + e);
            }
        };
    }
}