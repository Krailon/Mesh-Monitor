SENSORBOARD = mts400
COMPONENT=MeshMonitorAppC
CFLAGS += -I$(TOSDIR)/lib/net/ -I$(TOSDIR)/lib/net/ctp  -I$(TOSDIR)/lib/net/4bitle

include $(MAKERULES)
