using Newtonsoft.Json;
using System.Collections.ObjectModel;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    internal class ErrorReport
    {
        public string VisualStudioSolutionName { get; private set; }
        public string TwinCATProjectName { get; private set; }
        public DateTime TimeStamp { get; private set; }
        public ReadOnlyCollection<VisualStudioError> StaticAnalyzerErrors { get; private set; }
        public ReadOnlyCollection<VisualStudioError> OtherErrors { get; private set; }

        internal ErrorReport(string visualStudioSolutionPath, string twinCATProjectFilePath)
        {
            VisualStudioSolutionName = Path.GetFileNameWithoutExtension(visualStudioSolutionPath);
            TwinCATProjectName = Path.GetFileNameWithoutExtension(twinCATProjectFilePath);
            TimeStamp = DateTime.Now;
            StaticAnalyzerErrors = _staticAnalyzerErrors.AsReadOnly();
            OtherErrors = _otherErrors.AsReadOnly();
        }

        public void AddError(VisualStudioError error)
        {
            if (error.Code.StartsWith("SA"))
            {
                _staticAnalyzerErrors.Add(error);
                return;
            }
            _otherErrors.Add(error);
        }

        public string ToJson()
        {
            return JsonConvert.SerializeObject(this, Formatting.Indented);
        }

        private readonly List<VisualStudioError> _staticAnalyzerErrors = new();
        private readonly List<VisualStudioError> _otherErrors = new();
    }
}
