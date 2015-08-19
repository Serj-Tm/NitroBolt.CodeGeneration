using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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
                    string path;
                    if (queue.TryTake(out path, TimeSpan.FromSeconds(100)))
                    {
                        CodeProcess(path, ImmutableGenerator.Generate);
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

    }
}
