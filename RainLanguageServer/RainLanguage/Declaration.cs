using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage
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
    internal readonly struct Declaration(int library, Visibility visibility, DeclarationCategory category, int index, int define) : IEquatable<Declaration>
    {
        public readonly int library = library;
        public readonly Visibility visibility = visibility;
        public readonly DeclarationCategory category = category;
        public readonly int index = index;
        public readonly int define = define;
        public Declaration(int library, Visibility visibility, DeclarationCategory category, int index) : this(library, visibility, category, index, 0) { }
        public Type DefineType
        {
            get
            {
                switch (category)
                {
                    case DeclarationCategory.Invalid:
                    case DeclarationCategory.Variable:
                    case DeclarationCategory.Function:
                        break;
                    case DeclarationCategory.Enum:
                        return new Type(library, TypeCode.Enum, index, 0);
                    case DeclarationCategory.EnumElement:
                        return new Type(library, TypeCode.Enum, define, 0);
                    case DeclarationCategory.Struct:
                        return new Type(library, TypeCode.Struct, index, 0);
                    case DeclarationCategory.StructVariable:
                    case DeclarationCategory.StructFunction:
                        return new Type(library, TypeCode.Struct, define, 0);
                    case DeclarationCategory.Class:
                        return new Type(library, TypeCode.Handle, index, 0);
                    case DeclarationCategory.Constructor:
                    case DeclarationCategory.ClassVariable:
                    case DeclarationCategory.ClassFunction:
                        return new Type(library, TypeCode.Handle, define, 0);
                    case DeclarationCategory.Interface:
                        return new Type(library, TypeCode.Interface, index, 0);
                    case DeclarationCategory.InterfaceFunction:
                        return new Type(library, TypeCode.Interface, define, 0);
                    case DeclarationCategory.Delegate:
                        return new Type(library, TypeCode.Delegate, index, 0);
                    case DeclarationCategory.Task:
                        return new Type(library, TypeCode.Task, index, 0);
                    case DeclarationCategory.Native:
                        break;
                }
                throw new InvalidOperationException();
            }
        }
        public bool Equals(Declaration other) => library == other.library && category == other.category && index == other.index && define == other.define;
        public override bool Equals([NotNullWhen(true)] object? obj) => obj is Declaration other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(library, category, index, define);
        public static bool operator ==(Declaration a, Declaration b) => a.Equals(b);
        public static bool operator !=(Declaration a, Declaration b) => !a.Equals(b);
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
        public bool Managed => dimension > 0 || code >= TypeCode.Handle;
        public Type(Type type, int dimension) : this(type.library, type.code, type.index, dimension) { }
        public bool Equals(Type other) => library == other.library && code == other.code && index == other.index && dimension == other.dimension;
        public static bool operator ==(Type left, Type right) => left.Equals(right);
        public static bool operator !=(Type left, Type right) => !left.Equals(right);
        public override bool Equals(object? obj) => obj is Type type && Equals(type);
        public override int GetHashCode() => HashCode.Combine(library, code, index, dimension);
    }
    internal readonly struct Tuple(params Type[] types) : IEquatable<Tuple>, IEnumerable<Type>
    {
        private readonly Type[] types = types;
        public int Count => types.Length;
        public Type this[int index] => types[index];
        public TypeSpan this[Range range] => new(types[range]);
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
        public static implicit operator Tuple(Type[] types) => new(types);
        public static implicit operator Tuple(Type type) => new(type);
        public static implicit operator TypeSpan(Tuple tuple) => new(tuple.types);
        public override int GetHashCode()
        {
            var result = new HashCode();
            if (types != null)
                foreach (var type in types)
                    result.Add(type.GetHashCode());
            return result.ToHashCode();
        }

        public IEnumerator<Type> GetEnumerator()
        {
            foreach (var type in types) yield return type;
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public static readonly Tuple Empty = new([]);
    }
    internal readonly struct TypeSpan : IEquatable<TypeSpan>, IEnumerable<Type>
    {
        private readonly int start, count;
        private readonly IList<Type> types;
        public int Count => count;
        public Type this[int index] => types[start + index];
        public TypeSpan this[Range range]
        {
            get
            {
                var start = range.Start.GetOffset(count);
                var end = range.End.GetOffset(count);
                return new(this.start + start, end - start, types);
            }
        }

        public TypeSpan(IList<Type> types)
        {
            start = 0; count = types.Count;
            this.types = types;
        }
        public TypeSpan(int start, int count, IList<Type> types)
        {
            this.start = start; this.count = count;
            this.types = types;
        }
        public bool Equals(TypeSpan other)
        {
            if (count != other.count) return false;
            for (int i = 0; i < count; i++)
                if (this[i] != other[i]) return false;
            return true;
        }
        public static bool operator ==(TypeSpan left, TypeSpan right) => left.Equals(right);
        public static bool operator !=(TypeSpan left, TypeSpan right) => !left.Equals(right);
        public static implicit operator TypeSpan(Type[] types) => new(types);
        public static implicit operator Tuple(TypeSpan span)
        {
            if (span.start == 0 && span.count == span.types.Count && span.types is Type[] array) return new Tuple(array);
            var result = new Type[span.count];
            for (int i = 0; i < span.count; i++)
                result[i] = span[i];
            return result;
        }
        public override bool Equals(object? obj) => obj is TypeSpan span && Equals(span);
        public override int GetHashCode()
        {
            var result = new HashCode();
            foreach (var type in this)
                result.Add(type.GetHashCode());
            return result.ToHashCode();
        }
        public IEnumerator<Type> GetEnumerator()
        {
            for (var i = 0; i < count; ++i)
                yield return this[i];
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

    }
}
