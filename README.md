<h1>Docs</h1>
The <strong>CurrensNetwork</strong> C# library facilitates data transfer for various purposes such as chat or gaming.
<p>NuGet: https://www.nuget.org/packages/CurrensNetwork/1.2.1</p>
<p>Sections:</p>
<ul>
  <li><a href = "#baseinfo">Base information</a></li>
  <li><a href = "#host">Host class</a></li>
  <li><a href = "#client">Client class</a></li>
  <li><a href = "#networking">Networking class</a></li>
  <li><a href = "#rpc">Using of RPC</a></li>
  <li><a href = "#rpcto">Using of RocTo</a></li>
  <li><a href = "#packet">Packet</a></li>
</ul>
<div class="main" id="BaseSection">
  <h2>Base info</h2>
  <ul>
  <li>Everyone has UniqueID, its a Local end point converted to ulong</li>
  <li>Host always has ID 1</li>
</ul>

</div>
<div class="main" id="HostSection">
    <h2>Host</h2>
    <h3>Description</h3>
    <p>Represents a host for managing network connections and data communication.</p>
    <h3>Methods</h3>
    <ul>
      <li><code>Create(int port)</code> - сreates a host on the specified port to accept incoming connections</li>
    </ul>
    <h3>Events</h3>
    <ul>
        <li><code>OnClientConnected</code>: Occurs when a client is connected. Returns <code>TcpClient</code> of connected client.</li>
        <li><code>OnClientDisconnected</code>: Occurs when a client is disconnected. Returns <code>ulong</code> - ID of disconnected client.</li>
        <li><code>OnDataRecieved</code>: Occurs when data is received. Returns <code>Packet</code> object containing the received data.</li>
        <li><code>OnHostCreated</code>: Occurs when a host is successfully created. No return value.</li>
        <li><code>OnHostCreationFailure</code>: Occurs when host creation fails. Returns <code>Exception</code> - reason if failure.</li>
        <li><code>OnDataReceiveProgress</code>: Occurs when data is received from the network stream. Returns <code>int</code> - readed bytes.</li>
    </ul>
    <h3>Example</h3>
    
```cs
public void HostFunc(){
    Host host = new Host();

    host.OnHostCreated += () => { Console.WriteLine("Host created successfully."); };
    host.OnHostCreationFailure += (exception) => { Console.WriteLine($"Failed to create host: {exception.Message}"); };
    host.OnClientConnected += (client) => { Console.WriteLine($"Client connected: {client}"); };
    host.OnClientDisconnected += (clientId) => { Console.WriteLine($"Client disconnected: ID {clientId}"); };
    host.OnDataReceived += (packet) => { Console.WriteLine($"Data received: {packet.Data}"); };

    // Create the host on a specified port
    host.Create(8080);
}
```
</div>

<div class="main" id="ClientSection">
    <h2>Client</h2>
    <h3>Description</h3>
    <p>Represents a client for establishing connections to a remote server and handling data communication.</p>
    <h3>Methods</h3>
    <ul>
        <li><code>Connect(string IP, int Port)</code>: Establishes a connection to a remote server with the specified IP address and port.</li>
        <li><code>Disconnect()</code>: Disconnects the client from the remote connection.</li>
    </ul>
    <h3>Events</h3>
    <ul>
        <li><code>OnDataReceived</code>: Occurs when data is received from the remote server. Returns <code>Packet</code> object containing the received data.</li>
        <li><code>OnClientConnected</code>: Occurs when the client successfully connects to the remote server.</li>
        <li><code>OnClientDisconnected</code>: Occurs when the client is disconnected from the remote server.</li>
        <li><code>OnConnectionTerminated</code>: Occurs when the connection with the remote server is terminated.</li>
        <li><code>OnClientConnectionFailure</code>: Occurs when the client fails to establish a connection with the remote server. Returns <code>Exception</code> indicating the reason for failure.</li>
        <li><code>OnReceivingDataFailure</code>: Occurs when the client fails to receive data from the remote server. Returns <code>Exception</code> indicating the reason for failure.</li>
        <li><code>OnDataReceiveProgress</code>: Occurs when data is received from the network stream. Returns <code>int</code> - readed bytes.</li>
    </ul>
    <h3>Example</h3>
    
```cs
public void ClientFunc(){
    Client client = new Client();

    client.OnClientConnected += () => { Console.WriteLine("Client connected successfully."); };
    client.OnClientConnectionFailure += (exception) => { Console.WriteLine($"Failed to connect to server: {exception.Message}"); };
    client.OnClientDisconnected += () => { Console.WriteLine("Client disconnected."); };
    client.OnDataReceived += (packet) => { Console.WriteLine($"Data received: {packet.Data}"); };
    client.OnReceivingDataFailure += (exception) => { Console.WriteLine($"Failed to receive data: {exception.Message}"); };
    client.OnConnectionTerminated += () => { Console.WriteLine("Connection terminated."); };

    // Connect to the server with IP "192.168.1.100" and port 8080
    client.Connect("192.168.1.100", 8080);
}
```
</div>

<div class="main" id="NetwokingSection">
  <h2>Networking</h2>
  <p><code>Networking</code> contains fields with information</p>
  <strong>Properties:</strong>
    <ul>
        <li><code>UniqueID</code>: Represents the unique identifier for the host. No return value.</li>
        <li><code>IsHost</code>: Indicates whether the current instance is a host. No return value.</li>
        <li><code>ConnectedClients</code>: Contains a dictionary of connected clients. Returns <code>Dictionary</code> or <code>null</code> (if client).</li>
        <li><code>Host</code>: Stores the <code>TcpListener</code> for the host. No return value.</li>
        <li><code>ClientStream</code>: Stores the <code>NetworkStream</code> for the client. No return value.</li>
    </ul>
</div>

<div class="main" id="RpcSection">
  <h2>Rpc</h2>
  <p>Rpc calls given method on another clients, you can use it <code>Networking.Rpc(string NameOfMethod, object[] params)</code></p>
  <p>Every method, which calls with <code>Rpc</code> must have attribute <code>[RPC]</code></p>
  <p>Example of calling: </p>
    
```cs
public void YourFunc() {
    Networking.Rpc("WriteMessage", "Hello");
}
    
// Will print "Hello" at all clients
[RPC] 
public void WriteMessage(string message) {
    Console.WriteLine(message);
}
```

</div>

<div class="main" id="RpcToSection">
  <h2>RpcTo</h2>
  <p>RpcTo works as Rpc, but calls method only at client with specific <code>UniqueID</code></p>
  <p>You can get <code>UniqueID</code> in the dictionary <code>Networking.ConnectedClients</code> as a key</p>
  <p>You can use it <code>Networking.RpcTo(ulong ID, string NameOfMethod, object[] params)</code></p>
  <p>Example of calling: </p>
    
```cs
// This will call "WriteMessage" at host
// Because host always has ID 1
    
public void YourFunc() {
    Networking.RpcTo(1, "WriteMessage", "Hello");
}
    
[RPC]
public void WriteMessage(string message) {
    Console.WriteLine(message);
}
```
</div>

<div class = "main id = "PacketSection">
  <h2>Packet</h2>
    <p>The <code>Packet</code> class represents data used for transporting within the CurrensNetwork.</p>
    <p>It contains the following properties:</p>
    <ul>
        <li><code>Name</code>(<code>string</code>): Represents the name of the method to be called.</li>
        <li><code>Params</code>(<code>object[]</code>): Represents the arguments of the method.</li>
        <li><code>SendTo</code>(<code>ulong</code>): Represents the ID of the user to whom the data should be sent. Default value is 0(will send packet to all).</li>
    </ul>
    <p>This class is marked as serializable to enable its objects to be easily serialized and transmitted over the network.</p>
    <p><strong>Note:</strong> It's important to ensure that the <code>Name</code> property corresponds to a method name that exists and can be invoked.</p>
</div>
