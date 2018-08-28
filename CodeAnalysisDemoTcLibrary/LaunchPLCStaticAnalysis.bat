@echo off

rem The MIT License(MIT)

rem Copyright(c) 2018 Jakob Sagatowski

rem Permission is hereby granted, free of charge, to any person obtaining a copy of
rem this software and associated documentation files (the "Software"), to deal in
rem the Software without restriction, including without limitation the rights to
rem use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
rem the Software, and to permit persons to whom the Software is furnished to do so,
rem subject to the following conditions:

rem The above copyright notice and this permission notice shall be included in all
rem copies or substantial portions of the Software.

rem THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
rem IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
rem FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
rem COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
rem IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
rem CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.

rem -----------------------------------------------------------------------------

rem This is a windows batch file used to execute the TwinCAT static code analysis
rem loader program. This batch file can be used in conjunction with some automation
rem server software that launches this file once the static code analysis needs
rem to be done.

rem USAGE:
rem Set the below variable TC_STATIC_ANALYSIS_LOADER_PATH to the complete path of
rem where the executable TcStaticAnalysisLoader.exe resides.

SET TC_STATIC_ANALYSIS_LOADER_PATH=C:\Program Files (x86)\TcSAL\TcStaticAnalysisLoader.exe

rem Find the visual studio solution file.

FOR /r %%i IN (*.sln) DO (
    SET VISUAL_STUDIO_SOLUTION_PATH="%%i"
)

rem Find the TwinCAT solution file.

FOR /r %%i IN (*.tsproj) DO (
    SET TWINCAT_PROJECT_PATH="%%i"
)

rem Error handling of finding the files.

IF NOT DEFINED VISUAL_STUDIO_SOLUTION_PATH (
    echo Visual studio solution file path does not exist!
    GOTO Exit
) ELSE (
    echo VISUAL_STUDIO_SOLUTION_PATH found!
    echo The filepath to the visual studio solution file is: %VISUAL_STUDIO_SOLUTION_PATH%
)
IF NOT DEFINED TWINCAT_PROJECT_PATH (
    echo TwinCAT project file does not exist!
    GOTO Exit
) ELSE (
    echo TWINCAT_PROJECT_PATH found!
    echo The filepath to the TwinCAT project file is: %TWINCAT_PROJECT_PATH%
)

rem Run the TwinCAT automation interface application
IF EXIST "%TC_STATIC_ANALYSIS_LOADER_PATH%" (
    "%TC_STATIC_ANALYSIS_LOADER_PATH%" -v %VisualStudioSolutionFilePath% -t %TwinCATProjectFilePath%
) ELSE (
    echo The configured search path for TcStaticAnalysisLoader does not exist!
    GOTO Exit
)

rem %errorlevel% is a system wide environment variable that is set upon execution of a program
echo Exit code is %errorlevel%

EXIT /B %errorlevel%

:Exit
echo Failed!
EXIT /B 1