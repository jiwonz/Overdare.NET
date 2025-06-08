using UAssetAPI;
using UAssetAPI.ExportTypes;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    public static class Utility
    {
        public static FName GetNextName(UAsset asset, string baseName)
        {
            int n = 0;
            foreach (Export export in asset.Exports)
            {
                if (export.ObjectName.Value.Value == baseName) n++;
            }
            return new FName(asset, baseName, n + 1);
        }
    }
}
