namespace TRU.MeshMonitor {
    using System.Security.Cryptography;
	using com.ibm.saguaro.system;
	using com.ibm.iris;
	
	public class MeshMonitor {
		
		// Globals
        private const uint LIP_PORT = 0x66;
        private const uint FLAG_FAILED = 0x40;
		private const uint REPLY_SIZE = 0x40; // 64
		private const uint READ_INTERVAL = 5; // 5s
		
		private static byte[] Reply = new byte[REPLY_SIZE];
        private static SDev HumidTempSensor = new SDev();
        private static SDev LightSensor = new SDev();
        private static SDev AccelSensor = new SDev();

		static MeshMonitor() {
			Assembly.setDataHandler(on_Data);
            Assembly.setSystemInfoCallback(on_SysInfo);
            LIP.open(LIP_PORT);

            try {
                // Init read handlers
                LightSensor.setReadHandler(LightCallback);
                HumidTempSensor.setReadHandler(HumidTempCallback);
                AccelSensor.setReadHandler(AccelCallback);

                HumidTempSensor.open(IRIS.DID_MTS400_HUMID_TEMP, null, 0, 0);
                HumidTempSensor.read(Device.TIMED, 4, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));

			    LED.setState(IRIS.LED_GREEN, 1);
            }
            catch {
                LED.setState(IRIS.LED_RED, 1);
                return;
            }
        }

		private static int on_Data(uint Info, byte[] Buffer, uint Length) {
			Util.copyData(Buffer, 0, Reply, 0, Length);
			return 0;
		}

        private static int on_SysInfo(int Type, int Info)
        {
            if (Type == Assembly.SYSEV_DELETED)
            {
                try
                {
                    LIP.close(LIP_PORT);

                    if (HumidTempSensor != null)
                    {
                        HumidTempSensor.close();
                    }
                    if (LightSensor != null)
                    {
                        LightSensor.close();
                    }
                    if (AccelSensor != null)
                    {
                        AccelSensor.close();
                    }
                }
                catch (MoteException ex)
                {
                }
            }

            return 0;
        }

        private static int HumidTempCallback(uint ReadFlags, byte[] ReadData, uint ReadLength, uint ReadInfo, long ReadTime) {
            if ((ReadFlags & FLAG_FAILED) != 0)
            {
                LED.setState(IRIS.LED_YELLOW, 1);
                LED.setState(IRIS.LED_RED, 1);
                return -1;
            }
            else {
                uint payload_offset = LIP.getPortOff() + 1;

                // Copy sensor data into reply buffer
                LED.setState(IRIS.LED_YELLOW, 1);
                Util.copyData(new byte[] { 0x11, 0x22, 0x33, 0x44, 0x55 }, 0, Reply, payload_offset, 1); // debug
                //Util.copyData(new byte[] {11}, 0, Reply, payload_offset, 1); // Humidity+Temperature ID = 11
                //Util.copyData(ReadData, 0, Reply, payload_offset + 1, ReadLength);

                try {
                    if (HumidTempSensor.getState() != Device.S_CLOSED) {
                        HumidTempSensor.close();
                    }

                    // Queue light sensor read
                    LightSensor.open(IRIS.DID_MTS400_LIGHT, null, 0, 0);
                    LightSensor.read(Device.ASAP, 2, 0);
                }
                catch (MoteException ex) {
                    LED.setState(IRIS.LED_RED, 1);
                    return -1;
                }

                LED.setState(IRIS.LED_YELLOW, 0);
                return 0;
            }
        }

        private static int LightCallback(uint ReadFlags, byte[] ReadData, uint ReadLength, uint ReadInfo, long ReadTime) {
            // Check flags
            if ((ReadFlags & FLAG_FAILED) != 0) {
                LED.setState(IRIS.LED_YELLOW, 1);
                LED.setState(IRIS.LED_RED, 1);
                return -1;
            }
            else {
                uint payload_offset = LIP.getPortOff() + 1;

                // Copy sensor data into reply buffer
                LED.setState(IRIS.LED_YELLOW, 1);
                Util.copyData(new byte[] { 0x66, 0x77, 0x88 }, 0, Reply, payload_offset + 5, 1); // Debug
                //Util.copyData(new byte[] {22}, 0, Reply, payload_offset + 5, 1); // Light ID = 22
                //Util.copyData(ReadData, 0, Reply, payload_offset + 6, ReadLength);

                try {
                    if (LightSensor.getState() != Device.S_CLOSED) {
                        LightSensor.close();
                    }

                    // Queue accelerometer sensor read
                    AccelSensor.open(IRIS.DID_MTS400_ACCEL, null, 0, 0);
                    AccelSensor.read(Device.ASAP, 4, 0);
                }
                catch (MoteException ex) {
                    LED.setState(IRIS.LED_RED, 1);
                    return -1;
                }
                
                LED.setState(IRIS.LED_YELLOW, 0);
                return 0;
            }
        }

        private static int AccelCallback(uint ReadFlags, byte[] ReadData, uint ReadLength, uint ReadInfo, long ReadTime)
        {
            if ((ReadFlags & FLAG_FAILED) != 0)
            {
                LED.setState(IRIS.LED_YELLOW, 1);
                LED.setState(IRIS.LED_RED, 1);
                return -1;
            }
            else
            {
                uint payload_offset = LIP.getPortOff() + 1;

                // Copy sensor data into reply buffer and send
                LED.setState(IRIS.LED_YELLOW, 1);
                Util.copyData(new byte[] {0x99, 0xAA, 0xBB, 0xCC, 0xDD}, 0, Reply, payload_offset + 8, 1); // debug
                //Util.copyData(new byte[] {33}, 0, Reply, payload_offset + 8, 1); // Acceleration ID = 33
                //Util.copyData(ReadData, 0, Reply, payload_offset + 9, ReadLength);
                LIP.send(Reply, 0, REPLY_SIZE);

                try
                {
                    if (AccelSensor.getState() != Device.S_CLOSED)
                    {
                        AccelSensor.close();
                    }

                    // Queue humidity/temperature sensor read
                    HumidTempSensor.open(IRIS.DID_MTS400_HUMID_TEMP, null, 0, 0);
                    HumidTempSensor.read(Device.TIMED, 4, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));
                }
                catch (MoteException ex)
                {
                    LED.setState(IRIS.LED_RED, 1);
                    return -1;
                }

                LED.setState(IRIS.LED_YELLOW, 0);
                return 0;
            }
        }

	}

}
