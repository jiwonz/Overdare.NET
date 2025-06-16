namespace Overdare.UScriptClass
{
    public class LuaLocalScript : BaseLuaScript
    {
        public LuaLocalScript()
        {
            ClassName = nameof(LuaLocalScript);
        }

        public LuaLocalScript(SavedActor savedActor)
            : base(savedActor) { }
    }
}
