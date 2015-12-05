using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DeployDebugButton : ButtonBase
    {
        protected readonly bool m_debugButton;
        protected static bool s_deploying = false;
        protected static readonly List<OleMenuCommand> s_deployCommands = new List<OleMenuCommand>();

        public DeployDebugButton(Frc_ExtensionPackage package, int pkgCmdIdOfButton, bool debug) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, pkgCmdIdOfButton)
        {
            m_debugButton = debug;
            s_deployCommands.Add(m_oleMenuItem);
        }

        private void DisableAllButtons()
        {
            foreach (var oleMenuCommand in s_deployCommands)
            {
                oleMenuCommand.Visible = false;
            }
            s_deploying = true;
        }

        private void EnableAllButtons()
        {
            foreach (var oleMenuCommand in s_deployCommands)
            {
                oleMenuCommand.Visible = true;
            }
            s_deploying = false;
        }

        public override async void ButtonCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            if (!s_deploying)
            {
                try
                {
                    m_output.ProgressBarLabel = "Deploying Robot Code";

                    SettingsPageGrid page;
                    string teamNumber = m_package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    //Disable the deploy buttons
                    DisableAllButtons();
                    DeployManager m = new DeployManager(m_package.PublicGetService(typeof(DTE)) as DTE);
                    await m.DeployCode(teamNumber, page, m_debugButton);
                    EnableAllButtons();
                    m_output.ProgressBarLabel = "Robot Code Deploy Successful";
                }
                catch (Exception ex)
                {
                    m_output.WriteLine(ex.ToString());
                    EnableAllButtons();
                    m_output.ProgressBarLabel = "Robot Code Deploy Failed";
                }

            }
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                var dte = m_package.PublicGetService(typeof(DTE)) as DTE;
                var sb = (SolutionBuild2)dte.Solution.SolutionBuild;

                bool visable = false;
                if (sb.StartupProjects != null)
                {
                    string project = ((Array)sb.StartupProjects).Cast<string>().First();
                    Project startupProject = dte.Solution.Item(project);
                    var vsproject = startupProject.Object as VSLangProj.VSProject;
                    if (vsproject != null)
                    {
                        //If we are an assembly, and its named WPILib, enable the deploy
                        if ((from Reference reference in vsproject.References where reference.SourceProject == null select reference.Name).Any(name => name.Contains("WPILib")))
                        {
                            visable = true;
                        }
                    }
                }
                if (s_deploying)
                    visable = false;

                menuCommand.Visible = visable;
                if (menuCommand.Visible)
                {
                    bool enabled = ((Array)sb.StartupProjects).Cast<string>().Count() == 1;

                    menuCommand.Enabled = enabled;
                }
            }
        }
    }
}
