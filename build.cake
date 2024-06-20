var target = Argument("Target", "Install");
var configuration = Argument("configuration", "Debug");
var rainWorldDir = EnvironmentVariable<string>("RainWorldDir", "C:\\Program Files (x86)\\Steam\\steamapps\\common\\Rain World");
var workshopDir = EnvironmentVariable<string>("WorkshopDir", "C:\\Program Files (x86)\\Steam\\steamapps\\workshop");

const string ProjectName = "WavesMod";
const string ModId = "pkhead.waves";

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

// Tasks
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

Task("Install")
    .IsDependentOn("Build")
    .Does(() =>
{
    var modOutputDir = rainWorldDir + $"/RainWorld_Data/StreamingAssets/mods/{ModId}";

    EnsureDirectoryExists(modOutputDir);
    CleanDirectory(modOutputDir);
    CopyDirectory("./out", modOutputDir);
});

// execution
RunTarget(target);