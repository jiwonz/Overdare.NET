using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    public class SavedActor
    {
        public int ExportIndex;
        public NormalExport Export;

        public SavedActor(UAsset asset, int normalExportIndex)
        {
            ExportIndex = normalExportIndex;
            Export = asset.Exports[normalExportIndex] as NormalExport ?? throw new InvalidCastException("Export at index is not a NormalExport.");
        }

        public SavedActor(UAsset asset, FPackageIndex normalExportPackageIndex)
        {
            if (!normalExportPackageIndex.IsExport())
                throw new ArgumentException("Provided FPackageIndex is not an export index.", nameof(normalExportPackageIndex));
            ExportIndex = normalExportPackageIndex.Index - 1;
            Export = asset.Exports[ExportIndex] as NormalExport ?? throw new InvalidCastException("Export at index is not a NormalExport.");
        }
    }

    public class LoadedActor : SavedActor
    {
        internal readonly Map LinkedMap;

        public LoadedActor(Map map, int normalExportIndex) : base(map.Asset, normalExportIndex)
        {
            LinkedMap = map;
        }

        public LoadedActor(Map map, FPackageIndex normalExportPackageIndex) : base(map.Asset, normalExportPackageIndex)
        {
            LinkedMap = map;
        }
    }
}
