using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using VSLangProj;

namespace RobotDotNet.FRC_Extension
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the information needed to show this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidFRC_ExtensionPkgString)]
    //This attribute allows the extension to automatically update if it is a robot package.
    [ProvideAutoLoad("{f1536ef8-92ec-443c-9ed7-fdadf150da82}")]
    //This gives us an options page.
    [ProvideOptionPage(typeof(SettingsPageGrid), "FRC Options", "FRC Options", 0, 0, true)]
    public sealed class FRC_ExtensionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public FRC_ExtensionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }



        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Debug.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();



            // Add our command handlers for menu (commands must exist in the .vsct file)
            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if ( null != mcs )
            {
                //Creating the deploy button. The BeforeQueryStatus event allows us to enable or disable the
                //button based on if we are a WPILib project or not.
                CommandID menuCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidDeployCode);
                OleMenuCommand menuItem = new OleMenuCommand(DeployCodeCallback, menuCommandID );
                menuItem.BeforeQueryStatus += QueryDeployButton;
                mcs.AddCommand( menuItem );

                CommandID installCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet,
                    (int) PkgCmdIDList.cmdidInstall);
                MenuCommand installItem = new MenuCommand(InstallCallback, installCommandID);
                mcs.AddCommand(installItem);

                CommandID settingsCommandID = new CommandID(GuidList.guidFRC_ExtensionCmdSet,
                    (int)PkgCmdIDList.cmdidSettings);
                MenuCommand settingsItem = new MenuCommand(SettingsCallback, settingsCommandID);
                mcs.AddCommand(settingsItem);
            }
        }
        #endregion

        //This is called every time the menu is open, to check and see
        private void QueryDeployButton(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                var dte = GetService(typeof (DTE)) as DTE;
                var sb = (SolutionBuild2) dte.Solution.SolutionBuild;

                

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

                menuCommand.Visible = visable;
                if (menuCommand.Visible)
                    menuCommand.Enabled = ((Array) sb.StartupProjects).Cast<string>().Count() == 1;
            }
        }

        /// <summary>
        /// This function is the callback used to execute a command when the a menu item is clicked.
        /// See the Initialize method to see how the menu item is associated to this function using
        /// the OleMenuCommandService service and the MenuCommand class.
        /// </summary>
        private void DeployCodeCallback(object sender, EventArgs e)
        {
            OutputWriter.WriteToPane("Deploy Button Pressed.");

            //Writing Team Number
            SettingsPageGrid page = (SettingsPageGrid) GetDialogPage(typeof (SettingsPageGrid));
            OutputWriter.WriteToPane(page.TeamNumber.ToString());

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "FRC Extension",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.MenuItemCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }

        private void InstallCallback(object sender, EventArgs e)
        {
            OutputWriter.WriteToPane("Install Button Pressed.");


            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       "FRC Extension",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.InstallCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
        }


        private void SettingsCallback(object sender, EventArgs e)
        {
            OutputWriter.WriteToPane("Settings Button Pressed.");
            /*

            // Show a Message Box to prove we were here
            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(uiShell.ShowMessageBox(
                       0,
                       ref clsid,
                       " Message ",
                       string.Format(CultureInfo.CurrentCulture, "Inside {0}.SettingsCallback()", this.ToString()),
                       string.Empty,
                       0,
                       OLEMSGBUTTON.OLEMSGBUTTON_OK,
                       OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST,
                       OLEMSGICON.OLEMSGICON_INFO,
                       0,        // false
                       out result));
             * */

            this.ShowOptionPage(typeof (SettingsPageGrid));
        }
    }

    
}
