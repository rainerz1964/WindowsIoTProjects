// This module is inspired by the Python based implementation
// https://github.com/Phunkafizer/RaspyRFM
// for further technical details please refer to
// http://www.hoperf.com/upload/rf/RFM69CW-V1.1.pdf

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;
using Windows.Devices.Spi;
using Windows.Devices.Gpio;
#if DEBUG
using System.Diagnostics;
#endif



//------ Raspberry RFM Module connection -----
//
//-------------------------------------------------//
// Raspi | Raspi | Raspi | RFM69 | RFM12 | PCB con //
// Name  | GPIO  | Pin   | Name  |  Name |  Pin    //
//-------------------------------------------------//
// 3V3   |   -   |  17   |                   1
//  -    |  24   |  18   | DIO1  | FFIT  |   2
// MOSI  |  10   |  19   | MOSI  | SDI   |   3
// GND   |   -   |  20   | GND   | GND   |   4
// MISO  |   9   |  21   | MISO  | SDO   |   5 
//  -    |  25   |  22   | DIO0  | nIRQ  |   6
// SCKL  |  11   |  23   | SCK   | SCK   |   7
// CE0   |   8   |  24   | NSS   | nSEL  |   8
// CE1   |   7   |  26   | DIO2  | nFFS  |  10
//--------------------------------------------



namespace rfm69Base
{
    public class rfm
    {


        // RFM69 Registers
        private const byte RegFifo = 0x00;
        private const byte RegOpMode = 0x01;
        private const byte RegDataModul = 0x02;
        private const byte RegBitrateMsb = 0x03;
        private const byte RegBitrateLsb = 0x04;
        private const byte RegFdevMsb = 0x05;
        private const byte RegFdevLsb = 0x06;
        private const byte RegFrfMsb = 0x07;
        private const byte RegFrfMid = 0x08;
        private const byte RegFrfLsb = 0x09;
        private const byte RegPaLevel = 0x11;
        private const byte RegLna = 0x18;
        private const byte RegRxBw = 0x19;
        private const byte RegAfcBw = 0x1A;
        private const byte RegAfcFei = 0x1E;
        private const byte RegAfcMsb = 0x1F;
        private const byte RegAfcLsb = 0x20;
        private const byte RegFeiMsb = 0x21;
        private const byte RegFeiLsb = 0x22;
        private const byte RegRssiConfig = 0x23;
        private const byte RegRssiValue = 0x24;
        private const byte RegDioMapping1 = 0x25;
        private const byte RegDioMapping2 = 0x26;
        private const byte RegIrqFlags1 = 0x27;
        private const byte RegIrqFlags2 = 0x28;
        private const byte RegRssiThresh = 0x29;
        private const byte RegPreambleMsb = 0x2C;
        private const byte RegPreambleLsb = 0x2D;
        private const byte RegSyncConfig = 0x2E;
        private const byte RegSyncValue1 = 0x2F;     
        private const byte RegPacketConfig1 = 0x37;
        private const byte RegPayloadLength = 0x38;      
        private const byte RegFifoThresh = 0x3C;
        private const byte RegPacketConfig2 = 0x3D;
        private const byte RegTestDagc = 0x6F;

        private const byte InterPacketRxDelay = 4; // Bitposition
        private const byte RestartRx = 2;
        private const byte AutoRxRestartOn = 1;
        private const byte AesOn = 0;


        //Modulation type
        public enum modulationType
        {
            FSK = 0,
            OOK
        };

        //RFM69 modes
        private const byte MODE_SLEEP = 0;
        private const byte MODE_STDBY = 1;
        private const byte MODE_FS = 2;
        private const byte MODE_TX = 3;
        private const byte MODE_RX = 4;

        //other constants
        private const double FXOSC = 32e6;
        private const double FSTEP = FXOSC / (1<<19);

        private SpiDevice device;
        private SpiConnectionSettings settings;
        private bool rfmStatus = true;

        private GpioController controller;
        private GpioPin interruptPin;

        AutoResetEvent stopWaitHandle = new AutoResetEvent(false);

        private rfm ()
        {
            init();
            config();
            defaults();
        }

        private static rfm RFM69;

        public static rfm getRFM()
        {
            if (RFM69 == null)
                RFM69 = new rfm();
            return RFM69;
        }

        public void defaults()
        {
            setFrequency(433.92);
            setDatarate(2.66666666666);
            setModulationType(rfm.modulationType.OOK);
            setTXPower(13);
            byte[] syncPattern = new byte[0];
            setSyncPattern(syncPattern);
        }

        private async Task initSPI()
        {
            try {
                // Get a selector string for bus "SPI0"
                string aqs = SpiDevice.GetDeviceSelector("SPI0");

                // Find the SPI bus controller device with our selector string
                var dis = await DeviceInformation.FindAllAsync(aqs);
                if (dis.Count == 0)
                {
                    rfmStatus = false;
                    return; // "SPI0" not found on this system
                }

                // Use chip select line CS0
                settings = new SpiConnectionSettings(0);
                settings.ClockFrequency = Convert.ToInt32(5e6);
                settings.DataBitLength = 8;

                // Create an SpiDevice with our bus controller and SPI settings
                device = await SpiDevice.FromIdAsync(dis[0].Id, settings);
                if (device == null)
                    rfmStatus = false;
            }
            catch (Exception ex)
            {
                rfmStatus = false;
                throw new Exception("SPI Initialization Failed", ex);
            }
            return;
        }

        private async Task initGpio()
        {
            controller = await GpioController.GetDefaultAsync();
            if (controller == null)
            {
                rfmStatus = false;
                return;
            }
         
            interruptPin = controller.OpenPin(25);
            interruptPin.SetDriveMode(GpioPinDriveMode.Input);
             
        }

        private void config ()
        {
            writeReg(RegDataModul, 0x00);  // packet mode, modulation shaping, modulation
            writeReg(RegPayloadLength, 0x00);
            writeReg(RegPreambleMsb, 0x00);
            writeReg(RegPreambleLsb, 0x00);
            writeReg(RegSyncConfig, 0x00);   // sync off
            writeReg(RegPacketConfig1, 0x00);  // Fixed length, CRC off, no adr
            writeReg(RegPacketConfig2, 0x00); // 1<<AutoRxRestartOn
            writeReg(RegAfcFei, 1 << 3 | 1 << 1 | 0 << 2);  // AFC auto clear, clear AFC, afcAutoOn
            writeReg(RegTestDagc, 0x30);
            writeReg(RegRssiThresh, 0xE0);
            writeReg(RegFifoThresh, 0x8F);
            writeReg(RegBitrateMsb, 0x1A);
            writeReg(RegBitrateLsb, 0x0B);

            writeReg(RegOpMode, MODE_STDBY << 2);
            waitMode();
        }

        private void pinValueChanged(object sender, GpioPinValueChangedEventArgs evArgs)
        {
            if (evArgs.Edge == GpioPinEdge.RisingEdge)
            {
#if DEBUG
                Debug.WriteLine("GPIO Interrupt Pin RisingEdge detected by {0}", sender);
#endif
                stopWaitHandle.Set();
            }
        }


        private bool init ()
        {
            initSPI().Wait();
            initGpio().Wait();
            if (rfmStatus == false)
                return false;


            byte[] testBytes = { 0x55, 0xaa };
            // test presence of module
            for (int j = 0; j < 2; j++)
            {
                byte testByte = testBytes[j];
                for (int i = 0; i < 8; i++)
                {
                    byte register = (byte)(RegSyncValue1 + i);
                    writeReg((byte)(register), testByte);
                    byte status = readReg(register);
                    if (status != testByte)
                    {
                        rfmStatus = false;
                        return false;
                    }
                }
            }
            return true;

        }


        private void writeReg (byte register, byte value)
        {
            byte[] writebuf = { 0x7f, 0xff};
            byte[] readbuf = { 0x00, 0x00 };
            writebuf[0] &= register; // set highest bit to zero keep rest unchanged
            writebuf[0] |= 0x80;     // set highest bit to one keep rest unchanged <- highest bit equals one marks write operation to rfm69
            writebuf[1] &= value;
            device.TransferFullDuplex(writebuf, readbuf);
        }


        private void setReg (byte register, byte mask, byte value)
        {
            byte tmp = readReg(register);
            tmp &= (byte) (~ mask);
            tmp |= (byte) (value & mask);
            writeReg(register, tmp);

        }

        private void writeFrame(byte register, byte[] values)
        {
            int l = values.Length;
            byte[] writebuf = new byte[l + 1];
            byte[] readbuf = new byte[l + 1];
            writebuf[0] = 0x7f;
            writebuf[0] &= register;
            writebuf[0] |= 0x80;
            readbuf[0] = 0x00;
            for (int i = 1; i <= l; i++)
            {
                writebuf[i] = values[i - 1];
                readbuf[i] = 0x00;
            }
            device.TransferFullDuplex(writebuf, readbuf);
        }

        private void writeFrame2(byte register, byte[] values)
        {
            int l = values.Length;
            for (int i = 0; i < l; i++)
            {
                writeReg(register, values[i]);
            }
        }

        private void setDioMapping(int dio, int mapping)
        {
            if ((dio >= 0) && (dio <= 3))
                setReg(RegDioMapping1, (byte)(0xc0 >> (dio * 2)),
                                        (byte)(mapping << (6 - dio * 2)));
            else if (dio == 5)
                setReg(RegDioMapping2, (byte)(0x03 << 4), (byte)(mapping << 4));
        }

        private byte readReg (byte register)
        {
            byte[] regBuf = new byte[2];
            byte[] resBuf = new byte[2];
            regBuf[0] = (byte) (register & 0x7f);       // set highest bit to zero keep rest unchanged <- highest bit zero marks read operation to rfm69
            device.TransferFullDuplex(regBuf, resBuf);
            return resBuf[1];
        }

        private byte readRSSIValue ()
        {
            writeReg(RegRssiConfig, 0x01);
            while ((readReg(RegRssiConfig) & 0x02) == 0)
                continue;
            byte result = readReg(RegRssiValue);
            return result;
        }

        private void waitMode ()
        {
            byte b = (1 << 7);
            while ((readReg(RegIrqFlags1) & b) == 0)
                continue;
        }

        private void debugOutput ()
        {
            byte status = readReg(RegIrqFlags2);
            Debug.WriteLine("FifoFull: {0}", (status & 0x80) != 0x00);
            Debug.WriteLine("FifoNotEmpty: {0}", (status & 0x40) != 0x00);
            Debug.WriteLine("FifoLevel: {0}", (status & 0x20) != 0x00);
            Debug.WriteLine("FifoOverrun: {0}", (status & 0x10) != 0x00);
            Debug.WriteLine("PacketSent: {0}", (status & 0x08) != 0x00);
            Debug.WriteLine("PayloadReady: {0}", (status & 0x04) != 0x00);
            Debug.WriteLine("CrcOk: {0}", (status & 0x02) != 0x00);

            status = readReg(RegDataModul);
            int dataMode = (status & 0x60) >> 5;
            string mode =
                dataMode == 3 ? "Continuous mode without bit synchronizer" :
                dataMode == 2 ? "Continuous mode with bit synchronizer" :
                dataMode == 1 ? "reserved" :
                "Packet mode";
            string output = "DataMode: " + mode;
            Debug.WriteLine(output);
            int modulationType = (status & 0x18) >> 3;
            mode =
                modulationType >= 2 ? "reserved" :
                modulationType == 1 ? "OOK" : "FSK";
            output = "ModulationType: " + mode;
            Debug.WriteLine(output);
            int modulationShaping = status & 0x03;
            Debug.WriteLine("ModulationShaping: {0}", modulationShaping);

            status = readReg(RegFrfMsb);
            int frf = status;
            status = readReg(RegFrfMid);
            frf = frf * 256 + status;
            status = readReg(RegFrfLsb);
            frf = frf * 256 + status;
            Debug.WriteLine("Frf: {0}", frf);
            Debug.WriteLine("FSTEP: {0}", FSTEP);
            Debug.WriteLine("FRF: {0}", FSTEP * frf);

            status = readReg(RegPaLevel);
            bool Pa0On = (status & 0x80) != 0x00;
            bool Pa1On = (status & 0x40) != 0x00;
            bool Pa2On = (status & 0x20) != 0x00;
            int outputPower = (status & 0x1F);
            Debug.WriteLine("Pa0On: {0}", Pa0On);
            Debug.WriteLine("Pa1On: {0}", Pa1On);
            Debug.WriteLine("Pa2On: {0}", Pa2On);
            Debug.WriteLine("OutputPower: {0}", -18 + outputPower);

            status = readReg(RegBitrateMsb);
            int BitRateDenom = status;
            status = readReg(RegBitrateLsb);
            BitRateDenom = 256 * BitRateDenom + status;
            Debug.WriteLine("BitRate: {0}", FXOSC / BitRateDenom);

            status = readReg(RegOpMode);
            int sequencerOff = status & 0x80;
            string seqOutput = sequencerOff != 0 ?
                "SequencerOff: Mode is forced by the user" :
                "SequencerOff: Operating mode as selected with Mode bits in RegOpMode";
            Debug.WriteLine(seqOutput);
            int listenOn = status & 0x40;
            Debug.WriteLine("ListenOn: {0}", listenOn);
            int opMode = (status & 0x1c) >> 2;
            string opModeOutput =
                opMode == 0 ? "Mode: SLEEP" :
                opMode == 1 ? "Mode: STDBY" :
                opMode == 2 ? "Mode: FS" :
                opMode == 3 ? "Mode: TX" :
                opMode == 4 ? "Mode: RX" :
                "Mode: reserved";
            Debug.WriteLine(opModeOutput);
        }

        public void sendPacket (byte [] data)
        {
#if DEBUG
            Debug.WriteLine("rfm69: start sending the packet");
            debugOutput();
#endif
            writeReg(RegOpMode, MODE_STDBY << 2);
            waitMode();

            // flush data
            byte status = readReg(RegIrqFlags2);

            while (((byte)(status & 0x40)) == 0x40)
            {
                readReg(RegFifo);
                status = readReg(RegIrqFlags2);
            }

            interruptPin.ValueChanged += pinValueChanged;
            int length = data.Length;
            if (length > 64)
                return;

            writeReg(RegPayloadLength, 0x00);
           
            setDioMapping(0, 0); // DIO0 -> PacketSent
            writeFrame(RegFifo, data);
            writeReg(RegOpMode, (byte)(MODE_TX << 2)); // TX Mode
#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif

            // wait until packet sent
            stopWaitHandle.WaitOne();

#if DEBUG
            stopWatch.Stop();
            Debug.WriteLine ("rfm69: {0}s waited until send completed", stopWatch.ElapsedMilliseconds / 1000.0);
#endif

            interruptPin.ValueChanged -= pinValueChanged;

            writeReg(RegOpMode, (byte)(MODE_STDBY << 2));
            waitMode();            
        }

        public byte[] receivePacket(int length)
        {
            interruptPin.ValueChanged += pinValueChanged;
            writeReg(RegPayloadLength, (byte)length);
            setDioMapping(0, 1); // DIO0 -> PayLoadready
            setDioMapping(1, 3);
            setReg(RegOpMode, (byte)(0x07 << 2), (byte)(MODE_RX << 2)); //RX mode

#if DEBUG
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
#endif
            stopWaitHandle.WaitOne();

#if DEBUG
            stopWatch.Stop();
            Debug.WriteLine ("rfm69: {0}s waited until receive completed", stopWatch.ElapsedMilliseconds / 1000.0);
#endif

            byte[] result = new byte[length];
            for (int i = 0; i < length; i++)
                result[i] = readReg(RegFifo);

            interruptPin.ValueChanged -= pinValueChanged;
            writeReg(RegOpMode, 0x00); // idle mode
#if DEBUG
            Debug.WriteLine("rfm69: receiving packet completed");
#endif
            return result;
        }

        public void setFrequency (double value)
        {
            int fword = Convert.ToInt32(Math.Round(value * 1e6 / FSTEP));
            writeReg(RegFrfLsb, (byte)fword);
            writeReg(RegFrfMid, (byte) (fword >> 8));
            writeReg(RegFrfMsb, (byte) (fword >> 16));
        }

        public void setTXPower (int power)
        {
            int pwr = power + 18;
            writeReg(RegPaLevel, ((byte)(0x80 | (pwr & 0x1F))));
        }

        public void setDatarate (double rate)
        {
            int rte = Convert.ToInt32(Math.Round(FXOSC / (rate * 1000)));
            int high = rte / 256;
            int low = rte % 256;
            byte hByte = (byte)high;
            byte lByte = (byte)low;
            writeReg(RegBitrateMsb, hByte);
            writeReg(RegBitrateLsb, lByte);
        }

        public void setDeviation (double deviation)
        {
            int dev = Convert.ToInt32(Math.Round(deviation * 1000 / FSTEP));
            int high = dev / 256;
            int low = dev % 256;
            byte hByte = (byte)high;
            byte lByte = (byte)low;
            writeReg(RegFdevMsb, hByte);
            writeReg(RegFdevLsb, lByte);
        }

        public void setModulationType(modulationType value)
        {
            byte v = (byte)value;
            setReg(RegDataModul, 0x18, (byte)(v << 3));
        }

        public void setModulationsShaping(byte value)
        {
            setReg(RegDataModul, 0x03, value);
        }

        public void setSyncPattern (byte [] pattern)
        {
            byte conf = 0x00;
            int len = pattern.Length;
            if (len > 8)
                len = 8;

            if (len > 0)
            {
                byte tmp = (byte)(len - 1);        // regSyncConfig uses SyncSize - 1
                conf = (byte) ((tmp & 0x07) << 3); // syncSize is bit 3-5
                conf |= (byte) (1 << 7);           // bit7: SyncOn is 1
            }
            else
            {
                conf = (byte)(1 << 6); // bit7: SyncOn is 0, bit6: FifoFillCondition is 1
            }
            writeReg(RegSyncConfig, conf);
            for (int i = 0; i < len; i++)
                writeReg ((byte) (RegSyncValue1 + i), pattern[i]);
        }

        public void setBandwidth (double bandwidth)
        {
            double RxBw = FXOSC / bandwidth / 1000 / 4;
            int e = 0;
            while ((RxBw > 32) && (e < 7))
            {
                e += 1;
                RxBw /= 2;
            }
            RxBw = RxBw / 4 - 4;
            RxBw = RxBw > 0 ? RxBw : 0;
            int m = Convert.ToInt16(RxBw);
            setReg(RegRxBw, 0x1F, (byte)(m << 3 | e));
            setReg(RegAfcBw, 0x1F, (byte)(m << 3 | e));
        }

        public void setPreamble (int value)
        {
            int high = value / 256;
            int low = value % 256;
            byte hByte = (byte)high;
            byte lByte = (byte)low;
            writeReg(RegPreambleMsb, hByte);
            writeReg(RegPreambleLsb, hByte);
        }

        public void setLnaGain(byte value)
        {
            setReg(RegLna, 0x03, value);
        }

        public void setRssiThresh(byte value)
        {
            int th = -(value * 2);
            writeReg(RegRssiThresh, (byte) th);
        }
    }
}
