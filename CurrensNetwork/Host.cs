using System.ComponentModel;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace CurrensNetwork
{
    internal class Host
    {
        public delegate void _OnClientConnected(EndPoint endPoint);
        public delegate void _OnClientDisconnected(EndPoint endPoint);
        public delegate void _OnDataRecieved(string method, params object[] args);
        public delegate void _OnHostCreated();

        public event _OnClientConnected OnClientConnected;
        public event _OnClientDisconnected OnClientDisconnected;
        public event _OnDataRecieved OnDataRecieved;
        public event _OnHostCreated OnHostCreated;

        public TcpListener listener;
        public Dictionary<EndPoint, TcpClient> connectedClients = new Dictionary<EndPoint, TcpClient>();

        public BackgroundWorker connectionsChecker = new BackgroundWorker();
        public BackgroundWorker dataReciever = new BackgroundWorker();

        public void Create(int Port)
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start();

            connectionsChecker.DoWork += (s, e) => ConnectionsChecker();
            connectionsChecker.RunWorkerAsync();

            OnHostCreated?.Invoke();

            dataReciever.DoWork += (s, e) => DataReciever();
            dataReciever.RunWorkerAsync();
        }

        private void ConnectionsChecker()
        {
            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                connectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                OnClientConnected?.Invoke(tcpClient.Client.RemoteEndPoint);
                Console.WriteLine(connectedClients.Count);
            }
        }

        private void DataReciever()
        {
            Console.WriteLine(connectedClients.Count);
            while (true)
            {
                if (connectedClients.Count != 0)
                    foreach (var client in connectedClients.Keys.ToList())
                    {
                        if (connectedClients[client].Connected)
                        {
                            var stream = connectedClients[client].GetStream();
                            if (stream.DataAvailable)
                            {
                                BinaryFormatter formatter = new BinaryFormatter();
                                Packet RecievedPacket = (Packet)formatter.Deserialize(stream);
                                string methodName = RecievedPacket.Name;
                                object[] args = RecievedPacket.Params;
                                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                                foreach (Assembly assembly in assemblies)
                                {
                                    MethodInfo[] methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                                    .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0).ToArray(); // получаем долбанные методы

                                    foreach (var method in methods)
                                        if (method.Name == methodName)
                                        {
                                            Console.WriteLine("Invoke thread: " + Thread.CurrentThread.Name);
                                            method.Invoke(method.DeclaringType, args);
                                        }
                                }

                                SendData(RecievedPacket);
                                OnDataRecieved?.Invoke(methodName, args.ToArray());
                            }
                        }
                        else
                        {
                            connectedClients.Remove(client);
                            OnClientDisconnected?.Invoke(client);
                        }

                    }
            }
        }
        public void SendData(string method, params object[] args)
        {
            foreach (var stream in connectedClients.Values.ToList())
            {
                Packet packet = new Packet() { Name = method, Params = args };
                var _stream = stream.GetStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(_stream, packet);
            }
        }
        public void SendData(Packet packet)
        {
            foreach (var stream in connectedClients.Values.ToList())
            {
                var _stream = stream.GetStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(_stream, packet);
            }
        }
    }
}
