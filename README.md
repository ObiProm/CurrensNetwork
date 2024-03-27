<h1>Docs</h1>
With this library you can transfer data, you can use it for chat/game etc.
<p>Library contains in <strong>CurrensNetwork</strong> namespace</p>
<p>Sections:</p>
<ul>
  <li><a href="BaseSection">Base information</a></li>
  <li><a href="HostSection">Host class</a></li>
  <li><a href="ClientSection">Client class</a></li>
  <li><a href="HostSection">Using of RPC</a></li>
  <li><a href="HostSection">Using of RocTo</a></li>
  <li><a href="HostSection">Networking class</a></li>
  <li><a href="HostSection">Packet</a></li>
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
