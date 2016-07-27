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
    //[DeploymentItem("ReferenceAssemblies/HAL.dll")]
    //[DeploymentItem("ReferenceAssemblies/WPILib.dll")]
    //[DeploymentItem("ReferenceAssemblies/WPILib.Extras.dll")]
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
using WPILib.Commands;
using WPILib.Extras.AttributedCommandModel;
[ExportSubsystem(DefaultCommandType = typeof(Drive))] public class DriveTrain : Subsystem {}
class Drive
{
}
";
            var expected = new DiagnosticResult
            {
                Id = SubsystemDefaultCommandConstructorAnalyzer.DiagnosticId,
                Message = "The default command type needs to have a constructor that takes an instance of the subsystem.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 4, 2)
                        }
            };

            VerifyCSharpDiagnostic(test, expected);

            var fixtest = @"
using WPILib.Commands;
using WPILib.Extras.AttributedCommandModel;
[ExportSubsystem(DefaultCommandType = typeof(Drive))] public class DriveTrain : Subsystem {}
class Drive
{
    public Drive(DriveTrain subsystem)
    {
    }
}
";
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