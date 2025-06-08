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
        internal int?[] ParsedExportsIndexMap;
        internal int LevelPackageIndex;
        private LevelExport _level;

        private LuaInstance? TryLoadLuaDataModel()
        {
            foreach (var actorPackageIndex in _level.Actors)
            {
                if (!actorPackageIndex.IsExport()) continue;
                var actorExport = actorPackageIndex.ToExport(Asset);
                if (actorExport is not NormalExport normalExport) continue;
                var classType = normalExport.GetExportClassType();
                if (classType == null) continue;
                var classTypeName = classType.Value;
                if (classTypeName == null) continue;
                if (classTypeName.Value == "LuaDataModel") return NormalExportToLuaInstance(normalExport);
            }
            return null;
        }

        /// <summary>
        /// Load a LuaInstance from a NormalExport.
        /// Especially useful for LuaDataModel exports from UAsset.
        /// </summary>
        /// <param name="export"></param>
        private LuaInstance NormalExportToLuaInstance(NormalExport export)
        {
            var classType = export.GetExportClassType();
            if (classType == null) throw new Exception("Export does not have a class type.");
            var classTypeName = classType.Value;
            if (classTypeName == null) throw new Exception("Export class type name is null.");

            var luaInstance = LuaInstance.TryCreateFromClassName(classTypeName.Value) ?? new LuaInstance(export);
            luaInstance.Name = export["Name"] is StrPropertyData strProp ? strProp.Value.Value : null;

            if (export["LuaChildren"] is ArrayPropertyData childrenArr)
            {
                foreach (var child in childrenArr.Value)
                {
                    if (child is not ObjectPropertyData objProp || !objProp.Value.IsExport()) continue;
                    var childExport = objProp.Value.ToExport(export.Asset);
                    if (childExport is not NormalExport childNormalExport) continue;
                    var childInstance = NormalExportToLuaInstance(childNormalExport);
                    childInstance._savedExportIndex = objProp.Value;
                    childInstance.Parent = luaInstance;
                    ParsedExportsIndexMap[objProp.Value.Index - 1] = null;
                }
            }

            return luaInstance;
        }

        public Map(UAsset asset)
        {
            Asset = asset;
            LevelExport? level = null;
            ParsedExportsIndexMap = new int?[asset.Exports.Count];
            for (int i = 0; i < asset.Exports.Count; i++)
            {
                ParsedExportsIndexMap[i] = i;
                var export = asset.Exports[i];
                if (export is LevelExport foundLevel)
                {
                    level = foundLevel;
                    LevelPackageIndex = i;
                }
            }
            if (level == null)
            {
                throw new Exception("Map does not contain a LevelExport export.");
            }
            _level = level;
            var luaDataModel = TryLoadLuaDataModel();
            if (luaDataModel == null)
            {
                throw new Exception("Map does not contain a LuaDataModel export.");
            }
            LuaDataModel = luaDataModel;

            // Remove the null'ed exports from the LevelExport's Actors list.
            for (int i = level.Actors.Count - 1; i >= 0; i--)
            {
                var actorPackageIndex = level.Actors[i];
                if (!actorPackageIndex.IsExport()) continue;
                if (ParsedExportsIndexMap[actorPackageIndex.Index - 1] == null)
                {
                    level.Actors.RemoveAt(i);
                }
            }
        }

        public static Map Open(string path)
        {
            UAsset asset = new(path);
            return new(asset);
        }

        public void Save(string path)
        {
            LuaDataModel.Save(this, null);

            // TO-DO: Update every FPackageIndex and normalize the exports.

            Asset.Write(path);
        }

        internal FPackageIndex AddActor(FPackageIndex exportObject)
        {
            if (!exportObject.IsExport())
            {
                throw new ArgumentException("Export object cannot be null.", nameof(exportObject));
            }
            var parentPackageIndex = FPackageIndex.FromExport(Asset.Exports.Count);
            Asset.Exports.Add(exportObject.ToExport(Asset));
            _level.Actors.Add(exportObject);
            return parentPackageIndex;
        }

        internal FPackageIndex AddActor(NormalExport export)
        {
            var parentPackageIndex = FPackageIndex.FromExport(Asset.Exports.Count);
            Asset.Exports.Add(export);
            _level.Actors.Add(parentPackageIndex);
            return parentPackageIndex;
        }
    }
}
