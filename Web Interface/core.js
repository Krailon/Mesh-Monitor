var BUFF_SIZE = 64
var HTTP_PORT = 7777
var _http = require("http")
//var _fs = require("fs")
//var _tty = require("tty")
var buff = ""
var gateway

console.log("Listening for mote data...")
try {
	

	/*
	gateway = new _tty.ReadStream(_fs.openSync("/dev/ttyUSB1", "r"))
	gateway.setRawMode(true)
	gateway.on("data", function(data) {
		buff = data.toString()
		console.log("Got data: " + data.toString("hex"))
	})
	*/
}
catch (ex) {
	console.log(ex)
	process.exit(1)
}

/*
_http.createServer(function (req, res) {
	page_html = "<html><head><title>IRIS Interface</title></head><body>It works!</body></html>"

	res.writeHead(200, {"Content-Type": "text/html"})
	res.end(page_html);
}).listen(HTTP_PORT, "0.0.0.0")
*/

//console.log(">> Started HTTP server on " + HTTP_PORT)
