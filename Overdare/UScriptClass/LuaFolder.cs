﻿using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.PropertyTypes.Structs;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaFolder : LuaInstance
    {
        internal override bool NotCreatable => false;
        public SavedActor? RootComponent;

        private FPackageIndex? _sceneComponentClassIndex;
        internal FPackageIndex SceneComponentClassIndex
        {
            get { return GetClassIndex(Map, "SceneComponent", ref _sceneComponentClassIndex); }
        }

        public LuaFolder()
        {
            ClassName = nameof(LuaFolder);
        }

        internal override void Save(int? parentExportIndex, string? outputPath)
        {
            if (SavedActor != null)
            {
                base.Save(parentExportIndex, outputPath);
                return;
            }

            // Try to save a new LuaFolder because SavingActor was null
            if (Map == null)
                throw new InvalidOperationException("Cannot save a new LuaFolder without a Map.");

            var asset = Map.Asset;

            var rootComponentIndex = FPackageIndex.FromExport(asset.Exports.Count);
            var luaFolderIndex = FPackageIndex.FromExport(asset.Exports.Count + 1);
            NormalExport rootComponent = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = SceneComponentClassIndex,
                ObjectName = new(asset, "RootComponent"),
                OuterIndex = luaFolderIndex,
                ObjectFlags = EObjectFlags.RF_Transactional | EObjectFlags.RF_DefaultSubObject,
                SuperIndex = new(),
                TemplateIndex = new(),
                IsInheritedInstance = true,
                bNotAlwaysLoadedForEditorGame = true,
                Data = [],
            };
            var luaFolderClassName = Map.GetNextName(ClassName);
            NormalExport luaFolder = new(asset, [0, 0, 0, 0])
            {
                ClassIndex = ClassIndex,
                ObjectName = luaFolderClassName,
                OuterIndex = FPackageIndex.FromExport(Map.LevelPackageIndex),
                ObjectFlags = EObjectFlags.RF_Transactional,
                SuperIndex = new(),
                TemplateIndex = new(),
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
                        Value =
                        [
                            new GuidPropertyData()
                            {
                                Name = FName.FromString(asset, "ActorGuid"),
                                Value = Guid.NewGuid(), // This generates a new GUID
                            },
                        ],
                    },
                    new ObjectPropertyData()
                    {
                        Name = FName.FromString(asset, "RootComponent"),
                        Value = rootComponentIndex,
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

            asset.Exports.Add(rootComponent);
            Map.AddActor(luaFolder);

            RootComponent = new(Map, rootComponentIndex);
            SavedActor = new(Map, luaFolderIndex);

            base.Save(parentExportIndex, outputPath);
        }

        public LuaFolder(SavedActor savedActor)
            : base(savedActor)
        {
            if (savedActor.Export["RootComponent"] is ObjectPropertyData rootComponentProp)
            {
                RootComponent = new(savedActor.Map, rootComponentProp.Value);
            }
            else
            {
                throw new Exception(
                    "LuaFolder export does not have RootComponent property. Which is unexpected."
                );
            }
        }
    }
}
