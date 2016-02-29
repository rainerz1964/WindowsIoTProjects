// RFM69 defines a Universal App that uses App2App Communication in a fashion
// similar to the Blinky Web Server example 
// see https://ms-iot.github.io/content/en-US/win10/samples/BlinkyWebServer.htm for details


using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Http;
using Windows.ApplicationModel.Background;
using rfm69Base;

// The Background Application template is documented at http://go.microsoft.com/fwlink/?LinkID=533884&clcid=0x409

namespace rfm69
{
    public sealed class StartupTask : IBackgroundTask
    {
        private BackgroundTaskDeferral backgroundTaskDeferral;

        private void TaskInstance_Canceled(IBackgroundTaskInstance sender, BackgroundTaskCancellationReason reason)
        {
            // Release the deferral so that the app can be stopped.
            this.backgroundTaskDeferral.Complete();
        }

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            // 
            // TODO: Insert code to perform background work
            //
            // If you start any asynchronous methods here, prevent the task
            // from closing prematurely by using BackgroundTaskDeferral as
            // described in http://aka.ms/backgroundtaskdeferral
            //

            this.backgroundTaskDeferral = taskInstance.GetDeferral();
            taskInstance.Canceled += TaskInstance_Canceled;

            rfm myRfm = rfm.getRFM();

            communication myComm = new communication();
            string houseCode = "10110";
            string switchCode = "10000";
            brennenstuhl myBrenn = new brennenstuhl(houseCode, switchCode);
            myComm.registerCommand("Außensteckdose", myBrenn);
            myComm.open();
            
        }
    }
}
