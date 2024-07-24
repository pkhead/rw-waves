namespace WavesMod;

class WaveSpawnData
{
    [System.Flags]
    public enum SpawnModifiers
    {
        None = 0,
        RandomSpawn = 1,
        NoSkyExit = 2, // substitutes a vulture in maps where there is no sky exit
    }

    public struct WaveSpawn
    {
        public CreatureTemplate.Type template;
        public SpawnModifiers modifier;

        public WaveSpawn(CreatureTemplate.Type template, SpawnModifiers modifier = SpawnModifiers.None)
        {
            this.template = template;
            this.modifier = modifier;
        }
    }

    public record WaveData
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

    public static readonly WaveData[] Data = new WaveData[]
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
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
            }
        ),

        // Wave 6
        new(
            min: 3,
            max: 4,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
            }
        ),

        // Wave 7
        new(
            amount: 4,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
            }
        ),
        
        // Wave 8
        new(
            amount: 3,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
            }
        ),

        // Wave 9
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
            }
        ),

        // Wave 10
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.CyanLizard),
                new(CreatureTemplate.Type.CyanLizard),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.YellowLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
            }
        ),

        // Wave 11
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Centipede),
                new(CreatureTemplate.Type.Centipede),
                new(CreatureTemplate.Type.Centipede),
                new(CreatureTemplate.Type.Centipede),
                new(CreatureTemplate.Type.Centipede),
            }
        ),

        // Wave 12
        new(
            amount: 6,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
            }
        ),

        // Wave 13
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Vulture),
            }
        ),

        // Wave 14
        new(
            amount: 6,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.CyanLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.Vulture, SpawnModifiers.RandomSpawn)
            }
        ),

        // Wave 15
        new(
            amount: 6,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.CyanLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.Vulture, SpawnModifiers.RandomSpawn)
            }
        ),

        // Wave 16
        new(
            amount: 7,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
            }
        ),

        // Wave 17
        new(
            amount: 5,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.YellowLizard),
                new(CreatureTemplate.Type.Vulture),
                new(CreatureTemplate.Type.Vulture),
                new(CreatureTemplate.Type.Centipede, SpawnModifiers.NoSkyExit),
                new(CreatureTemplate.Type.Centipede, SpawnModifiers.NoSkyExit),
            }
        ),

        // Wave 18
        new(
            amount: 3,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.KingVulture),
                new(CreatureTemplate.Type.RedCentipede, SpawnModifiers.NoSkyExit),
                new(CreatureTemplate.Type.Scavenger),
                new(CreatureTemplate.Type.Scavenger),
            }
        ),

        // wave 19
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

        // wave 20 (imposibble!!!)
        new(
            amount: 1,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.RedLizard),
            }
        ),

        // FINAL WAVE... goes on forever
        new(
            min: 5,
            max: 8,
            spawns: new WaveSpawn[]
            {
                new(CreatureTemplate.Type.PinkLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.CyanLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.YellowLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.Vulture, SpawnModifiers.RandomSpawn),

                // duplicated twice to make the king vulture spawn rarer, lel...
                new(CreatureTemplate.Type.PinkLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.GreenLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.BlueLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.WhiteLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.CyanLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.YellowLizard, SpawnModifiers.RandomSpawn),
                new(CreatureTemplate.Type.Vulture, SpawnModifiers.RandomSpawn),

                new(CreatureTemplate.Type.KingVulture, SpawnModifiers.RandomSpawn),
            }
        ),
    };
}