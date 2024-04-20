using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;

namespace CurrensNetwork
{
    /// <summary>
    /// Represents a host for managing network connections and data communication.
    /// </summary>
    public class Host
    {
        public delegate void _OnClientConnected(TcpClient client);
        public delegate void _OnClientDisconnected(ulong ID);
        public delegate void _OnDataRecieved(Packet packet);
        public delegate void _OnHostCreated();
        public delegate void _OnHostCreationFailure(Exception ex);

        /// <summary>
        /// Event that occurs when a client is connected.
        /// </summary>
        /// <returns><see cref="TcpClient"/> of connected client</returns>
        public event _OnClientConnected OnClientConnected;
        /// <summary>
        /// Event that occurs when a client is disconnected.
        /// </summary>
        /// <returns><see cref="ulong"/> - ID of disconected client</returns>
        public event _OnClientDisconnected OnClientDisconnected;
        /// <summary>
        /// Event that occurs when data is received.
        /// </summary>
        /// <returns><see cref="Packet"/> object containing the received data</returns>
        public event _OnDataRecieved OnDataRecieved;
        /// <summary>
        /// Event that occurs when a host is succesfully created.
        /// </summary>
        public event _OnHostCreated OnHostCreated;
        /// <summary>
        /// Event that occurs when host creation fails.
        /// </summary>
        /// <returns><see cref="Exception"/> - reason if Failure </returns>
        public event _OnHostCreationFailure OnHostCreationFailure;

        private TcpListener listener;
        private Dictionary<EndPoint, TcpClient> connectedClients = new Dictionary<EndPoint, TcpClient>();

        private BackgroundWorker connectionsChecker = new BackgroundWorker();
        private BackgroundWorker dataReciever = new BackgroundWorker();

        /// <summary>
        /// Creates a host on the specified port to accept incoming connections.
        /// </summary>
        /// <param name="Port">The port number for hosting the server.</param>
        public void Create(int Port)
        {
            try
            {
                listener = new TcpListener(IPAddress.Any, Port);
                listener.Start();
                Networking.IsHost = true;
            }
            catch (Exception ex)
            {
                OnHostCreationFailure?.Invoke(ex);
            }
            Networking.UniqueID = 1;
            OnHostCreated?.Invoke();
            connectionsChecker.DoWork += (s, e) => ConnectionsChecker();
            connectionsChecker.RunWorkerAsync();

            dataReciever.DoWork += (s, e) => DataReciever();
            dataReciever.RunWorkerAsync();
        }

        private void ConnectionsChecker()
        {
            while (true)
            {
                var tcpClient = listener.AcceptTcpClient();
                connectedClients.Add(tcpClient.Client.RemoteEndPoint, tcpClient);
                Networking.ConnectedClients.Add(ulong.Parse(tcpClient.Client.RemoteEndPoint.ToString().Replace(".", "").Replace(":", "")), tcpClient);
                OnClientConnected?.Invoke(tcpClient);
            }
        }

        private void DataReciever()
        {
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
                                        if (method.Name == methodName && method.GetParameters().Length == args.Length)
                                        {
                                            if (RecievedPacket.SendTo == 0 || RecievedPacket.SendTo == 1)
                                            {
                                                var ClassInstance = Activator.CreateInstance(method.DeclaringType);
                                                method.Invoke(ClassInstance, args);
                                            }
                                        }
                                }
                                OnDataRecieved?.Invoke(RecievedPacket);
                                if (RecievedPacket.SendTo == 0)
                                    Networking.Rpc(RecievedPacket);
                                else
                                    if(RecievedPacket.SendTo != 1)
                                        Networking.RpcTo(RecievedPacket.SendTo, RecievedPacket);
                            }
                        }
                        else
                        {
                            connectedClients.Remove(client);
                            OnClientDisconnected?.Invoke(ulong.Parse(client.ToString().Replace(".", "").Replace(":", "")));
                        }

                    }
            }
        }
    }
}
