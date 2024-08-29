using System.Diagnostics;
using CommunityToolkit.WinUI.Notifications;
using Microsoft.UI.Xaml.Controls;
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SettingsEngine;
using UniGetUI.Core.Tools;
using UniGetUI.Interface.Enums;
using UniGetUI.PackageEngine.Enums;
using UniGetUI.PackageEngine.Interfaces;

namespace UniGetUI.PackageEngine.Operations
{

    public abstract class SourceOperation : AbstractOperation
    {
        protected IManagerSource Source;
        public SourceOperation(IManagerSource source)
        {
            Source = source;
            MainProcedure();
        }

        protected override async Task HandleCancelation()
        {
        }


        protected void ShowErrorNotification(string title, string body)
        {
            if (Settings.AreErrorNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Source.Name);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Urgent)
                    .SetTag(Source.Name)
                    .AddText(title)
                    .AddText(body)
                    .AddArgument("action", NotificationArguments.Show);
                AppNotification notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show toast notification");
                Logger.Error(ex);
            }
        }

        protected void ShowSuccessNotification(string title, string body)
        {
            if (Settings.AreSuccessNotificationsDisabled())
                return;

            try
            {
                AppNotificationManager.Default.RemoveByTagAsync(Source.Name);
                AppNotificationBuilder builder = new AppNotificationBuilder()
                    .SetScenario(AppNotificationScenario.Default)
                    .SetTag(Source.Name)
                    .AddText(title)
                    .AddText(body)
                    .AddArgument("action", NotificationArguments.Show);
                AppNotification notification = builder.BuildNotification();
                AppNotificationManager.Default.Show(notification);
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to show toast notification");
                Logger.Error(ex);
            }
        }
    }

    public class AddSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs>? OperationSucceeded;
        public AddSourceOperation(IManagerSource source) : base(source) { }
        protected override async Task<ProcessStartInfo> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetAddSourceParameters(Source));
            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetAddSourceParameters(Source));
            }

            return startInfo;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting adding source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            ];
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.GetAddSourceOperationVeredict(Source, ReturnCode, Output));
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });

            ShowErrorNotification(
                CoreTools.Translate("Installation failed"),
                CoreTools.Translate("Could not add source {source} to {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("Source addition failed"),
                CoreTools.Translate("Could not add source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            return result == ContentDialogResult.Primary ? AfterFinshAction.Retry : AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            OperationSucceeded?.Invoke(this, EventArgs.Empty);
            LineInfoText = CoreTools.Translate("The source {source} was added to {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });

            ShowSuccessNotification(
                CoreTools.Translate("Addition succeeded"),
                CoreTools.Translate("The source {source} was added to {manager} successfully",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = CoreTools.Translate("Adding source {source} to {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }

    public class RemoveSourceOperation : SourceOperation
    {

        public event EventHandler<EventArgs>? OperationSucceeded;
        public RemoveSourceOperation(IManagerSource source) : base(source) { }
        protected override async Task<ProcessStartInfo> BuildProcessInstance(ProcessStartInfo startInfo)
        {
            if (Source.Manager.Capabilities.Sources.MustBeInstalledAsAdmin)
            {
                if (Settings.Get("DoCacheAdminRights") || Settings.Get("DoCacheAdminRightsForBatches"))
                {
                    await CoreTools.CacheUACForCurrentProcess();
                }
                startInfo.FileName = CoreData.GSudoPath;
                startInfo.Arguments = $"\"{Source.Manager.Status.ExecutablePath}\" " + Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetRemoveSourceParameters(Source));

            }
            else
            {
                startInfo.FileName = Source.Manager.Status.ExecutablePath;
                startInfo.Arguments = Source.Manager.Properties.ExecutableCallArgs + " " + string.Join(" ", Source.Manager.GetRemoveSourceParameters(Source));
            }

            return startInfo;
        }

        protected override string[] GenerateProcessLogHeader()
        {
            return
            [
                "Starting remove source operation for source name=" + Source.Name + "with Manager name=" + Source.Manager.Name,
            ];
        }

        protected override Task<OperationVeredict> GetProcessVeredict(int ReturnCode, string[] Output)
        {
            return Task.Run(() => Source.Manager.GetRemoveSourceOperationVeredict(Source, ReturnCode, Output));
        }

        protected override async Task<AfterFinshAction> HandleFailure()
        {
            LineInfoText = CoreTools.Translate("Could not remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });

            ShowErrorNotification(
                CoreTools.Translate("Removal failed"),
                CoreTools.Translate("Could not add remove {source} from {manager}",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            ContentDialogResult result = await MainApp.Instance.MainWindow.NavigationPage.ShowOperationFailedDialog(
                ProcessOutput,
                CoreTools.Translate("Source removal failed"),
                CoreTools.Translate("Could remove source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            return result == ContentDialogResult.Primary ? AfterFinshAction.Retry : AfterFinshAction.ManualClose;
        }

        protected override async Task<AfterFinshAction> HandleSuccess()
        {
            OperationSucceeded?.Invoke(this, EventArgs.Empty);
            LineInfoText = CoreTools.Translate("The source {source} was removed from {manager} successfully", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });

            ShowSuccessNotification(
                CoreTools.Translate("Removal succeeded"),
                CoreTools.Translate("The source {source} was removed from {manager} successfully",
                    new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } })
            );

            await Task.Delay(0);
            return AfterFinshAction.TimeoutClose;
        }

        protected override void Initialize()
        {
            OperationTitle = CoreTools.Translate("Removing source {source} from {manager}", new Dictionary<string, object?> { { "source", Source.Name }, { "manager", Source.Manager.Name } });
            IconSource = new Uri("ms-appx:///Assets/Images/" + Source.Manager.Properties.ColorIconId + ".png");
        }
    }
}
