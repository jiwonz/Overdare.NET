using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        internal HashSet<int> DestroyedExportsIndexes = new();
        internal int LevelPackageIndex;
        private LevelExport _level;

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

            var luaInstance = new LuaInstance(objRef)
            {
                //luaInstance.Name = export["Name"] is StrPropertyData strProp && export.ObjectName.Value.Value == strProp.Value.Value ? strProp.Value.Value : null;
                RawName = export["Name"] is StrPropertyData strProp ? export.ObjectName : null,
                RawMap = this,
            };

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
            var luaDataModel = TryLoadLuaDataModel();
            if (luaDataModel == null)
            {
                throw new Exception("Map does not contain a LuaDataModel export.");
            }
            LuaDataModel = luaDataModel;
        }

        public static Map Open(string path)
        {
            UAsset asset = new(path);
            return new(asset);
        }

        public void Save(string path)
        {
            LuaDataModel.Save(this, null);

            // Update every FPackageIndex and normalize the exports.
            var UpdateList = new int?[Asset.Exports.Count];
            int next = 0;
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                if (DestroyedExportsIndexes.Contains(i))
                {
                    UpdateList[i] = null;
                    continue;
                }
                UpdateList[i] = next;
                next++;
            }
            KillResult killResult = new();
            FPackageIndexUpdater updater = new(UpdateList, Asset, killResult);
            Console.WriteLine(JsonConvert.SerializeObject(UpdateList, Formatting.Indented));
            //JToken.FromObject(Asset, updater.Serializer);
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                killResult.Value = false;
                JToken.FromObject(Asset.Exports[i], updater.Serializer);
                if (killResult.Value)
                {
                    //DestroyedExportsIndexes.Add(i);
                    Console.WriteLine($"bonus killed2 {i}");
                }
            }
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                killResult.Value = false;
                JToken.FromObject(Asset.Exports[i], updater.Serializer);
                if (killResult.Value)
                {
                    DestroyedExportsIndexes.Add(i);
                    Console.WriteLine($"bonus killed {i}");
                }
            }
            next = 0;
            for (int i = 0; i < Asset.Exports.Count; i++)
            {
                if (DestroyedExportsIndexes.Contains(i))
                {
                    UpdateList[i] = null;
                    continue;
                }
                UpdateList[i] = next;
                next++;
            }
            Console.WriteLine(JsonConvert.SerializeObject(UpdateList, Formatting.Indented));
            JToken.FromObject(Asset, updater.Serializer);

            for (int i = _level.Actors.Count - 1; i >= 0; i--)
            {
                var actorPackageIndex = _level.Actors[i];
                if (actorPackageIndex.IsNull()) _level.Actors.RemoveAt(i);
            }
            for (int i = Asset.Exports.Count - 1; i >= 0; i--)
            {
                if (DestroyedExportsIndexes.Contains(i))
                {
                    Console.WriteLine($"Removing export at index {i}");
                    Asset.Exports.RemoveAt(i);
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

        public FName GetNextName(string baseName)
        {
            return Utility.GetNextName(Asset, baseName);
        }
    }
}
