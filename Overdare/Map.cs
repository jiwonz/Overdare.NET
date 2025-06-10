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
        internal Dictionary<int, LuaInstance> UnlinkedExportsAndInstances = new();
        internal int LevelPackageIndex;
        private LevelExport _level;

        private LuaInstance? TryLoadLuaDataModel()
        {
            foreach (var actorPackageIndex in _level.Actors)
            {
                LoadedActor objRef = new(this, actorPackageIndex);
                if (objRef == null) continue;
                var normalExport = objRef.Export;
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
        private LuaInstance LoadIntoLuaInstance(LoadedActor loadedActor)
        {
            var export = loadedActor.Export;
            var classType = export.GetExportClassType() ?? throw new Exception("Export does not have a class type.");
            var classTypeName = classType.Value ?? throw new Exception("Export class type name is null.");

            var luaInstance = LuaInstance.CreateFromClassName(classTypeName.Value, loadedActor);
            luaInstance.Map = this;

            if (export["LuaChildren"] is ArrayPropertyData childrenArr)
            {
                foreach (var child in childrenArr.Value)
                {
                    if (child is not ObjectPropertyData objProp) continue;
                    LoadedActor childObjRef = new(this, objProp.Value);
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
            LuaDataModel.Save(null);
            foreach (var kv in UnlinkedExportsAndInstances)
            {
                kv.Value.Unlink();
            }
            for (int i = _level.Actors.Count - 1; i >= 0; i--)
            {
                var actorPackageIndex = _level.Actors[i];
                if (!actorPackageIndex.IsExport()) continue;
                if (UnlinkedExportsAndInstances.ContainsKey(actorPackageIndex.Index - 1)) _level.Actors.RemoveAt(i);
            }
            Asset.Write(path);
        }

        //public void Save(string path)
        //{
        //    LuaDataModel.Save(this, null);

        //    // Update every FPackageIndex and normalize the exports.
        //    var UpdateList = new int?[Asset.Exports.Count];
        //    int next = 0;
        //    for (int i = 0; i < Asset.Exports.Count; i++)
        //    {
        //        if (DestroyedExportsIndexes.Contains(i))
        //        {
        //            UpdateList[i] = null;
        //            continue;
        //        }
        //        UpdateList[i] = next;
        //        next++;
        //    }
        //    KillResult killResult = new();
        //    FPackageIndexUpdater updater = new(UpdateList, Asset, killResult);
        //    Console.WriteLine(JsonConvert.SerializeObject(UpdateList, Formatting.Indented));
        //    //JToken.FromObject(Asset, updater.Serializer);
        //    for (int i = 0; i < Asset.Exports.Count; i++)
        //    {
        //        killResult.Value = false;
        //        JToken.FromObject(Asset.Exports[i], updater.Serializer);
        //        if (killResult.Value)
        //        {
        //            //DestroyedExportsIndexes.Add(i);
        //            Console.WriteLine($"bonus killed2 {i}");
        //        }
        //    }
        //    for (int i = 0; i < Asset.Exports.Count; i++)
        //    {
        //        killResult.Value = false;
        //        JToken.FromObject(Asset.Exports[i], updater.Serializer);
        //        if (killResult.Value)
        //        {
        //            DestroyedExportsIndexes.Add(i);
        //            Console.WriteLine($"bonus killed {i}");
        //        }
        //    }
        //    next = 0;
        //    for (int i = 0; i < Asset.Exports.Count; i++)
        //    {
        //        if (DestroyedExportsIndexes.Contains(i))
        //        {
        //            UpdateList[i] = null;
        //            continue;
        //        }
        //        UpdateList[i] = next;
        //        next++;
        //    }
        //    Console.WriteLine(JsonConvert.SerializeObject(UpdateList, Formatting.Indented));
        //    JToken.FromObject(Asset, updater.Serializer);

        //    for (int i = _level.Actors.Count - 1; i >= 0; i--)
        //    {
        //        var actorPackageIndex = _level.Actors[i];
        //        if (actorPackageIndex.IsNull()) _level.Actors.RemoveAt(i);
        //    }
        //    for (int i = Asset.Exports.Count - 1; i >= 0; i--)
        //    {
        //        if (DestroyedExportsIndexes.Contains(i))
        //        {
        //            Console.WriteLine($"Removing export at index {i}");
        //            Asset.Exports.RemoveAt(i);
        //        }
        //    }

        //    Asset.Write(path);
        //}

        internal int AddActor(NormalExport export)
        {
            var parentPackageIndex = FPackageIndex.FromExport(Asset.Exports.Count);
            Asset.Exports.Add(export);
            _level.Actors.Add(parentPackageIndex);
            return parentPackageIndex.Index - 1;
        }

        public FName GetNextName(string baseName)
        {
            return Utility.GetNextName(Asset, baseName);
        }
    }
}
