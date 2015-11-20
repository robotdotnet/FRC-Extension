using System;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DeployDebugButton : ButtonBase
    {
        protected bool m_debugButton;
        protected bool m_deploying = false;

        public DeployDebugButton(Frc_ExtensionPackage package, int pkgCmdIdOfButton, bool debug) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, pkgCmdIdOfButton)
        {
            m_debugButton = debug;
        }

        public override async void ButtonCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            if (!m_deploying)
            {
                try
                {
                    m_output.ProgressBarLabel = "m_deploying Robot Code";

                    SettingsPageGrid page;
                    string teamNumber = m_package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    //Disable the deploy button
                    m_deploying = true;
                    menuCommand.Visible = false;
                    DeployManager m = new DeployManager(m_package.PublicGetService(typeof(DTE)) as DTE);
                    await m.DeployCode(teamNumber, page, m_debugButton);
                    m_deploying = false;
                    menuCommand.Visible = true;
                    m_output.ProgressBarLabel = "Robot Code Deploy Successful";
                }
                catch (Exception ex)
                {
                    m_output.WriteLine(ex.ToString());
                    m_deploying = false;
                    menuCommand.Visible = true;
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
                if (m_deploying)
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
