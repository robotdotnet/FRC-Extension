using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using TestHelper;
using FRC_Analyzers;

namespace FRC_Analyzers.Test
{
    [TestClass]
    public class UnitTest : CodeFixVerifier
    {

        //No diagnostics expected to show up
        [TestMethod]
        public void NoDiagnosticOnEmptyString()
        {
            var test = @"";

            VerifyCSharpDiagnostic(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public void DefaultCommandConstructorTakingSubsystem()
        {
            var test = @"
[ExportSubsystem(DefaultCommandType = typeof(Commands.Drive))] public class DriveTrain : Subsystem {}
class Drive
{
}
";
            var expected = new DiagnosticResult
            {
                Id = "FRC_Analyzers",
                Message = "The default command type needs to have a constructor that takes an instance of the subsystem.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 6, 2)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
[ExportSubsystem(DefaultCommandType = typeof(Commands.Drive))] public class DriveTrain : Subsystem {}
class Drive
{
    public Drive(DriveTrain subsystem)
    {

    }
}";
            VerifyCSharpFix(test, fixtest);
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new SubsystemDefaultCommandConstructorFixer();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new SubsystemDefaultCommandConstructorAnalyzer();
        }
    }
}