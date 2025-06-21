using System;
using System.IO;
using CompactJson;

namespace WavesMod;

class WaveSpawnData
{
    public static WaveData[] Read()
    {
        var jsonFile = AssetManager.ResolveFilePath("wavedata/default.json");
        using var stream = File.OpenText(jsonFile);
        return CompactJson.Serializer.Parse<WaveData[]>(stream);
    }

    [Flags]
    public enum SpawnModifiers
    {
        None = 0,
        RandomSpawn = 1,
        NoSkyExit = 2, // substitutes a vulture in maps where there is no sky exit
    }

    public class WaveSpawn
    {
        [JsonIgnoreMember]
        public CreatureTemplate.Type template;

        [JsonIgnoreMember]
        public SpawnModifiers modifier = SpawnModifiers.None;

        [JsonProperty("type")]
        public string Type {
            set
            {
                template = (CreatureTemplate.Type) (typeof(CreatureTemplate.Type).GetField(value)?.GetValue(null));

                if (template is null && (ModManager.MSC || ModManager.Watcher))
                    template = (CreatureTemplate.Type) (typeof(DLCSharedEnums.CreatureTemplateType).GetField(value)?.GetValue(null));

                if (template is null && ModManager.Watcher)
                    template = (CreatureTemplate.Type) (typeof(Watcher.WatcherEnums.CreatureTemplateType).GetField(value)?.GetValue(null));

                if (template is null && ModManager.MSC)
                    template = (CreatureTemplate.Type) (typeof(MoreSlugcats.MoreSlugcatsEnums.CreatureTemplateType).GetField(value)?.GetValue(null));
                
                if (template is null)
                    throw new ArgumentException("Unknown template type " + value, nameof(value));
                
                WavesMod.Instance.logger.LogInfo(value);
                WavesMod.Instance.logger.LogInfo(template);
            }
        }

        [JsonProperty("modifiers")]
        public string[] Modifiers {
            set
            {
                modifier = SpawnModifiers.None;
                foreach (var item in value)
                {
                    modifier |= item switch
                    {
                        "RandomSpawn" => SpawnModifiers.RandomSpawn,
                        "NoSkyExit" => SpawnModifiers.NoSkyExit,
                        _ => throw new ArgumentException($"Invalid spawn modifier {item}")
                    };
                }
            }
        }
    }

    public class WaveData
    {
        [JsonIgnoreMember]
        public int minCreatures, maxCreatures;
        public WaveSpawn[] spawns;

        [JsonProperty("amount")]
        public int Amount {
            set
            {
                minCreatures = maxCreatures = value;
            }
        }

        [JsonProperty("min")]
        public int Min {
            get => minCreatures;
            set => minCreatures = value;
        }

        [JsonProperty("max")]
        public int Max {
            get => maxCreatures;
            set => maxCreatures = value;
        }
    }
}