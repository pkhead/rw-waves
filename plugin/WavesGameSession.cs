using System.Collections.Generic;
using BepInEx;
using UnityEngine;
namespace WavesMod;

class WavesGameSession : ArenaGameSession
{
    public WavesGameSession(RainWorldGame game) : base(game)
    {}

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

        var abstractRoom = game.world.GetAbstractRoom(0);

        // get nodes that are dens
        var availableDens = new List<int>();
        for (int i = 0; i < abstractRoom.nodes.Length; i++)
        {
            if (abstractRoom.nodes[i].type == AbstractRoomNode.Type.Den)
                availableDens.Add(i);
        }

        // spawn creatures
        for (int i = 0; i < 2; i++)
        {
            if (availableDens.Count == 0) break;

            var denIndexIndex = Random.Range(0, availableDens.Count); // the index of the den index
            var coords = new WorldCoordinate(abstractRoom.index, -1, -1, availableDens[denIndexIndex]);

            var template = StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.PinkLizard);
            var creature = new AbstractCreature(game.world, template, null, coords, game.GetNewID());
            abstractRoom.MoveEntityToDen(creature);

            availableDens.RemoveAt(denIndexIndex);
        }
    }

    public override bool ShouldSessionEnd()
    {
        return thisFrameActivePlayers == 0;
    }
}