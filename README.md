# TcStaticCodeAnalysisLoader
TwinCAT static code analysis script and program.

Details about this project are available in a series of posts I've made about
continous integration/continous delivery (CI/CD) with TwinCAT available at:
- [Part one](http://alltwincat.com/2018/07/05/ci-cd-with-twincat-part-one/)
- [Part two](http://alltwincat.com/2018/07/26/ci-cd-with-twincat-part-two/)
- [Part three](http://alltwincat.com/2018/08/28/ci-cd-with-twincat-part-three/)
- [Part four](http://alltwincat.com/2018/10/04/ci-cd-with-twincat-part-four/)

It's strongly recommended to read these four documents to get an introduction
and vital background information to this software.

This repository includes the following content:

## [CodeAnalysisDemoTcLibrary](https://github.com/sagatowski/TcStaticAnalysisLoader/tree/master/CodeAnalysisDemoTcLibrary)
A TwinCAT PLC project that is to be analysed for static code analysis. This also
includes the windows batch-script to be executed by a Jenkins job when a GIT
repository change has been detected in the TwinCAT PLC project. This BAT-script
launches the TwinCAT static code analysis program described below (TcStaticAnalysisLoader).

## [TcStaticAnalysisLoader](https://github.com/sagatowski/TcStaticAnalysisLoader/tree/master/TcStaticAnalysisLoader)
A C# .NET program that does the static code analysis using Beckhoffs TE1200 product
and reports the result back to the caller as either ERROR, UNSTABLE or SUCCESSFUL
which can be used for instance in a Jenkins build slave. The static code analysis is filtered
out from the TwinCAT build procedure. This program is used together with the batch-script
from the CodeAnalysisDemoTcLibrary folder.

## Dependencies

- The [.NET 6.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0) must be installed on the system