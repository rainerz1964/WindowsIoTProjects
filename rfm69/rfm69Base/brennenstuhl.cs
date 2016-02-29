
using System;

namespace rfm69Base
{

    // the key success for Brennenstuhl plugs is a different
    // pulse length of 320us which translates to a bitrate of 3.125kHz
    // this code was tested with a Brennenstuhl RCS 1044N

    public class brennenstuhl : ISwitch
    {
        private int houseCode;
        private int plugCode;

        private const byte high = 0x8E;
        private const byte low = 0x88;

        private byte[] syncPattern = { };

        public brennenstuhl (string houseC, string plugC)
        {
            houseCode = Convert.ToInt32(houseC, 2);
            plugCode = Convert.ToInt32(plugC, 2);
        }
        public brennenstuhl(int houseC, int plugC)
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
            for (int i = syncStart+1; i < syncStart+4; i++)
                code[i] = 0x00;

            if (houseCode < 32)
            {
                int hc = houseCode;
                for (int i = 4+codeStart; i >= codeStart; i--)
                {
                    code[i] = (hc % 2 == 0) ? low : high;
                    hc /= 2;
                }
            }
            if (plugCode < 32)
            {
                int pc = plugCode;
                for (int i = codeStart+9; i >= codeStart+5; i--)
                {
                    code[i] = (pc % 2 == 0) ? low : high;
                    pc /= 2;
                }
            }

            // code[9 + codeStart] = high;
            code[10+codeStart] = on ? low : high;
            code[11+codeStart] = on ? high : low;

            for (int i = 0; i < 16; i++)
            {
                code[16 + i] =
                code[32 + i] =
                code[48 + i] = code[i];
            }
            return code;
        }

        public void config()
        {
            rfm.getRFM().defaults();
            rfm.getRFM().setDatarate(3.125);
            // this correlates to a pulse length of 320us
            // according to https://wiki.pilight.org/doku.php/elro_he
            // according 

        }

        public bool On()
        {
            byte[] co = getCode(true);
            config();
            rfm.getRFM().setSyncPattern(syncPattern);
            rfm.getRFM().sendPacket(co);
            return true;
        }
        public bool Off()
        {
            byte[] co = getCode(false);
            config();
            rfm.getRFM().setSyncPattern(syncPattern);
            rfm.getRFM().sendPacket(co);
            return true;
        }
    }
}
