using Microsoft.CodeAnalysis.CSharp.Syntax;
using NitroBolt.Functional;
using NitroBolt.Wui;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGeneration
{
    public class CodeChangeMonitor
    {
        public static void Monitor(string dir)
        {
            Console.WriteLine("CodeGenerator. Monitoring..");
            var queue = new BlockingCollection<string>(new ConcurrentQueue<string>());
            using (var watcher = CreateWatcher(dir, queue))
            {
                for (;;)
                {
                    try
                    {
                        string path;
                        if (queue.TryTake(out path, TimeSpan.FromSeconds(100)))
                        {
                            CodeProcess(path, ImmutableGenerator.Generate);
                        }
                    }
                    catch (Exception exc)
                    {
                        Console.Error.WriteLine(exc);
                    }
                }
            }
        }

        private static System.IO.FileSystemWatcher CreateWatcher(string dir, BlockingCollection<string> files)
        {
            var watcher = new System.IO.FileSystemWatcher(dir, "*.cs") { IncludeSubdirectories = true, InternalBufferSize = 1000000 };

            System.IO.FileSystemEventHandler handler = (_s, _e) =>
            {
                Console.WriteLine(_e.FullPath);
                Console.WriteLine("  {0}, {1}", _e.ChangeType, _e.Name);
                files.TryAdd(_e.FullPath);
            };
            watcher.Changed += handler;
            watcher.Created += handler;
            watcher.Deleted += handler;
            watcher.Renamed += (_s, _e) => handler(_s, _e);
            watcher.NotifyFilter = System.IO.NotifyFilters.FileName;
            watcher.EnableRaisingEvents = true;

            return watcher;
        }
        public static void ProcessAll(string dirPath)
        {
            var pathes = System.IO.Directory.GetFiles(dirPath, "*.cs", System.IO.SearchOption.AllDirectories);
            foreach (var path in pathes)
            {
                CodeProcess(path, ImmutableGenerator.Generate);
            }
        }

        public static bool CodeProcess(string codePath, Func<string, string> generator)
        {
            var gPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(codePath), System.IO.Path.GetFileNameWithoutExtension(codePath) + ".g.cs");
            var isG = System.IO.File.Exists(gPath);
            
            var code = generator(System.IO.File.ReadAllText(codePath));
            if (code == null)
                return false;
            var isChanged = isG ? System.IO.File.ReadAllText(gPath) != code : false;
            if (isChanged)
                System.IO.File.WriteAllText(gPath, code);
            Console.WriteLine($"{(isG ? "+" : " ")}{(isChanged ? "!" : " ")}{codePath}");
            return isChanged;
        }

        static Dictionary<string, object> CacheState = new Dictionary<string, object>();
        static T Cache<T>(Func<T> f, string key)
        {
            if (CacheState.ContainsKey(key))
            {
                var v = CacheState[key];
                if (v is T)
                    return (T)v;
                return default(T);
            }
            var v2 = f();
            CacheState[key] = v2;
            return v2;
        }

        //public static HElement Visualize()
        //{
        //    var files = Cache(() => Directory.GetFiles(@"p:\Projects\NitroBolt.Projects", "*.cs", SearchOption.AllDirectories)
        //         .Where(file => file.IndexOf(@"\obj\") < 0)
        //         .ToArray(),
        //         "files"
        //        );
        //    var fileGIndex = files.Where(file => file.EndsWith(".g.cs")).ToDictionary(file => file);

        //    var classes = Cache(() => files
        //        .Where(file => !file.EndsWith(".g.cs"))
        //        .SelectMany(file => new[] { $"{(fileGIndex.Find(file.Substring(0, file.Length - 3) + ".g.cs") != null ? "*" : "\u00a0")} {file}" }
        //          .Concat(ImmutableClasses(file).Select(@class => $"\u00a0\u00a0{@class}"))
        //        ), "classes");

        //    var results = 
        //        classes
        //        .Select(line =>
        //            {
        //                var isGood = line.TrimStart().StartsWith("+");
        //                return h.Div(h.style(isGood ? "color:blue;cursor:pointer;" : null), line, isGood? new hdata { {"command", "class" } }: null, h.onclick(";"));
        //            }
        //         );

        //    return h.Div
        //        (
        //          results,
        //          h.Div(h.style("color:green"), DateTime.UtcNow)
        //        );
        //}
        public static object Visualize()
        {
            var files = Cache(() => Directory.GetFiles(@"p:\Projects\NitroBolt.Projects", "*.cs", SearchOption.AllDirectories)
                 .Where(file => file.IndexOf(@"\obj\") < 0)
                 .ToArray(),
                 "files"
                );
            var fileGIndex = files.Where(file => file.EndsWith(".g.cs")).ToDictionary(file => file);

            var classes = Cache(() => files
                .Where(file => !file.EndsWith(".g.cs"))
                .SelectMany(file => new[] { $"{(fileGIndex.Find(file.Substring(0, file.Length - 3) + ".g.cs") != null ? "*" : "\u00a0")} {file}" }
                  .Concat(ImmutableClasses(file).Select(@class => $"\u00a0\u00a0{@class}"))
                ), "classes");

            var results =
                classes
                .Select(line =>
                    {
                        var isGood = line.TrimStart().StartsWith("+");
                        return new { isGood, line };
                        //return h.Div(h.style(isGood ? "color:blue;cursor:pointer;" : null), line, isGood ? new hdata { { "command", "class" } } : null, h.onclick(";"));
                    }
                 );
            return results;
        }

        public static int[] TestVisualize()
        {
            return
                  Enumerable.Range(10, 200)
                    .ToArray();
                
        }

        static readonly HBuilder h = null;
        static string IsReadonlyField(string filename)
        {
            var code = File.ReadAllText(filename);
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            return tree.GetRoot().ChildNodes().Select(node => $"{node.GetType().FullName}").JoinToString("\r\n");
        }
        static IEnumerable<string> ImmutableClasses(string filename)
        {
            var code = File.ReadAllText(filename);
            var tree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText(code);
            foreach (var @classOrOther in ImmutableClasses(tree.GetRoot()))
            {
                var @class = classOrOther as ClassDeclarationSyntax;
                if (@class == null)
                    yield return classOrOther?.ToString();
                else
                {
                    var isImmutable = @class.Members.OfType<FieldDeclarationSyntax>().Any(field => field.Modifiers.Any(mod => mod.ValueText == "readonly") && !field.Modifiers.Any(mod => mod.ValueText == "static"));
                    yield return $"{(isImmutable ? "+" : "\u00a0")} {@class.Identifier}";
                }
            }
        }
        static IEnumerable<object> ImmutableClasses(Microsoft.CodeAnalysis.SyntaxNode node)
        {
            foreach (var child in node.ChildNodes())
            {
                if (child is UsingDirectiveSyntax)
                    continue;
                if (child is AttributeListSyntax)
                    continue;
                if (child is EnumDeclarationSyntax)
                    continue;
                if (child is QualifiedNameSyntax)
                    continue;
                if (child is BaseListSyntax || child is FieldDeclarationSyntax || child is PropertyDeclarationSyntax || child is ConstructorDeclarationSyntax || child is MethodDeclarationSyntax)
                    continue;
                if (child is InterfaceDeclarationSyntax)
                    continue;

                var isDrill = child is NamespaceDeclarationSyntax || child is ClassDeclarationSyntax;
                var isView = !isDrill;

                if (child is ClassDeclarationSyntax)
                    isView = true;

                if (isDrill)
                {
                    foreach (var cc in ImmutableClasses(child))
                        yield return cc;
                }
                //if (child is ClassDeclarationSyntax)
                //{
                //    foreach (var member in )
                //    foreach (var cc in ImmutableClasses(child))
                //        yield return cc;
                //}
                if (isView)
                {
                    var @class = child as ClassDeclarationSyntax;
                    if (@class != null)
                        yield return @class;
                    else
                        yield return child.GetType().FullName;
                }
            }
        }
    }
}
