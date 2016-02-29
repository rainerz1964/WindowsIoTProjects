// defining the routes of the WEB API
// switches are defined in a global SwitchList

using System;
using System.Threading.Tasks;
using Devkoes.Restup.WebServer.Attributes;
using Devkoes.Restup.WebServer.Models.Schemas;
using rfm69Base;


namespace rfmWebApi
{
    [RestController(InstanceCreationType.PerCall)]
    class SwitchController
    {
        [UriFormat("/switches")]
        public GetResponse GetSwitches()
        {
            return new GetResponse(GetResponse.ResponseStatus.OK, SwitchList.list());
        }

        [UriFormat("/switch/{id}")]
        public GetResponse GetSwitch (int id)
        {
            Switch sw = SwitchList.contains(id);
            if (sw == null)
                return new GetResponse(GetResponse.ResponseStatus.NotFound);

            return new GetResponse(GetResponse.ResponseStatus.OK, sw); 

        }
        [UriFormat("/switch/{id}/on")]
        public async Task<PutResponse> SwitchOn(int id)
        {
            Switch sw = SwitchList.contains(id);
            if (sw == null)
                return new PutResponse(PutResponse.ResponseStatus.NotFound);
            sw.On();
            return await Task.FromResult(new PutResponse(PutResponse.ResponseStatus.OK));
        }
        [UriFormat("/switch/{id}/off")]
        public async Task<PutResponse> SwitchOff(int id)
        {
            Switch sw = SwitchList.contains(id);
            if (sw == null)
                return new PutResponse(PutResponse.ResponseStatus.NotFound);
            sw.Off();
            return await Task.FromResult(new PutResponse(PutResponse.ResponseStatus.OK));
        }
        [UriFormat("/switch/{id}/learn")]
        public async Task<PutResponse> Learn(int id)
        {
          Switch sw = SwitchList.contains(id);
            if (sw == null)
                return new PutResponse(PutResponse.ResponseStatus.NotFound);
            for (int i = 0; i < 3; i++)
                sw.On();
            return await Task.FromResult(new PutResponse(PutResponse.ResponseStatus.OK));
        }

        [UriFormat("/listener/datarate/{rate}")]
        public async Task<GetResponse> Listen(double rate)
        {
            byte [] networkId = { 0x80, 0x00, 0x00, 0x00 };
            standardListener listen = new standardListener (networkId, rate);
            int [] result = listen.getResult();
            return await Task.FromResult(new GetResponse(GetResponse.ResponseStatus.OK, result));
        }

    }
}
