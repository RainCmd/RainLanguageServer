namespace RainLanguageServer
{
    internal readonly struct SignatureInfo(string name, Info? info, SignatureInfo.ParameterInfo[] parameters)
    {
        public readonly struct ParameterInfo(string name, Info? info)
        {
            public readonly string name = name;
            public readonly Info? info = info;
        }
        public readonly string name = name;
        public readonly Info? info = info;
        public readonly ParameterInfo[] parameters = parameters;
    }
}
