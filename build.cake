/*
This Cake script assumes that four directories exist:
    plugin/     where the .csproj file is located
    assets/     these files will be copied directly into the mod build directory. contains modinfo.json, thumbnail.png and other
                assets perhaps within subdirectories.
    out/        the mod build directory. this is what rain world runs. if this doesn't exist, the cake script will automatically
                create it.
    deps/       optional, a place to store the DLLs of dependencies. the workshop dependency system places DLLs in here.

It will reference assemblies from the Rain World install directory as well as the Steam Workshop directories for its dependencies.
You may change which directories it searches through by setting environment variables, though by default, it searches through the
default Windows Steam directories.

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
and a copy of `out` renamed to the value of the ModId variable will be put in the Rain World mod folder.
You should then be able to view the mod in the Remix menu.

Running `dotnet cake --target=Build` instead will not put the mod into the Rain World mod folder.
*/

var target = Argument("Target", "Install");
var configuration = Argument("configuration", "Debug");
var rainWorldDir = EnvironmentVariable<string>("RainWorldDir", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Rain World");
var workshopDir = EnvironmentVariable<string>("WorkshopDir", "C:\\Program Files (x86)\\Steam\\steamapps\\workshop");

const string ProjectName = "WavesMod";
const string ModId = "pkhead.waves";

/// <summary>
/// Searches locally installed workshop items for one with a given id,
/// and copies the file at dllPath under the mod's root workshop directory
/// into the "deps" folder. You can then edit your .csproj file to reference
/// the copied dll.
/// </summary>
/// <param name="name">The name of the mod. Mainly used for display purposes.</param>
/// <param name="id">The ID of the mod. It is in the URL for the dependency mod's Steam Workshop page.</param>
/// <param name="dllPath">The path to the desired DLL relative to the dependency's mod folder.</param>
void RestoreWorkshopDependency(string name, uint id, FilePath dllPath)
{
    var workshopDownload = DirectoryPath.FromString(workshopDir).Combine("content/312520/" + id.ToString());
    if (!DirectoryExists(workshopDownload))
    {
        throw new Exception($"Workshop mod '{name}' is not downloaded on your computer.");
    }

    var fullDllPath = workshopDownload.CombineWithFilePath(dllPath);
    if (!FileExists(fullDllPath))
    {
        throw new Exception($"DLL Path '{fullDllPath}' does not exist.");
    }

    var fileCopy = DirectoryPath.FromString("./deps").GetFilePath(dllPath.GetFilename());

    if (!FileExists(fileCopy) || System.IO.File.GetLastWriteTime(fullDllPath.FullPath) > System.IO.File.GetLastWriteTime(fileCopy.FullPath))
    {
        EnsureDirectoryExists("./deps");
        Information("Update " + name);
        CopyFile(fullDllPath, fileCopy);
    }
}

// Task to restore workshop dependencies and the C# project
Task("Restore")
    .Does(() =>
{
    RestoreWorkshopDependency(
        name: "Dev Console",
        id: 2920528044,
        dllPath: "newest/plugins/DevConsole.dll"
    );

    DotNetRestore($"./plugin/{ProjectName}.csproj");
});

// Task to build the C# project
Task("Build")
    .IsDependentOn("Restore")
    .Does(() =>
{
    if (!HasEnvironmentVariable("RainWorldDir") || rainWorldDir == "")
    {
        throw new Exception("The environment variable 'RainWorldDir' is not provided");
    }

    DotNetBuild($"./plugin/{ProjectName}.csproj", new DotNetBuildSettings
    {
        Configuration = configuration,
        NoRestore = true
    });

    // create output folder
    EnsureDirectoryExists("./out");
    CleanDirectory("./out");
    CopyDirectory("./assets", "./out");
    CreateDirectory("./out/plugins");
    
    CopyFile($"./plugin/bin/{configuration}/net48/{ProjectName}.dll", $"./out/plugins/{ProjectName}.dll");
    CopyFile($"./plugin/bin/{configuration}/net48/{ProjectName}.pdb", $"./out/plugins/{ProjectName}.pdb");
});

// This task copies the out directory into the Rain World mod folder.
Task("Install")
    .IsDependentOn("Build")
    .Does(() =>
{
    var modOutputDir = rainWorldDir + $"/RainWorld_Data/StreamingAssets/mods/{ModId}";

    EnsureDirectoryExists(modOutputDir);
    CleanDirectory(modOutputDir);
    CopyDirectory("./out", modOutputDir);
});

// TODO: remove code duplication
Task("AssetsOnly")
    .Does(() =>
{
    EnsureDirectoryExists("./out");
    CleanDirectory("./out");
    CopyDirectory("./assets", "./out");
    CreateDirectory("./out/plugins");
    
    CopyFile($"./plugin/bin/{configuration}/net48/{ProjectName}.dll", $"./out/plugins/{ProjectName}.dll");
    CopyFile($"./plugin/bin/{configuration}/net48/{ProjectName}.pdb", $"./out/plugins/{ProjectName}.pdb");

    var modOutputDir = rainWorldDir + $"/RainWorld_Data/StreamingAssets/mods/{ModId}";

    EnsureDirectoryExists(modOutputDir);
    CleanDirectory(modOutputDir);
    CopyDirectory("./out", modOutputDir);
});

// execution
RunTarget(target);