using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Renci.SshNet.Common;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DeployDebugButton : ButtonBase
    {
        protected readonly bool m_debugButton;
        protected static bool s_deploying = false;
        protected static readonly List<OleMenuCommand> s_deployCommands = new List<OleMenuCommand>();

        private Project m_robotProject = null;

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
                    OutputWriter.Instance.Clear();
                    SettingsPageGrid page;
                    string teamNumber = m_package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    //Disable the deploy buttons
                    DisableAllButtons();
                    DeployManager m = new DeployManager(m_package.PublicGetService(typeof (DTE)) as DTE);
                    await m.DeployCode(teamNumber, page, m_debugButton, m_robotProject);
                    EnableAllButtons();
                    m_output.ProgressBarLabel = "Robot Code Deploy Successful";
                }
                catch (SshConnectionException)
                {
                    m_output.WriteLine("Connection to RoboRIO lost. Deploy aborted.");
                    EnableAllButtons();
                    m_output.ProgressBarLabel = "Robot Code Deploy Failed";
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

                bool visable = false;
                m_robotProject = null;

                foreach (Project project in dte.Solution.Projects)
                {
                    if (project.Globals.VariableExists["RobotProject"])
                    {
                        var vsproject = project.Object as VSLangProj.VSProject;

                        if (vsproject != null)
                        {
                            //If we are an assembly, and its named WPILib, enable the deploy
                            if ((from Reference reference in vsproject.References where reference.SourceProject == null select reference.Name).Any(name => name.Contains("WPILib")))
                            {
                                visable = true;
                                m_robotProject = project;
                                break;
                            }
                        }
                    }
                }

                if (s_deploying)
                    visable = false;

                menuCommand.Visible = visable;
                if (menuCommand.Visible)
                {
                    menuCommand.Enabled = true;
                }
            }
        }
    }
}
