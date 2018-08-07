using System.Collections.Generic;
using YamlDotNet.Serialization;

namespace Shuffle {
    public class Plan {

        [YamlMember(Alias = "modules", ApplyNamingConventions = false)]
        public List<PipelineObject> Modules { get; set; } = new List<PipelineObject>();

        [YamlIgnore]
        public List<Pipeline> Pipelines { get; set; } = new List<Pipeline>();

        [YamlMember(Alias = "pipelines")]
        public List<string> YamlPipelines { get; set; } = new List<string>();

    }
}