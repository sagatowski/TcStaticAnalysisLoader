using Newtonsoft.Json;

namespace AllTwinCAT.TcStaticAnalysisLoader.GitlabCI.CodeQuality
{
    internal class Lines
    {
        [JsonProperty("begin", Required = Required.Always)]
        public int Begin { get; set; }
    }

    internal class Location
    {
        /// <summary>
        /// File path relative to /code.
        /// </summary>
        [JsonProperty("path", Required = Required.Always)]
        public string Path { get; set; } = "";

        [JsonProperty("lines", Required = Required.Always)]
        public Lines Lines { get; set; } = new();

        public override string ToString()
        {
            return $"{Path}{Lines.Begin}";
        }
    }

}
