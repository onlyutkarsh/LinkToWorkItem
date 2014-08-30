// Guids.cs
// MUST match guids.h

using System;

namespace LinkToWorkItem
{
    internal static class GuidList
    {
        public const string guidLinkToWorkItemPkgString = "9a975c55-b7c2-41a7-b27e-a4e2a87b4f0b";
        public const string guidLinkToWorkItemCmdSetString = "c2bb4b03-4105-4409-9eb1-5fa687fb4b56";

        public static readonly Guid guidLinkToWorkItemCmdSet = new Guid(guidLinkToWorkItemCmdSetString);
    };
}