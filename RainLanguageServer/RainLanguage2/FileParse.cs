using LanguageServer.Parameters;
using System.Diagnostics.CodeAnalysis;

namespace RainLanguageServer.RainLanguage2
{
    internal static class FileParse
    {
        private static void CheckEnd(TextRange segment, int index, MessageCollector collector)
        {
            if (Lexical.TryAnalysis(segment, index, out var lexical, collector) && lexical.type != LexicalType.Annotation)
                collector.Add(lexical.anchor, ErrorLevel.Error, "意外的词条");
        }
        private static void CheckEnd(TextRange segment, TextPosition index, MessageCollector collector) => CheckEnd(segment, index - segment.start, collector);
        private static bool TryParseAttribute(TextLine line, List<TextRange> attributes, MessageCollector collector)
        {
            if (Lexical.TryAnalysis(line, 0, out var bracket, collector) && bracket.type == LexicalType.BracketLeft1)
            {
                var lexical = bracket;
                while (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector) && lexical.type == LexicalType.ConstString)
                {
                    attributes.Add(lexical.anchor);
                    if (Lexical.TryAnalysis(line, lexical.anchor.end, out lexical, collector))
                    {
                        if (lexical.type == LexicalType.BracketRight1) CheckEnd(line, lexical.anchor.end, collector);
                        else if (lexical.type == LexicalType.Comma || lexical.type == LexicalType.Semicolon) continue;
                        else collector.Add(lexical.anchor, ErrorLevel.Error, "意外的词条");
                    }
                    else collector.Add(bracket.anchor, ErrorLevel.Error, "缺少配对的符号");
                    return true;
                }
                if (lexical.anchor.Valid) collector.Add(lexical.anchor, ErrorLevel.Error, "应输出字符串常量");
                else collector.Add(bracket.anchor, ErrorLevel.Error, "需要输入属性");
            }
            return false;
        }
        private static bool TryParseImport(TextLine line, TextPosition position, MessageCollector collecter, [MaybeNullWhen(false)] out ImportSpaceInfo import)
        {
            if (Lexical.TryExtractName(line, position, out var name, collecter))
            {
                CheckEnd(line, name.name.end, collecter);
                name.qualify.Add(name.name);
                import = new ImportSpaceInfo(name.qualify);
                return true;
            }
            import = null;
            return false;
        }
        public static FileSpace ParseSpace(AbstractLibrary library, TextDocument document)
        {
            var collector = new MessageCollector();
            var reader = new LineReader(document);
            var attributes = new List<TextRange>();
            while (reader.ReadLine(out var line))
            {
                if (line.indent < 0) continue;
                if (TryParseAttribute(line, attributes, collector)) continue;
                if (!Lexical.TryAnalysis(line, 0, out var lexical, collector)) continue;
                if (lexical.type != LexicalType.Word || lexical.anchor != KeyWords.NAMESPACE) break;
                if (!Lexical.TryExtractName(line, lexical.anchor.end, out var name, collector)) break;
                FileSpace? space = null;
                foreach (var spaceName in name)
                {
                    if (space == null) space = new FileSpace(spaceName, null, library, document, collector);
                    else space = new FileSpace(spaceName, space, space.space.GetChild(spaceName.ToString()), document, collector);
                    space.range = line;
                }
                space!.space.attributes.AddRange(attributes);
                ParseSpace(space, reader, line.indent);
                space.range &= reader.GetLastValidLine();
                while (space.parent != null) space = space.parent;
                return space;
            }
            throw new Exception($"文件( {document.path} )解析失败：最外层命名空间名与库名不一致");
        }
        public static void ParseSpace(FileSpace space, LineReader reader, int parentIndent)
        {
            var attributes = new List<TextRange>();
            var annotations = new List<TextLine>();
            var indent = -1;
            while (reader.ReadLine(out var line))
            {
                if (line.indent == TextLine.ANNOTATION) annotations.Add(line);
                else if (line.indent == TextLine.EMPTY) annotations.Clear();
                else if (TryParseAttribute(line, attributes, space.collector)) continue;
                else if (Lexical.TryAnalysis(line, 0, out var lexical, space.collector))
                {
                    if (indent < 0)
                    {
                        indent = line.indent;
                        if (indent <= parentIndent) break;
                    }
                    else if (line.indent > indent) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                    else if (line.indent < indent)
                    {
                        if (parentIndent < 0) space.collector.Add(line[line.indent..(line.indent + 1)], ErrorLevel.Error, "对齐错误");
                        else
                        {
                            space.space.attributes.AddRange(attributes);
                            break;
                        }
                    }
                    if (lexical.type == LexicalType.Word)
                    {
                        if (lexical.anchor == KeyWords.IMPORT)
                        {
                            foreach (var attribute in attributes)
                                space.collector.Add(attribute, ErrorLevel.Error, "无效的属性");

                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var name, space.collector))
                            {
                                CheckEnd(line, name.name.end, space.collector);
                                name.qualify.Add(name.name);
                                space.imports.Add(new ImportSpaceInfo(name.qualify));
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else if (lexical.anchor == KeyWords.NAMESPACE)
                        {
                            if (Lexical.TryExtractName(line, lexical.anchor.end, out var name, space.collector))
                            {
                                CheckEnd(line, name.name.end, space.collector);
                                var index = space;
                                foreach (var spaceName in name)
                                    index = new FileSpace(spaceName, index, index.space.GetChild(spaceName.ToString()), space.document, space.collector) { range = line };
                                index.space.attributes.AddRange(attributes);
                                ParseSpace(index, reader, line.indent);
                                index.range &= reader.GetLastValidLine();
                                reader.Rollback();
                            }
                            else space.collector.Add(lexical.anchor, ErrorLevel.Error, "需要输入命名空间名");
                        }
                        else
                        {
                            //todo 解析声明
                        }
                    }
                    else if (lexical.IsReloadable)
                    {
                        //todo 解析重载函数
                    }
                    else space.collector.Add(lexical.anchor, ErrorLevel.Error, "意外的词条");
                    annotations.Clear();
                    attributes.Clear();
                }
            }
        }
    }
}
