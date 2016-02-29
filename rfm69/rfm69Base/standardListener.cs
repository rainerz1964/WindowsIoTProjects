using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace rfm69Base
{
    public class standardListener : IListen
    {
        private byte[] syncPattern = { };
        private double dataRate;

        public standardListener(byte [] networkId, double dRate)
        {
            syncPattern = networkId;
            dataRate = dRate;
        }

        public void config ()
        {
            rfm.getRFM().defaults();
            rfm.getRFM().setDatarate(dataRate);
        }

        public int[] getResult()
        {
            config();
            rfm.getRFM().setSyncPattern(syncPattern);
            byte[] res = rfm.getRFM().receivePacket(66);
            int l = res.Length;
            int[] result = new int[l];
            for (int i = 0; i < l; i++)
                result[i] = res[i];
            return result;

        }

        public byte[] network()
        {
            return syncPattern;
        }
    }
}
