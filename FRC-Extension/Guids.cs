// Guids.cs
// MUST match guids.h
using System;

namespace RobotDotNet.FRC_Extension
{
    static class GuidList
    {
        //GUID of the package
        public const string guidFRC_ExtensionPkgString = "5b8fea9b-2657-4c90-8236-d82478b7d6c9";
        //GUID of the commands set
        public const string guidFRC_ExtensionCmdSetString = "65e4afe2-0805-4a28-8342-93fdea1b3758";

        public static readonly Guid guidFRC_ExtensionCmdSet = new Guid(guidFRC_ExtensionCmdSetString);
    };
}