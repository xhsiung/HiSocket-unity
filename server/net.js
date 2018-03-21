var $ = require("noapp") ;

var net = require("net");

var ADDRESS = $.conf.net.ip;
var PORT = $.conf.net.port;

//Hex 
var Action = {
	Subscribe: '2',   //0x02, 
	UnSubscribe: '3', //0x03,
	Send:'4',         //0x04,
	BroadCast: '5',   //0x05,
};
    
class Client {
	constructor (socket) {
		this.address = socket.remoteAddress;
		this.port 	 = socket.remotePort;
		this.name    = `${this.address}:${this.port}`;
		this.socket  = socket;
		this.action = "";
		this.channel = "";
		this.id = "";
	}

	write (message) {
		this.socket.write(message);
	}
	
	isLocalHost() {
		return this.address === 'localhost';
	}
	
}


class Server{
    constructor(address,port){
        this.port = port || 5000;
        this.address = address || '127.0.0.1';
		this.clients = [];
		this.subscribes = [];
    }

    start (callback ) {
		var server = this;
		
	 	this.connection = net.createServer((socket) => {
	                console.log("conneted");
            
			var client = new Client(socket);

			//Validation, if the client is valid
			if (!server._validateClient(client)) {
				client.socket.destroy();
				return;
			}
			
			// Storing client for later usage
			server.clients.push(client);

			// Triggered on message received by this client
			socket.on('data', (data) => { 
				// Broadcasting the message				
				//server.broadcast(`${client.name} says: ${data}`, client);

				console.log("recieve data");		
				client.action = data[0].toString(16);
				client.channel = data[1].toString(16);
				client.id = data[2].toString(16) + data[3].toString(16);
				
				if ( client.action === Action.Subscribe ){
					console.log("subscribe");
					var isExist = false;
					var subobj = { socket: client.socket , channel: client.channel };

					for (var i = 0; i < server.subscribes.length; i++) {
						var sock  = server.subscribes[i]["socket"];
						var chann = server.subscribes[i]["channel"];

						if ( client.channel === chann  &&  client.socket === sock ){
							isExist = true;
							break;
						}   
					} 

					if (!isExist) server.subscribes.push( subobj );
					return;
					
				}


				if ( client.action === Action.UnSubscribe ){
					console.log("unsubscribe");
					for (var i = 0; i < server.subscribes.length; i++) {
						var sock = server.subscribes[i]["socket"];
						var chann = server.subscribes[i]["channel"];
						
						if ( client.channel ===  chann && client.socket === sock ) {   
							server.subscribes.splice( i , 1)
						}   
					}   
					return;
				}

				
				if ( client.action === Action.Send ){
					console.log("send");
					for (var i = 0; i < server.subscribes.length; i++) {
						var sock = server.subscribes[i]["socket"];
						var chann = server.subscribes[i]["channel"];
						if ( client.channel ===  chann ) {														
						
							sock.write( data );
						}
					}
					
					return ;
				}

				if ( client.action === Action.BroadCast ){
					server.broadcast( data , client);	 			
				}

				//1byte(action)+1byte(channel)+1byte(id)+data(bytes)
				//server.broadcast( data , client);
			});

			
			// Triggered when this client disconnects
			socket.on('end', () => {
				console.log("disconnect");
				
				//search index,remove one unit
				server.clients.splice(server.clients.indexOf(client), 1);

			    //remove subscirbes
				for (var i = 0; i < server.subscribes.length; i++) {   
					var sock = server.subscribes[i]["socket"];  
					if (sock === client.socket) {   
						server.subscribes.splice(i, 1);  
					}
				}
				
			});

		});
		
		// starting the server
		this.connection.listen(this.port, this.address);
		
		// setuping the callback of the start function
		if (callback != undefined) {
			this.connection.on('listening', callback);	
		}

	}
    
    broadcast (message, client) {
		this.clients.forEach((client) => {
			client.write(message);
		});
    }

    _validateClient (client){
        //return client.isLocalhost();
        return true;
    }
}

var server = new Server(ADDRESS,PORT);

// Starting our server
server.start(() => {
	console.log(`Server started at: ${ADDRESS}:${PORT}`);
});

