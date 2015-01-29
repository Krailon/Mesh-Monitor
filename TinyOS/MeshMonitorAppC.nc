configuration MeshMonitorAppC {}

implementation {
	// Data
	components MainC, MeshMonitorC, LedsC;
	components new TimerMilliC();
	components new Taos2550C() as LightSensor;
	components new Accel202C() as Accelerometer;
	components new SensirionSht11C() as HumidTempSensor;
	//components new Intersema5534C() as BaroPressureSensor;

	MeshMonitorC.Boot -> MainC.Boot;
	MeshMonitorC.Timer -> TimerMilliC;
	MeshMonitorC.IRLight -> LightSensor.InfraredLight;
	MeshMonitorC.VLight -> LightSensor.VisibleLight;
	MeshMonitorC.AccelX -> Accelerometer.X_Axis;
	MeshMonitorC.AccelY -> Accelerometer.Y_Axis;
	MeshMonitorC.Humid -> HumidTempSensor.Humidity;
	MeshMonitorC.Temp -> HumidTempSensor.Temperature;
	//MeshMonitorC.BaroPress -> BaroPressureSensor;
	MeshMonitorC.Leds -> LedsC;

	// Comms
	components CollectionC as Collector,
	ActiveMessageC,
	new CollectionSenderC(AM_MESH_MONITOR),
	SerialActiveMessageC,
	new SerialAMSenderC(AM_MESH_MONITOR);

	MeshMonitorC.RadioControl -> ActiveMessageC;
	MeshMonitorC.SerialControl -> SerialActiveMessageC;
	MeshMonitorC.RoutingControl -> Collector;
	MeshMonitorC.Send -> CollectionSenderC;
	MeshMonitorC.SerialSend -> SerialAMSenderC.AMSend;
	MeshMonitorC.Snoop -> Collector.Snoop[AM_MESH_MONITOR];
	MeshMonitorC.Receive -> Collector.Receive[AM_MESH_MONITOR];
	MeshMonitorC.RootControl -> Collector;

	// Serial
	components new PoolC(message_t, 10) as UARTMessagePoolP, new QueueC(message_t*, 10) as UARTQueueP;

	MeshMonitorC.UARTMessagePool -> UARTMessagePoolP;
	MeshMonitorC.UARTQueue -> UARTQueueP;

	components new PoolC(message_t, 20) as DebugMessagePool,
	new QueueC(message_t*, 20) as DebugSendQueue,
	new SerialAMSenderC(AM_CTP_DEBUG) as DebugSerialSender,
	UARTDebugSenderP as DebugSender;

	DebugSender.Boot -> MainC;
	DebugSender.UARTSend -> DebugSerialSender;
	DebugSender.MessagePool -> DebugMessagePool;
	DebugSender.SendQueue -> DebugSendQueue;
	Collector.CollectionDebug -> DebugSender;
}
