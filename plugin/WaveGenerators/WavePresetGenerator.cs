using UnityEngine;
using System.Collections.Generic;
namespace WavesMod;

class WavePresetGenerator : IWaveGenerator
{
    public int WaveCount => waveData.Length;
    private readonly WaveSpawnData.WaveData[] waveData;

    public WavePresetGenerator()
    {
        waveData = WaveSpawnData.Read().Waves;
    }

    public CreatureSpawnData[] GenerateWave(int wave, bool hasSkyExit)
    {
        var spawnData = waveData[Mathf.Min(wave, waveData.Length - 1)];

        int creaturesRemaining = Random.Range(spawnData.minCreatures, spawnData.maxCreatures+1);
        List<CreatureSpawnData> spawnList = new();

        static bool CreateSpawnData(WaveSpawnData.WaveSpawn jsonData, out CreatureSpawnData spawn)
        {
            if (jsonData.template.index == -1)
            {
                spawn = new CreatureSpawnData(null, null);
                var str = $"unknown creature template type {jsonData.template.value}!";
                Debug.LogWarning(str);
                WavesMod.Instance.logger.LogWarning(str);
                return false;
            }
            
            if (jsonData.IDs != null && jsonData.IDs.Length > 0)
            {
                var idNum = jsonData.IDs[Random.Range(0, jsonData.IDs.Length)];
                spawn = new CreatureSpawnData(jsonData.template, new EntityID(-1, idNum));
                return true;
            }
            else
            {
                spawn = new CreatureSpawnData(jsonData.template, null);
                return true;
            }
        }

        // spawn creatures not tagged with Random first
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            if (creaturesRemaining == 0) continue;

            // NoSkyExit modifier
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.NoSkyExit) && hasSkyExit)
                continue;
            
            // don't spawn in sky creatures if there is no sky exit
            if (WavesCreatureSpawner.IsSkyCreature(spawnData.spawns[i].template) && !hasSkyExit)
                continue;
            
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.RandomSpawn))
                continue;

            if (CreateSpawnData(spawnData.spawns[i], out var spawn))
            {
                spawnList.Add(spawn);
                creaturesRemaining--;
            }
        }

        // then for the remainder of the creatures randomly choose a creature
        // with the Random tag and spawn it
        // first, collect a list of creatures tagged with RandomSpawn
        List<CreatureSpawnData> randomSpawns = new();
        for (int i = 0; i < spawnData.spawns.Length; i++)
        {
            // NoSkyExit modifier
            if (spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.NoSkyExit) && hasSkyExit)
                continue;

            // don't spawn in sky creatures if there is no sky exit
            if (WavesCreatureSpawner.IsSkyCreature(spawnData.spawns[i].template) && !hasSkyExit)
                continue;
            
            if (!spawnData.spawns[i].modifier.HasFlag(WaveSpawnData.SpawnModifiers.RandomSpawn))
                continue;

            if (CreateSpawnData(spawnData.spawns[i], out var spawn))
            {
                randomSpawns.Add(spawn);
            }
        }

        // if no creatures were tagged with RandomSpawn, then just randomly select
        // all creatures
        if (randomSpawns.Count == 0)
        {
            for (int i = 0; i < spawnData.spawns.Length; i++)
            {
                if (CreateSpawnData(spawnData.spawns[i], out var spawn))
                    randomSpawns.Add(spawn);
            }   
        }

        while (creaturesRemaining > 0)
        {
            spawnList.Add(randomSpawns[Random.Range(0, randomSpawns.Count)]);
            creaturesRemaining--;
        }

        return spawnList.ToArray();
    }
}