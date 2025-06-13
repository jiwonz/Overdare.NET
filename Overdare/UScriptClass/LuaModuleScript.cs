namespace Overdare.UScriptClass
{
    public class LuaModuleScript : BaseLuaScript
    {
        public LuaModuleScript()
        {
            ClassName = nameof(LuaModuleScript);
        }

        public LuaModuleScript(LoadedActor loadedActor)
            : base(loadedActor) { }
    }
}
