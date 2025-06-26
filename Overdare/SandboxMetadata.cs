using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UAssetAPI;

namespace Overdare
{
    public class SandboxMetadata
    {
        public const UAssetAPI.UnrealTypes.EngineVersion UnrealEngineVersion = UAssetAPI
            .UnrealTypes
            .EngineVersion
            .VER_UE5_3;
        public const string AppName = "20687893280c48c787633578d3e0ca2e";

        public static readonly string DefaultTemplateUmapPath = Path.Combine(
            "Sandbox",
            "EditorResource",
            "Sandbox",
            "WorldTemplate",
            "Baseplate",
            "Baseplate.umap"
        );

        public required string ProgramPath { get; set; }
        public required string InstallationPath { get; set; }

        public string GetDefaultUMapPath()
        {
            return Path.Combine(InstallationPath, DefaultTemplateUmapPath);
        }

        public static SandboxMetadata FromEpicGamesLauncher()
        {
            string programDataPath = Environment.GetFolderPath(
                Environment.SpecialFolder.CommonApplicationData
            );
            string manifestsPath = Path.Combine(
                programDataPath,
                "Epic",
                "EpicGamesLauncher",
                "Data",
                "Manifests"
            );

            if (!Directory.Exists(manifestsPath))
            {
                throw new DirectoryNotFoundException("Manifest folder does not exist.");
            }

            string[] itemFiles = Directory.GetFiles(manifestsPath, "*.item");

            foreach (string file in itemFiles)
            {
                string content = File.ReadAllText(file);
                var manifest = JsonConvert.DeserializeObject<JObject>(content);
                if (manifest == null || manifest["AppName"]?.ToString() != AppName)
                {
                    continue;
                }
                var installLocation =
                    (manifest["InstallLocation"]?.ToString())
                    ?? throw new KeyNotFoundException("Install location not found in manifest.");
                installLocation = installLocation.FixDirectorySeparatorsForDisk();
                var launchExecutable =
                    (manifest["LaunchExecutable"]?.ToString())
                    ?? throw new KeyNotFoundException("Launch executable not found in manifest.");
                string programPath = Path.Combine(installLocation, launchExecutable);
                if (!File.Exists(programPath))
                {
                    throw new FileNotFoundException("Launch executable not found.");
                }

                SandboxMetadata metadata = new()
                {
                    ProgramPath = programPath,
                    InstallationPath = installLocation,
                };
                return metadata;
            }

            throw new FileNotFoundException(
                "Couldn't find Sandbox. Check `OVERDARE Studio` is installed in your Epic Games Launcher library."
            );
        }
    }
}
