using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;

namespace RobotDotNet.FRC_Extension
{
    class DeployManager
    {
        private readonly DTE m_dte;

        public DeployManager(DTE dte)
        {
            m_dte = dte;
        }

        public void DeployCode(SettingsPageGrid page)
        {
            string teamNumber = page.TeamNumber.ToString();
            bool forceUSB = page.ForceUSB;

            //Build Code
            var sb = (SolutionBuild2) m_dte.Solution.SolutionBuild;
            sb.Build(true);



            string path = GetStartupAssemblyPath();
            string robotExe = Path.GetFileName(path);
            string buildDir = Path.GetDirectoryName(path);

            Project startProject = GetStartupProject();


            //Connect to Robot Syncronously


        }

        internal string GetStartupAssemblyPath()
        {
            Project startupProject = GetStartupProject();
            return GetAssemblyPath(startupProject);
        }

        private Project GetStartupProject()
        {
            var sb = (SolutionBuild2)m_dte.Solution.SolutionBuild;
            string project = ((Array)sb.StartupProjects).Cast<string>().First();
            Project startupProject = m_dte.Solution.Item(project);
            return startupProject;
        }

        internal string GetAssemblyPath(Project vsProject)
        {
            string fullPath = vsProject.Properties.Item("FullPath").Value.ToString();
            string outputPath =
                vsProject.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            string outputDir = Path.Combine(fullPath, outputPath);
            string outputFileName = vsProject.Properties.Item("OutputFileName").Value.ToString();
            string assemblyPath = Path.Combine(outputDir, outputFileName);
            return assemblyPath;
        }
    }
}
