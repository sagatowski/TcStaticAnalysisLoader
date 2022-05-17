using Newtonsoft.Json;
using System.Text;

namespace AllTwinCAT.TcStaticAnalysisLoader.GitlabCI.CodeQuality
{
    internal class Issue : IEquatable<Issue>
    {
        /// <summary>
        /// A string explaining the issue that was detected.
        /// Descriptions must be a single line of text (no newlines), with no HTML formatting contained within. Ideally, descriptions should be fewer than 70 characters long, but this is not a requirement.
        /// Descriptions support one type of basic Markdown formatting, which is the use of backticks to produce inline<code> tags that are rendered in a fixed width font. Identifiers like class, method and variable names should be wrapped within backticks whenever possible for optimal rendering by tools that consume Engines data.
        /// </summary>
        [JsonProperty("description", Required = Required.Always)]
        public string Description { get; set; } = "";

        /// <summary>
        /// A Location object representing the place in the source code where the issue was discovered.
        /// </summary>
        [JsonProperty("location", Required = Required.Always)]
        public Location Location { get; set; } = new();

        /// <summary>
        /// A Severity string (info, minor, major, critical, or blocker) describing the potential impact of the issue found.
        /// </summary>
        [JsonProperty("severity", Required = Required.Always)]
        [Newtonsoft.Json.JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
        public Severity Severity { get; set; } = new();

        /// <summary>
        /// A unique, deterministic identifier for the specific issue being reported to allow a user to exclude it from future analyses.
        /// </summary>
        [JsonProperty("fingerprint", Required = Required.Always)]
        public string Fingerprint { get => ComputeMD5Hash(); }

        public bool Equals(Issue? other)
        {
            if (other == null)
            {
                return false;
            }
            return Fingerprint.Equals(other.Fingerprint);
        }

        public override bool Equals(object? obj)
        {
            return obj is Issue objIssue && Equals(objIssue);
        }

        public override int GetHashCode()
        {
            return Fingerprint.GetHashCode();
        }

        private string ComputeMD5Hash()
        {
            var hasString = $"{Description}{Location}{Severity}";
            using System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create();
            byte[] inputBytes = Encoding.ASCII.GetBytes(hasString);
            byte[] hashBytes = md5.ComputeHash(inputBytes);

            return Convert.ToHexString(hashBytes);
        }

    }
}
