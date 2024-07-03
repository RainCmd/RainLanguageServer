namespace RainLanguageServer.RainLanguage2
{
    internal enum DeclarationCategory
    {
        //                    library		        visibility                index         define
        Invalid,              //程序集              可见性                    索引          -    
        Variable,             //程序集              可见性                    索引          -    
        Function,             //程序集              可见性                    索引          -    
        Enum,                 //程序集              可见性                    索引          -    
        EnumElement,          //程序集              可见性                    索引          枚举索引    
        Struct,               //程序集              可见性                    索引          -    
        StructVariable,       //程序集              可见性                    索引          结构体索引    
        StructFunction,       //程序集              可见性                    索引          结构体索引    
        Class,                //程序集              可见性                    索引          -    
        Constructor,          //程序集              可见性                    索引          类索引    
        ClassVariable,        //程序集              可见性                    索引          类索引    
        ClassFunction,        //程序集              可见性                    索引          类索引    
        Interface,            //程序集              可见性                    索引          -    
        InterfaceFunction,    //程序集              可见性                    索引          接口索引    
        Delegate,             //程序集              可见性                    索引          -    
        Task,                 //程序集              可见性                    索引          -    
        Native,               //程序集              可见性                    索引          -    
    }
    internal readonly struct Declaration
    {
        public readonly int library;
        public readonly Visibility visibility;
        public readonly DeclarationCategory category;
        public readonly int index;
        public readonly int define;
    }
    internal enum TypeCode
    {
        Invalid,
        Struct,
        Enum,
        Handle,
        Interface,
        Delegate,
        Task,
    }
    internal readonly struct Type(int library, TypeCode code, int index, int dimension) : IEquatable<Type>
    {
        public readonly int library = library;
        public readonly TypeCode code = code;
        public readonly int index = index;
        public readonly int dimension = dimension;

        public bool Equals(Type other) => library == other.library && code == other.code && index == other.index && dimension == other.dimension;

        public static bool operator ==(Type left, Type right) => left.Equals(right);
        public static bool operator !=(Type left, Type right) => !left.Equals(right);
        public override bool Equals(object? obj) => obj is Type type && Equals(type);
        public override int GetHashCode() => HashCode.Combine(library, code, index, dimension);
    }
    internal readonly struct Tuple(params Type[] types) : IEquatable<Tuple>
    {
        private readonly Type[] types = types;
        public int Count => types.Length;
        public Type this[int index] => types[index];
        public bool Equals(Tuple other)
        {
            if (types == other.types) return true;
            if (types.Length != other.types.Length) return false;
            for (int i = 0; i < types.Length; i++)
                if (types[i] != other.types[i]) return false;
            return true;
        }
        public static bool operator ==(Tuple left, Tuple right) => left.Equals(right);
        public static bool operator !=(Tuple left, Tuple right) => !left.Equals(right);
        public override bool Equals(object? obj) => obj is Tuple tuple && Equals(tuple);
        public override int GetHashCode()
        {
            var result = new HashCode();
            if (types != null)
                foreach (var type in types)
                    result.Add(type.GetHashCode());
            return result.ToHashCode();
        }
    }
}
