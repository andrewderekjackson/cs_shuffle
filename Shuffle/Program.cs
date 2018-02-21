using System;
using System.CodeDom;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Shuffle {
    class Program {
        static void Main(string[] args)
        {
            var worker = new WorkerQueue();
            worker.Start();

            IDisposable watches = new CompositeDisposable(CreateWatches(worker));
            
            Console.WriteLine("Listening for changes... Press any key to exit.");
            Console.ReadLine();

            watches.Dispose();
            worker.Stop();
            
        }

        private static IEnumerable<IDisposable> CreateWatches(WorkerQueue worker)
        {

            yield return Shuffle
                    .Pipeline("Framework")
                    .From(@"C:\Source\Framework\bin\Module", @"*.*")
                    .To(@"C:\Source\Customization\packages\Aderant.Framework.Core\lib")
                    .To(@"C:\Source\Inquiries\packages\Aderant.Framework.Core\lib")
                    .To(@"c:\expertshare")
                    .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Customization")
                .From(@"C:\Source\Customization\bin\Module", @"*.*")
                .To(@"C:\Source\Inquiries\packages\Aderant.Customization\lib")
                .To(@"c:\expertshare")
                .Subscribe(worker);

            yield return Shuffle
                .Pipeline("Inquiries")
                .From(@"C:\Source\Inquiries\bin\Module", @"*.*")
                .To(@"c:\expertshare")
                .Subscribe(worker);
           

        }

    }

    public class Pipeline
    {

        public List<string> Parts { get; set; } = new List<string>();

        public IObservable<WatchEventArgs> Source { get; set; }

        public string Name { get; set; }
    }

    public class WatchEventArgs
    {

        public WatchEventArgs(FileSystemEventArgs eventArgs)
        {
            Event = eventArgs;
        }

        public FileSystemEventArgs Event { get; set; }

        public List<FileOperation> Operations { get; set; } = new List<FileOperation>();
        
    }

    public static class Shuffle {
        
        public static Pipeline From(this Pipeline pipeline, string folder, string filter) {

            var source = Observable.Create<WatchEventArgs>((observer) => {

                FileSystemWatcher watcher = new FileSystemWatcher(folder, filter);
                watcher.NotifyFilter = NotifyFilters.LastAccess 
                                       | NotifyFilters.LastWrite
                                       | NotifyFilters.FileName 
                                       | NotifyFilters.DirectoryName;

                watcher.IncludeSubdirectories = true;

                var created = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    v => watcher.Created += v, v => watcher.Created -= v).Select(v => v.EventArgs);

                var changed = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    v => watcher.Changed += v, v => watcher.Changed -= v).Select(v => v.EventArgs);

                var deleted = Observable.FromEventPattern<FileSystemEventHandler, FileSystemEventArgs>(
                    v => watcher.Deleted += v, v => watcher.Deleted -= v).Select(v => v.EventArgs);

                var renamed = Observable.FromEventPattern<RenamedEventHandler, RenamedEventArgs>(
                    v => watcher.Renamed += v, v => watcher.Renamed -= v).Select(v => v.EventArgs);

                watcher.EnableRaisingEvents = true;

                var sub = Observable
                    .Merge(created, changed, deleted, renamed)
                    .Select(v => new WatchEventArgs(v))
                    .Subscribe(observer);
                
                var disposables = new CompositeDisposable(sub, watcher);

                return () =>
                {
                    watcher.EnableRaisingEvents = false;

                    disposables.Dispose();
                    disposables = null;
                };

            });

            pipeline.Source = source;
            pipeline.Parts.Add($"Source: {folder} - {filter}");

            return pipeline;
            
        }

        
        public static IDisposable Subscribe(this Pipeline pipeline, WorkerQueue worker)
        {
            Console.WriteLine(pipeline.Name);
            Console.WriteLine(string.Join(Environment.NewLine, pipeline.Parts.Select(v => " * " + v)));
            Console.WriteLine();

            return pipeline.Source
                .SelectMany(v => v.Operations)
                .Subscribe(worker.Add);

        }

        internal static void AddIfNotExists(this List<FileOperation> list, FileOperation operation)
        {
            if (!list.Contains(operation))
            {
                list.Add(operation);
            }

        }

        public static Pipeline To(this Pipeline pipeline, params string [] targets)
        {
            foreach (var target in targets)
            {
                pipeline.Parts.Add($"Target: {target}");
            }

            pipeline.Source = pipeline.Source.Do(v =>
            {
                foreach (var target in targets)
                {
                    RenamedEventArgs renamedEventArgs = v.Event as RenamedEventArgs;
                    if (renamedEventArgs != null)
                    {
                        v.Operations.AddIfNotExists(new FileOperation(FileOperationType.Copy, renamedEventArgs.FullPath,
                            Path.Combine(target, v.Event.Name)));
                    }
                    else
                    {
                        v.Operations.AddIfNotExists(new FileOperation(FileOperationType.Copy, v.Event.FullPath,
                            Path.Combine(target, v.Event.Name)));
                    }
                }

            });

            return pipeline;
        }

        public static Pipeline Pipeline(string name)
        {
            return new Pipeline()
            {
                Name = name
            };
        }
    }

    public enum FileOperationType
    {
        Copy,
        Delete,
        Rename,
    }

    public class FileOperation {

        #region Equality 
        protected bool Equals(FileOperation other)
        {
            return Type == other.Type && string.Equals(SourceFile, other.SourceFile) && string.Equals(DestinationFile, other.DestinationFile);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((FileOperation) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (int) Type;
                hashCode = (hashCode * 397) ^ (SourceFile != null ? SourceFile.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (DestinationFile != null ? DestinationFile.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion


        public FileOperation(FileOperationType type, string source, string destination)
        {
            Type = type;
            SourceFile = source;
            DestinationFile = destination;
        }

        public override string ToString()
        {
            return $"{Type} [Source: {SourceFile}, Destination: {DestinationFile}]";
        }

        public FileOperationType Type { get; internal set; }

        public string SourceFile { get; internal set; }

        public string DestinationFile { get; internal set; }

        public void Execute()
        {
            Console.WriteLine($" --> {this}");

            switch (Type)
            {
                case FileOperationType.Copy:
                    try
                    {

                        if (Directory.Exists(SourceFile))
                        {
                            return;
                        }

                        var dir = Path.GetDirectoryName(DestinationFile);
                        if (dir != null && !Directory.Exists(dir))
                        {
                            Directory.CreateDirectory(dir);
                        }

                        File.Copy(SourceFile, DestinationFile, true);
                    }
                    catch (Exception ex) {
                        Console.WriteLine($"Failed:{ex.Message}");
                    }
                    break;
                case FileOperationType.Delete:
                    // not supported yet.
                    break;
                case FileOperationType.Rename:
                    // not supported yet.
                    break;
            }

        }
    }

    public class WorkerQueue
    {

        private ConcurrentQueue<FileOperation> queue = new ConcurrentQueue<FileOperation>();
        private Thread thread;
        private bool cancel = false;

        private void OnProcessQueue()
        {
            FileOperation operation;
            while (true)
            {

                if (cancel)
                {
                    return;
                }

                if (queue.TryDequeue(out operation))
                {
                    operation.Execute();
                }
                else
                {
                    Thread.Sleep(1000);
                }

                
            }
            
        }

        public void Start()
        {

            Console.WriteLine("Starting worker thread.");
            cancel = false;
            thread = new Thread(OnProcessQueue);
            thread.Start();
        }

        public void Stop()
        {
            if (thread != null)
            {
                Console.WriteLine("Stopping worker thread.");

                cancel = true;
                thread.Join(new TimeSpan(0, 0, 0, 10));
                thread = null;
            }
            
        }

        public void Add(FileOperation operation)
        {
            if (!queue.Contains(operation))
            {
                queue.Enqueue(operation);
            }
        }

        

    }



}
