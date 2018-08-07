using System;
using System.Collections.Generic;
using System.Linq;
using YamlDotNet.Serialization;

namespace Shuffle {

    public class Pipeline {
        
        public string Name { get; set; }

        public Module SourceModule { get; set; }

        public List<PipelineObject> TargetModules { get; set; } = new List<PipelineObject>();

        internal List<string> TargetPaths { get; set; } = new List<string>();

        public IObservable<WatchEventArgs> Source { get; set; }
        

        public override string ToString() {
            return $"{Name} => " + string.Join(",", TargetModules.Select(v => v.Name).Union(TargetPaths));
        }
        
    }
}