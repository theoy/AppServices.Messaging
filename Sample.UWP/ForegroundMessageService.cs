using AppServices.Messaging.Core;
using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace AppServices.Messaging.Sample.UWP
{
    public sealed class ForegroundMessageService : BaseMessageService
    {
        protected override string ORIGINATOR_ID => "foreground";
        protected override string APP_SERVICE_NAME => "com.messaging.appservices";
        protected override string PACKAGE_FAMILY_NAME => "b752f9a1-ff97-45e6-ae35-8d32c1f2df3c_80c4904e66sn0";

        public static readonly IMessageService Instance = new ForegroundMessageService();

        private ForegroundMessageService() { }

        /// <summary>
        /// Send a request and wait for a response.
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public async override Task<ValueSet> SendMessageForResponseAsync(ValueSet message)
        {
            // Update the message data if necessary
            //message["key"] = "value";

            // Call the base to send the updated message
            return await base.SendMessageForResponseAsync(message);
        }
    }
}
