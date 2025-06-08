using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace Overdare.UScriptClass
{
    public class LuaInstance
    {
        public string? Name;
        // TO-DO: This can be improved by adding a default Export like LuaModel or something.
        public NormalExport? Export;
        /// <summary>
        /// For already saved exports from the current asset.
        /// They're going to be null'ed then re-added when the instance is saved again.
        /// </summary>
        internal FPackageIndex? _savedExportIndex;
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

        internal LuaInstance()
        {

        }

        public LuaInstance(FPackageIndex exportObject)
        {
            ExportObject = exportObject;
        }

        public static LuaInstance? TryCreateFromClassName(string className) => className switch
        {
            "LuaFolder" => new LuaFolder(),
            _ => null,
        };

        internal virtual void Save(Map map, FPackageIndex? parentPackageIndex)
        {
            if (ExportObject != null)
                parentPackageIndex = map.AddActor(ExportObject);
            foreach (var child in _children)
            {
                child.Save(map, parentPackageIndex);
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
    }
}
