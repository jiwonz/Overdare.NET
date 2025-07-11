﻿using System.Diagnostics;
using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaInstance
    {
        internal virtual bool NotCreatable => true;
        public ClassTagFlags ClassTagFlags
        {
            get
            {
                var flags = ClassTagFlags.None;
                if (
                    SavedActor?.Export["bVisibleInLevelBrowser"] is BoolPropertyData prop
                    && !prop.Value
                )
                    flags |= ClassTagFlags.NotBrowsable;
                if (NotCreatable)
                    flags |= ClassTagFlags.NotCreatable;
                return flags;
            }
        }
        private string? _customName;
        public string? Name
        {
            get
            {
                if (_customName?.Length > 0)
                    return _customName;
                if (SavedActor != null && SavedActor.Export["Name"] is StrPropertyData strProp)
                {
                    return strProp.Value.Value;
                }
                return null;
            }
            set
            {
                if (value?.Length > 0)
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

        internal static FPackageIndex GetClassIndex(
            Map? mapProp,
            string className,
            ref FPackageIndex? classIndexProp
        )
        {
            if (classIndexProp != null)
                return classIndexProp;
            if (mapProp == null)
                throw new InvalidOperationException("Cannot get ClassIndex without Map");
            var asset = mapProp.Asset;
            var foundIndex = asset.SearchForImport(FName.FromString(asset, className));
            //Console.WriteLine($"{className} {foundIndex}");
            if (foundIndex != 0 && foundIndex < 0)
            {
                FPackageIndex foundClass = new(foundIndex);
                if (foundClass.ToImport(asset).ClassPackage.Value.Value == "/Script/CoreUObject")
                {
                    classIndexProp = foundClass;
                    return classIndexProp;
                }
            }
            foundIndex = asset.SearchForImport(FName.FromString(asset, "/Script/LuaAPI"));
            if (foundIndex == 0)
                throw new UnreachableException();
            //Console.WriteLine($"{className} {foundIndex}");
            classIndexProp = FPackageIndex.FromImport(asset.Imports.Count);
            asset.Imports.Add(
                new(
                    FName.FromString(asset, "/Script/CoreUObject"),
                    FName.FromString(asset, "Class"),
                    new(foundIndex),
                    FName.FromString(asset, className),
                    false
                )
                {
                    PackageName = FName.FromString(asset, "None"),
                }
            );
            return classIndexProp;
        }

        private FPackageIndex? _classIndex;
        internal FPackageIndex ClassIndex
        {
            get { return GetClassIndex(Map, ClassName, ref _classIndex); }
        }
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
        public SavedActor? SavedActor { get; internal set; }

        /// <summary>
        /// For already saved exports from the current asset.
        /// They're going to be null'ed then re-added when the instance is saved again.
        /// </summary>
        public bool ParentLocked { get; internal set; }
        private LuaInstance? _parent;
        public LuaInstance? Parent
        {
            get => _parent;
            set
            {
                if (ParentLocked)
                {
                    throw new InvalidOperationException(
                        "Cannot change ParentLocked instance's parent. This instance might be destroyed already."
                    );
                }

                if (IsAncestorOf(value))
                {
                    throw new InvalidOperationException(
                        "Attempt to set parent to an instance that would result in circular reference."
                    );
                }

                if (Parent == this)
                    throw new InvalidOperationException("Attempt to set as its own parent.");

                if (SavedActor != null)
                {
                    if (value == null)
                    {
                        if (
                            !SavedActor.Map.UnlinkedExportsAndInstances.ContainsKey(
                                SavedActor.ExportIndex
                            )
                        )
                        {
                            SavedActor.Map.UnlinkedExportsAndInstances.Add(
                                SavedActor.ExportIndex,
                                this
                            );
                        }
                    }
                    else
                    {
                        SavedActor.Map.UnlinkedExportsAndInstances.Remove(SavedActor.ExportIndex);
                    }
                }

                _parent?._children.Remove(this);
                value?._children.Add(this);
                _parent = value;
                Map = value?.Map;
            }
        }
        private readonly HashSet<LuaInstance> _children = [];

        /// <summary>
        /// Use `GetChildren()` to collect children instead of this property. This property meant to be set the initial children.
        /// </summary>
        public LuaInstance[] Children
        {
            set
            {
                if (ParentLocked)
                {
                    throw new InvalidOperationException(
                        "Cannot change ParentLocked instance's children. This instance might be destroyed already."
                    );
                }
                _children.Clear();
                foreach (var child in value)
                {
                    if (child.Parent != this)
                    {
                        child.Parent = this;
                    }
                    _children.Add(child);
                }
            }
        }

        protected internal LuaInstance()
        {
            ClassName = "";
        }

        public LuaInstance(SavedActor savedActor)
        {
            SavedActor = savedActor;
            Map = savedActor.Map;
            var classType =
                savedActor.Export.GetExportClassType()
                ?? throw new InvalidOperationException("Export does not have a class type.");
            ClassName =
                classType.Value?.Value
                ?? throw new InvalidOperationException("Export class type name is null.");
        }

        public static LuaInstance? CreateFromClassName(string className) =>
            className switch
            {
                "LuaFolder" => new LuaFolder(),
                "LuaScript" => new LuaScript(),
                "LuaModuleScript" => new LuaModuleScript(),
                "LuaLocalScript" => new LuaLocalScript(),
                _ => null,
            };

        public static LuaInstance CreateFromClassName(string className, SavedActor savedActor) =>
            className switch
            {
                "LuaFolder" => new LuaFolder(savedActor),
                "LuaScript" => new LuaScript(savedActor),
                "LuaModuleScript" => new LuaModuleScript(savedActor),
                "LuaLocalScript" => new LuaLocalScript(savedActor),
                _ => new LuaInstance(savedActor),
            };

        internal FName? GetNextName()
        {
            if (Map == null)
                throw new InvalidOperationException("Map is required to get a next FName.");
            FName? newName = null;
            if (_customName?.Length > 0)
            {
                newName = Map.GetNextName(_customName);
            }
            //else if (SavedActor != null)
            //{
            //    var export = SavedActor.Export;
            //    if (export["Name"] is StrPropertyData strProp)
            //    {
            //        newName = strProp.Value == export.ObjectName.Value ? export.ObjectName : null;
            //    }
            //    else
            //    {
            //        newName = null;
            //    }
            //}
            return newName;
        }

        internal virtual void Save(int? parentExportIndex, string? outputPath)
        {
            if (Map == null)
                throw new InvalidOperationException("Map is required to save a LuaInstance.");

            if (SavedActor != null && SavedActor.Map != Map)
            {
                throw new InvalidOperationException(
                    "SavedActor is saved in a different Map, cannot save LuaInstance."
                );
            }
            if (SavedActor == null)
            {
                throw new InvalidOperationException("SavedActor is required to save a LuaInstance");
            }
            var export = SavedActor.Export;
            var asset = export.Asset;
            // Apply properties Name(ObjectName, Name, ActorName) and Parent (LuaChildren is set later because we need ExportReference which can be set after their .Save() method called)
            var newName = GetNextName();
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
                    Value = FPackageIndex.FromExport(parentExportIndex.Value),
                };
            }

            parentExportIndex = SavedActor.ExportIndex;

            var childrenArray = GetChildren();
            var luaChildrenValue = new PropertyData[childrenArray.Length];
            for (int i = 0; i < childrenArray.Length; i++)
            {
                var child = childrenArray[i];
                child.Save(parentExportIndex, outputPath);
                if (child.SavedActor == null)
                {
                    throw new InvalidOperationException(
                        "SavedActor is required to save a LuaInstance"
                    );
                }

                luaChildrenValue[i] = new ObjectPropertyData()
                {
                    Name = FName.FromString(asset, i.ToString()),
                    Value = FPackageIndex.FromExport(child.SavedActor.ExportIndex),
                };
            }
            if (childrenArray.Length == 0)
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
                SavedActor.Export["LuaChildren"] = new ArrayPropertyData()
                {
                    Name = FName.FromString(asset, "LuaChildren"),
                    Value = luaChildrenValue,
                };
            }
        }

        internal void Unlink()
        {
            if (SavedActor == null)
            {
                throw new InvalidOperationException(
                    "SavingActor is required to save a LuaInstance"
                );
            }

            var export = SavedActor.Export;
            var asset = export.Asset;
            for (int i = export.Data.Count - 1; i >= 0; i--)
            {
                var name = export.Data[i].Name;
                if (
                    name == FName.FromString(asset, "LuaChildren")
                    || name == FName.FromString(asset, "Parent")
                )
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

            return [.. results];
        }

        public void Destroy()
        {
            Parent = null;
            ParentLocked = true;
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

        public string GetFullName()
        {
            if (Parent == null)
            {
                return Name ?? "(DataModel)";
            }
            return $"{Parent.GetFullName()}.{Name ?? ClassName}";
        }
    }

    [Flags]
    public enum ClassTagFlags
    {
        None = 0,
        NotBrowsable = 1,
        NotCreatable = 2,
    }
}
