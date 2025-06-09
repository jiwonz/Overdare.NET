using Overdare.UScriptClass;

namespace Overdare
{
    internal class Example
    {
        private static void Main(string[] args)
        {
            var map = Map.Open("input.umap");
            //Console.WriteLine(JsonConvert.SerializeObject(map.LuaDataModel.GetDescendants().Select(x => x.GetClassName(map)), Formatting.Indented));
            var workspace = map.LuaDataModel.FindFirstChildOfClass("LuaWorkspace");
            var folder = workspace?.FindFirstChild("LuaFolder");
            Console.WriteLine($"folder: {folder}");
            folder?.Destroy();
            LuaFolder newFolder = new()
            {
                Parent = workspace,
                Name = "NewFolder"
            };
            map.Save("out.umap");
            //map.Save("nah.umap");
            //Console.WriteLine(map.Asset.SerializeJsonObject(newFolder.ExportReference.ToExport(map.Asset), true));
        }
    }
}
