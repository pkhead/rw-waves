# Waves
A Rain World mod that reimplements the "Waves" arena mode that was scrapped by Videocult.

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