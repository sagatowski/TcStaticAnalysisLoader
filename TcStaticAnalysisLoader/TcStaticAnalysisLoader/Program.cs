﻿/* 
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
using TCatSysManagerLib;

namespace AllTwinCAT.TcStaticAnalysisLoader
{
    class Program
    {
        [STAThread]
        static int Main(string[] args)
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
                return Constants.RETURN_ERROR;
            }

            logger?.LogDebug("TcStaticAnalysisLoader.exe : argument 1: {vsPath}", options.VisualStudioSolutionFilePath);
            logger?.LogDebug("TcStaticAnalysisLoader.exe : argument 2: {tsPath}", options.TwincatProjectFilePath);

            /* Verify that the Visual Studio solution file and TwinCAT project file exists.*/
            if (!File.Exists(options.VisualStudioSolutionFilePath))
            {
                logger?.LogError("ERROR: Visual studio solution {vsPath} does not exist!", options.VisualStudioSolutionFilePath);
                return Constants.RETURN_ERROR;
            }
            if (!File.Exists(options.TwincatProjectFilePath))
            {
                logger?.LogError("ERROR : TwinCAT project file {tsPath} does not exist!", options.TwincatProjectFilePath);
                return Constants.RETURN_ERROR;
            }


            /* Find visual studio version */
            string vsVersion = "";
            bool foundVsVersionLine = false;
            foreach (string line in File.ReadLines(options.VisualStudioSolutionFilePath))
            {
                if (line.StartsWith("VisualStudioVersion"))
                {
                    string version = line.Substring(line.LastIndexOf('=') + 2);
                    logger?.LogInformation("In Visual Studio solution file, found visual studio version {version}", version);
                    string[] numbers = version.Split('.');
                    string major = numbers[0];
                    string minor = numbers[1];

                    bool isNumericMajor = int.TryParse(major, out int n);
                    bool isNumericMinor = int.TryParse(minor, out int n2);

                    if (isNumericMajor && isNumericMinor)
                    {
                        vsVersion = major + "." + minor;
                        foundVsVersionLine = true;
                    }
                    break;
                }
            }

            if (!foundVsVersionLine)
            {
                logger?.LogError("Did not find Visual studio version in Visual studio solution file");
                return Constants.RETURN_ERROR;
            }

            /* Find TwinCAT project version */
            string tcVersion = "";
            bool foundTcVersionLine = false;
            foreach(string line in File.ReadLines(@options.TwincatProjectFilePath))
            {
                if (line.Contains("TcVersion"))
                {
                    string version = line.Substring(line.LastIndexOf("TcVersion=\""));
                    int pFrom = version.IndexOf("TcVersion=\"") + "TcVersion=\"".Length;
                    int pTo = version.LastIndexOf("\">");
                    if (pTo > pFrom)
                    {
                        tcVersion = version.Substring(pFrom, pTo - pFrom);
                        foundTcVersionLine = true;
                        logger?.LogInformation("In TwinCAT project file, found version {version}", tcVersion);
                    }
                    break;
                }
            }
            if (!foundTcVersionLine)
            {
                logger?.LogError("Did not find TcVersion in TwinCAT project file");
                return Constants.RETURN_ERROR;
            }


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
                return Constants.RETURN_ERROR;
            }

            MessageFilter.Register();

            /* Make sure the DTE loads with the same version of Visual Studio as the
             * TwinCAT project was created in
             */
            string VisualStudioProgId = "VisualStudio.DTE." + vsVersion;
            var type = Type.GetTypeFromProgID(VisualStudioProgId);
            if (type == null)
            {
                logger?.LogError("Unable to obtain the type of visual studio program id\n" +
                    "The needed version was {version}", VisualStudioProgId);
                return Constants.RETURN_ERROR;
            }
            var instanceObject = Activator.CreateInstance(type);
            if (instanceObject == null)
            {
                logger?.LogError("Unable to create a visual studio instance\n" +
                    "The needed version was {version}", VisualStudioProgId);
                return Constants.RETURN_ERROR;
            }
            DTE2 dte = (DTE2)instanceObject;

            dte.SuppressUI = true;
            dte.MainWindow.Visible = false;
            EnvDTE.Solution visualStudioSolution = dte.Solution;
            visualStudioSolution.Open(@options.VisualStudioSolutionFilePath);
            EnvDTE.Project pro = visualStudioSolution.Projects.Item(1);

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
            visualStudioSolution.SolutionBuild.Clean(true);
            visualStudioSolution.SolutionBuild.Build(true);

            ErrorItems errors = dte.ToolWindows.ErrorList.ErrorItems;

            logger?.LogInformation("Errors count: {errors}", errors.Count);
            int tcStaticAnalysisWarnings = 0;
            int tcStaticAnalysisErrors = 0;
            for (int i = 1; i <= errors.Count; i++)
            {
                ErrorItem item = errors.Item(i);
                if (item.Description.StartsWith("SA") && (item.ErrorLevel != vsBuildErrorLevel.vsBuildErrorLevelLow))
                {
                    logger?.LogInformation("Description: {description}\n" +
                        "ErrorLevel: {errorLevel}\n" +
                        "Filename: {fileName}",
                        item.Description, item.ErrorLevel, item.FileName);
                    if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelMedium)
                        tcStaticAnalysisWarnings++;
                    else if (item.ErrorLevel == vsBuildErrorLevel.vsBuildErrorLevelHigh)
                        tcStaticAnalysisErrors++;
                }
            }

            dte.Quit();

            MessageFilter.Revoke();

            /* Return the result to the user */
            if (tcStaticAnalysisErrors > 0)
                return Constants.RETURN_ERROR;
            else if (tcStaticAnalysisWarnings > 0)
                return Constants.RETURN_UNSTABLE;
            else
                return Constants.RETURN_SUCCESSFULL;
        }
    }
}
