using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    /// <summary>
    /// Represents a saved actor in a UAsset file.
    /// </summary>
    public class SavedActor
    {
        public int ExportIndex;
        public NormalExport Export;
        internal readonly Map Map;

        public SavedActor(Map map, int normalExportIndex)
        {
            Map = map;
            ExportIndex = normalExportIndex;
            Export =
                map.Asset.Exports[normalExportIndex] as NormalExport
                ?? throw new InvalidCastException("Export at index is not a NormalExport.");
        }

        public SavedActor(Map map, FPackageIndex normalExportPackageIndex)
        {
            if (!normalExportPackageIndex.IsExport())
            {
                throw new ArgumentException(
                    "Provided FPackageIndex is not an export index.",
                    nameof(normalExportPackageIndex)
                );
            }

            Map = map;
            ExportIndex = normalExportPackageIndex.Index - 1;
            Export =
                map.Asset.Exports[ExportIndex] as NormalExport
                ?? throw new InvalidCastException("Export at index is not a NormalExport.");
        }
    }
}
