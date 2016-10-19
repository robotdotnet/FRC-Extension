using System;
using System.Linq;
using System.Runtime.InteropServices;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using VSLangProj;

namespace RobotDotNet.FRC_Extension.Buttons
{
    public class SetMainRobotButton : ButtonBase
    {
        public SetMainRobotButton(Frc_ExtensionPackage package) : base(package, true, GuidList.guidFRC_ExtensionCmdSet, (int)PkgCmdIDList.cmdidSetRobotProject)
        {
        }

        public override void QueryCallback(object sender, EventArgs e)
        {
            var menuCommand = sender as OleMenuCommand;
            if (menuCommand != null)
            {
                if (GetSelectedProject() != null)
                {
                    menuCommand.Visible = true;
                    menuCommand.Enabled = true;
                }
                else
                {
                    menuCommand.Visible = false;
                    menuCommand.Enabled = false;
                }               
            }
        }

        public Project GetSelectedProject()
        {
            IntPtr hierarchyPointer = IntPtr.Zero;
            IntPtr selectionContainerPointer = IntPtr.Zero;

            try
            {


                Object selectedObject = null;
                IVsMultiItemSelect multiItemSelect;
                uint projectItemId;

                IVsMonitorSelection monitorSelection =
                    (IVsMonitorSelection)Microsoft.VisualStudio.Shell.Package.GetGlobalService(
                        typeof(SVsShellMonitorSelection));

                monitorSelection.GetCurrentSelection(out hierarchyPointer,
                    out projectItemId,
                    out multiItemSelect,
                    out selectionContainerPointer);

                IVsHierarchy selectedHierarchy = Marshal.GetTypedObjectForIUnknown(
                    hierarchyPointer,
                    typeof(IVsHierarchy)) as IVsHierarchy;

                if (selectedHierarchy != null)
                {
                    if (ErrorHandler.Failed(selectedHierarchy.GetProperty(
                        projectItemId,
                        (int)__VSHPROPID.VSHPROPID_ExtObject,
                        out selectedObject)))
                    {
                        return null;
                    }
                }

                Project project = selectedObject as Project;

                var vsproject = project?.Object as VSProject;

                if (vsproject != null)
                {
                    //If we are an assembly, and its named WPILib, enable the deploy
                    foreach (Reference reference in vsproject.References)
                    {
                        string name = reference.Name;
                        if (name.Contains("WPILib"))
                        {
                            return project;
                        }
                        /*
                        if (reference.SourceProject == null)
                        {
                            
                        }
                        */
                    }
                }

                return null;
            }
            finally
            {
                if (selectionContainerPointer != IntPtr.Zero)
                {
                    Marshal.Release(selectionContainerPointer);
                }

                if (hierarchyPointer != IntPtr.Zero)
                {
                    Marshal.Release(hierarchyPointer);
                }
            }
        }


        public override void ButtonCallback(object sender, EventArgs e)
        {
            Project project = GetSelectedProject();

            if (project == null)
            {
                return;
            }

            ClearAllProjectsInSolution();

            project.Globals["RobotProject"] = "yes";
            project.Globals.VariablePersists["RobotProject"] = true;
            project.Save();
        }

        private void ClearAllProjectsInSolution()
        {
            var dte = Package.PublicGetService(typeof(DTE)) as DTE;
            var sb = dte?.Solution;
            if (sb == null)
            {
                return;
            }


            foreach (Project p in sb.Projects)
            {
                var globals = p.Globals;
                if (globals == null)
                {
                    continue;
                }
                if (globals.VariableExists["RobotProject"])
                {
                    globals["RobotProject"] = "no";
                }
                p.Save();
            }
        }
    }
}
