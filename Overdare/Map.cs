using Overdare.UScriptClass;
using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    public class Map
    {
        internal readonly UAsset Asset;
        public LuaInstance LuaDataModel;
        internal Dictionary<int, LuaInstance> UnlinkedExportsAndInstances = [];
        internal int LevelPackageIndex;
        private readonly LevelExport _level;
        private Dictionary<string, int> _nextFNameNumberIndexMap = [];

        private LuaInstance? TryLoadLuaDataModel()
        {
            foreach (var actorPackageIndex in _level.Actors)
            {
                SavedActor savedActor = new(this, actorPackageIndex);
                var normalExport = savedActor.Export;
                var classType = normalExport.GetExportClassType();
                if (classType == null)
                    continue;
                var classTypeName = classType.Value;
                if (classTypeName == null)
                    continue;
                if (classTypeName.Value == "LuaDataModel")
                    return LoadIntoLuaInstance(savedActor);
            }
            return null;
        }

        /// <summary>
        /// Load a LuaInstance from a NormalExport.
        /// Especially useful for LuaDataModel exports from UAsset.
        /// </summary>
        /// <param name="savedActor"></param>
        private LuaInstance LoadIntoLuaInstance(SavedActor savedActor)
        {
            var export = savedActor.Export;
            var classType =
                export.GetExportClassType()
                ?? throw new Exception("Export does not have a class type.");
            var classTypeName =
                classType.Value ?? throw new Exception("Export class type name is null.");

            var luaInstance = LuaInstance.CreateFromClassName(classTypeName.Value, savedActor);
            luaInstance.Map = this;

            if (export["LuaChildren"] is ArrayPropertyData childrenArr)
            {
                foreach (var child in childrenArr.Value)
                {
                    if (child is not ObjectPropertyData objProp)
                        continue;
                    SavedActor childObjRef = new(this, objProp.Value);
                    if (childObjRef == null)
                        continue;
                    var childInstance = LoadIntoLuaInstance(childObjRef);
                    childInstance.Parent = luaInstance;
                }
            }

            return luaInstance;
        }

        public Map(UAsset asset)
        {
            Asset = asset;
            LevelExport? level = null;
            for (int i = 0; i < asset.Exports.Count; i++)
            {
                var export = asset.Exports[i];
                if (export is LevelExport foundLevel)
                {
                    level = foundLevel;
                    LevelPackageIndex = i;
                    break;
                }
            }
            if (level == null)
            {
                throw new Exception("Map does not contain a LevelExport export.");
            }
            _level = level;
            var luaDataModel =
                TryLoadLuaDataModel()
                ?? throw new Exception("Map does not contain a LuaDataModel export.");
            LuaDataModel = luaDataModel;
        }

        public Map(byte[] buffer)
            : this(new MemoryStream(buffer)) { }

        private static UAsset UAssetFromReader(AssetBinaryReader reader, string? filePath)
        {
            UAsset asset = new()
            {
                Mappings = null,
                CustomSerializationFlags = CustomSerializationFlags.None,
                FilePath = filePath,
            };
            asset.SetEngineVersion(SandboxMetadata.UnrealEngineVersion);
            reader.Asset = asset;
            asset.Read(reader);
            return asset;
        }

        public Map(Stream stream)
            : this(UAssetFromReader(new AssetBinaryReader(stream), null)) { }

        public Map(byte[] buffer, string filePath)
            : this(new MemoryStream(buffer), filePath) { }

        public Map(Stream stream, string filePath)
            : this(UAssetFromReader(new AssetBinaryReader(stream), filePath)) { }

        public static Map Open(string path)
        {
            byte[] buffer = File.ReadAllBytes(path);
            Map map = new(buffer, path);
            return map;
        }

        public MemoryStream WriteData()
        {
            return Asset.WriteData();
        }

        public void Save(string path)
        {
            var oldFilePath = Asset.FilePath;
            Asset.FilePath = path;
            LuaDataModel.Save(null, path);
            foreach (var kv in UnlinkedExportsAndInstances)
            {
                kv.Value.Unlink();
            }
            for (int i = _level.Actors.Count - 1; i >= 0; i--)
            {
                var actorPackageIndex = _level.Actors[i];
                if (!actorPackageIndex.IsExport())
                    continue;
                if (UnlinkedExportsAndInstances.ContainsKey(actorPackageIndex.Index - 1))
                    _level.Actors.RemoveAt(i);
            }
            Asset.Write(path);
            Asset.FilePath = oldFilePath; // Restore original file path after writing
        }

        internal int AddActor(NormalExport export)
        {
            var parentPackageIndex = FPackageIndex.FromExport(Asset.Exports.Count);
            Asset.Exports.Add(export);
            _level.Actors.Add(parentPackageIndex);
            return parentPackageIndex.Index - 1;
        }

        private bool IsDuplicated(string name, int number)
        {
            foreach (var export in Asset.Exports)
            {
                if (
                    export.ObjectName.Value.Value.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && export.ObjectName.Number == number
                )
                {
                    return true;
                }
            }
            foreach (var import in Asset.Imports)
            {
                if (
                    import.ObjectName.Value.Value.Equals(
                        name,
                        StringComparison.OrdinalIgnoreCase
                    )
                    && import.ObjectName.Number == number
                )
                {
                    return true;
                }
            }
            return false;
        }

        public FName GetNextName(string baseName)
        {
            var nameForSearch = baseName.ToLower();
            if (_nextFNameNumberIndexMap.TryGetValue(nameForSearch, out int nextIndex))
            {
                while (IsDuplicated(baseName, nextIndex))
                {
                    nextIndex++;
                }
                _nextFNameNumberIndexMap[nameForSearch] = nextIndex + 1;
            }
            else
            {
                int n = 0;
                foreach (var export in Asset.Exports)
                {
                    if (
                        export.ObjectName.Value.Value.Equals(
                            nameForSearch,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        n++;
                    }
                }
                foreach (var import in Asset.Imports)
                {
                    if (
                        import.ObjectName.Value.Value.Equals(
                            nameForSearch,
                            StringComparison.OrdinalIgnoreCase
                        )
                    )
                    {
                        n++;
                    }
                }
                nextIndex = n;
                _nextFNameNumberIndexMap[nameForSearch] = n + 1;
            }
            return new FName(Asset, baseName, nextIndex);
        }
    }
}
