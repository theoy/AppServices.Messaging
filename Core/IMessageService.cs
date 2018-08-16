using System.Threading.Tasks;
using Windows.Foundation.Collections;

namespace AppServices.Messaging.Core
{
    public interface IMessageService
    {
        Task<bool> TryOpenConnectionAsync();

        Task<ValueSet> SendMessageForResponseAsync(ValueSet message);

        void CloseConnection();
    }
}
