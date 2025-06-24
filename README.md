# Waves
Adds a new game mode to Arena. Conquer never-ending waves of hungry creatures, each one more difficult than the last. You will need to kill every hostile creature in order to move on to the next wave. The game is over once every player has ran out of lives. Try to see how long you can last!

This is based off the scrapped Waves game mode present in some alpha builds of Rain World.

## Wave Spawn Data Format
Wave spawn data is specified in a JSON format. Below is the format's specifications in a TypeScript-esque format:
```
root: WaveData[]            data for each wave.

type WaveData = {
    min?: int               minimum number of creatures that will spawn. required if "amount" is not used.
    max?: int               maximum number of creatures that will spawn. required if "amount" is not used.
    amount?: int            sets min and max to this value. do not use this with min & max.
    spawns: WaveSpawn[]     list of creatures to spawn.
}

type WaveSpawn = {
    type: string            name of the creature template. ex: PinkLizard, GreenLizard, Scavenger
    ids?: int[]             list of IDs to randomly choose from.
    modifiers?: string[]    list of spawn modifiers to apply. there are currently only two:
                                NoSkyExit:      spawn this creature if and only if there are no sky exits in the level.
                                                note that creatures that require a sky exit (i.e. vultures) will not spawn
                                                if the level has no sky exit, so this modifier is intended to be used for
                                                substituting vultures in enclosed levels.

                                RandomSpawn:    if a wave needs to spawn more creatures than the number of creatures in the
                                                spawn list that do not have the RandomSpawn modifier, the remaining creatures
                                                will be randomly picked from the collection of creatures that have this
                                                modifier.
}
```

## Building
This project will reference assemblies from the Rain World install directory as well as the Steam Workshop directories for its dependencies.
You may change which directories it searches through by setting environment variables, though by default, it searches through the Windows Steam directories.

PowerShell (Windows):
```powershell
# if installed through Steam
$env:RainWorldDir = "C:\Program Files (x86)\Steam\steamapps\common\Rain World"
$env:WorkshopDir = "C:\Program Files (x86)\Steam\steamapps\workshop"
```
Bash (Linux):
```bash
# i don't have steam on linux i just searched this up
export RainWorldDir="~/.steam/root/steamapps/common/Rain World"
export WorkshopDir="~/.steam/root/steamapps/workshop"
```

Then to build the project, run these commands:
```bash
# install cake build tool for this repository
# (only needs to be run once)
dotnet tool restore

# build the project
dotnet cake
```
Once executed, there will be a folder called `out` containing the mod contents,
and a copy of `out` will be put in the Rain World mod folder.

Running `dotnet cake --target=Build` instead will not put the mod into the Rain World mod folder.