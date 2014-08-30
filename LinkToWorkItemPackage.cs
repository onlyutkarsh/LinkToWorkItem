using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;

namespace LinkToWorkItem
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidLinkToWorkItemPkgString)]
    public sealed class LinkToWorkItemPackage : Package
    {
        public LinkToWorkItemPackage()
        {
        }

        #region Package Members

        protected override void Initialize()
        {
            base.Initialize();

            OleMenuCommandService mcs = GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (null != mcs)
            {
                CommandID menuCommandID = new CommandID(GuidList.guidLinkToWorkItemCmdSet, (int)PkgCmdIDList.cmdidAssignWorkItem);
                MenuCommand menuItem = new MenuCommand(OnLinToWorkItemClick, menuCommandID);
                mcs.AddCommand(menuItem);
            }
        }

        #endregion Package Members

        private void OnLinToWorkItemClick(object sender, EventArgs e)
        {
            var window = new SearchWorkItemWindow(this, this);
            window.ShowDialog();
        }
    }
}