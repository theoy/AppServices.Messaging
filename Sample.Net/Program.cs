using System.Threading;
using System.Threading.Tasks;

namespace AppServices.Messaging.Sample.Net
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Task.Run(async () => { 
                Thread appServiceThread = new Thread(new ThreadStart(ThreadProc));
                appServiceThread.Start();

                while(appServiceThread.IsAlive)
                {
                    await Task.Delay(1 * 1000); // Delay to keep the thread open while still working
                }
            }).Wait();
        }

        /// <summary>
        /// Open the app service connection
        /// </summary>
        static async void ThreadProc()
        {
            try
            {
                await BackgroundMessageService.Instance.TryOpenConnectionAsync();
            }
            catch
            {
            }
        }
    }
}
