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
                if (
                    export.ObjectName.Value.Value.Equals(
                        baseName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    n++;
                }
            }
            foreach (var import in asset.Imports)
            {
                if (
                    import.ObjectName.Value.Value.Equals(
                        baseName,
                        StringComparison.OrdinalIgnoreCase
                    )
                )
                {
                    n++;
                }
            }
            return new FName(asset, baseName, n);
        }
    }
}
