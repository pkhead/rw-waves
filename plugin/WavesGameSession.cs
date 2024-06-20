using System;
using System.Collections.Generic;
using ArenaBehaviors;
using BepInEx;
using UnityEngine;
namespace WavesMod;
using Random = UnityEngine.Random;

class WavesGameSession : ArenaGameSession
{
    // if all tracked creatures are dead, initiate the next wave
    private readonly List<AbstractCreature> trackedCreatures;
    public int wave = -1;
    private int nextWaveTimer = -1;

    private enum SpawnModifier
    {
        None,
        RandomSpawn
    }

    private struct WaveSpawn
    {
        public CreatureTemplate.Type template;
        public SpawnModifier modifier;

        public WaveSpawn(CreatureTemplate.Type template, SpawnModifier modifier = SpawnModifier.None)
        {
            this.template = template;
            this.modifier = modifier;
        }
    }

    private record WaveData
    {
        public int minCreatures, maxCreatures;
        public WaveSpawn[] spawns;

        public WaveData(int min, int max, WaveSpawn[] spawns)
        {
            minCreatures = min;
            maxCreatures = max;
            this.spawns = spawns;
        }

        public WaveData(int amount, WaveSpawn[] spawns)
        {
            minCreatures = amount;
            maxCreatures = amount;
            this.spawns = spawns;
        }
    }

    private static readonly WaveData[] spawnTable = new WaveData[]
    {
        // Wave 1
        new(
            amount: 1,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard)
            }
        ),

        // Wave 2
        new(
            amount: 2,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.PinkLizard)
            }
        ),

        // Wave 3
        new(
            amount: 3,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.GreenLizard),
            }
        ),

        // Wave 4
        new(
            amount: 3,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.BlueLizard),
                new(CreatureTemplate.Type.GreenLizard),
            }
        ),

        // Wave 5
        new(
            min: 3,
            max: 4,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.GreenLizard),
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifier.RandomSpawn),
            }
        ),

        // Wave 6
        new(
            min: 3,
            max: 4,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifier.RandomSpawn),
            }
        ),

        // Wave 7
        new(
            amount: 6,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
            }
        ),

        // FINAL WAVE... goes on forever
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.CyanLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.YellowLizard, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.Centipede, SpawnModifier.RandomSpawn),
                new(CreatureTemplate.Type.Scavenger, SpawnModifier.RandomSpawn),
            }
        ),
    };

    public WavesGameSession(RainWorldGame game) : base(game)
    {
        rainCycleTimeInMinutes = 0;
        trackedCreatures = new();

        // TODO: modify this.arenaSitting instead of doing this
        if (noRain is null)
            AddBehavior(new NoRain(this));

        if (respawnFlies is null)
            AddBehavior(new RespawnFlies(this));
    }

    public override void Initiate()
    {
        Debug.Log("Initiate waves game session!");

        SpawnPlayers(room, null);
        base.Initiate();
        AddHUD();
    }

    public override void SpawnCreatures()
    {
        base.SpawnCreatures();
        NewWave();
    }

    private void NewWave()
    {
        wave++;

        Debug.Log("Spawn wave " + wave);

        var abstractRoom = game.world.GetAbstractRoom(0);
        abstractRoom.realizedRoom?.PlaySound(SoundID.UI_Multiplayer_Game_Start, 0f, 1f, 1f);

        // get nodes that are dens
        var availableDens = new List<int>();
        for (int i = 0; i < abstractRoom.nodes.Length; i++)
        {
            if (abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
                availableDens.Add(i);
        }

        // spawn creatures
        trackedCreatures.Clear();
        var spawnData = spawnTable[Math.Min(wave, spawnTable.Length - 1)];

        int creaturesRemaining = Math.Min(Random.Range(spawnData.minCreatures, spawnData.maxCreatures), availableDens.Count);
        void SpawnCreature(CreatureTemplate.Type type)
        {
            var denIndexIndex = Random.Range(0, availableDens.Count); // the index of the den index
            var coords = new WorldCoordinate(abstractRoom.index, -1, -1, availableDens[denIndexIndex]);

            var template = StaticWorld.GetCreatureTemplate(type);
            var creature = new AbstractCreature(game.world, template, null, coords, game.GetNewID());
            abstractRoom.MoveEntityToDen(creature);

            creaturesRemaining--;
            availableDens.RemoveAt(denIndexIndex);
            trackedCreatures.Add(creature);
        }

        // spawn creatures not tagged with Random first
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (creaturesRemaining == 0) return;
            if (spawnData.spawns[i].modifier != SpawnModifier.RandomSpawn)
                SpawnCreature(spawnData.spawns[i].template);
        }

        // then for the remainder of the creatures randomly choose a creature
        // with the Random tag and spawn it
        List<CreatureTemplate.Type> randomSpawns = new();
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (spawnData.spawns[i].modifier == SpawnModifier.RandomSpawn)
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
            SpawnCreature(randomSpawns[Random.Range(0, randomSpawns.Count - 1)]);
    }

    public override bool ShouldSessionEnd()
    {
        return thisFrameActivePlayers == 0;
    }

    public override void Update()
    {
        base.Update();

        if (nextWaveTimer > 0)
        {
            if (--nextWaveTimer == 0)
                NewWave();
        }
        else
        {
            bool noCreaturesRemaining = true;
            foreach (var creature in trackedCreatures)
            {
                if (creature.state.alive)
                {
                    noCreaturesRemaining = false;
                    break;
                }
            }

            if (noCreaturesRemaining)
            {
                nextWaveTimer = 80;
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
                trackedCreatures.RemoveAt(i);
            }
        }
    }
}