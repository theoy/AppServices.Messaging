using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace AppServices.Messaging.Core
{
    public abstract class BaseMessageService : IMessageService
    {
        protected abstract string ORIGINATOR_ID { get; } // A unique ID for this implementation of the service.
        protected abstract string APP_SERVICE_NAME { get; }
        protected abstract string PACKAGE_FAMILY_NAME { get; }

        private AppServiceConnection _appServiceConnection;
        private TaskCompletionSource<ValueSet> _messageRequestTaskCompletionSource;

        public void CloseConnection()
        {
            _appServiceConnection?.Dispose();
            _appServiceConnection = null;
        }

        /// <summary>
        /// Send a message to the MessengerTask
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public virtual async Task<ValueSet> SendMessageForResponseAsync(ValueSet message)
        {
            var success = await TryOpenConnectionAsync();
            if (!success)
            {
                // TODO: Handle failed connection
                return null;
            }

            message["_from"] = ORIGINATOR_ID;
            message["_type"] = "message";

            _messageRequestTaskCompletionSource = new TaskCompletionSource<ValueSet>();
            await _appServiceConnection.SendMessageAsync(message);

            return await _messageRequestTaskCompletionSource.Task;
        }

        public async Task<bool> TryOpenConnectionAsync()
        {
            // Is a connection already open?
            if (_appServiceConnection != null)
            {
                return true;
            }

            try
            {
                // Set up a new app service connection
                _appServiceConnection = new AppServiceConnection
                {
                    AppServiceName = APP_SERVICE_NAME,
                    PackageFamilyName = PACKAGE_FAMILY_NAME
                };
                _appServiceConnection.ServiceClosed += AppServiceConnection_ServiceClosed;
                _appServiceConnection.RequestReceived += AppServiceConnection_RequestReceived;

                // Open the connection
                AppServiceConnectionStatus status = await _appServiceConnection.OpenAsync();

                // If the new connection opened successfully, send a success message
                if (status == AppServiceConnectionStatus.Success)
                {
                    var message = new ValueSet
                    {
                        ["_from"] = ORIGINATOR_ID,
                        ["_type"] = "sync"
                    };

                    await _appServiceConnection.SendMessageAsync(message);
                }
                else
                {
                    //Clean up before we go
                    CloseConnection();
                }

                return status == AppServiceConnectionStatus.Success;
            }
            catch
            {
                // TODO: Handle failure with retry logic, error state, etc.
                return false;
            }
        }

        protected virtual void OnRequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args) { }

        private void AppServiceConnection_RequestReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var response = args.Request.Message;
            _messageRequestTaskCompletionSource?.TrySetResult(response);

            OnRequestReceived(sender, args);
        }

        protected virtual void OnServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args) { }

        private void AppServiceConnection_ServiceClosed(AppServiceConnection sender, AppServiceClosedEventArgs args)
        {
            CloseConnection();
            OnServiceClosed(sender, args);
        }
    }
}
