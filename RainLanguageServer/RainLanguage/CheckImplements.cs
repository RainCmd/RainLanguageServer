﻿namespace RainLanguageServer.RainLanguage
{
    internal static class CheckImplements
    {
        private static void CollectInherits(Manager manager, List<Type> inherits, HashSet<AbstractInterface> set)
        {
            foreach (var type in inherits)
                if (manager.TryGetDeclaration(type, out var declaration) && declaration is AbstractInterface abstractInterface && set.Add(abstractInterface))
                    CollectInherits(manager, abstractInterface.inherits, set);
        }
        private static void CheckFunction(HashSet<AbstractInterface> set, AbstractInterface.Function implement)
        {
            var name = implement.name.ToString();
            foreach (var abstractInterface in set)
                foreach (var function in abstractInterface.functions)
                    if (function.name == name && function.signature == implement.signature)
                    {
                        if (function.returns != implement.returns)
                        {
                            var msg = new Message(implement.name, ErrorLevel.Error, "与继承的接口函数同名同参，但返回值不同");
                            msg.AddRelated(function.name, "冲突的函数");
                            implement.file.space.collector.Add(msg);
                        }
                        implement.overrides.Add(function);
                    }
        }
        private static void CheckFunction(Manager manager, Type index, AbstractClass.Function implement, string name)
        {

            if (manager.TryGetDeclaration(index, out var declaration) && declaration is AbstractClass inhert)
                foreach (var abstractClass in manager.GetInheritIterator(inhert))
                    foreach (var function in abstractClass.functions)
                        if (function.name == name && function.signature == implement.signature)
                        {
                            function.implements.Add(implement);
                            implement.overrides.Add(function);
                            if (function.returns != implement.returns)
                            {
                                var msg = new Message(implement.name, ErrorLevel.Error, "与重写函数同名同参，但返回值不同");
                                msg.AddRelated(function.name, "冲突的函数");
                                implement.file.space.collector.Add(msg);
                                return;
                            }
                        }
        }
        private static bool ContainsFunction(AbstractInterface.Function function, AbstractClass define)
        {
            var name = function.name.ToString();
            foreach (var implement in define.functions)
                if (implement.name == name && implement.signature == function.signature)
                {
                    if (implement.returns != function.returns)
                        define.file.space.collector.Add(implement.name, ErrorLevel.Error, "函数返回值类型与接口函数返回值类型不一致");
                    function.implements.Add(implement);
                    implement.overrides.Add(function);
                    return true;
                }
            return false;
        }
        public static void Check(Manager manager, AbstractLibrary library)
        {
            var interfaceSet = new HashSet<AbstractInterface>();
            foreach (var abstractInterface in library.interfaces)
            {
                CollectInherits(manager, abstractInterface.inherits, interfaceSet);
                interfaceSet.Remove(abstractInterface);
                foreach (var function in abstractInterface.functions)
                    CheckFunction(interfaceSet, function);
                interfaceSet.Clear();
            }
            var unimplements = new List<AbstractInterface.Function>();
            foreach (var abstractClass in library.classes)
            {
                foreach (var function in abstractClass.functions)
                    CheckFunction(manager, abstractClass.parent, function, function.name.ToString());
                CollectInherits(manager, abstractClass.inherits, interfaceSet);

                foreach (var inhertInterface in interfaceSet)
                    foreach (var function in inhertInterface.functions)
                        if (!ContainsFunction(function, abstractClass))
                            unimplements.Add(function);

                if (unimplements.Count > 0)
                {
                    var msg = new Message(abstractClass.name, ErrorLevel.Error, "有接口函数未实现");
                    foreach (var function in unimplements)
                        msg.AddRelated(function.name, "未实现的函数");
                    abstractClass.file.space.collector.Add(msg);
                    unimplements.Clear();
                }
                interfaceSet.Clear();
            }
        }
    }
}
