using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace MasteryLibrary.src.Utilities
{
    public class StatConfiguration
    {
        public string ScalingMethod { get; set; } = "Linear";
        public Dictionary<string, object> ScalingParams { get; set; } = new Dictionary<string, object>();

        public StatConfiguration() { }

        [JsonConstructor]
        public StatConfiguration(string scalingMethod, Dictionary<string, object> scalingParams)
        {
            ScalingMethod = scalingMethod ?? throw new ArgumentNullException(nameof(scalingMethod));
            ScalingParams = scalingParams ?? new Dictionary<string, object>();
        }
    }
}
