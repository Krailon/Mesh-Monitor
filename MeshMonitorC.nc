#include "Timer.h"
#include "MeshMonitor.h"

module MeshMonitorC @safe() {
	uses {
		// Init
		interface Boot;
		interface SplitControl as RadioControl;
		interface SplitControl as SerialControl;
		interface StdControl as RoutingControl;

		// Comms
		interface Send;
		interface Receive as Snoop;
		interface Receive;
		interface AMSend as SerialSend;
		interface CollectionPacket;
		interface RootControl;
		interface Queue<message_t *> as UARTQueue;
		interface Pool<message_t> as UARTMessagePool;

		// Data
		interface Timer<TMilli>;
		interface Read<uint8_t> as IRLight;
		interface Read<uint8_t> as VLight;
		interface Read<uint16_t> as AccelX;
		interface Read<uint16_t> as AccelY;
		interface Read<uint16_t> as Humid;
		interface Read<uint16_t> as Temp;
		//interface Read<uint16_t> as BaroPress;
		interface Leds;
	}
}

implementation {
	task void uartSendTask();
	static void startTimer();
	static void sendData();
	static void fatal();

	message_t sendBuff, uartBuff;
	bool sendBusy = FALSE, uartBusy = FALSE;
	uint16_t sampleInterval = SAMPLE_INTERVAL;
	uint8_t dataLen = sizeof(reading_msg_t);

	uint8_t vlight, irlight;
	uint16_t accelx, accely, temp, humid;

	event void Boot.booted() {
		if (call RadioControl.start() != SUCCESS)
			fatal();

		if (call RoutingControl.start() != SUCCESS)
			fatal();
	}

	event void RadioControl.startDone(error_t err) {
		if (err != SUCCESS)
			fatal();

		if (dataLen > call Send.maxPayloadLength())
			fatal();

		if (call SerialControl.start() != SUCCESS)
			fatal();
	}

	event void SerialControl.startDone(error_t err) {
		if (err != SUCCESS)
			fatal();

		// Detect and set root
		if (TOS_NODE_ID % 100 == 0)
			call RootControl.setRoot();
		else
			startTimer();
	}

	static void startTimer() {
		if (call Timer.isRunning())
			call Timer.stop();

		call Timer.startPeriodic(sampleInterval);
	}

	event void RadioControl.stopDone(error_t err) {}

	event void SerialControl.stopDone(error_t err) {}

	// Only root deals with this
	event message_t* Receive.receive(message_t* msg, void* payload, uint8_t len) {
		reading_msg_t* rec = (reading_msg_t*)payload;
		reading_msg_t* buff;

		if (uartBusy == FALSE) {
			buff = (reading_msg_t*)call SerialSend.getPayload(&uartBuff, dataLen);
			if (len != dataLen || buff == NULL)
				return msg;

			//memcpy(buff, rec, dataLen);
			buff->nodeid = rec->nodeid;
			buff->vlight = rec->vlight;
			buff->irlight = rec->irlight;
			buff->temp = rec->temp;
			//buff->humid = rec->humid;
			//buff->accelx = rec->accelx;
			//buff->accely = rec->accely;

			post uartSendTask();
		}
		else {
			// uart is busy, queue it up
			message_t* qMsg = call UARTMessagePool.get();
			if (qMsg == NULL)
				return msg;

			buff = (reading_msg_t*)call SerialSend.getPayload(qMsg, dataLen);
			if (buff == NULL)
				return msg;

			//memcpy(buff, rec, dataLen);
			buff->nodeid = rec->nodeid;
			buff->vlight = rec->vlight;
			buff->irlight = rec->irlight;
			//buff->accelx = rec->accelx;
			//buff->accely = rec->accely;
			buff->temp = rec->temp;
			//buff->humid = rec->humid;

			if (call UARTQueue.enqueue(qMsg) != SUCCESS) {
				call UARTMessagePool.put(qMsg);
				fatal();
			}
		}

		return msg;
	}

	task void uartSendTask() {
		if (call SerialSend.send(0xffff, &uartBuff, dataLen) == SUCCESS)
			uartBusy = TRUE;
	}

	event void SerialSend.sendDone(message_t* msg, error_t err) {
		uartBusy = FALSE;
		if (call UARTQueue.empty() == FALSE) {
			// Send off the next message in the queue
			message_t* qMsg = call UARTQueue.dequeue();
			if (qMsg == NULL) {
				fatal();
				return;
			}

			memcpy(&uartBuff, qMsg, dataLen);
			if (call UARTMessagePool.put(qMsg) != SUCCESS) {
				fatal();
				return;
			}

			post uartSendTask();
		}
	}

	// Listening for other nodes' messages
	event message_t* Snoop.receive(message_t* msg, void* payload, uint8_t len) {
		return msg;
	}

	event void Timer.fired() {
		call Leds.led0Off();
		call Leds.led1Off();
		call Leds.led2Off();

		// Initiate sensor read
		if (call VLight.read() != SUCCESS)
			call Leds.led0On();
	}

	event void VLight.readDone(error_t result, uint8_t data) {
		if (result == SUCCESS) {
			vlight = data;
			call IRLight.read();
		}
		else {
			call Leds.led0On();
		}
	}

	event void IRLight.readDone(error_t result, uint8_t data) {
		if (result == SUCCESS) {
			irlight = data;
			call Temp.read(); //AccelX.read();
		}
		else {
			call Leds.led0On();
		}
	}

	event void AccelX.readDone(error_t result, uint16_t data) {
		//if (result == SUCCESS) {
		//accelx = data;
		//call AccelY.read();
		//}
		//else {
			//call Leds.led0On();
			//call Leds.led2On();
		//}
	}

	event void AccelY.readDone(error_t result, uint16_t data) {
		//if (result == SUCCESS) {
		//accely = data;
		//sendData();
		//call Humid.read();
		//}
		//else {
			//call Leds.led2On();
		//}
	}

	event void Temp.readDone(error_t result, uint16_t data) {
		if (result == SUCCESS) {
			temp = data;
			sendData();
		}
		else {
			call Leds.led0On();
		}
	}

	event void Humid.readDone(error_t result, uint16_t data) {
		//humid = data;
		//sendData();
	}

	static void sendData() {
		if (!sendBusy) {
			reading_msg_t* msg = (reading_msg_t*)call Send.getPayload(&sendBuff, dataLen);

			if (msg == NULL)
				fatal();

			msg->nodeid = TOS_NODE_ID;
			msg->vlight = vlight;
			msg->irlight = irlight;
			//msg->accelx = accelx;
			//msg->accely = accely;
			msg->temp = temp;
			//msg->humid = humid;

			if (call Send.send(&sendBuff, dataLen) == SUCCESS) {
				sendBusy = TRUE;
				call Leds.led1On();
			}
			else {
				call Leds.led0On();
			}
		}
		else {
			call Leds.led2On();
		}
	}

	event void Send.sendDone(message_t* msg, error_t err) {
		//if (sizeof(msg) == dataLen) {
		sendBusy = FALSE;
		call Leds.led1Off();
		//}
	}

	static void fatal() {
		call Timer.stop();
		call Leds.led0On();
		call Leds.led1On();
		call Leds.led2On();
	}
}
