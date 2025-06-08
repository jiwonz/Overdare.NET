using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaFolder : LuaInstance
    {
        public LuaFolder()
        {

        }

        internal override void Save(Map map, FPackageIndex parentPackageIndex)
        {
            var asset = map.Asset;

            var rootComponentIndex = FPackageIndex.FromExport(asset.Exports.Count);
            var luaFolderIndex = FPackageIndex.FromExport(asset.Exports.Count + 1);
            NormalExport rootComponent = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = new(asset.SearchForImport(new FName(asset, "SceneComponent"))),
                ObjectName = new FName(asset, "RootComponent"),
                OuterIndex = luaFolderIndex,
                ObjectFlags = EObjectFlags.RF_Transactional | EObjectFlags.RF_DefaultSubObject,
                SuperIndex = new(),
                TemplateIndex = new(),
                IsInheritedInstance = true,
                bNotAlwaysLoadedForEditorGame = true,
                Data = []
            };
            FName luaFolderName = Utility.GetNextName(asset, "LuaFolder");
            NormalExport luaFolder = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = new(asset.SearchForImport(new FName(asset, "LuaFolder"))),
                ObjectName = luaFolderName,
                OuterIndex = FPackageIndex.FromExport(map.LevelPackageIndex),
                ObjectFlags = EObjectFlags.RF_Transactional,
                SuperIndex = new(),
                TemplateIndex = new(),
                IsInheritedInstance = false,
                bNotAlwaysLoadedForEditorGame = false,
                Data =
                [
                    new StrPropertyData()
            {
                Name = FName.FromString(asset, "Name"),
                Value = luaFolderName.Value,
            },
            new StrPropertyData()
            {
                Name = FName.FromString(asset, "ActorLabel"),
                Value = luaFolderName.Value,
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
            parentPackageIndex != null ? new ObjectPropertyData()
            {
                Name = FName.FromString(asset, "Parent"),
                Value = parentPackageIndex
            } : null,
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
        ]
            };
            map.AddActor()
            base.Save(map, parentPackageIndex);
        }
    }
}
