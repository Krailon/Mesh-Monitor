#!/usr/bin/env python

import sys, tos, signal

AM_MESH_MONITOR = 0x93

def signal_handler(signal, frame):
	print "\nCaught SIGINT, exiting!"
	sys.exit(0)

# Set up SIGINT catcher
signal.signal(signal.SIGINT, signal_handler)

class ReadingMessage(tos.Packet):
	def __init__(self, packet = None):
		tos.Packet.__init__(self, [
						('nodeid', 'int', 2),
						('vlight', 'int', 1),
						('irlight', 'int', 1),
						('accelx', 'int', 2),
						('accely', 'int', 2)
					], packet)
						#('temp', 'int', 2),
						#('humid', 'int', 2)
					#], packet)

if '-h' in sys.argv:
	print "Usage:", sys.argv[0], "serial@/dev/ttyUSB0:57600"
	sys.exit()

am = tos.AM()
print "Incoming packets:"
while True:
	p = am.read()
	if p and p.type == AM_MESH_MONITOR:
		rMsg = ReadingMessage(p.data)
		print "[", rMsg.nodeid, "]", rMsg.vlight, rMsg.irlight, rMsg.accelx, rMsg.accely #, rMsg.temp, rMsg.humid
