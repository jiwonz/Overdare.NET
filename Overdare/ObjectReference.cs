using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    public class ObjectReference
    {
        public int NormalExportIndex;

        public ObjectReference(int normalExportIndex)
        {
            NormalExportIndex = normalExportIndex;
        }

        public ObjectReference(UAsset asset, FPackageIndex normalExportPackageIndex)
        {
            var export = normalExportPackageIndex.ToExport(asset);
            if (export is not NormalExport) throw new Exception("Only NormalExport can be an ObjectReference");
            NormalExportIndex = normalExportPackageIndex.Index - 1;
        }

        public static ObjectReference? TryFromPackageIndex(UAsset asset, FPackageIndex packageIndex)
        {
            if (!packageIndex.IsExport()) return null;
            var export = packageIndex.ToExport(asset);
            if (export is not NormalExport) return null;
            return new ObjectReference(packageIndex.Index - 1);
        }

        public NormalExport ToExport(UAsset asset)
        {
            return (NormalExport)asset.Exports[NormalExportIndex];
        }

        public FPackageIndex ToPackageIndex()
        {
            return FPackageIndex.FromExport(NormalExportIndex);
        }
    }
}
