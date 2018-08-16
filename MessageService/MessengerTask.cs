using AppServices.Messaging.Core;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.ApplicationModel.Background;
using Windows.Foundation.Collections;
using Windows.System;

namespace AppServices.Messaging.MessageService
{
    /// <summary>
    /// 
    /// </summary>
    public sealed class MessengerTask : IBackgroundTask
    {
        private const string FOREGROUND_PROTOCOL = "messaging://";
        private const string BACKGROUND_PARAMETER_GROUP_ID = "SyncGroup";
        private const string PACKAGE_FAMILY_NAME = "b752f9a1-ff97-45e6-ae35-8d32c1f2df3c_80c4904e66sn0";

        private BackgroundTaskDeferral _backgroundTaskDeferral;
        private static Stack<MessageItem> _messageStack;

        private static AppServiceConnection _foregroundConnection;
        private static AppServiceConnection _backgroundConnection;
        private static TaskCompletionSource<bool> _foregroundAppLaunchCompletionSource;
        private static TaskCompletionSource<bool> _backgroundAppLaunchCompletionSource;

        public MessengerTask()
        {
            _messageStack = new Stack<MessageItem>();
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var details = taskInstance.TriggerDetails as AppServiceTriggerDetails;

            // Safety check. Ignore calls from other packages
            if (details.CallerPackageFamilyName != PACKAGE_FAMILY_NAME) return;

            // Get a deferral so that the service isn't terminated.
            _backgroundTaskDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            // Retrieve the app service connection and set up a listener for incoming app service requests.
            if (_foregroundConnection == null)
            {
                _foregroundConnection = details.AppServiceConnection;
                _foregroundConnection.RequestReceived += AppServiceConnection_RequestReceived;
                _foregroundConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
            else
            {
                _backgroundConnection = details.AppServiceConnection;
                _backgroundConnection.RequestReceived += AppServiceConnection_RequestReceived;
                _backgroundConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="args"></param>
        private async void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            // Get a deferral because we use an awaitable API below to respond to the message
            // and we don't want this call to get cancelled while we are waiting.
            var messageDeferral = args.GetDeferral();

            try
            {
                var message = new MessageItem(args.Request);
                _messageStack.Push(message);

                // SyncMessage is only for the MessengerTask to consume
                if (message.IsSyncMessage)
                {
                    var tcs = (message.Originator == MessageOriginator.Background)
                        ? _backgroundAppLaunchCompletionSource
                        : _foregroundAppLaunchCompletionSource;
                    tcs?.TrySetResult(true);
                }
                else
                {
                    // Send the message
                    await SendMessageAsync(message);
                }
            }
            catch (Exception)
            {
                // your exception handling code here
            }
            finally
            {
                // Complete the deferral so that the platform knows that we're done responding to the app service call.
                // Note for error handling: this must be called even if SendResponseAsync() throws an exception.
                messageDeferral.Complete();
            }
        }

        /// <summary>
        /// Inspects a message and sends it to the correct app.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task SendMessageAsync(MessageItem message)
        {
            var connection = (message.Originator == MessageOriginator.Foreground)
                ? _backgroundConnection
                : _foregroundConnection;

            if (connection == null)
            {
                connection = await LaunchAppForMessageAsync(message);
            }

            await connection.SendMessageAsync(message.MessageData);
        }

        /// <summary>
        /// Launch an app endpoint and return once the connection is open
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        private async Task<AppServiceConnection> LaunchAppForMessageAsync(MessageItem message)
        {
            if (message.Originator == MessageOriginator.Foreground)
            {
                await LaunchBackgroundAppAsync();
                return _backgroundConnection;
            }
            else
            {
                await LaunchForegroundAppAsync();
                return _foregroundConnection;
            }
        }

        /// <summary>
        /// Launches the FullTrustProcess and waits for it to send an init success message.
        /// </summary>
        /// <returns></returns>
        private async Task LaunchBackgroundAppAsync()
        {
            // Launch the Win32. It will start by opening a connection to the appservice as well.
            await Windows.ApplicationModel.FullTrustProcessLauncher.LaunchFullTrustProcessForCurrentAppAsync(BACKGROUND_PARAMETER_GROUP_ID);

            // Once the second connection has been made, they can talk to each other via the messenger
            _backgroundAppLaunchCompletionSource = new TaskCompletionSource<bool>();
            await _backgroundAppLaunchCompletionSource.Task;
        }

        /// <summary>
        /// Launch the Foreground application and wait for it to send an init success message
        /// </summary>
        /// <returns></returns>
        private async Task LaunchForegroundAppAsync()
        {
            var uri = new Uri(FOREGROUND_PROTOCOL);
            var options = new LauncherOptions()
            {
                // Specify a pfn to ensure other apps don't intercept the message
                TargetApplicationPackageFamilyName = PACKAGE_FAMILY_NAME
            };
            var data = new ValueSet();
            await Launcher.LaunchUriAsync(uri, options, data);

            _foregroundAppLaunchCompletionSource = new TaskCompletionSource<bool>();
            await _foregroundAppLaunchCompletionSource.Task;
        }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            sender?.Dispose();
        }

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            _backgroundTaskDeferral?.Complete();
            _backgroundTaskDeferral = null;
        }
    }
}

