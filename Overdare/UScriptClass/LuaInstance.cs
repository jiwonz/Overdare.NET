using UAssetAPI.PropertyTypes.Objects;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaInstance
    {
        public FName? Name;
        // TO-DO: This can be improved by adding a default Export like LuaModel or something.
        public ObjectReference? ExportReference;
        /// <summary>
        /// For already saved exports from the current asset.
        /// They're going to be null'ed then re-added when the instance is saved again.
        /// </summary>
        //internal FPackageIndex? _savedExportIndex;
        private LuaInstance? _parent;
        public LuaInstance? Parent
        {
            get => _parent;
            set
            {
                if (IsAncestorOf(value))
                {
                    throw new InvalidOperationException($"Attempt to set parent to an instance that would result in circular reference");
                }

                if (Parent == this)
                    throw new InvalidOperationException($"Attempt to set as its own parent");

                _parent?._children.Remove(this);
                value?._children.Add(this);
                _parent = value;
            }
        }
        private readonly HashSet<LuaInstance> _children = [];
        internal bool _destroyed = false;

        // Not public because It is not normal case to create LuaInstance without ExportReference
        internal LuaInstance()
        {

        }

        public LuaInstance(ObjectReference objRef)
        {
            ExportReference = objRef;
        }

        //public static LuaInstance? TryCreateFromClassName(string className) => className switch
        //{
        //    "LuaFolder" => new LuaFolder(),
        //    _ => null,
        //};

        internal virtual void Save(Map map, ObjectReference? parentObjRef)
        {
            var asset = map.Asset;
            if (ExportReference != null)
            {
                if (_destroyed)
                {
                    map.ParsedExportsIndexMap[ExportReference.NormalExportIndex] = null;
                    return;
                }

                var export = ExportReference.ToExport(asset);
                // Apply properties Name(ObjectName, Name, ActorName) and Parent (LuaChildren is set later because we need ExportReference which can be set after their .Save() method called)
                if (Name != null)
                {
                    export.ObjectName = Name;
                    export["Name"] = new StrPropertyData()
                    {
                        Name = FName.FromString(asset, "Name"),
                        Value = Name.Value,
                    };
                    export["ActorLabel"] = new StrPropertyData()
                    {
                        Name = FName.FromString(asset, "ActorLabel"),
                        Value = Name.Value,
                    };
                }
                if (parentObjRef != null)
                {
                    export["Parent"] = new ObjectPropertyData()
                    {
                        Name = FName.FromString(asset, "Parent"),
                        Value = parentObjRef.ToPackageIndex()
                    };
                }

                parentObjRef = ExportReference;
            }
            if (parentObjRef == null) throw new InvalidOperationException("Invalid parent");
            var childrenArray = GetChildren();
            var luaChildrenValue = new PropertyData[childrenArray.Length];
            for (int i = 0; i < childrenArray.Length; i++)
            {
                var child = childrenArray[i];
                child.Save(map, parentObjRef);
                if (child.ExportReference == null) throw new InvalidOperationException("The LuaInstance.Save() method did not create and add any Export.");
                luaChildrenValue[i] = new ObjectPropertyData()
                {
                    Name = FName.FromString(asset, i.ToString()),
                    Value = child.ExportReference.ToPackageIndex()
                };
            }
            if (childrenArray.Length > 0 && ExportReference != null)
            {
                var export = ExportReference.ToExport(asset);
                export["LuaChildren"] = new ArrayPropertyData()
                {
                    Name = FName.FromString(asset, "LuaChildren"),
                    Value = luaChildrenValue
                };
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
            _destroyed = true;
        }

        public string GetClassName(Map map)
        {
            if (ExportReference == null) throw new InvalidOperationException("ExportReference is null, cannot determine class name.");
            var export = ExportReference.ToExport(map.Asset);
            var classType = export.GetExportClassType();
            if (classType == null) throw new InvalidOperationException("Export does not have a class type.");
            return classType.Value?.Value ?? throw new InvalidOperationException("Export class type name is null.");
        }

        // Get Instance name or from ExportReference if it is not set.
        public string GetName(Map map)
        {
            if (ExportReference == null) throw new InvalidOperationException("ExportReference is null, cannot determine name.");
            var export = ExportReference.ToExport(map.Asset);
            if (export.ObjectName.Value == null) throw new InvalidOperationException("Export ObjectName is null.");
            return Name?.Value.Value ?? export.ObjectName.Value.Value;
        }

        public LuaInstance? FindFirstChild(Map map, string name)
        {
            foreach (var child in GetChildren())
            {
                if (child.GetName(map) == name)
                {
                    return child;
                }
            }
            return null;
        }

        public LuaInstance? FindFirstChildOfClass(Map map, string className)
        {
            foreach (var child in GetChildren())
            {
                if (child.GetClassName(map) == className)
                {
                    return child;
                }
            }
            return null;
        }
    }
}
