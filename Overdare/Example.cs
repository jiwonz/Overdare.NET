using Overdare.UScriptClass;

namespace Overdare
{
    internal static class Example
    {
        private static void PrintInstance(LuaInstance instance, int indent = 0)
        {
            var source = instance is BaseLuaScript luaScript
                ? $" (source: {luaScript.Source})"
                : "";
            Console.WriteLine(
                $"{new string(' ', indent)}{instance.ClassName} {instance.Name}{source}"
            );
            foreach (var child in instance.GetChildren())
            {
                PrintInstance(child, indent + 2);
            }
        }

        private static void Main()
        {
            var map = Map.Open("input.umap");
            //Console.WriteLine(JsonConvert.SerializeObject(map.LuaDataModel.GetDescendants().Select(x => x.ClassName), Formatting.Indented));
            var workspace = map.LuaDataModel.FindFirstChildOfClass("LuaWorkspace");
            var folder = workspace?.FindFirstChildOfClass("LuaFolder");
            Console.WriteLine($"folder: {folder}");
            folder?.Destroy();
            LuaScript _ = new()
            {
                Source = "print('Hello, world!')\n",
                Name = "HelloWorldScript",
                Parent = workspace,
                Children =
                [
                    new LuaModuleScript() { Source = "return { apple = true }\n", Name = "Apple" },
                    new LuaModuleScript()
                    {
                        Source = "return { hello = function() print('hallo') end }\n",
                        Name = "Hello",
                    },
                    new LuaModuleScript()
                    {
                        Source = "return { hello = function() print('hallo') end }\n",
                        Name = "Package",
                        Children =
                        [
                            new LuaModuleScript()
                            {
                                Source = "return { dep = require(script.Dependency) }\n",
                                Name = "Dependency",
                                Children =
                                [
                                    new LuaLocalScript()
                                    {
                                        Source = "print('This is a local script')\n",
                                    },
                                ],
                            },
                        ],
                    },
                ],
            };
            PrintInstance(map.LuaDataModel);
            map.Save("out.umap");
            //map.Save("nah.umap");
            //Console.WriteLine(map.Asset.SerializeJsonObject(newFolder.ExportReference.ToExport(map.Asset), true));
        }
    }
}
