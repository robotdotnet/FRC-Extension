// PkgCmdID.cs
// MUST match PkgCmdID.h
using System;

namespace RobotDotNet.FRC_Extension
{
    static class PkgCmdIDList
    {
        //Deploy Code Button ID
        public const uint cmdidDeployCode = 0x100;
        //Settings Button ID
        public const uint cmdidSettings = 0x102;
        //Netconsole Button ID
        public const uint cmdidNetconsole = 0x103;
        //
        public const uint cmdidDownloadMono = 0x104;
        //
        public const uint cmdidInstallMono = 0x105;

        public const uint cmdidDebugCode = 0x106;

        public const uint cmdidAboutButton = 0x0220;

        public const uint cmdidKillButton = 0x107;

    };
}