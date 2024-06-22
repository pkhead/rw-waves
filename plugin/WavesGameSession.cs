using System;
using System.Collections.Generic;
using ArenaBehaviors;
using UnityEngine;
namespace WavesMod;
using Random = UnityEngine.Random;

class WavesGameSession : ArenaGameSession
{
    // if all tracked creatures are dead, initiate the next wave
    private readonly List<AbstractCreature> trackedCreatures;
    private WavesCreatureSpawner creatureSpawner = null;

    public int wave = -1;
    private int nextWaveTimer = -1;

    public WavesGameSession(RainWorldGame game) : base(game)
    {
        rainCycleTimeInMinutes = 0;
        trackedCreatures = new();
        
        arenaSitting.gameTypeSetup.levelItems = true;
        arenaSitting.gameTypeSetup.spearHitScore = 0;
        arenaSitting.gameTypeSetup.foodScore = 0;
        arenaSitting.gameTypeSetup.rainWhenOnePlayerLeft = false;

        // i think this will make award one point for killing any type of creature?
        for (int i = 0; i < arenaSitting.gameTypeSetup.killScores.Length; i++)
        {
            arenaSitting.gameTypeSetup.killScores[i] = 1;
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

    private void NewWave()
    {
        wave++;

        Debug.Log("Spawn wave " + wave);

        var abstractRoom = game.world.GetAbstractRoom(0);

        // get nodes that are dens
        var availableDens = new List<int>();
        for (int i = 0; i < abstractRoom.nodes.Length; i++)
        {
            if (abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
                availableDens.Add(i);
        }

        // spawn creatures
        trackedCreatures.Clear();
        var spawnData = WaveSpawnData.Data[Math.Min(wave, WaveSpawnData.Data.Length - 1)];

        int creaturesRemaining = Random.Range(spawnData.minCreatures, spawnData.maxCreatures+1);
        List<CreatureTemplate.Type> spawnList = new();

        // spawn creatures not tagged with Random first
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (creaturesRemaining == 0) return;
            if (spawnData.spawns[i].modifier != WaveSpawnData.SpawnModifier.RandomSpawn)
            {
                spawnList.Add(spawnData.spawns[i].template);
                creaturesRemaining--;
            }
        }

        // then for the remainder of the creatures randomly choose a creature
        // with the Random tag and spawn it
        List<CreatureTemplate.Type> randomSpawns = new();
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (spawnData.spawns[i].modifier == WaveSpawnData.SpawnModifier.RandomSpawn)
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
        
        creatureSpawner = new WavesCreatureSpawner(abstractRoom, spawnList.ToArray());
        creatureSpawner.CreatureSpawned += OnSpawn;
    }

    public override bool ShouldSessionEnd()
    {
        return thisFrameActivePlayers == 0;
    }

    private void OnSpawn(AbstractCreature creature)
    {
        trackedCreatures.Add(creature);
    }

    public override void Update()
    {
        base.Update();

        if (game.paused) return;

        if (creatureSpawner is not null && creatureSpawner.Update())
            creatureSpawner = null;

        if (nextWaveTimer > 0)
        {
            if (--nextWaveTimer == 0)
            {
                NewWave();
                AnnounceWave();
                room.PlaySound(SoundID.UI_Multiplayer_Game_Start, 0f, 1f, 1f);
            }
        }
        else
        {
            bool noCreaturesRemaining = true;
            for (int i = trackedCreatures.Count - 1; i >= 0; i--)
            {
                var creature = trackedCreatures[i];

                if (creature.state.alive)
                {
                    noCreaturesRemaining = false;
                }
                else if (creature.realizedCreature?.room is not null)
                {
                    creature.realizedCreature.room.AddObject(new DespawnAnimation(creature.realizedCreature));
                    trackedCreatures.RemoveAt(i);
                }
            }

            if (noCreaturesRemaining)
            {
                nextWaveTimer = 120;
            }
        }
    }

    public void KillAll()
    {
        for (int i = trackedCreatures.Count - 1; i >= 0; i--)
        {
            var creature = trackedCreatures[i];

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
    }
}