<h1>Docs</h1>
With this library you can transfer data, you can use it for chat/game etc.
<p>Library contains in <strong>CurrensNetwork</strong> namespace</p>
<p>Sections:</p>
<ul>
  <li><a href = "#baseinfo">Base information</a></li>
  <li><a href = "#host">Host class</a></li>
  <li><a href = "#client">Client class</a></li>
  <li><a href = "#networking">Networking class</a></li>
  <li><a href = "#rpc">Using of RPC</a></li>
  <li><a href = "#rpcto">Using of RocTo</a></li>
  <li><a href = "">Packet</a></li>
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
  <p>To start hosting you need to use <code>Create(int Port)</code>, like</p>

    public void HostFunc(){
        Host host = new Host();
        Console.Write("Enter port: ");
        int port = int.Parse(Console.ReadLine());
        host.Create(port);
    }
<p>Also <code>Host</code> has some events</p>
<ul>
    <li><code>OnClientConnected</code> will return <code>TcpClient</code> of connected client</li>
    <li><code>OnClientDisconnected</code> will return <code>TcpClient</code> of disconnected client</li>
    <li><code>OnDataRecieved</code> will return <code>Packet</code> of recieved data</li>
    <li><code>OnHostCreated</code> invokes when host successfully started</li>
    <li><code>OnHostCreationFailture</code> invokes on host creation error, returns <code>Exeption</code></li>
</ul>

</div>
<div class="main" id="ClientSection">
  <h2>Client</h2>
  <p>To start hosting you need to use <code>Connect(string IP, int Port)</code>, like</p>

    public void ClientFunc(){
        Client client = new Client();
        Console.Write("Enter IP: ");
        string IP = Console.ReadLine();
        Console.Write("Enter port: ");
        int port = int.Parse(Console.ReadLine());
        host.Create(IP, port);
    }
<p><code>Client</code> has 5 events</p>
<ul>
    <li><code>OnClientConnected</code> invokes when client successfully connects</li>
    <li><code>OnClientDisconnected</code> invokes when client disconnects</li>
    <li><code>OnDataRecieved</code> will return <code>Packet</code> of recieved data</li>
    <li><code>OnConnectionTerminated</code> invokes when host stops connection/error</li>
    <li><code>OnClientConnectionFailture</code> invokes when client can't connect to server(host)</li>
</ul>
</div>

<div class="main" id="NetwokingSection">
  <h2>Networking</h2>
  <p><code>Networking</code> contains fields with information</p>
  <ul>
    <li><code>UniqueID</code> is a ID of user</li>
    <li><code>IsHost</code> is a bool, which contains user's host status(<code>true</code> or <code>false</code>)</li>
    <li><code>ConnectedClients</code> is a <code>Dictionary</code> with <code>EndPoint</code> as a key and <code>TcpClient</code> as a value</code></li>
    <li><code>ClientsIds</code> is a <code>Dictionary</code> with <code>ulong</code>(Client's ID) and <code>EndPoint</code> as a value</li>
    <li><code>Host</code> is a <code>TcpListener</code>, if user does not hosting contains <code>null</code></li>
    <li><code>ClientStream</code> contains client's <code>NetworkStream</code>, contains <code>null</code> if user is not client</li>
  </ul>
</div>

<div class="main" id="RpcSection">
  <h2>Rpc</h2>
  <p>Rpc calls given method on another clients, you can use it <code>Networking.Rpc(string NameOfMethod, object[] params)</code></p>
  <p>Every method, which calls with <code>Rpc</code> must have attribute <code>[RPC]</code></p>
  <p>Example of calling: </p>

    public void YourFunc() {
        Networking.Rpc("WriteMessage", "Hello");
    }
    
    // Will print "Hello" at all clients
    [RPC] 
    public void WriteMessage(string message) {
        Console.WriteLine(message);
    }
</div>

<div class="main" id="RpcToSection">
  <h2>RpcTo</h2>
  <p>RpcTo works as Rpc, but calls method only at client with specific <code>UniqueID</code></p>
  <p>You can get <code>UniqueID</code> in the dictionary <code>Networking.ClientIds</code>(Id is a key)</p>
  <p>You can use it <code>Networking.RpcTo(ulong ID, string NameOfMethod, object[] params)</code></p>
  <p>Example of calling: </p>

    // This will call "WriteMessage" at host
    // Because host always has ID 1
    
    public void YourFunc() {
        Networking.RpcTo(1, "WriteMessage", "Hello");
    }
    
    [RPC]
    public void WriteMessage(string message) {
        Console.WriteLine(message);
    }
</div>
