using System.Diagnostics;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class BaseLuaScript : LuaInstance
    {
        internal override bool NotCreatable => false;
        public string Source = string.Empty;

        private static (string? Path, FName? ObjectName) TryGetSourceResources(NormalExport export)
        {
            var luaCode = export["LuaCode"];
            if (luaCode is not ObjectPropertyData obj || !obj.Value.IsImport())
                return (null, null);

            var asset = export.Asset;
            var import = obj.Value.ToImport(asset);
            var package = import.OuterIndex.ToImport(asset);
            var sourcePath = package.ObjectName.ToString()[6..]; // Remove "/User/"
            sourcePath = Path.ChangeExtension(sourcePath.FixDirectorySeparatorsForDisk(), "lua");
            sourcePath = Path.Combine(
                Path.GetDirectoryName(asset.FilePath) ?? string.Empty,
                sourcePath
            );
            return (sourcePath, import.ObjectName);
        }

        private void TrySaveSource(NormalExport export)
        {
            var sourceResources = TryGetSourceResources(export);
            if (string.IsNullOrEmpty(sourceResources.Path) || sourceResources.ObjectName == null)
                return;

            var asset = CreateLuaCodeUAsset(sourceResources.ObjectName.ToString());
            var dir = Path.GetDirectoryName(sourceResources.Path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(sourceResources.Path, Source);
            asset.Write(Path.ChangeExtension(sourceResources.Path, "uasset"));
        }

        /// <summary>
        /// Saves the source code(only code) of the Lua script to the file system.
        /// </summary>
        /// <exception cref="InvalidOperationException"></exception>
        public void SaveSource()
        {
            if (SavedActor == null)
            {
                throw new InvalidOperationException(
                    $"Cannot save source for {ClassName} without a SavedActor."
                );
            }
            var export = SavedActor.Export;
            var sourceResources = TryGetSourceResources(export);
            if (string.IsNullOrEmpty(sourceResources.Path))
                return;

            if (File.Exists(Path.ChangeExtension(sourceResources.Path, "uasset")))
            {
                throw new InvalidOperationException(
                    $"Cannot save source without a pair of LuaCode .uasset file."
                );
            }

            var dir = Path.GetDirectoryName(sourceResources.Path);
            if (!string.IsNullOrEmpty(dir))
            {
                Directory.CreateDirectory(dir);
            }
            File.WriteAllText(sourceResources.Path, Source);
        }

        private readonly Stream? LuaCodeUAssetResource = Assembly
            .GetExecutingAssembly()
            .GetManifestResourceStream("Overdare.Resources.LuaCode.uasset");

        private UAsset CreateLuaCodeUAsset(string objectName) // objectName should be unique
        {
            UAsset asset = new()
            {
                Mappings = null,
                CustomSerializationFlags = CustomSerializationFlags.None,
            };
            asset.SetEngineVersion(SandboxMetadata.UnrealEngineVersion);
            AssetBinaryReader reader = new(LuaCodeUAssetResource!) { Asset = asset };
            asset.Read(reader);
            asset.Exports[1].ObjectName = FName.FromString(asset, objectName); // The second export is the LuaCode export
            var hash = MD5.HashData(Encoding.UTF8.GetBytes(objectName));
            var hex = BitConverter.ToString(hash).Replace("-", "").ToUpperInvariant();
            if (asset.Exports[2] is not MetaDataExport metaDataExport)
                throw new UnreachableException();
            metaDataExport.RootMetaData[FName.FromString(asset, "PackageLocalizationNamespace")] =
                FString.FromString(hex);

            return asset;
        }

        protected internal BaseLuaScript()
            : base() { }

        protected internal BaseLuaScript(SavedActor savedActor)
            : base(savedActor)
        {
            var sourcePath = TryGetSourceResources(savedActor.Export).Path;
            if (sourcePath != null && File.Exists(sourcePath))
            {
                Source = File.ReadAllText(sourcePath);
            }
        }

        internal override void Save(int? parentExportIndex, string? outputPath)
        {
            if (SavedActor != null)
            {
                TrySaveSource(SavedActor.Export);
                base.Save(parentExportIndex, outputPath);
                return;
            }

            // Try to save a new LuaFolder because SavingActor was null
            if (Map == null)
            {
                throw new InvalidOperationException(
                    $"Cannot save a new {ClassName} without a Map."
                );
            }

            var asset = Map.Asset;

            var luaScriptIndex = FPackageIndex.FromExport(asset.Exports.Count);
            var luaScriptClassName = Map.GetNextName(ClassName);

            var classIndex = ClassIndex; // Should get this first before adding the same class named(if Name was not given) imports above
            var packageIndex = FPackageIndex.FromImport(asset.Imports.Count);
            var luaCodeIndex = FPackageIndex.FromImport(asset.Imports.Count + 1);
            var newName = GetNextName() ?? luaScriptClassName;
            asset.Imports.Add(
                new(
                    FName.FromString(asset, "/Script/CoreUObject"),
                    FName.FromString(asset, "Package"),
                    new FPackageIndex(),
                    FName.FromString(asset, "/User/Lua/" + newName),
                    false
                )
                {
                    PackageName = FName.FromString(asset, "None"),
                }
            );
            asset.Imports.Add(
                new(
                    FName.FromString(asset, "/Script/LuaMachine"),
                    FName.FromString(asset, "LuaCode"),
                    packageIndex,
                    newName,
                    false
                )
                {
                    PackageName = FName.FromString(asset, "None"),
                }
            );

            luaScriptClassName = Map.GetNextName(ClassName);
            NormalExport luaScript = new(asset, [0, 0, 0, 0])
            {
                Asset = asset,
                ClassIndex = classIndex,
                ObjectName = luaScriptClassName,
                OuterIndex = FPackageIndex.FromExport(Map.LevelPackageIndex),
                ObjectFlags = EObjectFlags.RF_Transactional,
                SuperIndex = new(),
                TemplateIndex = new(),
                bNotAlwaysLoadedForEditorGame = true,
                Data =
                [
                    new ObjectPropertyData()
                    {
                        Name = FName.FromString(asset, "LuaCode"),
                        Value = luaCodeIndex,
                    },
                    new StrPropertyData()
                    {
                        Name = FName.FromString(asset, "ActorLabel"),
                        Value = FString.FromString(luaScriptClassName.ToString()),
                    },
                    new StructPropertyData()
                    {
                        Name = FName.FromString(asset, "ActorGuid"),
                        StructType = FName.FromString(asset, "Guid"),
                        SerializeNone = true,
                        StructGUID = Guid.Empty,
                        Value =
                        [
                            new GuidPropertyData()
                            {
                                Name = FName.FromString(asset, "ActorGuid"),
                                Value = Guid.NewGuid(),
                            },
                        ],
                    },
                    new BoolPropertyData()
                    {
                        Name = FName.FromString(asset, "bHidden"),
                        Value = true,
                    },
                    new BoolPropertyData()
                    {
                        Name = FName.FromString(asset, "bActorEnableCollision"),
                        Value = false,
                    },
                ],
            };

            Map.AddActor(luaScript);

            SavedActor = new(Map, luaScriptIndex);
            TrySaveSource(SavedActor.Export);

            base.Save(parentExportIndex, outputPath);
        }
    }
}
