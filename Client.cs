using System;
using System.Linq;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.ComponentModel;
using System.Net;

namespace CurrensNetwork
{
    public class Client
    {
        public delegate void _OnDataRecieved(string method, params object[] args);
        public delegate void _OnClientConnected();
        public delegate void _OnClientDisconnected();
        public delegate void _OnConnectionTerminated();
        public delegate void _OnClientConnectionFailture();
        /// <summary>
        /// Invokes when client recieve any data from host
        /// </summary>
        public event _OnDataRecieved OnDataRecieved;
        /// <summary>
        /// Invokes when client sucesfuly connects
        /// </summary>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Invokes when client disconnects
        /// </summary>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Invokes when host stops connection/error
        /// </summary>
        public event _OnConnectionTerminated OnConnectionTerminated;
        /// <summary>
        /// Invokes when client can't connect to server(host)
        /// </summary>
        public event _OnClientConnectionFailture OnClientConnectionFailture;

        private TcpClient client = new TcpClient();
        private NetworkStream stream;

        private BackgroundWorker dataReciever = new BackgroundWorker();
        public void Connect(string IP, int Port)
        {
            try { client = new TcpClient(); client.Connect(IP, Port); stream = client.GetStream(); }
            catch { OnClientConnectionFailture?.Invoke(); return; }
            Networking.ClientStream = stream;
            var ip = (client.Client.LocalEndPoint as IPEndPoint);
            Networking.UniqueID = ulong.Parse(ip.Address.MapToIPv4().ToString().Replace(".", "") + ip.Port.ToString());
            OnClientConnected?.Invoke();

            dataReciever = new BackgroundWorker();
            dataReciever.DoWork += (s, e) => DataReciever();
            dataReciever.WorkerSupportsCancellation = true;
            dataReciever.RunWorkerAsync();
        }

        public void Disconnect()
        {
            OnClientDisconnected?.Invoke();
            stream.Close();
            client.Close();
            Networking.ClientStream = null;
        }

        private void DataReciever()
        {
            while (client.Connected)
            {
                try
                {
                    BinaryFormatter formatter = new BinaryFormatter();
                    Packet RecievedPacket = (Packet)formatter.Deserialize(stream);
                    string methodName = RecievedPacket.Name;
                    object[] args = RecievedPacket.Params;

                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    OnDataRecieved?.Invoke(methodName, args);
                    foreach (Assembly assembly in assemblies)
                    {
                        MethodInfo[] methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                        .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0).ToArray(); // получаем долбанные методы

                        foreach (var method in methods)
                            if (method.Name == methodName && method.GetParameters().Length == args.Length)
                            {
                                var ClassInstance = Activator.CreateInstance(method.DeclaringType);
                                method.Invoke(ClassInstance, args);
                            }
                    }
                }
                catch { Console.WriteLine("Error on recieving data(maybe host stopped connecton)"); }
            }
            Disconnect();
            OnConnectionTerminated?.Invoke();
            dataReciever = null;
        }
    }
}