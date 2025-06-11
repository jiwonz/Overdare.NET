using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaInstance
    {
        private string? _customName;
        public string? Name
        {
            get
            {
                if (_customName != null && _customName.Length > 0) return _customName;
                if (SavingActor is LoadedActor loadedActor && loadedActor.Export["Name"] is StrPropertyData strProp)
                {
                    return strProp.Value.Value;
                }
                return null;
            }
            set
            {
                if (value != null && value.Length > 0)
                {
                    _customName = value;
                }
                else
                {
                    _customName = null;
                }
            }
        }
        public string ClassName { get; init; }
        private Map? _mountedMap;
        public Map? Map
        {
            get => _mountedMap;
            set
            {
                foreach (var child in _children)
                {
                    child.Map = value;
                }
                _mountedMap = value;
            }
        }
        // TO-DO: This can be improved by adding a default Export like LuaModel or something.
        public SavedActor? SavingActor;
        /// <summary>
        /// For already saved exports from the current asset.
        /// They're going to be null'ed then re-added when the instance is saved again.
        /// </summary>
        //internal FPackageIndex? _savedExportIndex;
        public bool ParentLocked { get; internal set; }
        private LuaInstance? _parent;
        public LuaInstance? Parent
        {
            get => _parent;
            set
            {
                if (ParentLocked)
                {
                    throw new InvalidOperationException("Cannot change ParentLocked instance's parent. This instance might be destroyed already.");
                }

                if (IsAncestorOf(value))
                {
                    throw new InvalidOperationException($"Attempt to set parent to an instance that would result in circular reference.");
                }

                if (Parent == this)
                    throw new InvalidOperationException($"Attempt to set as its own parent.");


                if (SavingActor is LoadedActor loadedActor)
                {
                    if (value == null)
                    {
                        loadedActor.LinkedMap.UnlinkedExportsAndInstances.Add(loadedActor.ExportIndex, this);
                    }
                    else
                    {
                        loadedActor.LinkedMap.UnlinkedExportsAndInstances.Remove(loadedActor.ExportIndex);
                    }
                }

                _parent?._children.Remove(this);
                value?._children.Add(this);
                _parent = value;
                Map = value?.Map;
            }
        }
        private readonly HashSet<LuaInstance> _children = [];

        protected internal LuaInstance()
        {
            ClassName = "";
        }

        public LuaInstance(LoadedActor loadedActor)
        {
            SavingActor = loadedActor;
            Map = loadedActor.LinkedMap;
            var classType = loadedActor.Export.GetExportClassType();
            if (classType == null) throw new InvalidOperationException("Export does not have a class type.");
            ClassName = classType.Value?.Value ?? throw new InvalidOperationException("Export class type name is null.");
        }

        public static LuaInstance? CreateFromClassName(string className) => className switch
        {
            "LuaFolder" => new LuaFolder(),
            _ => null,
        };

        public static LuaInstance CreateFromClassName(string className, LoadedActor loadedActor) => className switch
        {
            "LuaFolder" => new LuaFolder(loadedActor),
            _ => new LuaInstance(loadedActor),
        };

        internal virtual void Save(int? parentExportIndex)
        {
            if (Map == null) throw new InvalidOperationException("Map is required to save a LuaInstance.");
            if (SavingActor == null) throw new InvalidOperationException("SavingActor is required to save a LuaInstance");
            if (SavingActor is LoadedActor loadedActor && loadedActor.LinkedMap != Map)
            {
                throw new InvalidOperationException("SavingActor is a LoadedActor from a different Map, cannot save LuaInstance.");
            }
            var export = SavingActor.Export;
            var asset = export.Asset;
            // Apply properties Name(ObjectName, Name, ActorName) and Parent (LuaChildren is set later because we need ExportReference which can be set after their .Save() method called)
            FName? newName = null;
            if (_customName != null && _customName.Length > 0)
            {
                newName = Map.GetNextName(_customName);
            }
            else if (SavingActor is LoadedActor)
            {
                if (export["Name"] is StrPropertyData strProp)
                {
                    newName = strProp.Value == export.ObjectName.Value ? export.ObjectName : null;
                }
                else
                {
                    newName = null;
                }
            }
            if (newName != null)
            {
                export.ObjectName = newName;
                export["Name"] = new StrPropertyData()
                {
                    Name = FName.FromString(asset, "Name"),
                    Value = newName.Value,
                };
                export["ActorLabel"] = new StrPropertyData()
                {
                    Name = FName.FromString(asset, "ActorLabel"),
                    Value = FString.FromString(newName.ToString()),
                };
            }
            if (parentExportIndex != null)
            {
                export["Parent"] = new ObjectPropertyData()
                {
                    Name = FName.FromString(asset, "Parent"),
                    Value = FPackageIndex.FromExport(parentExportIndex.Value)
                };
            }

            parentExportIndex = SavingActor.ExportIndex;

            var childrenArray = GetChildren();
            var luaChildrenValue = new PropertyData[childrenArray.Length];
            for (int i = 0; i < childrenArray.Length; i++)
            {
                var child = childrenArray[i];
                child.Save(parentExportIndex);
                if (child.SavingActor == null) throw new InvalidOperationException("SavingActor is required to save a LuaInstance");
                luaChildrenValue[i] = new ObjectPropertyData()
                {
                    Name = FName.FromString(asset, i.ToString()),
                    Value = FPackageIndex.FromExport(child.SavingActor.ExportIndex)
                };
            }
            if (childrenArray.Length <= 0)
            {
                for (int i = export.Data.Count - 1; i >= 0; i--)
                {
                    if (export.Data[i].Name == FName.FromString(asset, "LuaChildren"))
                    {
                        export.Data.RemoveAt(i);
                    }
                }
            }
            else
            {
                SavingActor.Export["LuaChildren"] = new ArrayPropertyData()
                {
                    Name = FName.FromString(asset, "LuaChildren"),
                    Value = luaChildrenValue
                };
            }
        }

        internal void Unlink()
        {
            if (SavingActor == null) throw new InvalidOperationException("SavingActor is required to save a LuaInstance");
            var export = SavingActor.Export;
            var asset = export.Asset;
            for (int i = export.Data.Count - 1; i >= 0; i--)
            {
                var name = export.Data[i].Name;
                if (name == FName.FromString(asset, "LuaChildren") || name == FName.FromString(asset, "Parent"))
                {
                    export.Data.RemoveAt(i);
                }
            }
        }

        public bool IsAncestorOf(LuaInstance? descendant)
        {
            while (descendant != null)
            {
                if (descendant == this)
                    return true;

                descendant = descendant.Parent;
            }
            return false;
        }

        public LuaInstance[] GetChildren()
        {
            return [.. _children];
        }

        public LuaInstance[] GetDescendants()
        {
            var results = new List<LuaInstance>();

            foreach (var child in _children)
            {
                // Add this child to the results.
                results.Add(child);

                // Add its descendants to the results.
                LuaInstance[] descendants = child.GetDescendants();
                results.AddRange(descendants);
            }

            return results.ToArray();
        }

        public void Destroy()
        {
            Parent = null;
            ParentLocked = true;
            //if (ExportReference != null)
            //{
            //    Map.DestroyedExportsIndexes.Add(ExportReference.NormalExportIndex);
            //    var packageIndex = ExportReference.ToPackageIndex();
            //    for (int i = 0; i < Map.Asset.Exports.Count; i++)
            //    {
            //        if (Map.Asset.Exports[i].OuterIndex.Index == packageIndex.Index)
            //        {
            //            Console.WriteLine("You died too");
            //            Map.DestroyedExportsIndexes.Add(i);
            //        }
            //    }
            //}
            //_destroyed = true;
            while (_children.Count != 0)
            {
                var child = _children.First();
                child.Destroy();
            }
        }

        public LuaInstance? FindFirstChild(string name)
        {
            foreach (var child in GetChildren())
            {
                if (child.Name == name)
                {
                    return child;
                }
            }
            return null;
        }

        public LuaInstance? FindFirstChildOfClass(string className)
        {
            foreach (var child in GetChildren())
            {
                if (child.ClassName == className)
                {
                    return child;
                }
            }
            return null;
        }

        public LuaInstance? FindFirstChildOfClass<T>()
        {
            foreach (var child in GetChildren())
            {
                if (child is T)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
