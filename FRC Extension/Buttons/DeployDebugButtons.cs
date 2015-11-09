using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DeployDebugButtons
    {
        private readonly OutputWriter m_output;

        private readonly Frc_ExtensionPackage m_package;

        private OleMenuCommand m_deployMenuItem;
        private OleMenuCommand m_debugMenuItem;
        private bool m_deploying = false;

        public DeployDebugButtons(Frc_ExtensionPackage package)
        {
            m_output = OutputWriter.Instance;
            m_package = package;

            OleMenuCommandService mcs = m_package.PublicGetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                //Creating the deploy button. The BeforeQueryStatus event allows us to enable or disable the
                //button based on if we are a WPILib project or not.
                CommandID deployCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidDeployCode);
                m_deployMenuItem = new OleMenuCommand((sender, e) => DeployCodeCallback(sender, e, false), deployCommandID);
                m_deployMenuItem.BeforeQueryStatus += QueryDeployButton;
                mcs.AddCommand(m_deployMenuItem);

                //Debug version of the deploy button
                CommandID debugCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidDebugCode);
                m_debugMenuItem = new OleMenuCommand((sender, e) => DeployCodeCallback(sender, e, true), debugCommandID);
                m_debugMenuItem.BeforeQueryStatus += QueryDeployButton;
                mcs.AddCommand(m_debugMenuItem);
            }
        }

        //This is called every time the menu is open, to check and see
        private void QueryDeployButton(object sender, EventArgs e)
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

        /// <summary>
        /// The function is called when the deploy button is pressed.
        /// </summary>
        private async void DeployCodeCallback(object sender, EventArgs e, bool debug)
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
                    m_output.ProgressBarLabel = "Deploying Robot Code";

                    SettingsPageGrid page;
                    string teamNumber = m_package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    //Disable the deploy button
                    m_deploying = true;
                    menuCommand.Visible = false;
                    DeployManager m = new DeployManager(m_package.PublicGetService(typeof(DTE)) as DTE);
                    await m.DeployCode(teamNumber, page, debug);
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
    }
}
