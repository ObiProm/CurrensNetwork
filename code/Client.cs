using System.Net.Sockets;
using System.Reflection;
using System.ComponentModel;
using System.Net;
using System;
using System.Linq;

namespace CurrensNetwork
{
    /// <summary>
    /// Represents a client for establishing connections to a remote server and handling data communication.
    /// </summary>
    public class Client
    {
        public delegate void _OnDataReceived(Packet packet);
        public delegate void _OnClientConnected();
        public delegate void _OnClientDisconnected();
        public delegate void _OnReceivingDataFailure(Exception exception);
        public delegate void _OnConnectionTerminated();
        public delegate void _OnClientConnectionFailure(Exception exception);

        /// <summary>
        /// Represents the event handler for data received from the remote server.
        /// </summary>
        /// <returns><see cref="Packet"/> object containing the received data.</returns>
        public event _OnDataReceived OnDataReceived;
        /// <summary>
        /// Represents the event handler for successful connection to the remote server.
        /// </summary>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Represents the event handler for disconnection from the remote server.
        /// </summary>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Represents the event handler for termination of the connection with the remote server.
        /// </summary>
        public event _OnConnectionTerminated OnConnectionTerminated;
        /// <summary>
        /// Represents the event handler for failure to establish connection with the remote server.
        /// </summary>
        /// <returns><see cref="Exception"/> - reason of failure</returns>
        public event _OnClientConnectionFailure OnClientConnectionFailure;
        /// <summary>
        /// Represents the event handler for failure to receive data from the remote server.
        /// </summary>
        /// <returns><see cref="Exception"/> - reason of failure</returns>
        public event _OnReceivingDataFailure OnReceivingDataFailure;

        private TcpClient client = new TcpClient();
        private NetworkStream stream;

        private BackgroundWorker dataReceiver = new BackgroundWorker();

        /// <summary>
        /// Establishes a connection to a remote server with the specified IP address and port.
        /// </summary>
        /// <param name="IP">The IP address of the remote server.</param>
        /// <param name="Port">The port number for the connection.</param>
        public void Connect(string IP, int Port)
        {
            try
            {
                client = new TcpClient();
                client.Connect(IP, Port);
                stream = client.GetStream();
            }
            catch(Exception ex)
            {
                OnClientConnectionFailure?.Invoke(ex);
                return;
            }

            Networking.ClientStream = stream;

            var ip = client.Client.LocalEndPoint as IPEndPoint;
            Networking.UniqueID = ulong.Parse(ip.Address.MapToIPv4().ToString().Replace(".", "") + ip.Port.ToString());

            OnClientConnected?.Invoke();

            dataReceiver = new BackgroundWorker();
            dataReceiver.DoWork += (s, e) => DataReceiver();
            dataReceiver.WorkerSupportsCancellation = true;
            dataReceiver.RunWorkerAsync();
        }


        /// <summary>
        /// Disconnects the client from the remote connection.
        /// </summary>
        public void Disconnect()
        {
            OnClientDisconnected?.Invoke();
            stream.Close();
            client.Close();

            Networking.ClientStream = null;
            dataReceiver = null;
        }

        private void DataReceiver()
        {
            while (client.Connected)
            {
                try
                {
                    Packet ReceivedPacket = Packet.Unpack(stream);
                    string methodName = ReceivedPacket.Name;
                    object[] args = ReceivedPacket.Params;

                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    OnDataReceived?.Invoke(ReceivedPacket);
                    foreach (Assembly assembly in assemblies)
                    {
                        MethodInfo[] methods = assembly.GetTypes().SelectMany(t => t.GetMethods())
                        .Where(m => m.GetCustomAttributes(typeof(RPC), false).Length > 0).ToArray(); 

                        foreach (var method in methods)
                            if (method.Name == methodName && method.GetParameters().Length == args.Length)
                            {
                                var ClassInstance = Activator.CreateInstance(method.DeclaringType);
                                method.Invoke(ClassInstance, args);
                            }
                    }
                }
                catch(Exception ex)
                {
                    OnReceivingDataFailure?.Invoke(ex);
                }
            }
            Disconnect();
            OnConnectionTerminated?.Invoke();
        }
    }
}