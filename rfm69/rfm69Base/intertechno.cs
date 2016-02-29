using System;


namespace rfm69Base
{
    // for technical details see e.g.
    // http://blog.johjoh.de/funksteckdosen-umrechnung-fur-culcuxd/
    // house code is 4 binary digits
    // plug code is 4 binary digits
    // whereas in many documents for codes 0 and F is used 
    // there are options available to use both
    // 1 and F
    // both house code and plug code are 4 digits long

    // the intertechno code was tested with a selflearning
    // GRR-3500
     
    public class intertechno : ISwitch
    {
        private int houseCode;
        private int plugCode;

        private const byte high = 0x8E;
        private const byte low = 0x88;

        private byte[] syncPattern = { };
        

        public intertechno(string houseC, string plugC, char highVal = 'f')
        {
            string houseC1 = houseC;
            string plugC1 = plugC;
            if (highVal == 'f')
            {
                houseC1 = houseC1.Replace('f', '1');
                houseC1 = houseC1.Replace('F', '1');
                plugC1 = plugC1.Replace('f', '1');
                plugC1 = plugC1.Replace('F', '1');
            }
            houseCode = Convert.ToInt32(houseC1, 2);
            plugCode = Convert.ToInt32(plugC1, 2);
        }

        public intertechno(int houseC, int plugC)
        {
            houseCode = houseC;
            plugCode = plugC;
        }
        public byte[] getCode(bool on)
        {
            byte[] code = new byte[64];

            int syncStart = 0;
            int codeStart = 4;

            code[syncStart] = 0x80;
            for (int i = syncStart + 1; i < syncStart + 4; i++)
                code[i] = 0x00;

            if (houseCode < 32)
            {
                int hc = houseCode;
                for (int i = 3 + codeStart; i >= codeStart; i--)
                {
                    code[i] = (hc % 2 == 0) ? low : high;
                    hc /= 2;
                }
            }
            if (plugCode < 32)
            {
                int pc = plugCode;
                for (int i = codeStart + 7; i >= codeStart + 4; i--)
                {
                    code[i] = (pc % 2 == 0) ? low : high;
                    pc /= 2;
                }
            }

            code[8 + codeStart] = low;
            code[9 + codeStart] = high;
            code[10 + codeStart] = high;
            code[11 + codeStart] = on ? high : low;

            for (int i = 0; i < 16; i++)
                code[i + 48] = code[i + 32] = code[i + 16] = code[i];

            return code;
        }

        public void config ()
        {
            rfm.getRFM().defaults();
        }

        public bool On()
        {
            config ();
            rfm.getRFM().setSyncPattern(syncPattern);
            rfm.getRFM().sendPacket(getCode(true));
            return true;
        }
        public bool Off()
        {
            config();
            rfm.getRFM().setSyncPattern(syncPattern);
            rfm.getRFM().sendPacket(getCode(false));
            return true;
        }
    }
}
