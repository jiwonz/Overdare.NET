using Newtonsoft.Json;
using Overdare.UScriptClass;

namespace Overdare
{
    internal class Example
    {
        private static void Main(string[] args)
        {
            var map = Map.Open("input.umap");
            //Console.WriteLine(JsonConvert.SerializeObject(map.LuaDataModel.GetDescendants().Select(x => x.GetClassName(map)), Formatting.Indented));
            var workspace = map.LuaDataModel.FindFirstChildOfClass(map, "LuaWorkspace");
            LuaFolder newFolder = new()
            {
                Parent = workspace,
                Name = Utility.GetNextName(map.Asset, "NewFolder")
            };
            map.Save("out.umap");
            //map.Save("nah.umap");
            //Console.WriteLine(map.Asset.SerializeJsonObject(map.Asset.Exports[map.LevelPackageIndex], true));
        }
    }
}
