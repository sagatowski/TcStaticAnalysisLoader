using CommandLine;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    internal class Options
    {
        [Option('v', "VisualStudioSolutionFilePath", Required = true, HelpText = "Set the path to the solution (.sln file)")]
        public string VisualStudioSolutionFilePath { get; set; } = string.Empty;

        [Option('t', "TwincatProjectFilePath", Required = true, HelpText = "Set the path to the twincat project file (.tsproj file)")]
        public string TwincatProjectFilePath { get; set; } = string.Empty;

        [Option('r', "ReportPath", Required = false, HelpText = "Set the path where the error report will be generated")]
        public string ReportPath { get; set; } = string.Empty;

        [Option('f', "ReportFormat", Required = false, HelpText = "Set the output format for the report file. Valid options: Default, Gitlab")]
        public string ReportFormat { get; set; } = "Default";
    }
}
