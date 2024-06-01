 Docs
The **CurrensNetwork** C# library facilitates data transfer for various purposes such as chat or gaming.

Sections:
- [Base information](#baseinfo)
- [Server class](#server)
- [Host class](#host)
- [Client class](#client)
- [NetworkState](#networkstate)
- [Networking class](#networking)
- [Using of RPC](#rpc)
- [Using of RpcTo](#rpcto)
- [Packet](#packet)
- [FAQ](#faq)

## Base info
- Everyone has UniqueID, its a Local end point converted to ulong
- Host always has ID 1

## Server
### Description
Represents a host for managing network connections and data communication.
### Fields
- `MaxClients` - max clients, which can connect to server(default is max `int`)
- `ConnectedClients` - `Dictionary<EndPoint, TcpClient>` of all clients
### Methods
- `Start(int port)` - creates a host on the specified port to accept incoming connections
- `Stop()` - stops the server
### Events
- `OnClientConnected`: Occurs when a client is connected. Returns `ulong`(ID) and `TcpClient` of connected client.
- `OnClientDisconnected`: Occurs when a client is disconnected. Returns `ulong` - ID of disconnected client.
- `OnDataRecieved`: Occurs when data is received. Returns `Packet` object containing the received data.
- `OnServerStarted`: Occurs when the server is successfully created. No return value.
- `OnServerStopped`: Occurs when the server has stopped. No return value.
- `OnServerStartingFailure`: Occurs when server creation fails. Returns `string` - message of failure.
- `OnDataReceiveProgress`: Occurs when data is received from the network stream. Returns `int` - readed bytes.
### Example
```csharp
public void ServerFunc(){
    Server server = new Server();

    // Just setting up events
    server.OnServerStarted += () => { Console.WriteLine("Server started successfully."); };
    server.OnServerStopped += () => { Console.WriteLine("Server stopped!"); };

    // Now, only 5 clients can connect to the server
    server.MaxClients = 5

    // Create the server on a specified port
    server.Start(8080);
}
```
## Host
### Description
A mixture of 2 classes: [Server](#server) and [Client](#client), allows you to conveniently interact with both of them
### Fields
- `Client` -  [client](#client) of current host
### Methods
- `Create(int port)` - creates a host on the specified port to accept incoming connections
- `Stop()` - stops the host and client at the same time
### Events
No unique events, look at [server events](#events)
### Example
```csharp
public void HostFunc(){
    Host host = new Host();

    // Just setting up events
    host.OnServerCreated += () => { Console.WriteLine("Host created successfully!"); };
    host.Client.OnClientConnected => () += { Console.WriteLine("Host's client connected successfully!"); };

    // Create the host on a specified port
    host.Create(8080);
}
```

## Client
### Description
Represents a client for establishing connections to a remote server and handling data communication.
### Fields
- `ConnectTimeout`: the maximum time to attempt to connect to the server in miliseconds(default is 10000 - 10 seconds)
### Methods
- `Connect(string IP, int Port)`: Establishes a connection to a remote server with the specified IP address and port.
- `Disconnect()`: Disconnects the client from the remote connection.
### Events
- `OnDataReceived`: Occurs when data is received from the remote server. Returns `Packet` object containing the received data.
- `OnClientConnected`: Occurs when the client successfully connects to the remote server.
- `OnClientDisconnected`: Occurs when the client is disconnected from the remote server.
- `OnConnectionTerminated`: Occurs when the connection with the remote server is terminated.
- `OnClientConnectionFailure`: Occurs when the client fails to establish a connection with the remote server. Returns `string` - message of failure.
- `OnReceivingDataFailure`: Occurs when the client fails to receive data from the remote server. Returns `string` - message of failure.
- `OnDataReceiveProgress`: Occurs when data is received from the network stream. Returns `int` - readed bytes.
### Example
```csharp
public void ClientFunc(){
    Client client = new Client();

    // Just setting up events
    client.OnClientConnected += () => { Console.WriteLine("Client connected successfully."); };
    client.OnClientConnectionFailure += (error) => { Console.WriteLine($"Failed to connect to server: {error}"); };

    // Setting the time to try to connect (5 seconds)
    client.ConnectTimeout = 5000;

    // Connect to the server with IP "192.168.1.100" and port 8080
    client.Connect("192.168.1.100", 8080);
}
```
## NetworkState
`NetworkState` is `enum`, which contatins currenst network state, it can be:
- `Host`
- `Client`
- `Server`
- `None`

## Networking
`Networking` contains fields with information
**Properties:**
- `UniqueID`: Represents the unique identifier for the host.
- `NetworkState`: Contains `NetworkState` class - the state of network.
- `Host`: Contains current `Host` or `null`.
- `Client`: Contains current `Client` or `null`.
- `Server`: Contains current `Server` or `null`.

## Rpc
Rpc calls given method on another clients, you can use it `Networking.Rpc(string NameOfMethod, object[] params)`<br>
Every method, which calls with `Rpc` must have attribute `[RPC]` and be `public`

Example of calling:
```csharp
public void YourFunc() {
    Networking.Rpc("WriteMessage", "Hello");
}
    
// Will print "Hello" at all clients
[RPC]
public void WriteMessage(string message) {
    Console.WriteLine(message);
}
```
<!---
**Warning!**
You should be aware that DoLocally can lead to double executions on the client (but not on the host), be careful when using this feature (this element will be simplified in the future)
-->

## RpcTo
RpcTo works as Rpc, but calls method only at client with specific `UniqueID`.<br>
You can get `UniqueID` in the dictionary `Networking.ConnectedClients` as a key<br>
Structure of calling: `Networking.RpcTo(ulong ID, string NameOfMethod, object[] params)`

Example of calling:
```csharp
// This will call "AddSrore" at server
// Because server always has ID 1
int GameScore = 0; // Imagine, we have a game with game score
    
public void YourFunc() {
    Networking.RpcTo(1, "AddSrore", 3);
}
    
[RPC]
public void AddSrore(int count) {
    GameScore += count;
}
```

## Packet
The `Packet` class represents data used for transporting within the CurrensNetwork.
It contains the following properties:
- `Name`(string): Represents the name of the method to be called.
- `Params`(object[]): Represents the arguments of the method.
- `SendTo`(ulong): Represents the ID of the user to whom the data should be sent. Default value is 0(will send packet to all).
This class is marked as serializable to enable its objects to be easily serialized and transmitted over the network.

**Note:** It's important to ensure that the `Name` property corresponds to a method name that exists and can be invoked.

## FAQ
- How I can get count of connected clients using `Server` or `Host` class? - Use `ConnectedClients` field, but more precisely its `ConnectedClients.Count`