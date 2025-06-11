using Overdare.UScriptClass;

namespace Overdare
{
    internal class Example
    {
        private static void PrintInstance(LuaInstance instance, int indent = 0)
        {
            var source = instance is BaseLuaScript luaScript ? $" (source: {luaScript.Source})" : "";
            Console.WriteLine($"{new string(' ', indent)}{instance.ClassName} {instance.Name}{source}");
            foreach (var child in instance.GetChildren())
            {
                PrintInstance(child, indent + 2);
            }
        }

        private static void Main(string[] args)
        {
            var map = Map.Open("input.umap");
            //Console.WriteLine(JsonConvert.SerializeObject(map.LuaDataModel.GetDescendants().Select(x => x.ClassName), Formatting.Indented));
            var workspace = map.LuaDataModel.FindFirstChildOfClass("LuaWorkspace");
            var folder = workspace?.FindFirstChildOfClass("LuaFolder");
            Console.WriteLine($"folder: {folder}");
            folder?.Destroy();
            LuaScript script = new()
            {
                Source = "print('Hello, world!')\n",
                Name = "HelloWorldScript",
                Parent = workspace,
            };
            PrintInstance(map.LuaDataModel);
            map.Save("out.umap");
            //map.Save("nah.umap");
            //Console.WriteLine(map.Asset.SerializeJsonObject(newFolder.ExportReference.ToExport(map.Asset), true));
        }
    }
}
