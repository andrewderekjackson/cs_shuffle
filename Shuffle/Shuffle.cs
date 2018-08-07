using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;

namespace Shuffle {
    public static class Shuffle {

        public static Pipeline From(this Pipeline pipeline, Module from) {
            return From(pipeline, from, "*.dll", "*.pdb");
        }

        public static Pipeline From(this Pipeline pipeline, Module from, params string[] filters) {

            IObservable<WatchEventArgs> source = null;

            foreach (var filter in filters) {

                var newSource = Observable.Create<WatchEventArgs>((observer) => {

                    FileSystemWatcher watcher = new FileSystemWatcher(from.BinPath, filter);

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

                    return () => {
                        watcher.EnableRaisingEvents = false;

                        disposables.Dispose();
                        disposables = null;
                    };

                });

                if (source == null) {
                    source = newSource;
                } else {
                    source = source.Merge(newSource);
                }

            }

            pipeline.SourceModule = from;
            pipeline.Source = source;

            return pipeline;

        }


        public static IDisposable Subscribe(this Pipeline pipeline, WorkerQueue worker) {
            var targets = string.Join(", ", pipeline.TargetModules.Select(v => v.Name));
            Console.WriteLine($" * {pipeline.Name} => {targets}");

            return pipeline
                .Source
                .SelectMany(v => v.Operations)
                .Subscribe(worker.Add);
        }

        public static Pipeline To(this Pipeline pipeline, params PipelineObject[] targets) {

            foreach (PipelineObject targetModule in targets) {
                pipeline.TargetModules.Add(targetModule);
            }

            pipeline.Source = pipeline.Source.Do(v => {
                
                foreach (var targetObj in targets) {

                    var relativePaths = new List<string>();
                    foreach (var path in pipeline.SourceModule.GetTargetPaths(targetObj)) {
                        relativePaths.Add(path);
                    }

                    foreach (var relativePath in relativePaths) {

                        var targetPath = Path.Combine(targetObj.Path, relativePath);
                        var targetFile = Path.Combine(targetPath, v.Event.Name);

                        RenamedEventArgs renamedEventArgs = v.Event as RenamedEventArgs;
                        if (renamedEventArgs != null) {
                            v.Operations.Add(new FileOperation(FileOperationType.Copy, renamedEventArgs.FullPath, targetFile));
                        } else {
                            v.Operations.Add(new FileOperation(FileOperationType.Copy, v.Event.FullPath, targetFile));
                        }
                    }
                }

            });

            return pipeline;

        }

        //public static Pipeline To(this Pipeline pipeline, params string[] targets) {

        //    foreach (var target in targets) {
        //        pipeline.TargetPaths.Add(target);
        //    }

        //    pipeline.Source = pipeline.Source.Do(v => {
                
        //        foreach (var target in targets) {
        //            RenamedEventArgs renamedEventArgs = v.Event as RenamedEventArgs;
        //            if (renamedEventArgs != null) {
        //                v.Operations.Add(new FileOperation(FileOperationType.Copy, renamedEventArgs.FullPath, Target.Combine(target, v.Event.Name)));
        //            } else {
        //                v.Operations.Add(new FileOperation(FileOperationType.Copy, v.Event.FullPath, Target.Combine(target, v.Event.Name)));
        //            }
        //        }

        //    });

        //    return pipeline;
        //}

        public static Pipeline Pipeline(string name) {
            return new Pipeline() {
                Name = name
            };
        }
    }
}