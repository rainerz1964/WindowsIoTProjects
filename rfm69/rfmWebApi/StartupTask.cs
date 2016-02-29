// Web API for rf switches using 433.92 MHz
// connected to a Raspberry pi running WindowsIoT
// based on Devkoes Restup Server

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using rfm69Base;
using Devkoes.Restup.WebServer;


// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace rfmWebApi
{
    public sealed class StartupTask : IBackgroundTask
    {

        private RestWebServer WebServer;

        private BackgroundTaskDeferral Deferral;

        public async void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            Deferral = taskInstance.GetDeferral();

            WebServer = new RestWebServer(8888, "api");

            WebServer.RegisterController<SwitchController>();


            await WebServer.StartServerAsync();

            // Dont release deferral, otherwise app will stop

        }
    }
}
