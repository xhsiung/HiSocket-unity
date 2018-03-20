
var net = require("net");
var ADDRESS = '127.0.0.1';
var PORT = 7777 ;

class Client {	
	constructor (socket) {
		this.address = socket.remoteAddress;
		this.port 	 = socket.remotePort;
		this.name    = `${this.address}:${this.port}`;
		this.socket  = socket;
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
				console.log( Buffer.from(data,'hex') );
				

				//1byte(ID)+1byte(channel)+data(bytes)
				server.broadcast( data , client);
			});
			
			// Triggered when this client disconnects
			socket.on('end', () => {
				// Removing the client from the list
				
				//search index,remove one unit
				server.clients.splice(server.clients.indexOf(client), 1);

				// Broadcasting that this player left
				//server.broadcast(`${client.name} disconnected.\n`);
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
		/*
		this.clients.forEach((client) => {
			if (client === clientSender)
				return;
			client.receiveMessage(message);
		});*/
		
		client.write(message);
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

