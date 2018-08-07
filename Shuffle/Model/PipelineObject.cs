using System;
using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Shuffle {
    public abstract class PipelineObject {

        /// <summary>
        /// The name of the module.
        /// </summary>
        [YamlMember(Order = 1, Alias = "name", ApplyNamingConventions = false)]
        public string Name { get; set; }

        /// <summary>
        /// The path to the root.
        /// </summary>
        [YamlMember(Order = 2, Alias = "path", ApplyNamingConventions = false)]
        public string Path { get; set; }

        /// <summary>
        /// Gets the relative paths where the files for the source module will be placed in the target module.
        /// </summary>
        public abstract IEnumerable<string> GetTargetPaths(PipelineObject target);
        
        public abstract string YamlType { get; }

    }
}