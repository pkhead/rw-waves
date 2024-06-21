namespace WavesMod;

class WaveSpawnData
{
    public enum SpawnModifier
    {
        None,
        RandomSpawn
    }

    public struct WaveSpawn
    {
        public CreatureTemplate.Type template;
        public SpawnModifier modifier;

        public WaveSpawn(CreatureTemplate.Type template, SpawnModifier modifier = SpawnModifier.None)
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
                new(CreatureTemplate.Type.PinkLizard),
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
}