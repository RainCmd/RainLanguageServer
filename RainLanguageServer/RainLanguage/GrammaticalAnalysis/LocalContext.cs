namespace RainLanguageServer.RainLanguage.GrammaticalAnalysis
{
    internal readonly struct Local(bool parameter, TextRange range, Type type)
    {
        public readonly bool parameter = parameter;
        public readonly TextRange range = range;
        public readonly Type type = type;
        public readonly HashSet<TextRange> read = [];
        public readonly HashSet<TextRange> write = [];
    }
    internal class LocalContextSnapshoot : List<Local>
    {
        public LocalContextSnapshoot() { }
        public LocalContextSnapshoot(IEnumerable<Local> locals) : base(locals) { }
        public LocalContextSnapshoot AddLocal(Local local)
        {
            var result = new LocalContextSnapshoot(this);
            var name = local.range.ToString();
            result.RemoveAll(value => value.range == name);
            result.Add(local);
            return result;
        }
    }
    internal class LocalContext
    {
        public readonly Local? thisValue;
        private readonly Stack<LocalContextSnapshoot> snapshoot = [];
        private readonly List<Dictionary<string, Local>> localStack = [[]];
        private readonly MessageCollector collector;
        public LocalContextSnapshoot Snapshoot => snapshoot.Peek();
        public LocalContext(MessageCollector collector, AbstractDeclaration? declaration = null)
        {
            this.collector = collector;
            if (declaration != null)
                thisValue = Add(true, KeyWords.THIS, declaration.name, declaration.declaration.DefineType);
            snapshoot.Push([.. localStack[^1].Values]);
        }
        public LocalContext(MessageCollector collector, AbstractDeclaration declaration, List<Local> locals)
        {
            this.collector = collector;
            thisValue = Add(true, KeyWords.THIS, declaration.name, declaration.declaration.DefineType);
            foreach (var local in locals)
                if (local.range != KeyWords.DISCARD_VARIABLE)
                    localStack[^1][local.range.ToString()] = local;
            snapshoot.Push([.. localStack[^1].Values]);
        }
        public void PushBlock()
        {
            localStack.Add([]);
            snapshoot.Push(snapshoot.Peek());
        }

        public void PopBlock()
        {
            localStack.RemoveAt(^1);
            snapshoot.Pop();
        }

        public Local Add(bool parameter, string name, TextRange range, Type type)
        {
            var local = new Local(parameter, range, type);
            if (name != KeyWords.DISCARD_VARIABLE)
            {
                if (TryGetLocal(name, out var overrideLocal))
                {
                    var msg = new Message(range, ErrorLevel.Info, "局部变量名覆盖了前面的局部变量");
                    msg.AddRelated(overrideLocal.range, "被覆盖的局部变量");
                    collector.Add(msg);
                }
                localStack[^1][name] = local;
                if (snapshoot.Count > 0)
                {
                    var current = snapshoot.Pop();
                    current = current.AddLocal(local);
                    snapshoot.Push(current);
                }
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
