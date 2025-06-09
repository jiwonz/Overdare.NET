using Newtonsoft.Json.Linq;
using Overdare.UScriptClass;
using System.Reflection.Emit;
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
        private FPackageIndexUpdater PackageIndexUpdater;

        private LuaInstance? TryLoadLuaDataModel()
        {
            foreach (var actorPackageIndex in _level.Actors)
            {
                var objRef = ObjectReference.TryFromPackageIndex(Asset, actorPackageIndex);
                if (objRef == null) continue;
                var normalExport = objRef.ToExport(Asset);
                var classType = normalExport.GetExportClassType();
                if (classType == null) continue;
                var classTypeName = classType.Value;
                if (classTypeName == null) continue;
                if (classTypeName.Value == "LuaDataModel") return LoadIntoLuaInstance(objRef);
            }
            return null;
        }

        /// <summary>
        /// Load a LuaInstance from a NormalExport.
        /// Especially useful for LuaDataModel exports from UAsset.
        /// </summary>
        /// <param name="export"></param>
        private LuaInstance LoadIntoLuaInstance(ObjectReference objRef)
        {
            var export = objRef.ToExport(Asset);
            var classType = export.GetExportClassType();
            if (classType == null) throw new Exception("Export does not have a class type.");
            var classTypeName = classType.Value;
            if (classTypeName == null) throw new Exception("Export class type name is null.");

            var luaInstance = new LuaInstance(objRef);
            //luaInstance.Name = export["Name"] is StrPropertyData strProp && export.ObjectName.Value.Value == strProp.Value.Value ? strProp.Value.Value : null;
            luaInstance.Name = export["Name"] is StrPropertyData strProp ? export.ObjectName : null;

            if (export["LuaChildren"] is ArrayPropertyData childrenArr)
            {
                foreach (var child in childrenArr.Value)
                {
                    if (child is not ObjectPropertyData objProp) continue;
                    var childObjRef = ObjectReference.TryFromPackageIndex(Asset, objProp.Value);
                    if (childObjRef == null) continue;
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

            PackageIndexUpdater = new(ParsedExportsIndexMap, Asset);
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
            int next = 0;
            for (int i = 0; i < ParsedExportsIndexMap.Length; i++)
            {
                var value = ParsedExportsIndexMap[i];
                if (value == null) continue;
                ParsedExportsIndexMap[i] = next;
                next++;
            }
            JToken.FromObject(Asset, PackageIndexUpdater.Serializer);
            for (int i = _level.Actors.Count - 1; i >= 0; i--)
            {
                var actorPackageIndex = _level.Actors[i];
                if (!actorPackageIndex.IsExport()) continue;
                if (actorPackageIndex.Index - 1 >= ParsedExportsIndexMap.Length) continue;
                if (ParsedExportsIndexMap[actorPackageIndex.Index - 1] == null)
                {
                    _level.Actors.RemoveAt(i);
                }
            }

            Asset.Write(path);
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
