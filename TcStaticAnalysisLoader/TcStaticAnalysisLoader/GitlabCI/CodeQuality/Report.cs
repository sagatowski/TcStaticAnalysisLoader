namespace AllTwinCAT.TcStaticAnalysisLoader.GitlabCI.CodeQuality
{
    internal class Report
    {
        internal Report(ErrorReport report)
        {
            _report = report;
        }

        public string ToJson()
        {
            var issues = new List<Issue>();
            foreach (var error in _report.StaticAnalyzerErrors)
            {
                issues.Add(GitLabIssueFromVisualStudioError(error));
            }
            foreach (var error in _report.OtherErrors)
            {
                issues.Add(GitLabIssueFromVisualStudioError(error));
            }
            var nonDuplicatedIssues = issues.Distinct().ToList();
            return Newtonsoft.Json.JsonConvert.SerializeObject(nonDuplicatedIssues, Newtonsoft.Json.Formatting.Indented);
        }

        private static Issue GitLabIssueFromVisualStudioError(VisualStudioError error)
        {
            var issue = new Issue()
            {
                Description = $"{error.Code}: {error.Description}",
                Location = new Location()
                {
                    Path = error.Location.FileName,
                    Lines = new Lines()
                    {
                        Begin = error.Location.Line
                    }
                }
            };
            switch (error.ErrorLevel)
            {
                case VisualStudioErrorLevel.Low:
                    issue.Severity = Severity.Minor;
                    break;
                case VisualStudioErrorLevel.High:
                    issue.Severity = Severity.Critical;
                    break;
                case VisualStudioErrorLevel.Medium:
                    issue.Severity = Severity.Major;
                    break;
            }
            return issue;

        }

        private readonly ErrorReport _report;
    }
}
