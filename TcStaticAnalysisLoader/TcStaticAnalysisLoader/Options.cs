using CommandLine;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    internal class Options
    {
        [Option('v', "VisualStudioSolutionFilePath", Required = true, HelpText = "Set the path to the solution (.sln file)")]
        public string VisualStudioSolutionFilePath { get; set; } = string.Empty;

        [Option('t', "TwincatProjectFilePath", Required = true, HelpText = "Set the path to the twincat project file (.tsproj file)")]
        public string TwincatProjectFilePath { get; set; } = string.Empty;

    }
}
