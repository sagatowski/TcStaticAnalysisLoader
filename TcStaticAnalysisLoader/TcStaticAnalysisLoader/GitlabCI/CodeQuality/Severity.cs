namespace AllTwinCAT.TcStaticAnalysisLoader.GitlabCI.CodeQuality
{
    internal enum Severity
    {
        [System.Runtime.Serialization.EnumMember(Value = @"info")]
        Info,

        [System.Runtime.Serialization.EnumMember(Value = @"minor")]
        Minor,

        [System.Runtime.Serialization.EnumMember(Value = @"major")]
        Major,

        [System.Runtime.Serialization.EnumMember(Value = @"critical")]
        Critical,

        [System.Runtime.Serialization.EnumMember(Value = @"blocker")]
        Blocker
    }
}
