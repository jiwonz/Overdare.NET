using UAssetAPI;
using UAssetAPI.UnrealTypes;

namespace Overdare
{
    public static class Utility
    {
        public static FName GetNextName(UAsset asset, string baseName)
        {
            int n = 0;
            foreach (var export in asset.Exports)
            {
                if (export.ObjectName.Value.Value == baseName) n++;
            }
            foreach (var import in asset.Imports)
            {
                if (import.ObjectName.Value.Value == baseName) n++;
            }
            return new FName(asset, baseName, n);
        }
    }
}
