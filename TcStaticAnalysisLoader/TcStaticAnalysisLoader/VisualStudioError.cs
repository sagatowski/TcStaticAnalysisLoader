using EnvDTE80;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    internal enum VisualStudioErrorLevel
    {
        Low = 1,
        Medium = 2,
        High = 4
    }


    internal class FileLocation
    {
        internal FileLocation(string project, string fileName, int line, int column, string solutionPath = "")
        {
            Project = project;
            Line = line;
            Column = column;
            var solutionDirectory = (string.IsNullOrWhiteSpace(solutionPath) ? string.Empty : Path.GetDirectoryName(solutionPath));
            if (!string.IsNullOrWhiteSpace(solutionDirectory) && fileName.Contains(solutionDirectory))
            {
                FileName = fileName.Remove(0, solutionDirectory.Length + 1);
            }
            else
            {
                FileName = fileName;
            }
        }

        public string Project { get; private set; }
        public string FileName { get; private set; }
        public int Line { get; private set; }
        public int Column { get; private set; }

    }

    internal class VisualStudioError
    {
        public VisualStudioError(ErrorItem comError, string solutionPath = "")
        {
            ErrorLevel = (VisualStudioErrorLevel)comError.ErrorLevel;
            Location = new(comError.Project, comError.FileName, comError.Line, comError.Column, solutionPath);
            var descriptionSplited = comError.Description.Split(':');
            if (descriptionSplited.Length > 1)
            {
                Code = descriptionSplited[0];
                Description = comError.Description.Remove(0, Code.Length + 1).Trim();
            }
            else
            {
                Code = string.Empty;
                Description = comError.Description;
            }
        }

        public VisualStudioErrorLevel ErrorLevel { get; private set; }

        public FileLocation Location { get; private set; }

        public string Code { get; private set; }

        public string Description { get; private set; }
    }
}
