using System;
using System.IO;
using CompactJson;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace WavesMod;

class WaveRandomizerGenerator : IWaveGenerator
{
    public int WaveCount => -1;

    class CreatureRandomData
    {
        [JsonProperty("creature")]
        public string Creature { get; set; }

        [JsonProperty("creatures")]
        public string[] Creatures { get; set; }

        [JsonProperty("points")]
        public int Points { get; set; }

        [JsonProperty("max")]
        public int Max { get; set; } = int.MaxValue;

        [JsonProperty("curveStart")]
        public float CurveStart { get; set; }

        [JsonProperty("startWeight")]
        public float StartWeight { get; set; } = 0.0f;

        [JsonProperty("curvePeak")]
        public float CurvePeak { get; set; }
        
        [JsonProperty("peakWeight")]
        public float PeakWeight { get; set; }

        [JsonProperty("curveEnd")]
        public float CurveEnd { get; set; }

        [JsonProperty("endWeight")]
        public float EndWeight { get; set; }
    }

    private CreatureRandomData[] creatureData;
    private readonly System.Random rng;

    public WaveRandomizerGenerator(int? seed = null)
    {
        var randomDataPath = AssetManager.ResolveFilePath("wavedata/randconfig.json");
        creatureData = Serializer.Parse<CreatureRandomData[]>(File.ReadAllText(randomDataPath));

        if (seed is not null)
        {
            rng = new System.Random(seed.Value);
        }
        else
        {
            rng = null;
        }
    }

    float GenRandomNumber()
    {
        if (rng is not null)
        {
            return (float)rng.NextDouble();
        }
        else
        {
            return UnityEngine.Random.value;
        }
    }

    struct Partition
    {
        public float min;
        public float max;
        public CreatureRandomData data;

        public Partition(float min, float max, CreatureRandomData data)
        {
            this.min = min;
            this.max = max;
            this.data = data;
        }
    }

    (float weight, CreatureRandomData data)[] CalcProbabilityWeights(int wave, Func<CreatureRandomData, bool> filterFunc)
    {
        var probabilityWeights = new List<(float weight, CreatureRandomData data)>(creatureData.Length);

        for (int i = 0; i < creatureData.Length; i++)
        {
            if (!filterFunc(creatureData[i])) continue;

            var data = creatureData[i];
            float weight;
            if (wave <= data.CurvePeak)
            {
                var p = (data.CurvePeak - wave) / (data.CurvePeak - data.CurveStart);
                weight = (data.PeakWeight - data.StartWeight) * Mathf.Pow(0.01f, p*p) + data.StartWeight;
            }
            else
            {
                var p = (wave - data.CurvePeak) / (data.CurveEnd - data.CurvePeak);
                weight = (data.PeakWeight - data.EndWeight) * Mathf.Pow(0.01f, p*p) + data.EndWeight;
            }

            if (weight >= 0.01f)
                probabilityWeights.Add((weight, creatureData[i]));
        }

        return probabilityWeights.ToArray();
    }

    void BuildProbabilityPartitions(IEnumerable<(float weight, CreatureRandomData data)> weights, List<Partition> output, Func<CreatureRandomData, bool> filter)
    {
        var probabilityWeights = weights.Where(x => filter(x.data)).ToArray();

        // get sum of probability weights
        var sum = 0f;
        foreach (var (weight, data) in probabilityWeights)
        {
            sum += weight;
        }
        
        // create partitions
        output.Clear();
        var runningPos = 0f;
        for (int i = 0; i < probabilityWeights.Length; i++)
        {
            var width = probabilityWeights[i].weight / sum;
            output.Add(new Partition(runningPos, runningPos + width, probabilityWeights[i].data));
            runningPos += width;
        }
    }

    static bool CreatureFilter(CreatureRandomData data, bool hasSkyExit)
    {
        bool ProcessSingularCreature(string creatureName)
        {
            var template = new CreatureTemplate.Type(creatureName, false);
            if (template.index == -1)
            {
                var str = "unrecognized creature template type " + creatureName;
                WavesMod.Instance.logger.LogWarning(str);
                Debug.LogWarning(str);
                return false;
            }

            if (WavesCreatureSpawner.IsSkyCreature(template) && !hasSkyExit)
                return false;
            
            return true;
        }

        if (data.Creatures is not null)
        {
            foreach (var creatureName in data.Creatures)
            {
                if (!ProcessSingularCreature(creatureName))
                    return false;
            }
        }
        else // assume data.Creature is not null either
        {
            if (!ProcessSingularCreature(data.Creature))
                return false;
        }

        return true;
    }

    public CreatureSpawnData[] GenerateWave(int wave, bool hasSkyExit)
    {
        var weights = CalcProbabilityWeights(wave, (data) => CreatureFilter(data, hasSkyExit));

        if (weights.Length == 0)
        {
            var str = "no creatures available?";
            WavesMod.Instance.logger.LogWarning(str);
            Debug.LogWarning(str);

            return Array.Empty<CreatureSpawnData>();
        }

        var pointsRemaining = wave + 1;
        List<CreatureSpawnData> spawnList = new();
        List<Partition> possiblePartitions = new();
        var creatureCounts = new int[creatureData.Length];

        for (int i = 0; i < creatureCounts.Length; i++)
            creatureCounts[i] = 0;

        bool PassFilter(CreatureRandomData data)
        {
            if (data.Points > pointsRemaining) return false;
            var i = creatureData.IndexOf(data);
            if (i != -1)
            {
                if (creatureCounts[i] > data.Max) return false;
            }

            return true;
        }

        {
            var tmpList = new List<Partition>();
            BuildProbabilityPartitions(weights, tmpList, PassFilter);
            Debug.Log($"BEGIN PARTITIONS (Points: {pointsRemaining})");
            foreach (var part in tmpList)
            {
                Debug.Log($"{part.data.Creatures?[0] ?? part.data.Creature} chance:{100f * (part.max - part.min)}%");
            }
            Debug.Log("END PARTITIONS");
        }

        while (pointsRemaining > 0)
        {
            // rebuild partition list
            BuildProbabilityPartitions(weights, possiblePartitions, PassFilter);

            if (possiblePartitions.Count == 0)
            {
                Debug.Log("no more possible creatures");
                break;
            }

            // sample a partition
            Partition? chosenPart = null;
            var randNum = GenRandomNumber();
            foreach (var part in possiblePartitions)
            {
                if (randNum >= part.min && randNum < part.max)
                {
                    chosenPart = part;
                    break;
                }
            }

            if (!chosenPart.HasValue)
            {
                var str = "no probability partitions chosen?";
                WavesMod.Instance.logger.LogWarning(str);
                Debug.LogWarning(str);
                return Array.Empty<CreatureSpawnData>();
            }

            var data = chosenPart.Value.data;

            pointsRemaining -= data.Points;
            if (data.Creatures is not null)
            {
                Debug.Log($"spawn group {data.Creatures[0]}, et al.");
                foreach (var creatureName in data.Creatures)
                {
                    var template = new CreatureTemplate.Type(creatureName, false);
                    if (template.index == -1) continue;
                    spawnList.Add(new CreatureSpawnData(template, null));
                }
            }
            else // assume data.Creature is not null
            {
                Debug.Log($"spawn single {data.Creature}");
                var template = new CreatureTemplate.Type(data.Creature, false);
                if (template.index != -1)
                    spawnList.Add(new CreatureSpawnData(template, null));
            }
        }

        return spawnList.ToArray();
    }
}