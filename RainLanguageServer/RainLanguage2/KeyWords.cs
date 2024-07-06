namespace RainLanguageServer.RainLanguage2
{
    internal static class KeyWords
    {
        public const string NAMESPACE = "namespace";
        public const string IMPORT = "import";
        public const string NATIVE = "native";
        public const string PUBLIC = "public";
        public const string INTERNAL = "internal";
        public const string SPACE = "space";
        public const string PROTECTED = "protected";
        public const string PRIVATE = "private";
        public const string ENUM = "enum";
        public const string STRUCT = "struct";
        public const string CLASS = "class";
        public const string INTERFACE = "interface";
        public const string CONST = "const";

        public const string GLOBAL = "global";
        public const string BASE = "base";
        public const string THIS = "this";
        public const string TRUE = "true";
        public const string FALSE = "false";
        public const string NULL = "null";
        public const string VAR = "var";
        public const string BOOL = "bool";
        public const string BYTE = "byte";
        public const string CHAR = "char";
        public const string INTEGER = "integer";
        public const string REAL = "real";
        public const string REAL2 = "real2";
        public const string REAL3 = "real3";
        public const string REAL4 = "real4";
        public const string TYPE = "type";
        public const string STRING = "string";
        public const string HANDLE = "handle";
        public const string ENTITY = "entity";
        public const string DELEGATE = "delegate";
        public const string TASK = "task";
        public const string ARRAY = "array";

        public const string IF = "if";
        public const string ELSEIF = "elseif";
        public const string ELSE = "else";
        public const string WHILE = "while";
        public const string FOR = "for";
        public const string BREAK = "break";
        public const string CONTINUE = "continue";
        public const string RETURN = "return";
        public const string IS = "is";
        public const string AS = "as";
        public const string AND = "and";
        public const string OR = "or";
        public const string START = "start";
        public const string NEW = "new";
        public const string WAIT = "wait";
        public const string EXIT = "exit";
        public const string TRY = "try";
        public const string CATCH = "catch";
        public const string FINALLY = "finally";

        public const string DISCARD_VARIABLE = "_";
        public static bool IsKeyWorld(string value)
        {
            if (value == NAMESPACE) return true;
            if (value == IMPORT) return true;
            if (value == NATIVE) return true;
            if (value == PUBLIC) return true;
            if (value == INTERNAL) return true;
            if (value == SPACE) return true;
            if (value == PROTECTED) return true;
            if (value == PRIVATE) return true;
            if (value == ENUM) return true;
            if (value == STRUCT) return true;
            if (value == CLASS) return true;
            if (value == INTERFACE) return true;
            if (value == CONST) return true;

            if (value == GLOBAL) return true;
            if (value == BASE) return true;
            if (value == THIS) return true;
            if (value == TRUE) return true;
            if (value == FALSE) return true;
            if (value == NULL) return true;
            if (value == VAR) return true;
            if (value == BOOL) return true;
            if (value == BYTE) return true;
            if (value == CHAR) return true;
            if (value == INTEGER) return true;
            if (value == REAL) return true;
            if (value == REAL2) return true;
            if (value == REAL3) return true;
            if (value == REAL4) return true;
            if (value == TYPE) return true;
            if (value == STRING) return true;
            if (value == HANDLE) return true;
            if (value == ENTITY) return true;
            if (value == DELEGATE) return true;
            if (value == TASK) return true;
            if (value == ARRAY) return true;

            if (value == IF) return true;
            if (value == ELSEIF) return true;
            if (value == ELSE) return true;
            if (value == WHILE) return true;
            if (value == FOR) return true;
            if (value == BREAK) return true;
            if (value == CONTINUE) return true;
            if (value == RETURN) return true;
            if (value == IS) return true;
            if (value == AS) return true;
            if (value == AND) return true;
            if (value == OR) return true;
            if (value == START) return true;
            if (value == NEW) return true;
            if (value == WAIT) return true;
            if (value == EXIT) return true;
            if (value == TRY) return true;
            if (value == CATCH) return true;
            if (value == FINALLY) return true;

            return false;
        }
    }
}
