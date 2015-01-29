namespace TRU.MeshMonitor {
	using com.ibm.saguaro.system;
	using com.ibm.iris;
	
	public class MeshMonitor {
		
		// Globals
		private const uint YELLOW_LED = 0;
		private const uint GREEN_LED = 1;
		private const uint RED_LED = 2;
		private const byte TEMP_PWR_PIN = IRIS.PIN_PW0; // PC0
		private const byte LIGHT_PWR_PIN = IRIS.PIN_INT5; // PE5
		private const uint ADC_CHANNEL_MASK = 0x02;
		private const uint REPLY_SIZE = 0x40; // 64
		
		private static byte[] Reply = new byte[REPLY_SIZE];
		private static ADC ADC_Device = new ADC();
		private static GPIO GPIO_Device = new GPIO();

		static MeshMonitor() {
			LED.setState(YELLOW_LED, 1);

			Assembly.setDataHandler(onData);
			GPIO_Device.open();
			GPIO_Device.configureOutput(TEMP_PWR_PIN, GPIO.OUT_SET);
			ADC_Device.open(ADC_CHANNEL_MASK, GPIO.NO_PIN, 0, 0); // Manual power; No warmup; No interval (ltr)
			ADC_Device.setReadHandler(ADCReadCallback);
			ADV_Device.read(Device.TIMED, 1, Time.currentTicks() + Time.toTickSpan(Time.SECONDS, 2));

			LED.setState(YELLOW_LED, 0); 
			LED.setState(GREEN_LED, 1);
		}

		private static int onData(uint Info, byte[] Buffer, uint Length) {
			Util.copyData(Buffer, 0, Reply, 0, Length);
			return 0;
		}
	}
}
