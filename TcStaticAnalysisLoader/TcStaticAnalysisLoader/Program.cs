/* 
The MIT License(MIT)

Copyright(c) 2018 Jakob Sagatowski

Permission is hereby granted, free of charge, to any person obtaining a copy of
this software and associated documentation files (the "Software"), to deal in
the Software without restriction, including without limitation the rights to
use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
the Software, and to permit persons to whom the Software is furnished to do so,
subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
*/

using CommandLine;
using EnvDTE80;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using TCatSysManagerLib;
using System.Linq;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Create a logger
            var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
            var logger = loggerFactory.CreateLogger<Program>();

            Options options = new();
            var result = Parser.Default.ParseArguments<Options>(args)
                                .WithParsed(o => options = o);
            // If help requested end the execution
            if (result.Tag == ParserResultType.NotParsed)
            {
                Environment.Exit(Constants.RETURN_ERROR);
            }

            logger?.LogDebug("TcStaticAnalysisLoader.exe : argument 1: {vsPath}", options.VisualStudioSolutionFilePath);
            logger?.LogDebug("TcStaticAnalysisLoader.exe : argument 2: {tsPath}", options.TwincatProjectFilePath);

            /* Verify that the Visual Studio solution file and TwinCAT project file exists.*/
            if (!File.Exists(options.VisualStudioSolutionFilePath))
            {
                logger?.LogError("ERROR: Visual studio solution {vsPath} does not exist!", options.VisualStudioSolutionFilePath);
                Environment.Exit(Constants.RETURN_ERROR);
            }
            if (!File.Exists(options.TwincatProjectFilePath))
            {
                logger?.LogError("ERROR : TwinCAT project file {tsPath} does not exist!", options.TwincatProjectFilePath);
                Environment.Exit(Constants.RETURN_ERROR);
            }

            /* Find TwinCAT project version */
            var tcVersion = GetTwinCATVersion(options.TwincatProjectFilePath, logger);

            /* Make sure TwinCAT version is at minimum version 3.1.4022.0 as the static code
             * analysis tool is only supported from this version and onward
             */
            var versionMin = new Version(Constants.MIN_TC_VERSION_FOR_SC_ANALYSIS);
            var versionDetected = new Version(tcVersion);
            var compareResult = versionDetected.CompareTo(versionMin);
            if (compareResult < 0)
            {
                logger?.LogError("The detected TwinCAT version in the project does not support TE1200 static code analysis\n" +
                    "The minimum version that supports TE1200 is {version}", Constants.MIN_TC_VERSION_FOR_SC_ANALYSIS);
                Environment.Exit(Constants.RETURN_ERROR);
            }

            MessageFilter.Register();

            // Generate DTE for VS solution
            var dte = GetDTEFromVisualStudioSolution(options.VisualStudioSolutionFilePath, logger);

            ITcRemoteManager remoteManager = (ITcRemoteManager)dte.GetObject("TcRemoteManager");
            remoteManager.Version = tcVersion;
            ITcAutomationSettings settings = (ITcAutomationSettings)dte.GetObject("TcAutomationSettings");
            settings.SilentMode = true; // Only available from TC3.1.4020.0 and above

            /* Build the solution and collect any eventual errors. Make sure to
             * filter out everything that is 
             * - Either a warning or an error
             * - Starts with the string "SA", which is everything from the TE1200
             *   static code analysis tool
             */
            dte.Solution.SolutionBuild.Clean(true);
            dte.Solution.SolutionBuild.Build(true);

            // Get active errors
            var report = new ErrorReport(options.VisualStudioSolutionFilePath, options.TwincatProjectFilePath);
            var comErrors = dte.ToolWindows.ErrorList.ErrorItems;
            for (var ii = 1; ii < comErrors.Count + 1; ii++)
            {
                if (comErrors.Item(ii).ErrorLevel != vsBuildErrorLevel.vsBuildErrorLevelLow)
                {
                    report.AddError(new VisualStudioError(comErrors.Item(ii), options.VisualStudioSolutionFilePath));
                }
            }

            // Print all SA errors on screen
            var staticAnalyzerErrors = report.StaticAnalyzerErrors.ToList();
            staticAnalyzerErrors.ForEach(e => logger?.LogInformation("Description: {description}\n" +
                        "ErrorLevel: {errorLevel}\n" +
                        "Filename: {fileName}",
                        e.Description, e.ErrorLevel, e.Location.FileName));

            dte.Quit();

            MessageFilter.Revoke();

            // Write error report if required
            if (!string.IsNullOrEmpty(options.ReportPath))
            {
                File.WriteAllText(options.ReportPath, report.ToJson());
            }

            /* Return the result to the user */
            if (staticAnalyzerErrors.Any(e => e.ErrorLevel == VisualStudioErrorLevel.High))
                Environment.Exit(Constants.RETURN_ERROR);
            else if (staticAnalyzerErrors.Any(e => e.ErrorLevel == VisualStudioErrorLevel.Medium))
                Environment.Exit(Constants.RETURN_UNSTABLE);
            else
                Environment.Exit(Constants.RETURN_SUCCESSFULL);
        }

        private static DTE2 GetDTEFromVisualStudioSolution(string visualStudioSolutionFilePath, ILogger? logger = null)
        {
            // Get Visual Studio version
            var vsVersion = GetVisualStudioVersion(visualStudioSolutionFilePath, logger);

            /* Make sure the DTE loads with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            string VisualStudioProgId = "VisualStudio.DTE." + vsVersion;
            var type = Type.GetTypeFromProgID(VisualStudioProgId);
            if (type == null)
            {
                logger?.LogError("Unable to obtain the type of visual studio program id\n" +
                    "The needed version was {version}", VisualStudioProgId);
                Environment.Exit(Constants.RETURN_ERROR);
            }
            var instanceObject = Activator.CreateInstance(type);
            if (instanceObject == null)
            {
                logger?.LogError("Unable to create a visual studio instance\n" +
                    "The needed version was {version}", VisualStudioProgId);
                Environment.Exit(Constants.RETURN_ERROR);
            }
            DTE2 dte = (DTE2)instanceObject;

            dte.SuppressUI = true;
            dte.MainWindow.Visible = false;
            dte.Solution.Open(visualStudioSolutionFilePath);
            return dte;
        }

        private static string GetVisualStudioVersion(string visualStudioSolutionFilePath, ILogger? logger = null)
        {
            var vsVersionRegex = new Regex(@"^VisualStudioVersion\s*=\s*(\d+.\d+)", RegexOptions.Multiline);
            var match = vsVersionRegex.Match(File.ReadAllText(visualStudioSolutionFilePath));
            if (match.Groups.Count < 2)
            {
                logger?.LogError("Did not find Visual studio version in Visual studio solution file");
                Environment.Exit(Constants.RETURN_ERROR);
            }
            logger?.LogInformation("In Visual Studio solution file, found visual studio version {version}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }

        private static string GetTwinCATVersion(string twincatProjectFilePath, ILogger? logger = null)
        {
            var tcVersionRegex = new Regex("TcVersion\\s*=\\s*\"(\\d.+)?\"", RegexOptions.Multiline);
            var match = tcVersionRegex.Match(File.ReadAllText(twincatProjectFilePath));
            if (match.Groups.Count < 2)
            {
                logger?.LogError("Did not find TcVersion in TwinCAT project file");
                Environment.Exit(Constants.RETURN_ERROR);
            }
            logger?.LogInformation("In TwinCAT project file, found version {version}", match.Groups[1].Value);
            return match.Groups[1].Value;
        }
    }
}
