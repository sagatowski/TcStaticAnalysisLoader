# TcStaticCodeAnalysisLoader
TwinCAT static code analysis script and program.

Details about this project are available in a series of posts I've made about
continous integration/continous delivery (CI/CD) with TwinCAT available at:
- [Part one](http://alltwincat.com/2018/07/05/ci-cd-with-twincat-part-one/)
- [Part two](http://alltwincat.com/2018/07/26/ci-cd-with-twincat-part-two/)
- [Part three](http://alltwincat.com/2018/08/28/ci-cd-with-twincat-part-three/)
- Part four (coming soon)

This repository includes the following content:

## [CodeAnalysisDemoLibrary](https://github.com/sagatowski/TcStaticAnalysisLoader/CodeAnalysisDemoTcLibrary)
A TwinCAT PLC project that is to be analysed for static code analysis. This also
includes the windows batch-script to be executed by a Jenkins job when a GIT
repository change has been detected in the TwinCAT PLC project. This BAT-script
launches the TwinCAT static code analysis starter project described below.

## TcStaticAnalysisLoader
Coming when part four of the series has been published.