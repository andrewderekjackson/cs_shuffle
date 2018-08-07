using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Shuffle {
    /// <summary>
    /// An object which only defines a target.
    /// </summary>
    public class Target : PipelineObject {

        public Target(string name, string path) {
            Name = name;
            Path = path;
        }

        [YamlMember(Order = 3, Alias = "type", ApplyNamingConventions = false)]
        public override string YamlType => "target";


        public override IEnumerable<string> GetTargetPaths(PipelineObject target) {
            yield return "";
        }

    }
}