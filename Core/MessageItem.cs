using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;

namespace AppServices.Messaging.Core
{
    public sealed class MessageItem
    {
        public AppServiceRequest Request { get; private set; }
        public ValueSet MessageData => Request?.Message;
        public ValueSet ResponseData { get; set; }

        public MessageOriginator Originator { get; private set; }
        public MessageType Type { get; private set; }

        public bool IsSyncMessage => Type == MessageType.Sync;

        public MessageItem(AppServiceRequest request)
        {
            Request = request;

            Originator = ((string)MessageData["_from"] == "foreground")
                ? MessageOriginator.Foreground
                : MessageOriginator.Background;

            Type = ((string)MessageData["_type"] == "sync")
                ? MessageType.Sync
                : MessageType.Message;
        }
    }
}
