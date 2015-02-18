var BUFF_SIZE = 64
var HTTP_PORT = 7777
var _http = require("http")
var _spawn = require("child_process").spawn
var buff = new Buffer(Array(BUFF_SIZE))
var boot_beacon = new Buffer([0, 2, 0, 0, ])

console.log("Listening for mote data...")
try {
	var reader = _spawn("cat", ["/dev/ttyUSB1"])

	reader.stdout.on("data", function(data) {
		console.log(typeof(data))
		//Buffer.concat(buff, data) // Append new data to buffer
		//console.log(hex(data))
	})
}
catch (ex) {
	console.log(ex)
	process.exit(1)
}
