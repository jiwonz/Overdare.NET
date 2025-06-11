using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaFolder : LuaInstance
    {
        private const string _ClassName = "LuaFolder";
        public SavedActor? RootComponent;

        public LuaFolder()
        {
            ClassName = _ClassName;
        }

        internal override void Save(int? parentExportIndex)
        {
            if (SavingActor != null)
            {
                base.Save(parentExportIndex);
                return;
            }

            if (Map == null)
                throw new InvalidOperationException("Cannot save a new LuaFolder without a Map.");

            var asset = Map.Asset;

            var rootComponentIndex = FPackageIndex.FromExport(asset.Exports.Count);
            var luaFolderIndex = FPackageIndex.FromExport(asset.Exports.Count + 1);
            NormalExport rootComponent = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = new(asset.SearchForImport(new FName(asset, "SceneComponent"))),
                ObjectName = new(asset, "RootComponent"),
                OuterIndex = luaFolderIndex,
                ObjectFlags = EObjectFlags.RF_Transactional | EObjectFlags.RF_DefaultSubObject,
                SuperIndex = new(),
                TemplateIndex = new(),
                IsInheritedInstance = true,
                bNotAlwaysLoadedForEditorGame = true,
                Data = []
            };
            var luaFolderClassName = Map.GetNextName(_ClassName);
            NormalExport luaFolder = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = new(asset.SearchForImport(new FName(asset, _ClassName))),
                ObjectName = luaFolderClassName,
                OuterIndex = FPackageIndex.FromExport(Map.LevelPackageIndex),
                ObjectFlags = EObjectFlags.RF_Transactional,
                SuperIndex = new(),
                TemplateIndex = new(),
                IsInheritedInstance = false,
                bNotAlwaysLoadedForEditorGame = false,
                Data =
                [
                    new StrPropertyData()
                    {
                        Name = FName.FromString(asset, "ActorLabel"),
                        Value = FString.FromString(luaFolderClassName.ToString()),
                    },
                    new StructPropertyData()
                    {
                        Name = FName.FromString(asset, "ActorGuid"),
                        StructType = FName.FromString(asset, "Guid"),
                        SerializeNone = true,
                        StructGUID = Guid.Empty,
                        Value = new List<PropertyData>()
                        {
                            new GuidPropertyData()
                            {
                                Name = FName.FromString(asset, "ActorGuid"),
                                Value = Guid.NewGuid() // This generates a new GUID  
                            }
                        }
                    },
                    new ObjectPropertyData()
                    {
                        Name = FName.FromString(asset, "RootComponent"),
                        Value = rootComponentIndex
                    },
                    new BoolPropertyData()
                    {
                        Name = FName.FromString(asset, "bHidden"),
                        Value = true
                    },
                    new BoolPropertyData()
                    {
                        Name = FName.FromString(asset, "bActorEnableCollision"),
                        Value = false
                    },
                    new BoolPropertyData()
                    {
                        Name = FName.FromString(asset, "EnabledMobility"),
                        Value = true
                    }
                ]
            };
            asset.Exports.Add(rootComponent);
            Map.AddActor(luaFolder);

            RootComponent = new(asset, rootComponentIndex);
            SavingActor = new(asset, luaFolderIndex);

            base.Save(parentExportIndex);
        }

        public LuaFolder(LoadedActor loadedActor) : base(loadedActor)
        {
            if (loadedActor.Export["RootComponent"] is ObjectPropertyData rootComponentProp)
                RootComponent = new LoadedActor(loadedActor.LinkedMap, rootComponentProp.Value);
            else
                throw new Exception("LuaFolder export does not have RootComponent property. Which is unexpected.");
        }
    }
}
