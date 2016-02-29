using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.AppService;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using rfm69Base;

namespace rfm69
{
    class communication
    {
        private ICollection<KeyValuePair<string, ISwitch>> commandList;
        public AppServiceConnection appServiceConnection
        {
            get; set;
        }

        public communication()
        {
            // Initialize the AppServiceConnection
            appServiceConnection = new AppServiceConnection();
            appServiceConnection.PackageFamilyName = "ExpressServerIoT_js8vr7zzshjs8";
            appServiceConnection.AppServiceName = "App2AppComService";

            commandList = new Dictionary<string, ISwitch>();
        }

        public void registerCommand(string name, ISwitch sw)
        {
            commandList.Add (new KeyValuePair<string, ISwitch> (name, sw));
        }

        public async void open ()
        { 
            // Send a initialize request 
            var res = await appServiceConnection.OpenAsync();
            if (res == AppServiceConnectionStatus.Success)
            {
                var message = new ValueSet();
                message.Add("Command", "Initialize");
                var response = await appServiceConnection.SendMessageAsync(message);
                if (response.Status != AppServiceResponseStatus.Success)
                {
                    throw new Exception("Failed to send message");
                }
                appServiceConnection.RequestReceived += OnMessageReceived;
            }
        }


        private void OnMessageReceived(AppServiceConnection sender, AppServiceRequestReceivedEventArgs args)
        {
            var message = args.Request.Message;
            string newState = message["State"] as string;
            switch (newState)
            {
                case "On":
                    {
                        foreach (KeyValuePair<string, ISwitch> curr in commandList)
                        {
                            curr.Value.On();
                        }
                        break;
                    }
                case "Off":
                    {
                        foreach (KeyValuePair<string, ISwitch> curr in commandList)
                        {
                            curr.Value.Off();
                        }
                        break;
                    }
                case "Unspecified":
                default:
                    {
                        // Do nothing 
                        break;
                    }
            }
        }
    }
}
