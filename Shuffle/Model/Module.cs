using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using YamlDotNet.Serialization;

namespace Shuffle {

    /// <summary>
    /// An object which defines both a source and target.
    /// </summary>
    public class Module : PipelineObject {

        public string BinPath => System.IO.Path.Combine(Path, @"Bin\Module");

        public IList<string> Requires { get; set; } = new List<string>();


        public IList<string> Provides { get; set; } = new List<string>();

        public override string YamlType {
            get {
                return "module";
            }
        }

        /// <summary>
        /// Gets the relative paths where the files for the source module will be placed in the target module.
        /// </summary>
        public override IEnumerable<string> GetTargetPaths(PipelineObject target) {

            if (target is Target) {
                // if we're copying to a target path, just copy into the root.
                yield return "";
                yield break;
            }

            // copy into each of the nuget folders provided by the module.
            foreach (var path in Provides) {
                yield return $@"packages\{path}\lib";
            }

            if (target is TfsModule) {
                yield return @"Dependencies";
            }
            
        }
    
        public override string ToString() {
            return $"{Name}";
        }
                
    }
}