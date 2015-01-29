#ifndef MESH_MONITOR_H
#define MESH_MONITOR_H

enum {
  SAMPLE_INTERVAL = 1000,
  AM_MESH_MONITOR = 0x93
};

typedef nx_struct readingMsg {
	nx_uint16_t nodeid;
	nx_uint8_t vlight;
	nx_uint8_t irlight;
	//nx_uint16_t accelx;
	//nx_uint16_t accely;
	nx_uint16_t temp;
	//nx_uint16_t humid;
} reading_msg_t;

#endif
