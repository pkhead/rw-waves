namespace WavesMod;

public class ArenaGameTypeID
{
    public static ArenaSetup.GameTypeID Waves;

    public static void RegisterValues()
    {
        // Waves mode is the third arena game type, before Challenges and Safari
        Waves = new ArenaSetup.GameTypeID("Waves", false)
        {
            index = 2
        };

        ExtEnum<ArenaSetup.GameTypeID>.values.entries.Insert(Waves.index, "Waves");
    }

    public static void UnregisterValues()
    {
        if (Waves is not null)
        {
            Waves.Unregister();
            Waves = null;
        }
    }
}