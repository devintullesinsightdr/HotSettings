﻿using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Settings;
using Microsoft.VisualStudio.Text.Editor;
using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.TextManager.Interop;
using EnvDTE;
using System.Windows;

namespace HotSettings
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TrackActiveItemsCommandHandler
    {
        // Command Guid and ID
        private readonly Guid HotSettingsPackageCmdSetGuid = Constants.HotSettingsCmdSetGuid;
        private const int ToggleTrackActiveItemCmdId = Constants.ToggleTrackActiveItemCmdId;

        // UserSettingsStore properties
        private const string SOLUTION_NAVIGATOR_GROUP = @"ApplicationPrivateSettings\SolutionNavigator";
        private const string TRACK_ACTIVE_ITEM_IN_SOLN_EXP = "TrackSelCtxInSlnExp";

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private readonly Package package;

        private SettingsStore SettingsStore;
        private IEditorOptionsFactoryService OptionsService;
        private IVsTextManager2 TextManager;
        //private IVsEditorAdaptersFactoryService EditorAdaptersFactoryService;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TrackActiveItemsCommandHandler Instance { get; private set; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private System.IServiceProvider ServiceProvider
        {
            get
            {
                return this.package;
            }
        }

        private WritableSettingsStore _userSettingsStore;
        private WritableSettingsStore UserSettingsStore
        {
            get
            {
                if (_userSettingsStore == null)
                {
                    ShellSettingsManager settingsManager = new ShellSettingsManager(this.ServiceProvider);
                    _userSettingsStore = settingsManager.GetWritableSettingsStore(SettingsScope.UserSettings);
                }
                return _userSettingsStore;
            }
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(Package package)
        {
            Instance = new TrackActiveItemsCommandHandler(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackActiveItemsCommandHandler"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TrackActiveItemsCommandHandler(Package package)
        {
            this.package = package ?? throw new ArgumentNullException("package");

            ShellSettingsManager settingsManager = new ShellSettingsManager(package);
            SettingsStore = settingsManager.GetReadOnlySettingsStore(SettingsScope.UserSettings);

            OptionsService = ServicesUtil.GetMefService<IEditorOptionsFactoryService>(this.ServiceProvider);
            TextManager = (IVsTextManager2)ServiceProvider.GetService(typeof(SVsTextManager));

            RegisterGlobalCommands();
        }

        private void RegisterGlobalCommands()
        {
            if (ServiceProvider.GetService(typeof(IMenuCommandService)) is OleMenuCommandService commandService)
            {
                // Register command handler for TrackActiveItems menu command
                CommandID menuCommandID = new CommandID(HotSettingsPackageCmdSetGuid, ToggleTrackActiveItemCmdId);
                OleMenuCommand oleMenuCommand = new OleMenuCommand(this.MenuItemCallback, menuCommandID);
                oleMenuCommand.BeforeQueryStatus += this.OnBeforeQueryStatus;
                commandService.AddCommand(oleMenuCommand);
            }
        }

        /// <summary>
        /// This function is run whenever VS is considering displaying the command.
        /// For menu items, it runs it before opening the menu. For toolbar items, it seems to run is frequently.
        /// Here you can change the visibility of a menu item. Also set its Checked state, and other properties (like Enabled).
        /// </summary>
        public void OnBeforeQueryStatus(object sender, EventArgs e)
        {
            OleMenuCommand command = (OleMenuCommand)sender;
            switch ((uint)command.CommandID.ID)
            {
                case ToggleTrackActiveItemCmdId:
                    QueryStatusToggleTrackActiveItems(command);
                    break;
            }
        }

        private void MenuItemCallback(object sender, EventArgs e)
        {
            MenuCommand command = (MenuCommand)sender;
            // Dispatch the action
            switch (command.CommandID.ID)
            {
                case ToggleTrackActiveItemCmdId:
                    HandleToggleTrackActiveItem(command);
                    break;
            }
        }

        private void QueryStatusToggleTrackActiveItems(OleMenuCommand command)
        {
            command.Enabled = true; // TODO: Set true if there is a solution
            command.Visible = true; // TODO: Set true if there is a solution
            command.Checked = IsTrackActiveItemInSolnExpEnabled();
        }

        private bool IsTrackActiveItemInSolnExpEnabled()
        {
            // The value is 0*System.Boolean*True or 0*System.Boolean*False
            string trackActiveString = UserSettingsStore.GetString(SOLUTION_NAVIGATOR_GROUP, TRACK_ACTIVE_ITEM_IN_SOLN_EXP);
            return trackActiveString.ToLower().Contains("true");
        }

        private void HandleToggleTrackActiveItem(MenuCommand command)
        {
            // Optimisation: Don't check the UserSettingStore. It has just been checked during QueryStatus.
            // Instead, set new value based on current checked state of MenuCommand.
            bool newCheckedState = !command.Checked;
            if (newCheckedState)
            {
                EnableTrackActiveItemInSolnExp();
            } else
            {
                MessageBox.Show("I'm sorry - I haven't figured out how to turn this off yet. :-(");
            }
        }

        private void EnableTrackActiveItemInSolnExp()
        {
            DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
            dte.ExecuteCommand("View.TrackActivityinSolutionExplorer");
        }

        //private void SetTrackActiveItemInSolnExpEnabled(bool bNewValue)
        //{
        //    // The value is 0*System.Boolean*True or 0*System.Boolean*False
        //    string trackActiveString = $"0*System.Boolean*{(bNewValue ? "True" : "False")}";
        //    UserSettingsStore.SetString(SOLUTION_NAVIGATOR_GROUP, TRACK_ACTIVE_ITEM_IN_SOLN_EXP, trackActiveString);
        //}

        //private void SetTrackActiveItemInSolnExpEnabled(bool bNewValue)
        //{
        //    // The value is 0*System.Boolean*True or 0*System.Boolean*False
        //    string trackActiveString = $"0*System.Boolean*{(bNewValue ? "True" : "False")}";

        //    IntPtr inArgPtr = Marshal.AllocCoTaskMem(200);
        //    Marshal.GetNativeVariantForObject(trackActiveString, inArgPtr);

        //    Guid cmdGroup = default(Guid); // TODO: Find Guid for View.TrackActivityinSolutionExplorer
        //    uint cmdID = 0;     // TODO: Find cmdId for View.TrackActivityinSolutionExplorer

        //    IOleCommandTarget commandTarget = Package.GetGlobalService(typeof(SUIHostCommandDispatcher)) as IOleCommandTarget;
        //    int hr = commandTarget.Exec(ref cmdGroup, cmdID, (uint)OLECMDEXECOPT.OLECMDEXECOPT_DODEFAULT, inArgPtr, IntPtr.Zero);
        //}

        //private void SetTrackActiveItemInSolnExp(bool bNewValue)
        //{
        //    DTE dte = (DTE)ServiceProvider.GetService(typeof(DTE));
        //    dte.ExecuteCommand("View.TrackActivityinSolutionExplorer", bNewValue ? "true" : "fasle");
        //    // Returns error: Command "View.TrackActivityinSolutionExplorer" does not accept arguments or switches.
        //}


    }
}
