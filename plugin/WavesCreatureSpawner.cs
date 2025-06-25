using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace WavesMod;

// ok well uh
// turns out there can be more than one creature in a den
// and they come out and stuff and everything works fine.
// However with this WavesCreatureSpawner, they come out more slowly,
// which may be more desirable.

struct CreatureSpawnData
{
    public CreatureTemplate.Type type;
    public EntityID? id;

    public CreatureSpawnData(CreatureTemplate.Type type, EntityID? id)
    {
        this.type = type;
        this.id = id;
    }
}

class WavesCreatureSpawner
{
    private Action<AbstractCreature> creatureSpawnCb;

    private readonly AbstractRoom room;
    private readonly Queue<CreatureSpawnData> creatureQueue;
    private readonly int[] denIndices;
    private readonly int[] shuffledDenIndexIndices;

    public static bool IsSkyCreature(CreatureTemplate.Type templateType)
    {
        return templateType == CreatureTemplate.Type.Vulture ||
               templateType == CreatureTemplate.Type.KingVulture ||
               templateType == DLCSharedEnums.CreatureTemplateType.MirosVulture;
    }

    public WavesCreatureSpawner(AbstractRoom room, CreatureSpawnData[] creatures, Action<AbstractCreature> spawnCallback)
    {
        this.room = room;
        creatureSpawnCb = spawnCallback;
        
        // get nodes that are dens
        denIndices = new int[room.dens];
        int j = 0;
        for (int i = 0; i < room.nodes.Length; i++)
        {
            if (room.nodes[i].type == AbstractRoomNode.Type.Den)
            {
                denIndices[j++] = i;
            }
        }

        bool skyAccess = room.AnySkyAccess;
        if (!skyAccess)
            Debug.Log("Room has no sky exit, not spawning vultures");

        shuffledDenIndexIndices = new int[denIndices.Length];

        // spawn vultures in off-screen dens
        // if there are no sky exits, still iterate through this loop because
        // i need to exclude vultures from the creature queue
        creatureQueue = new Queue<CreatureSpawnData>();

        foreach (var creatureData in creatures)
        {
            if (IsSkyCreature(creatureData.type))
            {
                if (!skyAccess) continue;
                var creature = CreateCreature(creatureData, room.world.offScreenDen.index, 0);
                room.world.offScreenDen.AddEntity(creature);

                creatureSpawnCb(creature);
            }
            else
            {
                creatureQueue.Enqueue(creatureData);
            }
        }
    }

    /// <summary>
    /// Update the WavesCreatureSpawner.
    /// </summary>
    /// <returns>True if the creature spawner is finished, false if not.</returns>
    public bool Update(WavesGameSession session)
    {
        while (creatureQueue.Count > 0)
        {
            ShuffleDenSearch();
            bool success = false;

            // spawn the next queued creature in any available den
            for (int i = 0; i < shuffledDenIndexIndices.Length; i++)
            {
                var den = denIndices[shuffledDenIndexIndices[i]];
                bool isDenOccupied = false;

                // check if a creature is in this den
                foreach (var creature in session.trackedCreatures)
                {
                    if (creature.pos.abstractNode == den)
                    {
                        isDenOccupied = true;
                        break;
                    }
                }

                // if not occupied, spawn creature
                if (!isDenOccupied)
                {
                    Debug.Log("spawn creature " + creatureQueue.Count);
                    var type = creatureQueue.Dequeue();
                    SpawnCreature(type, den);
                    success = true;
                    break;
                }
            }

            if (!success) break;
        }

        return creatureQueue.Count == 0;
    }

    private void ShuffleDenSearch()
    {
        // shuffle den index index queue
        for (int i = 0; i < shuffledDenIndexIndices.Length; i++)
        {
            shuffledDenIndexIndices[i] = i;
        }

        for (int i = 0; i < shuffledDenIndexIndices.Length - 1; i++)
        {
            int swapIndex = i;
            while (swapIndex == i) swapIndex = Random.Range(0, shuffledDenIndexIndices.Length);
            (shuffledDenIndexIndices[i], shuffledDenIndexIndices[swapIndex]) = (shuffledDenIndexIndices[swapIndex], shuffledDenIndexIndices[i]);
        }
    }

    private void SpawnCreature(CreatureSpawnData spawn, int abstractNode)
    {
        var creature = CreateCreature(spawn, room.index, abstractNode);
        room.MoveEntityToDen(creature);

        creatureSpawnCb(creature);
    }

    private AbstractCreature CreateCreature(CreatureSpawnData creatureSpawn, int roomIndex, int abstractNode)
    {
        var coords = new WorldCoordinate(roomIndex, -1, -1, abstractNode);
        var template = StaticWorld.GetCreatureTemplate(creatureSpawn.type);
        var creature = new AbstractCreature(room.world, template, null, coords, creatureSpawn.id ?? room.world.game.GetNewID());

        return creature;
    }
}