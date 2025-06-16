namespace Overdare.UScriptClass
{
    public class LuaScript : BaseLuaScript
    {
        public LuaScript()
        {
            ClassName = nameof(LuaScript);
        }

        public LuaScript(SavedActor savedActor)
            : base(savedActor) { }
    }
}
