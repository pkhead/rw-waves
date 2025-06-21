# Waves
Adds a new game mode to Arena. Conquer never-ending waves of hungry creatures, each one more difficult than the last. You will need to kill every hostile creature in order to move on to the next wave. The game is over once every player has ran out of lives. Try to see how long you can last!

This is based off the scrapped Waves game mode present in some alpha builds of Rain World.

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