using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace AppServices.Messaging.Sample.UWP
{
    /// <summary>
    /// 
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            var request = new ValueSet
            {
                ["request"] = MessageTextBox.Text
            };

            var response = await ForegroundMessageService.Instance.SendMessageForResponseAsync(request);

            System.Diagnostics.Debug.WriteLine("Incoming response...");
            foreach (var key in response?.Keys)
            {
                System.Diagnostics.Debug.WriteLine($"[{key}]={response[key]?.ToString()}");
                if (response[key] is ValueSet)
                {
                    var valueSet = response[key] as ValueSet;
                    foreach (var subkey in valueSet.Keys)
                    {
                        System.Diagnostics.Debug.WriteLine($"    [{subkey}]={valueSet[subkey]?.ToString()}");
                    }
                }
            }
        }
    }
}
