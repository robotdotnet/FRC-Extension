using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Renci.SshNet.Common;
using RobotDotNet.FRC_Extension.RoboRIOCode;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class DeployDebugButton : ButtonBase
    {
        protected readonly bool DebugButton;
        protected static bool Deploying;
        protected static readonly List<OleMenuCommand> DeployCommands = new List<OleMenuCommand>();

        private Project m_robotProject;

        public DeployDebugButton(Frc_ExtensionPackage package, int pkgCmdIdOfButton, bool debug) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, pkgCmdIdOfButton)
        {
            DebugButton = debug;
            DeployCommands.Add(OleMenuItem);
        }

        private void DisableAllButtons()
        {
            foreach (var oleMenuCommand in DeployCommands)
            {
                oleMenuCommand.Enabled = false;
            }
            Deploying = true;
        }

        private void EnableAllButtons()
        {
            foreach (var oleMenuCommand in DeployCommands)
            {
                oleMenuCommand.Enabled = true;
            }
            Deploying = false;
        }

        public override async void ButtonCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand == null)
            {
                return;
            }
            if (!Deploying)
            {
                try
                {
                    Output.ProgressBarLabel = "Deploying Robot Code";
                    OutputWriter.Instance.Clear();
                    SettingsPageGrid page;
                    string teamNumber = Package.GetTeamNumber(out page);

                    if (teamNumber == null) return;

                    //Disable the deploy buttons
                    DisableAllButtons();
                    DeployManager m = new DeployManager(Package.PublicGetService(typeof (DTE)) as DTE);
                    bool success = await m.DeployCode(teamNumber, page, DebugButton, m_robotProject);
                    EnableAllButtons();
                    Output.ProgressBarLabel = success ? "Robot Code Deploy Successful" : "Robot Code Deploy Failed";
                }
                catch (SshConnectionException)
                {
                    Output.WriteLine("Connection to RoboRIO lost. Deploy aborted.");
                    EnableAllButtons();
                    Output.ProgressBarLabel = "Robot Code Deploy Failed";
                }
                catch (Exception ex)
                {
                    Output.WriteLine(ex.ToString());
                    EnableAllButtons();
                    Output.ProgressBarLabel = "Robot Code Deploy Failed";
                }

            }
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                var dte = Package.PublicGetService(typeof(DTE)) as DTE;

                bool visable = false;
                m_robotProject = null;

                SettingsPageGrid grid = (SettingsPageGrid) Package.PublicGetDialogPage(typeof (SettingsPageGrid));

                if (grid.DebugMode)
                {
                    var sb = (SolutionBuild2) dte.Solution.SolutionBuild;

                    if (sb.StartupProjects != null)
                    {
                        if (sb.StartupProjects != null)
                        {
                            string project = ((Array) sb.StartupProjects).Cast<string>().First();
                            Project startupProject = dte.Solution.Item(project);
                            var vsproject = startupProject.Object as VSProject;
                            if (vsproject != null)
                            {
                                //If we are an assembly, and its named WPILib, enable the deploy
                                if (
                                    (from Reference reference in vsproject.References
                                        where reference.SourceProject == null
                                        select reference.Name).Any(name => name.Contains("WPILib")))
                                {
                                    m_robotProject = startupProject;
                                    visable = true;
                                }
                            }
                        }
                    }
                }
                else
                {
                    foreach (Project project in dte.Solution.Projects)
                    {
                        if (project.Globals.VariableExists["RobotProject"])
                        {
                            if (project.Globals["RobotProject"].ToString() != "yes")
                            {
                                continue;
                            }
                            var vsproject = project.Object as VSProject;

                            if (vsproject != null)
                            {
                                //If we are an assembly, and its named WPILib, enable the deploy
                                if (
                                    (from Reference reference in vsproject.References
                                        where reference.SourceProject == null
                                        select reference.Name).Any(name => name.Contains("WPILib")))
                                {
                                    visable = true;
                                    m_robotProject = project;
                                    break;
                                }
                            }
                        }
                    }
                }

                if (Deploying)
                    visable = false;

                menuCommand.Enabled = visable;

                menuCommand.Visible = true;
            }
        }
    }
}
