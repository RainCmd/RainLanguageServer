namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal readonly struct Local(bool parameter, int index, TextRange range, Type type)
    {
        public readonly bool parameter = parameter;
        public readonly int index = index;
        public readonly TextRange range = range;
        public readonly Type type = type;
        public readonly HashSet<TextRange> read = [];
        public readonly HashSet<TextRange> write = [];
    }
    internal class LocalContext
    {
        public readonly Local? thisValue;
        private readonly List<Local> locals = [];
        private readonly List<Dictionary<string, Local>> localStack = [[]];
        private readonly MessageCollector collector;
        public LocalContext(MessageCollector collector, AbstractDeclaration? declaration = null)
        {
            this.collector = collector;
            if (declaration != null)
                thisValue = Add(true, KeyWords.THIS, declaration.name, declaration.declaration.DefineType);
        }
        public void PushBlock() => localStack.Add([]);
        public void PopBlock() => localStack.RemoveAt(^1);
        public Local Add(bool parameter, string name, TextRange range, Type type)
        {
            var local = new Local(parameter, locals.Count, range, type);
            locals.Add(local);
            if (name != KeyWords.DISCARD_VARIABLE)
            {
                if (TryGetLocal(name, out var overrideLocal))
                {
                    var msg = new Message(range, ErrorLevel.Info, "局部变量名覆盖了前面的局部变量");
                    msg.AddRelated(overrideLocal.range, "被覆盖的局部变量");
                    collector.Add(msg);
                }
                localStack[^1][name] = local;
            }
            return local;
        }
        public Local Add(TextRange name, Type type, bool parameter = false) => Add(parameter, name.ToString(), name, type);
        public bool TryGetLocal(string name, out Local local)
        {
            for (var i = localStack.Count - 1; i >= 0; i--)
                if (localStack[i].TryGetValue(name, out local))
                    return true;
            local = default;
            return false;
        }
    }
}
