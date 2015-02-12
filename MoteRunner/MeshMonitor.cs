namespace TRU.MeshMonitor {
	using com.ibm.saguaro.system;
	using com.ibm.iris;
	
	public class MeshMonitor {
		
		// Globals
		private const uint ADC_CHANNEL_MASK = 0x02;
		private const uint REPLY_SIZE = 0x40; // 64
		private const uint READ_INTERVAL = 5; // 5s 
		private const byte TEMP_PWR_PIN = IRIS.PIN_PW0; // PC0
		private const byte LIGHT_PWR_PIN = IRIS.PIN_INT5; // PE5
		
		private static byte[] Reply = new byte[REPLY_SIZE];
		private static ADC ADC_Device = new ADC();
		private static GPIO GPIO_Device = new GPIO();

		static MeshMonitor() {
			LED.setState(IRIS.LED_YELLOW, 1);

			Assembly.setDataHandler(onData);
			GPIO_Device.open();
			GPIO_Device.configureOutput(LIGHT_PWR_PIN, GPIO.OUT_SET);
			ADC_Device.open(ADC_CHANNEL_MASK, GPIO.NO_PIN, 0, 0); // Manual power; No warmup; No interval (ltr)
			ADC_Device.setReadHandler(ADCReadCallback);
			ADC_Device.read(Device.TIMED, 1, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, READ_INTERVAL));

			LED.setState(IRIS.LED_YELLOW, 0);
			LED.setState(IRIS.LED_GREEN, 1);
		}

		private static int onData(uint Info, byte[] Buffer, uint Length) {
			Util.copyData(Buffer, 0, Reply, 0, Length);
			return 0;
		}

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

	}

}
