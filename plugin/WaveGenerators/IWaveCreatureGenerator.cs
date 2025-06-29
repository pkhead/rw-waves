namespace WavesMod;

interface IWaveGenerator
{
    /// <summary>
    /// Returns the number of waves in this generator.
    /// A value of -1 indicates that it is endless.
    /// </summary>
    public int WaveCount { get; }

    public CreatureSpawnData[] GenerateWave(int waveNumber, bool hasSkyExit);
}