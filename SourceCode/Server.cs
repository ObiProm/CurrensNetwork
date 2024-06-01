using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace CurrensNetwork
{
    /// <summary>
    /// Represents a host for managing network connections and data communication.
    /// </summary>
    public class Server
    {
        public delegate void _OnClientConnected(ulong ID, TcpClient client);
        public delegate void _OnClientDisconnected(ulong ID);
        public delegate void _OnDataRecieved(Packet packet);
        public delegate void _OnServerStarted();
        public delegate void _OnServerStartingFailure(string ex);
        public delegate void _OnDataReceiveProgress(int loaded);
        public delegate void _OnServerStopped();

        /// <summary>
        /// Occurs when the client connects to the server
        /// </summary>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Occurs when a client is disconnected.
        /// </summary>
        /// <returns>Returns ID of disconnected client</returns>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Occurs when data is received.
        /// </summary>
        /// <returns>Returns Packet object containing the received data</returns>
        public event _OnDataRecieved OnDataReceived;
        /// <summary>
        /// Occurs when a host is successfully created.
        /// </summary>
        public event _OnServerStarted OnServerStarted;
        /// <summary>
        /// Occurs when server starting fails.
        /// </summary>
        /// <returns>Returns <see cref="string"/> of failure</returns>
        public event _OnServerStartingFailure OnServerStartingFailure;
        /// <summary>
        /// Occurs on downloading data progress, returns count of read bytes.
        /// </summary>
        public event _OnDataReceiveProgress OnDataReceiveProgress;
        /// <summary>
        /// Event that occurs when a server has stopped.
        /// </summary>
        public event _OnServerStopped OnServerStopped;

        public int MaxClients { get; set; } = int.MaxValue;
        public Dictionary<EndPoint, TcpClient> ConnectedClients { get; private set; } = new Dictionary<EndPoint, TcpClient>();

        private Thread connectionsChecker = null;
        private TcpListener listener;
        private bool CheckingForConnections = true;
        private readonly object locker = new object();

        /// <summary>
        /// Creates a host on the specified port to accept incoming connections.
        /// </summary>
        /// <param name="Port">The port number for hosting the server.</param>
        public void Start(int Port)
        {
            if (Port < 0 || Port > 65536)
                throw new ArgumentOutOfRangeException($"Port value must be between 1 and 65536");
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                Networking.NetworkState = NetworkState.Server;
                CheckingForConnections = true;
            }
            catch (Exception ex)
            {
                OnServerStartingFailure?.Invoke(ex.Message);
            }
            OnServerStarted?.Invoke();

            connectionsChecker = new Thread(ConnectionsChecker);
            connectionsChecker.Start();
            Networking.Server = this;

            _ = Task.Run(async () => await DataReceiver());
        }
        /// <summary>
        /// Stops the server
        /// </summary>
        public void Stop()
        {
            Networking.NetworkState = NetworkState.None;
            Networking.Server = null;
            listener.Stop();

            lock (locker)
            {
                foreach (var client in ConnectedClients.Values)
                    client.Close();
                ConnectedClients.Clear();
            }

            CheckingForConnections = false;
            connectionsChecker.Interrupt();
            OnServerStopped?.Invoke();
        }

        private void ConnectionsChecker()
        {
            while (CheckingForConnections)
            {
                try
                {
                    var tcpClient = listener.AcceptTcpClient();
                    lock (locker)
                    {
                        ConnectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                    }
                    ulong ID = ulong.Parse(tcpClient.Client.RemoteEndPoint.ToString().Replace(".", "").Replace(":", ""));
                    Networking.ConnectedClients.Add(ID, tcpClient);
                    OnClientConnected?.Invoke(ID, tcpClient);
                }
                catch { }

                if (MaxClients >= ConnectedClients.Count)
                {
                    CheckingForConnections = false;
                    CheckingForConnections = false;
                    connectionsChecker.Interrupt();
                }
            }
        }
        private async Task<Packet> ReceivePacketAsync(NetworkStream stream)
        {
            byte[] buffer = new byte[1024];
            int bytesRead;
            StringBuilder stringBuilder = new StringBuilder();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                OnDataReceiveProgress?.Invoke(bytesRead);
                stringBuilder.Append(Encoding.UTF8.GetString(buffer, 0, bytesRead));
                if (!stream.DataAvailable)
                    break;
            }
            string _prepacket = stringBuilder.ToString();
            XmlSerializer serializer = new XmlSerializer(typeof(Packet));
            StringReader reader = new StringReader(_prepacket);
            return (Packet)serializer.Deserialize(reader);
        }

        private async Task DataReceiver()
        {
            while (true)
            {
                if (ConnectedClients.Count == 0) continue;

                foreach (var client in ConnectedClients.Where(c => c.Value.Connected && c.Value.GetStream().DataAvailable).ToList())
                {
                    var stream = client.Value.GetStream();
                    Packet receivedPacket = await ReceivePacketAsync(stream);

                    OnDataReceived?.Invoke(receivedPacket);

                    ulong SendTo = receivedPacket.SendTo;
                    if (SendTo == 0)
                        Networking.Rpc(receivedPacket);
                    else if (SendTo == 1)
                        Networking.InvokeRpcMethod(receivedPacket);
                    else
                        Networking.RpcTo(receivedPacket.SendTo, receivedPacket);
                }

                lock (locker)
                    foreach (var client in ConnectedClients.Where(c => !c.Value.Connected).ToList())
                    {
                        ConnectedClients.Remove(client.Key);
                        OnClientDisconnected?.Invoke(ulong.Parse(client.Key.ToString().Replace(".", "").Replace(":", "")));
                        if (MaxClients < ConnectedClients.Count)
                        {
                            CheckingForConnections = true;
                            connectionsChecker = new Thread(ConnectionsChecker);
                            connectionsChecker.Start();
                        }
                    }
            }
        }
    }
}
