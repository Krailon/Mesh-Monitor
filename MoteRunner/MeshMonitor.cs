namespace TRU.MeshMonitor {
	using com.ibm.saguaro.system;
	using com.ibm.iris;
	
	public class MeshMonitor {
		
		// Globals
        private const uint FLAG_FAILED = 0x40;
		private const uint ADC_CHANNEL_MASK = 0x02;
		private const uint REPLY_SIZE = 0x40; // 64
		private const uint READ_INTERVAL = 5; // 5s
		private const byte TEMP_PWR_PIN = IRIS.PIN_PW0; // PC0
		private const byte LIGHT_PWR_PIN = IRIS.PIN_INT5; // PE5
		
		private static byte[] Reply = new byte[REPLY_SIZE];
		private static ADC ADC_Device = new ADC();
		private static GPIO GPIO_Device = new GPIO();

        private static SDev HumidTempSensor = new SDev();
        private static SDev LightSensor = new SDev();
        //private static SDev AccelSensor = new SDev();

		static MeshMonitor() {
			LED.setState(IRIS.LED_YELLOW, 1);

			Assembly.setDataHandler(on_Data);
            Assembly.setSystemInfoCallback(on_SysInfo);
            try {
                // Init read handlers
                LightSensor.setReadHandler(LightCallback);
                HumidTempSensor.setReadHandler(HumidTempCallback);

                HumidTempSensor.open(IRIS.DID_MTS400_HUMID_TEMP, null, 0, 0);
                HumidTempSensor.read(Device.TIMED, 4, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));

                LED.setState(IRIS.LED_YELLOW, 0);
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
                    if (HumidTempSensor != null)
                    {
                        HumidTempSensor.close();
                    }
                    if (LightSensor != null)
                    {
                        LightSensor.close();
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
                Util.copyData(new byte[] {0}, 0, Reply, payload_offset, 1); // Humidity+Temperature ID = 0
                Util.copyData(ReadData, 0, Reply, payload_offset + 1, ReadLength);

                if (HumidTempSensor.getState() != Device.S_CLOSED) {
                    HumidTempSensor.close();
                }

                // Queue light sensor read
                LightSensor.open(IRIS.DID_MTS400_LIGHT, null, 0, 0);
                LightSensor.read(Device.ASAP, 2, 0);

                LED.setState(IRIS.LED_YELLOW, 0);
            }

            return 0;
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

                // Copy sensor data into reply buffer and send
                LED.setState(IRIS.LED_YELLOW, 1);
                Util.copyData(new byte[] {1}, 0, Reply, payload_offset + 5, 1); // Light ID = 1
                Util.copyData(ReadData, 0, Reply, payload_offset + 6, ReadLength);
                LIP.send(Reply, 0, REPLY_SIZE);

                if (LightSensor.getState() != Device.S_CLOSED)
                {
                    LightSensor.close();
                }

                // Queue humidity/temperature sensor read
                //HumidTempSensor.open(IRIS.DID_MTS400_HUMID_TEMP, null, 0, 0);
                //HumidTempSensor.read(Device.TIMED, 4, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));
                
                LED.setState(IRIS.LED_YELLOW, 0);
            }

            return 0;
        }

		/*
        private static int ADCReadCallback(uint ReadFlags, byte[] ReadData, uint ReadLength, uint ReadInfo, long ReadTime) {
			uint offset = LIP.getPortOff() + 1;
			byte[] sensor;

			LED.setState(IRIS.LED_YELLOW, 1);
			if (GPIO_Device.doPin(GPIO.CTRL_READ, TEMP_PWR_PIN) == 0) {
				sensor = new byte[] {0, 0}; // 0 => Temperature
				GPIO_Device.configureOutput(TEMP_PWR_PIN, GPIO.OUT_SET);
				GPIO_Device.configureOutput(LIGHT_PWR_PIN, GPIO.OUT_CLR);
			}
			else {
				sensor = new byte[] {1, 0}; // 1 => Light
				GPIO_Device.configureOutput(LIGHT_PWR_PIN, GPIO.OUT_SET);
				GPIO_Device.configureOutput(TEMP_PWR_PIN, GPIO.OUT_CLR);
			}

			Util.copyData(sensor, 0, Reply, offset, 2);
			Util.copyData(ReadData, 0, Reply, offset + 2, 2);
			LIP.send(Reply, 0, REPLY_SIZE);

            ADC_Device.read(Device.TIMED, 1, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));
			LED.setState(IRIS.LED_YELLOW, 0);
            return 0;
		}
         */

	}

}
