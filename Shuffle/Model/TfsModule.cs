using System.Collections.Generic;

namespace Shuffle {
    public class TfsModule : Module {
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