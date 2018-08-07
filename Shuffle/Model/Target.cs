using System;
using System.Collections.Generic;

namespace Shuffle {
    /// <summary>
    /// An object which only defines a target.
    /// </summary>
    public class Target : PipelineObject {

        public Target(string name, string path) {
            Name = name;
            Path = path;
        }


        public override IEnumerable<string> GetTargetPaths(PipelineObject target) {
            yield return "";
        }

    }
}