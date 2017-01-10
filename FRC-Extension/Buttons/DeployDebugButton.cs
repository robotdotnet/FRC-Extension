using System;
using System.Collections.Generic;
using System.Linq;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Renci.SshNet.Common;
using RobotDotNet.FRC_Extension.RoboRIOCode;
using RobotDotNet.FRC_Extension.SettingsPages;
using VSLangProj;
using Task = System.Threading.Tasks.Task;

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

        private async Task DisableAllButtonsAsync()
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            foreach (var oleMenuCommand in DeployCommands)
            {
                oleMenuCommand.Enabled = false;
            }
            Deploying = true;
        }

        private async Task EnableAllButtonsAsync()
        {
            await ThreadHelperExtensions.SwitchToUiThread();
            foreach (var oleMenuCommand in DeployCommands)
            {
                oleMenuCommand.Enabled = true;
            }
            Deploying = false;
        }

        protected override async Task ButtonCallbackAsync(object sender, EventArgs e)
        {
            await ThreadHelperExtensions.SwitchToUiThread();
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
                    await OutputWriter.Instance.ClearAsync().ConfigureAwait(false);
                    string teamNumber = await Package.GetTeamNumberAsync().ConfigureAwait(false);

                    if (teamNumber == null) return;

                    //Disable the deploy buttons
                    await DisableAllButtonsAsync().ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    DeployManager m = new DeployManager(Package.PublicGetService<DTE>());
                    bool success = await m.DeployCodeAsync(teamNumber, DebugButton, m_robotProject).ConfigureAwait(false);
                    await EnableAllButtonsAsync().ConfigureAwait(false);
                    Output.ProgressBarLabel = success ? "Robot Code Deploy Successful" : "Robot Code Deploy Failed";
                }
                catch (SshConnectionException)
                {
                    await Output.WriteLineAsync("Connection to RoboRIO lost. Deploy aborted.").ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    await EnableAllButtonsAsync().ConfigureAwait(true);
                    Output.ProgressBarLabel = "Robot Code Deploy Failed";
                }
                catch (Exception ex)
                {
                    await Output.WriteLineAsync(ex.ToString()).ConfigureAwait(false);
                    await ThreadHelperExtensions.SwitchToUiThread();
                    await EnableAllButtonsAsync().ConfigureAwait(true);
                    Output.ProgressBarLabel = "Robot Code Deploy Failed";
                }

            }
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                var dte = Package.PublicGetService<DTE>();

                bool visable = false;
                m_robotProject = null;

                if (SettingsProvider.ExtensionSettingsPage.DebugMode)
                {
                    var sb = (SolutionBuild2)dte.Solution.SolutionBuild;

                    if (sb.StartupProjects != null)
                    {
                        if (sb.StartupProjects != null)
                        {
                            string project = ((Array)sb.StartupProjects).Cast<string>().First();
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
                        if (project.Globals == null)
                        {
                            //If globals are null, continue, as the project is probably disabled.
                            continue;
                        }
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
                                bool any = false;
                                foreach (Reference reference in vsproject.References)
                                {
                                    string name = reference.Name;
                                    if (name.Contains("WPILib"))
                                    {
                                        any = true;
                                        break;
                                    }
                                    /*
                                    if (reference.SourceProject == null)
                                    {

                                    }
                                    */
                                }
                                if (any)
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
