using Microsoft.VisualStudio.Shell;
using static Microsoft.VisualStudio.Threading.JoinableTaskFactory;

namespace RobotDotNet.FRC_Extension
{
    internal static class ThreadHelperExtensions
    {
        public static MainThreadAwaitable SwitchToUiThread()
        {
            return ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
        }
    }
}
