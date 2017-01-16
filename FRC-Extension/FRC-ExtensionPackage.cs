using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using RobotDotNet.FRC_Extension.Buttons;
using RobotDotNet.FRC_Extension.MonoCode;
using RobotDotNet.FRC_Extension.RoboRIOCode;
using RobotDotNet.FRC_Extension.SettingsPages;
using RobotDotNet.FRC_Extension.WPILibFolder;
using Task = System.Threading.Tasks.Task;

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
    [ProvideOptionPage(typeof(TeamSettingsPage), "FRC Options", "Team Options", 0, 0, true)]
    [ProvideOptionPage(typeof(ExtensionSettingsPage), "FRC Options", "Extension Options", 1, 1, true)]
    [ProvideOptionPage(typeof(DependencySettingsPage), "FRC Options", "Dependency Options", 2, 2, true)]
    [ProvideBindingPath]
    public sealed class Frc_ExtensionPackage : Package
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require
        /// any Visual Studio service because at this point the package object is created but
        /// not sited yet inside Visual Studio environment. The place to do all the other
        /// initialization is the Initialize method.
        /// </summary>
        public Frc_ExtensionPackage()
        {
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));
        }

        //Store our local variables so we can control certain functions
        private OutputWriter m_writer;
        private MonoFile m_monoFile;

        public static Frc_ExtensionPackage Instance { get; private set; }

        internal InterfaceType PublicGetService<InterfaceType, ServiceType>() => (InterfaceType)GetService(typeof(ServiceType));

        internal InterfaceType PublicGetService<InterfaceType>() => (InterfaceType)GetService(typeof(InterfaceType));

        internal DialogPage PublicGetDialogPage(Type dialogPageType)
        {
            return GetDialogPage(dialogPageType);
        }

        private DeployDebugButton m_deployButton;
        private DeployDebugButton m_debugButton;

        private KillButton m_killButton;

        //private MonoButtons m_monoButtons;
        private DownloadMonoButton m_downloadMonoButton;
        private InstallMonoButton m_installMonoButton;
        private SaveMonoButton m_saveMonoButton;

        private AboutButton m_aboutButton;
        private NetConsoleButton m_netConsoleButton;
        private SettingsButton m_settingsButton;

        private SetMainRobotButton m_setMainRobotButton;

        /////////////////////////////////////////////////////////////////////////////
        // Overridden Package Implementation
        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Instance = this;
            Debug.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();

            m_writer = OutputWriter.Instance;

            var teamPage = (TeamSettingsPage)PublicGetDialogPage(typeof(TeamSettingsPage));
            var extPage = (ExtensionSettingsPage)PublicGetDialogPage(typeof(ExtensionSettingsPage));
            var depPage = (DependencySettingsPage)PublicGetDialogPage(typeof(DependencySettingsPage));

            SettingsProvider.SetSettingsPages(teamPage, extPage, depPage);


            m_deployButton = new DeployDebugButton(this, (int)PkgCmdIDList.cmdidDeployCode, false);
            m_debugButton = new DeployDebugButton(this, (int)PkgCmdIDList.cmdidDebugCode, true);

            m_killButton = new KillButton(this);


            string monoFolder = WPILibFolderStructure.CreateMonoFolder();

            string monoFile = monoFolder + Path.DirectorySeparatorChar + DeployProperties.MonoVersion;

            m_monoFile = new MonoFile(monoFile);

            m_installMonoButton = new InstallMonoButton(this, m_monoFile);
            m_downloadMonoButton = new DownloadMonoButton(this, m_monoFile, m_installMonoButton);
            m_saveMonoButton = new SaveMonoButton(this, m_monoFile);


            m_aboutButton = new AboutButton(this);
            m_netConsoleButton = new NetConsoleButton(this);
            m_settingsButton = new SettingsButton(this);

            m_setMainRobotButton = new SetMainRobotButton(this);
        }
        #endregion

        internal async Task<string> GetTeamNumberAsync()
        {
            var page = SettingsProvider.TeamSettingsPage;
            //Get Team Number
            string teamNumber = page.TeamNumber.ToString();
            if (teamNumber == "0")
            {
                //If its 0, we pop up a window asking teams to set it.
                await TeamNumberNotSetErrorPopupAsync().ConfigureAwait(false);
                return null;
            }
            return teamNumber;
        }

        public async Task TeamNumberNotSetErrorPopupAsync()
        {
            await OutputWriter.Instance.WriteLineAsync("Team Number Not Set").ConfigureAwait(false);
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            IVsUIShell uiShell = (IVsUIShell)GetService(typeof(SVsUIShell));
            Guid clsid = Guid.Empty;
            int result;

            uiShell.ShowMessageBox(0, ref clsid, "Team Number Not Set",
                "Please see your team number. Click OK will open up the settings menu.", string.Empty, 0,
                OLEMSGBUTTON.OLEMSGBUTTON_OKCANCEL, OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST, OLEMSGICON.OLEMSGICON_INFO, 0, out result);

            if (result == 1)
            {
                ShowOptionPage(typeof(TeamSettingsPage));
            }
        }
    }
}
