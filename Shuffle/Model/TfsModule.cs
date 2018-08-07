using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Shuffle {
    public class TfsModule : Module {
       
        [YamlMember(Order = 3, Alias = "type", ApplyNamingConventions = false)]
        public override string YamlType => "tfs_module";

        /// <summary>
        /// Gets the relative paths where the files for the source module will be placed in the target module.
        /// </summary>
        public override IEnumerable<string> GetTargetPaths(PipelineObject target) {

            if (target is Target) {
                // if we're copying to a target path, just copy into the root.
                yield return "";
                yield break;
            }

            yield return @"Dependencies";
        }



    }
}