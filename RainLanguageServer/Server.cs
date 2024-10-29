using LanguageServer;
using LanguageServer.Parameters;
using LanguageServer.Parameters.General;
using LanguageServer.Parameters.TextDocument;
using Newtonsoft.Json.Linq;
using RainLanguageServer.RainLanguage;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using Message = LanguageServer.Message;

namespace RainLanguageServer
{
    [RequiresDynamicCode("Calls LanguageServer.Reflector.GetRequestType(MethodInfo)")]
    internal partial class Server(Stream input, Stream output, int timeout = 0) : ServiceConnection(input, output, timeout)
    {
        private class DocumentLoader(string? root, Server server) : IEnumerable<TextDocument>
        {
            private readonly string? root = root;
            private readonly Server server = server;

            public IEnumerator<TextDocument> GetEnumerator()
            {
                if (root == null) yield break;
                foreach (var path in Directory.GetFiles(root, "*.rain", SearchOption.AllDirectories))
                {
                    string unifiedPath = new UnifiedPath(path);
                    if (server.TryGetDoc(unifiedPath, out var document)) yield return document;
                    else using (var sr = File.OpenText(unifiedPath))
                            yield return new TextDocument(unifiedPath, sr.ReadToEnd());
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
        private Manager? manager;
        protected override Result<InitializeResult, ResponseError<InitializeErrorData>> Initialize(InitializeParams param, CancellationToken token)
        {
            var kernelDefinePath = param.initializationOptions?.kernelDefinePath?.Value as string;
            if (kernelDefinePath == null)
                return Result<InitializeResult, ResponseError<InitializeErrorData>>.Error(Message.ServerError(ErrorCodes.ServerNotInitialized, new InitializeErrorData(false)));

            var imports = param.initializationOptions?.imports is JToken jtoken ? jtoken.ToObject<string[]>() : null;

            var result = new InitializeResult() { capabilities = GetServerCapabilities() };

            result.capabilities.experimental = param.capabilities?.experimental;
            if (result.capabilities.completionProvider != null)
                result.capabilities.completionProvider.triggerCharacters = [".", ">"];

            var projectName = param.initializationOptions?.projectName?.Value as string;
            manager = new Manager(projectName ?? "RainTest", kernelDefinePath, imports, LoadRelyLibrary, () => new DocumentLoader(new UnifiedPath(param.rootUri), this), () => documents.Values);
            manager.Reparse(false);

            return Result<InitializeResult, ResponseError<InitializeErrorData>>.Success(result);
        }
        protected override void Initialized()
        {
            if (manager != null)
                lock (manager)
                    foreach (var space in manager.fileSpaces.Values)
                        RefreshDiagnostics(space);
        }
        private TextDocument[] LoadRelyLibrary(string library)
        {
            var text = Proxy.SendRequest<string, string>("rainlanguage/loadRely", library).Result;
            return [new TextDocument(Manager.ToRainScheme(library), text)];
        }

        protected override Result<CompletionResult, ResponseError> Completion(CompletionParams param, CancellationToken token)
        {
            if (manager != null)
            {
                var infos = new List<CompletionInfo>();
                ManagerOperator.Completion(manager, param.textDocument.uri, param.position, infos);
                if (infos.Count > 0)
                {
                    var items = new CompletionItem[infos.Count];
                    for (var i = 0; i < infos.Count; i++)
                    {
                        var info = infos[i];
                        items[i] = new CompletionItem(info.lable) { kind = info.kind, documentation = new MarkupContent(MarkupKind.Markdown, info.documentation) };
                    }
                    return Result<CompletionResult, ResponseError>.Success(new CompletionResult(items));
                }
            }
            return Result<CompletionResult, ResponseError>.Error(Message.ServerError(ErrorCodes.RequestCancelled));
        }
        protected override Result<CompletionItem, ResponseError> ResolveCompletionItem(CompletionItem param, CancellationToken token) => Result<CompletionItem, ResponseError>.Success(param);
        protected override Result<SignatureHelp, ResponseError> SignatureHelp(TextDocumentPositionParams param, CancellationToken token)
        {
            if (manager != null)
            {
                if (ManagerOperator.TrySignatureHelp(manager, param.textDocument.uri, param.position, out var infos, out var functionIndex, out var parameterIndex))
                {
                    var signatures = new SignatureInformation[infos.Count];
                    for (var x = 0; x < infos.Count; x++)
                    {
                        var info = infos[x];
                        var sign = new SignatureInformation(infos[x].name);
                        if (info.info != null) sign.documentation = info.info.Value.GetDocumentation();
                        sign.parameters = new ParameterInformation[info.parameters.Length];
                        for (var y = 0; y < info.parameters.Length; y++)
                        {
                            var parameter = info.parameters[y];
                            sign.parameters[y] = new ParameterInformation(parameter.name);
                            if (parameter.info != null) sign.parameters[y].documentation = parameter.info.Value.GetDocumentation();
                        }
                        signatures[x] = sign;
                    }
                    var result = new SignatureHelp(signatures) { activeSignature = functionIndex, activeParameter = parameterIndex };
                    return Result<SignatureHelp, ResponseError>.Success(result);
                }
            }
            return Result<SignatureHelp, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }
        protected override Result<LanguageServer.Parameters.Range, ResponseError> PrepareRename(TextDocumentPositionParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.TryRename(manager, param.textDocument.uri, param.position, out var ranges) && ranges.Count > 0)
            {
                if (manager.fileSpaces.TryGetValue(new UnifiedPath(param.textDocument.uri), out var space))
                {
                    var position = new TextPosition(space.document, space.document[(int)param.position.line].start.charactor + (int)param.position.character);
                    foreach (var range in ranges)
                        if (range.Contain(position))
                            return Result<LanguageServer.Parameters.Range, ResponseError>.Success(TR2R(range));
                }
            }
            return Result<LanguageServer.Parameters.Range, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled, "该元素不能重命名"));
        }

        protected override Result<WorkspaceEdit, ResponseError> Rename(RenameParams param, CancellationToken token)
        {
            if (manager != null)
            {
                if (ManagerOperator.TryRename(manager, param.textDocument.uri, param.position, out var ranges))
                {
                    var result = new WorkspaceEdit() { changes = [] };
                    var changes = new Dictionary<TextDocument, List<TextEdit>>();
                    foreach (var range in ranges)
                    {
                        if (!changes.TryGetValue(range.start.document, out var edits)) changes.Add(range.start.document, edits = []);
                        edits.Add(new TextEdit(TR2R(range), param.newName));
                    }
                    foreach (var change in changes)
                        result.changes.Add(new Uri(change.Key.path), [.. change.Value]);
                    return Result<WorkspaceEdit, ResponseError>.Success(result);
                }
            }
            return Result<WorkspaceEdit, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }
        protected override Result<Location[], ResponseError> FindReferences(ReferenceParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.FindReferences(manager, param.textDocument.uri, param.position, out var result))
            {
                var locations = new List<Location>();
                foreach (var item in result) locations.Add(TR2L(item));
                return Result<Location[], ResponseError>.Success([.. locations]);
            }
            return Result<Location[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<LocationSingleOrArray, ResponseError> GotoDefinition(TextDocumentPositionParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.TryGetDefinition(manager, param.textDocument.uri, param.position, out var result))
                return Result<LocationSingleOrArray, ResponseError>.Success(TR2L(result));
            return Result<LocationSingleOrArray, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<Hover, ResponseError> Hover(TextDocumentPositionParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.OnHover(manager, param.textDocument.uri, param.position, out var info))
            {
                if (info.markdown) return Result<Hover, ResponseError>.Success(new Hover(new MarkupContent(MarkupKind.Markdown, info.info), TR2R(info.range)));
                else return Result<Hover, ResponseError>.Success(new Hover(info.info, TR2R(info.range)));
            }
            return Result<Hover, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<DocumentHighlight[], ResponseError> DocumentHighlight(TextDocumentPositionParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.OnHighlight(manager, param.textDocument.uri, param.position, out var infos))
            {
                var results = new DocumentHighlight[infos.Count];
                for (int i = 0; i < infos.Count; i++)
                    results[i] = new DocumentHighlight(TR2R(infos[i].range)) { kind = infos[i].kind };
                return Result<DocumentHighlight[], ResponseError>.Success(results);
            }
            return Result<DocumentHighlight[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<DocumentSymbolResult, ResponseError> DocumentSymbols(DocumentSymbolParams param, CancellationToken token)
        {
            if (manager != null && ManagerOperator.TryGetDocumentSymbols(manager, param.textDocument.uri, out var result))
                return Result<DocumentSymbolResult, ResponseError>.Success(result.ToArray());
            return Result<DocumentSymbolResult, ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<CodeLens[], ResponseError> CodeLens(CodeLensParams param, CancellationToken token)
        {
            if (manager != null)
            {
                var list = ManagerOperator.CollectCodeLens(manager, param.textDocument.uri);
                var codeLens = new CodeLens[list.Count];
                for (int i = 0; i < list.Count; i++)
                {
                    var info = list[i];
                    codeLens[i] = new CodeLens(TR2R(info.range)) { command = new Command(info.title, info.command) { arguments = info.arguments } };
                }
                return Result<CodeLens[], ResponseError>.Success(codeLens);
            }
            return Result<CodeLens[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<FoldingRange[], ResponseError> FoldingRange(FoldingRangeRequestParam param, CancellationToken token)
        {
            if (TryGetDoc(param.textDocument.uri, out var document))
            {
                var regions = new Stack<int>();
                var result = new List<FoldingRange>();
                var indents = new Stack<int>();
                var lines = new Stack<int>();
                var lastLine = -1;
                for (var i = 0; i < document.LineCount; i++)
                {
                    var line = document[i];
                    if (line.indent >= 0)
                    {
                        if (indents.Count > 0)
                        {
                            if (indents.Peek() < line.indent)
                            {
                                lines.Push(lastLine);
                                indents.Push(line.indent);
                            }
                            else if (indents.Peek() > line.indent)
                            {
                                while (lines.Count > 0 && indents.Peek() > line.indent)
                                {
                                    result.Add(new FoldingRange() { startLine = lines.Pop(), endLine = lastLine });
                                    indents.Pop();
                                }
                            }
                        }
                        else indents.Push(line.indent);
                        lastLine = line.line;
                    }
                    else
                    {
                        var text = line.ToString();
                        if (RegionRegex().IsMatch(text)) regions.Push(line.line);
                        else if (EndregionRegex().IsMatch(text) && regions.Count > 0) result.Add(new FoldingRange() { endLine = line.line, startLine = regions.Pop() });
                    }
                }
                while (lines.Count > 0) result.Add(new FoldingRange() { startLine = lines.Pop(), endLine = lastLine });
                if (result.Count > 0) return Result<FoldingRange[], ResponseError>.Success([.. result]);
            }
            return Result<FoldingRange[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }
        [JsonRpcMethod("rainlanguage/getSemanticTokens")]
        private Result<SemanticToken[], ResponseError> GetSemanticTokens(SemanticTokenParam param)
        {
            if (manager != null)
            {
                var collector = ManagerOperator.CollectSemanticToken(manager, param.uri);
                return Result<SemanticToken[], ResponseError>.Success(collector.GetResult());
            }
            return Result<SemanticToken[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }

        protected override Result<InlayHintResult[], ResponseError> InlayHint(InlayHintParams param, CancellationToken token)
        {
            if (manager != null)
            {
                var infos = new List<InlayHintInfo>();
                ManagerOperator.CollectInlayHint(manager, param.textDocument.uri, infos);
                if (infos.Count > 0)
                {
                    var results = new InlayHintResult[infos.Count];
                    for (var i = 0; i < infos.Count; i++)
                    {
                        var info = infos[i];
                        var line = info.position.Line;
                        results[i] = new InlayHintResult(new Position(line.line, info.position.charactor - line.start.charactor), info.label);
                        if (info.kind != null)
                            results[i].kind = (long)info.kind.Value;
                        if (info.tooltip != null)
                            results[i].tooltip = info.tooltip.Value.GetDocumentation();
                    }
                    return Result<InlayHintResult[], ResponseError>.Success(results);
                }
            }
            return Result<InlayHintResult[], ResponseError>.Error(Message.ServerError(ErrorCodes.ServerCancelled));
        }
        protected override Result<InlayHintResult, ResponseError> InlayHintResolve(InlayHintResult param, CancellationToken token) => Result<InlayHintResult, ResponseError>.Success(param);

        #region 文档相关
        private readonly Dictionary<string, TextDocument> documents = [];
        private bool TryGetDoc(string path, out TextDocument document)
        {
            lock (documents)
                return documents.TryGetValue(path, out document!);
        }
        protected override void DidOpenTextDocument(DidOpenTextDocumentParams param)
        {
            string path = new UnifiedPath(param.textDocument.uri);
            TextDocument? document = null;
            if (manager != null && manager.fileSpaces.TryGetValue(path, out var fileSpace))
            {
                if (param.textDocument.text != fileSpace.document.text)
                {
                    document = fileSpace.document;
                    fileSpace.document.Set(param.textDocument.text);
                }
                lock (documents)
                    documents[path] = fileSpace.document;
            }
            else lock (documents)
                    documents[path] = document = new TextDocument(path, param.textDocument.text);
            if (document != null) OnChanged();
        }
        protected override void DidChangeTextDocument(DidChangeTextDocumentParams param)
        {
            if (TryGetDoc(new UnifiedPath(param.textDocument.uri), out var document))
            {
                document.OnChanged(param.contentChanges);
                OnChanged();
            }
        }
        protected override void DidCloseTextDocument(DidCloseTextDocumentParams param)
        {
            Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams(new Uri(param.textDocument.uri), []));
            lock (documents)
                documents.Remove(new UnifiedPath(param.textDocument.uri));
            OnChanged();
        }
        #endregion

        private void OnChanged()
        {
            if (manager != null)
            {
                lock (manager)
                {
                    manager.Reparse(true);
                    foreach (var space in manager.fileSpaces.Values)
                        RefreshDiagnostics(space);
                }
            }
        }

        /// <summary>
        /// 刷新文件的诊断信息
        /// </summary>
        /// <param name="files"></param>
        private void RefreshDiagnostics(FileSpace space)
        {
            var diagnostics = new List<Diagnostic>();
            foreach (var msg in space.collector)
            {
                var diagnostic = new Diagnostic(TR2R(msg.range), msg.message);
                switch (msg.level)
                {
                    case ErrorLevel.Error:
                        diagnostic.severity = DiagnosticSeverity.Error;
                        break;
                    case ErrorLevel.Warning:
                        diagnostic.severity = DiagnosticSeverity.Warning;
                        break;
                    case ErrorLevel.Info:
                        diagnostic.severity = DiagnosticSeverity.Information;
                        break;
                    case ErrorLevel.Hint:
                        diagnostic.severity = DiagnosticSeverity.Hint;
                        break;
                }
                if (msg.related.Count > 0)
                {
                    diagnostic.relatedInformation = new DiagnosticRelatedInformation[msg.related.Count];
                    for (var i = 0; i < msg.related.Count; i++)
                        diagnostic.relatedInformation[i] = new DiagnosticRelatedInformation(TR2L(msg.related[i].range), msg.related[i].message);
                }
                if (msg.unnecessary) diagnostic.tags = [DiagnosticTag.Unnecessary];
                diagnostics.Add(diagnostic);
            }
            Proxy.TextDocument.PublishDiagnostics(new PublishDiagnosticsParams(new Uri(space.document.path), [.. diagnostics]));
        }

        public static Location TR2L(TextRange range)
        {
            return new Location(new Uri(range.start.document.path), TR2R(range));
        }
        public static LanguageServer.Parameters.Range TR2R(TextRange range)
        {
            var startLine = range.start.Line;
            var endLine = range.end.Line;
            return new LanguageServer.Parameters.Range(new Position(range.start.Line.line, range.start - startLine.start), new Position(range.end.Line.line, range.end - endLine.start));
        }

        [GeneratedRegex(@"^\s*//\s*region\b")]
        private static partial Regex RegionRegex();
        [GeneratedRegex(@"^\s*//\s*endregion\b")]
        private static partial Regex EndregionRegex();
    }
}
