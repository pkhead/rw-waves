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

class WavesCreatureSpawner
{
    public event Action<AbstractCreature> CreatureSpawned;

    private readonly AbstractRoom room;
    private readonly Queue<CreatureTemplate.Type> creatureQueue;
    private readonly int[] denIndices;
    private readonly int[] shuffledDenIndexIndices;

    public WavesCreatureSpawner(AbstractRoom room, CreatureTemplate.Type[] creatures)
    {
        this.room = room;
        creatureQueue = new Queue<CreatureTemplate.Type>(creatures);
        
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

        shuffledDenIndexIndices = new int[denIndices.Length];
    }

    /// <summary>
    /// Update the WavesCreatureSpawner.
    /// </summary>
    /// <returns>True if the creature spawner is finished, false if not.</returns>
    public bool Update()
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
                foreach (var creature in room.entitiesInDens)
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

    private void SpawnCreature(CreatureTemplate.Type type, int abstractNode)
    {
        var coords = new WorldCoordinate(room.index, -1, -1, abstractNode);
        var template = StaticWorld.GetCreatureTemplate(type);
        var creature = new AbstractCreature(room.world, template, null, coords, room.world.game.GetNewID());
        room.MoveEntityToDen(creature);

        CreatureSpawned?.Invoke(creature);
    }
}