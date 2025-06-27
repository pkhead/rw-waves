using System;
using System.IO;
using CompactJson;
using UnityEngine;

namespace WavesMod;

class WaveSpawnData
{
    public static string ConfigDirectory = Path.Combine(Application.persistentDataPath, "ModConfigs", "pkhead.waves");
    public static string PresetDirectory = Path.Combine(ConfigDirectory, "presets");
    static readonly string SelectedPresetFile = Path.Combine(ConfigDirectory, "selected_preset");

    public const string DefaultPresetName = "Default";
    
    private static string _selectedPreset = "Default";
    public static string SelectedPreset {
        get => _selectedPreset;
        set
        {
            if (value == _selectedPreset) return;

            _selectedPreset = value;
            File.WriteAllText(SelectedPresetFile, _selectedPreset);
        }
    }

    public static void InitPresetDirectory()
    {
        if (File.Exists(SelectedPresetFile))
        {
            _selectedPreset = File.ReadAllText(SelectedPresetFile);
        }
        Directory.CreateDirectory(PresetDirectory);

        var defaultPresetLoc = Path.Combine(PresetDirectory, DefaultPresetName + ".json");
        var srcFile = AssetManager.ResolveFilePath("wavepreset-default.json");
        File.WriteAllText(defaultPresetLoc, File.ReadAllText(srcFile));

        WavesMod.Instance.logger.LogInfo("Preset directory: " + PresetDirectory);
    }

    public static WavesPreset Read()
    {
        var jsonFile = Path.Combine(PresetDirectory, SelectedPreset + ".json");
        using var stream = File.OpenText(jsonFile);
        var data = CompactJson.Serializer.Parse<WavesPreset>(stream);

        // foreach (var spawn in data[0].spawns)
        // {
        //     Debug.Log(spawn.template);
        //     if (spawn.IDs != null)
        //     {
        //         foreach (var i in spawn.IDs)
        //             Debug.Log("potential id: " + i);
        //     }
        //     else
        //     {
        //         Debug.Log("creature had no IDs");
        //     }
        // }

        return data;
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
                template = new CreatureTemplate.Type(value, false);
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

        [JsonProperty("ids")]
        [JsonEmitNullValue]
        public int[] IDs { get; set; }
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

    public class WavesPreset
    {
        [JsonProperty("description")]

        public string Description;
        [JsonProperty("waves")]
        public WaveData[] Waves;
    }
}