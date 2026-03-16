using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
#if UNITY_EDITOR
using EditorAssembliesType = UnityEditor.Compilation.AssembliesType;
using EditorAssembly = UnityEditor.Compilation.Assembly;
using EditorCompilationPipeline = UnityEditor.Compilation.CompilationPipeline;
#endif

namespace ProjectMagicEscape.Editor.RoslynGateway
{
    internal static class RoslynDoCodeExecutor
    {
        private static readonly string[] EntryMethodNames = { "Execute", "Main", "Run" };
        private static readonly object CompileContextSync = new object();

        private static bool _compileContextReady;
        private static MetadataReference[] _preparedReferences = Array.Empty<MetadataReference>();
        private static string[] _preparedDefines = Array.Empty<string>();

        public static GatewayDoCodeResult Execute(string code, int timeoutSec)
        {
            Stopwatch totalWatch = Stopwatch.StartNew();
            GatewayDoCodeResult result;

            if (string.IsNullOrWhiteSpace(code))
            {
                result = CreateErrorResult("CompileError", "InputError", "代码字符串为空，无法编译");
                result.compile.usedMode = "none";
                result.timingMs.total = 0;
                return result;
            }

            try
            {
                EnsureCompileContextReady();

                if (LooksLikeSnippet(code))
                {
                    string wrapped = BuildWrappedSnippetSource(code);
                    result = TryCompileAndExecute(wrapped, "wrappedSnippet");
                }
                else
                {
                    result = TryCompileAndExecute(code, "fullSource");

                    if (!result.success
                        && string.Equals(result.state, "CompileError", StringComparison.Ordinal)
                        && ContainsCompileDiagnostic(result, "CS8805"))
                    {
                        string wrapped = BuildWrappedSnippetSource(code);
                        result = TryCompileAndExecute(wrapped, "wrappedSnippet");
                    }
                }
            }
            catch (Exception ex)
            {
                result = CreateErrorResult("RuntimeError", ex.GetType().FullName, ex.Message, ex.ToString());
            }

            totalWatch.Stop();
            result.timingMs.total = (int)totalWatch.ElapsedMilliseconds;
            return result;
        }

        private static GatewayDoCodeResult TryCompileAndExecute(string sourceCode, string usedMode)
        {
            GatewayDoCodeResult result = new GatewayDoCodeResult();
            result.requestId = string.Empty;
            result.success = false;
            result.state = "CompileError";
            result.compile.usedMode = usedMode;

            Stopwatch compileWatch = Stopwatch.StartNew();
            CompilationBundle compileBundle = CompileSourceToAssembly(sourceCode);
            compileWatch.Stop();
            result.timingMs.compile = (int)compileWatch.ElapsedMilliseconds;

            AppendCompileDiagnostics(result, compileBundle.Diagnostics);

            if (!compileBundle.Success || compileBundle.CompiledAssembly == null)
            {
                if (compileBundle.Exception != null)
                {
                    result.error.type = compileBundle.Exception.GetType().FullName;
                    result.error.message = compileBundle.Exception.Message;
                    result.error.stackTrace = compileBundle.Exception.ToString();
                }

                result.success = false;
                result.state = "CompileError";
                return result;
            }

            Stopwatch executeWatch = Stopwatch.StartNew();
            bool invokeSuccess = TryInvokeAssemblyEntry(compileBundle.CompiledAssembly, out object invokeResult, out Exception invokeException, out string invokeFailureReason);
            executeWatch.Stop();
            result.timingMs.execute = (int)executeWatch.ElapsedMilliseconds;

            if (!invokeSuccess)
            {
                result.success = false;
                result.state = "RuntimeError";
                if (invokeException != null)
                {
                    result.error.type = invokeException.GetType().FullName;
                    result.error.message = invokeException.Message;
                    result.error.stackTrace = invokeException.ToString();
                }
                else
                {
                    result.error.type = "InvokeError";
                    result.error.message = invokeFailureReason;
                    result.error.stackTrace = string.Empty;
                }

                return result;
            }

            result.result = DoCodeResultSerializer.Serialize(invokeResult);
            result.success = true;
            result.state = "Success";
            return result;
        }

        private static CompilationBundle CompileSourceToAssembly(string sourceCode)
        {
            CompilationBundle bundle = new CompilationBundle();
            bundle.Diagnostics = Array.Empty<Diagnostic>();

            try
            {
                EnsureCompileContextReady();
            }
            catch (Exception ex)
            {
                bundle.Exception = ex;
                bundle.Success = false;
                return bundle;
            }

            CSharpParseOptions parseOptions = BuildParseOptions();
            CSharpCompilationOptions compilationOptions = BuildCompilationOptions();
            string assemblyName = $"__AIGatewayRuntime_{Guid.NewGuid():N}";

            SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(sourceCode, parseOptions);
            CSharpCompilation compilation = CSharpCompilation.Create(
                assemblyName,
                new[] { syntaxTree },
                _preparedReferences,
                compilationOptions);

            using (MemoryStream peStream = new MemoryStream())
            using (MemoryStream pdbStream = new MemoryStream())
            {
                EmitResult emitResult;
                try
                {
                    emitResult = compilation.Emit(
                        peStream,
                        pdbStream,
                        options: new EmitOptions(debugInformationFormat: DebugInformationFormat.PortablePdb));
                }
                catch (Exception ex)
                {
                    bundle.Exception = ex;
                    bundle.Success = false;
                    return bundle;
                }

                Diagnostic[] diagnostics = emitResult.Diagnostics.IsDefaultOrEmpty
                    ? Array.Empty<Diagnostic>()
                    : emitResult.Diagnostics.ToArray();
                bundle.Diagnostics = diagnostics;

                if (!emitResult.Success)
                {
                    bundle.Success = false;
                    return bundle;
                }

                try
                {
                    byte[] peBytes = peStream.ToArray();
                    byte[] pdbBytes = pdbStream.ToArray();
                    bundle.CompiledAssembly = pdbBytes.Length > 0
                        ? Assembly.Load(peBytes, pdbBytes)
                        : Assembly.Load(peBytes);
                    bundle.Success = true;
                    return bundle;
                }
                catch (Exception ex)
                {
                    bundle.Exception = ex;
                    bundle.Success = false;
                    return bundle;
                }
            }
        }

        private static CSharpParseOptions BuildParseOptions()
        {
            string[] defines = _preparedDefines ?? Array.Empty<string>();
            return CSharpParseOptions.Default
                .WithLanguageVersion(LanguageVersion.Latest)
                .WithPreprocessorSymbols(defines);
        }

        private static CSharpCompilationOptions BuildCompilationOptions()
        {
            return new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Release,
                allowUnsafe: true,
                concurrentBuild: false);
        }

        private static bool ContainsCompileDiagnostic(GatewayDoCodeResult result, string diagnosticCode)
        {
            if (result == null || result.compile == null || result.compile.diagnostics == null || string.IsNullOrWhiteSpace(diagnosticCode))
            {
                return false;
            }

            foreach (GatewayDiagnosticEntry entry in result.compile.diagnostics)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.code))
                {
                    continue;
                }

                if (string.Equals(entry.code.Trim(), diagnosticCode, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendCompileDiagnostics(GatewayDoCodeResult result, IEnumerable<Diagnostic> diagnostics)
        {
            if (result == null || diagnostics == null)
            {
                return;
            }

            foreach (Diagnostic diagnostic in diagnostics)
            {
                if (diagnostic == null)
                {
                    continue;
                }

                if (diagnostic.Severity == DiagnosticSeverity.Hidden)
                {
                    continue;
                }

                GatewayDiagnosticEntry entry = new GatewayDiagnosticEntry();
                entry.code = diagnostic.Id ?? string.Empty;
                entry.message = diagnostic.GetMessage() ?? string.Empty;
                entry.severity = diagnostic.Severity == DiagnosticSeverity.Error
                    ? "Error"
                    : diagnostic.Severity == DiagnosticSeverity.Warning
                        ? "Warning"
                        : "Info";

                int line = 0;
                int column = 0;
                if (diagnostic.Location != Location.None && diagnostic.Location.IsInSource)
                {
                    FileLinePositionSpan span = diagnostic.Location.GetLineSpan();
                    line = span.StartLinePosition.Line + 1;
                    column = span.StartLinePosition.Character + 1;
                }

                entry.line = line;
                entry.column = column;
                result.compile.diagnostics.Add(entry);
            }
        }

        private static bool TryInvokeAssemblyEntry(
            Assembly assembly,
            out object invokeResult,
            out Exception invokeException,
            out string invokeFailureReason)
        {
            invokeResult = null;
            invokeException = null;
            invokeFailureReason = string.Empty;

            if (assembly == null)
            {
                invokeFailureReason = "编译成功但未拿到程序集实例";
                return false;
            }

            MethodInfo targetMethod = FindEntryMethod(assembly, out Type targetType, out bool requiresInstance);
            if (targetMethod == null)
            {
                invokeFailureReason = "编译成功，但未找到可调用入口方法（Execute/Main/Run 无参）";
                return false;
            }

            try
            {
                object targetInstance = null;
                if (requiresInstance)
                {
                    targetInstance = Activator.CreateInstance(targetType);
                }

                object rawResult = targetMethod.Invoke(targetInstance, null);

                if (rawResult is Task task)
                {
                    task.GetAwaiter().GetResult();

                    Type taskType = task.GetType();
                    if (taskType.IsGenericType)
                    {
                        PropertyInfo resultProperty = taskType.GetProperty("Result");
                        rawResult = resultProperty != null ? resultProperty.GetValue(task) : null;
                    }
                    else
                    {
                        rawResult = null;
                    }
                }

                invokeResult = rawResult;
                return true;
            }
            catch (TargetInvocationException ex)
            {
                invokeException = ex.InnerException ?? ex;
                return false;
            }
            catch (Exception ex)
            {
                invokeException = ex;
                return false;
            }
        }

        private static MethodInfo FindEntryMethod(Assembly assembly, out Type targetType, out bool requiresInstance)
        {
            targetType = null;
            requiresInstance = false;

            Type[] types;
            try
            {
                types = assembly.GetTypes();
            }
            catch
            {
                return null;
            }

            BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

            foreach (Type type in types)
            {
                if (type == null)
                {
                    continue;
                }

                foreach (string methodName in EntryMethodNames)
                {
                    MethodInfo staticMethod = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
                    if (staticMethod != null && staticMethod.IsStatic && !staticMethod.ContainsGenericParameters)
                    {
                        targetType = type;
                        requiresInstance = false;
                        return staticMethod;
                    }
                }

                foreach (string methodName in EntryMethodNames)
                {
                    MethodInfo instanceMethod = type.GetMethod(methodName, flags, null, Type.EmptyTypes, null);
                    if (instanceMethod == null || instanceMethod.IsStatic || instanceMethod.ContainsGenericParameters)
                    {
                        continue;
                    }

                    if (type.GetConstructor(Type.EmptyTypes) == null)
                    {
                        continue;
                    }

                    targetType = type;
                    requiresInstance = true;
                    return instanceMethod;
                }
            }

            return null;
        }

        private static string BuildWrappedSnippetSource(string source)
        {
            string normalized = source.Replace("\r\n", "\n");
            string[] lines = normalized.Split('\n');
            List<string> userUsings = new List<string>();
            StringBuilder bodyBuilder = new StringBuilder();
            bool parsingUsings = true;

            foreach (string line in lines)
            {
                string trimmed = line.Trim();

                if (parsingUsings && string.IsNullOrWhiteSpace(trimmed))
                {
                    continue;
                }

                if (parsingUsings && trimmed.StartsWith("using ", StringComparison.Ordinal) && trimmed.EndsWith(";", StringComparison.Ordinal) && !trimmed.Contains("{"))
                {
                    userUsings.Add(trimmed);
                    continue;
                }

                parsingUsings = false;
                bodyBuilder.AppendLine(line);
            }

            if (bodyBuilder.Length == 0)
            {
                bodyBuilder.AppendLine(normalized);
            }

            HashSet<string> finalUsings = new HashSet<string>(StringComparer.Ordinal)
            {
                "using System;",
                "using System.Linq;",
                "using System.Reflection;",
                "using UnityEngine;",
                "using UnityEditor;",
                "using UnityEditor.MemoryProfiler;",
                "using System.Collections.Generic;",
                "using System.IO;",
                "using System.Text;"
            };

            foreach (string usingLine in userUsings)
            {
                finalUsings.Add(usingLine);
            }

            StringBuilder wrapped = new StringBuilder();
            foreach (string usingLine in finalUsings)
            {
                wrapped.AppendLine(usingLine);
            }

            wrapped.AppendLine("public static class __AIGatewaySnippetEntry");
            wrapped.AppendLine("{");
            wrapped.AppendLine("    public static object Execute()");
            wrapped.AppendLine("    {");
            wrapped.Append(bodyBuilder.ToString());
            wrapped.AppendLine("        return null;");
            wrapped.AppendLine("    }");
            wrapped.AppendLine("}");

            return wrapped.ToString();
        }

        private static bool LooksLikeSnippet(string code)
        {
            if (string.IsNullOrWhiteSpace(code))
            {
                return true;
            }

            string trimmed = code.TrimStart();
            if (trimmed.StartsWith("using ", StringComparison.Ordinal) && !trimmed.Contains(" class ") && !trimmed.Contains(" namespace "))
            {
                return true;
            }

            if (trimmed.StartsWith("namespace ", StringComparison.Ordinal)
                || trimmed.StartsWith("class ", StringComparison.Ordinal)
                || trimmed.StartsWith("public class ", StringComparison.Ordinal)
                || trimmed.StartsWith("internal class ", StringComparison.Ordinal)
                || trimmed.StartsWith("struct ", StringComparison.Ordinal)
                || trimmed.StartsWith("record ", StringComparison.Ordinal)
                || trimmed.StartsWith("interface ", StringComparison.Ordinal))
            {
                return false;
            }

            if (code.Contains(" class ")
                || code.Contains(" namespace ")
                || code.Contains(" struct ")
                || code.Contains(" record ")
                || code.Contains(" interface "))
            {
                return false;
            }

            return true;
        }

        private static void EnsureCompileContextReady()
        {
            if (_compileContextReady)
            {
                return;
            }

            lock (CompileContextSync)
            {
                if (_compileContextReady)
                {
                    return;
                }

                PrepareCompileContext();
                _compileContextReady = true;
            }
        }

        private static void PrepareCompileContext()
        {
            List<MetadataReference> references = new List<MetadataReference>(256);
            HashSet<string> referencePaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> defines = new HashSet<string>(StringComparer.Ordinal);

            TryAddReferencesFromCompilationPipeline(references, referencePaths, defines);
            AddFallbackRuntimeReferences(references, referencePaths);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null)
                {
                    continue;
                }

                string name;
                try
                {
                    name = assembly.GetName().Name;
                }
                catch
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                if (!IsLikelyCompileDependency(name))
                {
                    continue;
                }

                AddReferenceByLoadedAssemblyPath(references, referencePaths, assembly);
            }

            _preparedReferences = references.ToArray();
            _preparedDefines = defines.ToArray();
        }

        private static void AddFallbackRuntimeReferences(List<MetadataReference> references, HashSet<string> keys)
        {
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(object).Assembly);
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(Task).Assembly);
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(Enumerable).Assembly);
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(UnityEngine.Debug).Assembly);
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(GatewayDoCodeResult).Assembly);

#if UNITY_EDITOR
            AddReferenceByLoadedAssemblyPath(references, keys, typeof(UnityEditor.Editor).Assembly);
#endif

            AddReferenceByName(references, keys, "UnityEngine.CoreModule");
            AddReferenceByName(references, keys, "UnityEngine");
            AddReferenceByName(references, keys, "UnityEditor.CoreModule");
            AddReferenceByName(references, keys, "UnityEditor");
            AddReferenceByName(references, keys, "netstandard");
            AddReferenceByName(references, keys, "System.Runtime");
            AddReferenceByName(references, keys, "System.Core");
            AddReferenceByName(references, keys, "mscorlib");
            AddReferenceByName(references, keys, "Microsoft.CodeAnalysis");
            AddReferenceByName(references, keys, "Microsoft.CodeAnalysis.CSharp");
        }

        private static void TryAddReferencesFromCompilationPipeline(
            List<MetadataReference> references,
            HashSet<string> keys,
            HashSet<string> defines)
        {
#if UNITY_EDITOR
            EditorAssembly targetAssembly = null;
            EditorAssembly[] assemblies;

            try
            {
                assemblies = EditorCompilationPipeline.GetAssemblies(EditorAssembliesType.Editor);
            }
            catch
            {
                return;
            }

            if (assemblies == null || assemblies.Length == 0)
            {
                return;
            }

            foreach (EditorAssembly assembly in assemblies)
            {
                if (assembly == null)
                {
                    continue;
                }

                if (string.Equals(assembly.name, "Assembly-CSharp-Editor", StringComparison.Ordinal))
                {
                    targetAssembly = assembly;
                    break;
                }

                if (targetAssembly == null && string.Equals(assembly.name, "Assembly-CSharp", StringComparison.Ordinal))
                {
                    targetAssembly = assembly;
                }
            }

            if (targetAssembly == null)
            {
                return;
            }

            AddReferenceByFilePath(references, keys, targetAssembly.outputPath);

            string[] compiledReferences = targetAssembly.compiledAssemblyReferences;
            if (compiledReferences != null)
            {
                foreach (string referencePath in compiledReferences)
                {
                    AddReferenceByFilePath(references, keys, referencePath);
                }
            }

            string[] editorDefines = targetAssembly.defines;
            if (editorDefines == null)
            {
                return;
            }

            foreach (string define in editorDefines)
            {
                if (string.IsNullOrWhiteSpace(define))
                {
                    continue;
                }

                defines.Add(define.Trim());
            }
#endif
        }

        private static bool IsLikelyCompileDependency(string assemblyName)
        {
            return assemblyName.StartsWith("System", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("UnityEngine", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("UnityEditor", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Unity.", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("ProjectMagicEscape", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Assembly-CSharp", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Battle.", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Inventory.", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Game.", StringComparison.OrdinalIgnoreCase)
                || assemblyName.StartsWith("Microsoft.CodeAnalysis", StringComparison.OrdinalIgnoreCase)
                || string.Equals(assemblyName, "mscorlib", StringComparison.OrdinalIgnoreCase)
                || string.Equals(assemblyName, "Newtonsoft.Json", StringComparison.OrdinalIgnoreCase);
        }

        private static void AddReferenceByName(List<MetadataReference> references, HashSet<string> keys, string assemblyName)
        {
            if (string.IsNullOrWhiteSpace(assemblyName))
            {
                return;
            }

            string normalized = NormalizeAssemblyName(assemblyName);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly == null)
                {
                    continue;
                }

                string loadedName;
                try
                {
                    loadedName = assembly.GetName().Name;
                }
                catch
                {
                    continue;
                }

                if (!string.Equals(loadedName, normalized, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                AddReferenceByLoadedAssemblyPath(references, keys, assembly);
                return;
            }

            try
            {
                Assembly loaded = Assembly.Load(new AssemblyName(normalized));
                AddReferenceByLoadedAssemblyPath(references, keys, loaded);
            }
            catch
            {
            }
        }

        private static string NormalizeAssemblyName(string assemblyName)
        {
            string name = assemblyName.Trim();
            if (name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                return Path.GetFileNameWithoutExtension(name);
            }

            return name;
        }

        private static void AddReferenceByAssembly(List<MetadataReference> references, HashSet<string> keys, Assembly assembly)
        {
            AddReferenceByLoadedAssemblyPath(references, keys, assembly);
        }

        private static void AddReferenceByLoadedAssemblyPath(List<MetadataReference> references, HashSet<string> keys, Assembly assembly)
        {
            if (assembly == null)
            {
                return;
            }

            string location;
            try
            {
                location = assembly.Location;
            }
            catch
            {
                return;
            }

            AddReferenceByFilePath(references, keys, location);
        }

        private static void AddReferenceByFilePath(List<MetadataReference> references, HashSet<string> keys, string assemblyPath)
        {
            if (string.IsNullOrWhiteSpace(assemblyPath))
            {
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(assemblyPath);
            }
            catch
            {
                return;
            }

            if (!File.Exists(fullPath))
            {
                return;
            }

            if (!keys.Add(fullPath))
            {
                return;
            }

            try
            {
                references.Add(MetadataReference.CreateFromFile(fullPath));
            }
            catch
            {
            }
        }

        private static GatewayDoCodeResult CreateErrorResult(string state, string errorType, string message, string stackTrace = "")
        {
            GatewayDoCodeResult result = new GatewayDoCodeResult();
            result.success = false;
            result.state = state;
            result.error.type = errorType;
            result.error.message = message;
            result.error.stackTrace = stackTrace ?? string.Empty;
            return result;
        }

        private sealed class CompilationBundle
        {
            public bool Success;
            public Assembly CompiledAssembly;
            public Diagnostic[] Diagnostics;
            public Exception Exception;
        }
    }
}

