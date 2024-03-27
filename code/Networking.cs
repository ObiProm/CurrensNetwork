using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;

namespace CurrensNetwork
{
    /// <summary>
    /// This class was created for easy-calling RPC.
    /// It also stores some data
    /// </summary>
    public static class Networking
    {
        public static ulong UniqueID { get; set; }
        public static bool IsHost { get; set; }
        public static Dictionary<EndPoint, TcpClient> ConnectedClients = new Dictionary<EndPoint, TcpClient>();
        public static Dictionary<ulong, EndPoint> ClientIds = new Dictionary<ulong, EndPoint>();
        public static TcpListener Host { get; set; }
        public static NetworkStream ClientStream { get; set; }
        public static void Rpc(string method, params object[] args)
        {
            
            if (IsHost)
            {
                foreach (var stream in ConnectedClients.Values)
                {
                    Packet packet = new Packet() { Name = method, Params = args };
                    var _stream = stream.GetStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(_stream, packet);
                }
            }
            else
            {
                Packet packet = new Packet() { Name = method, Params = args };
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ClientStream, packet);
            }
        }
        public static void Rpc(Packet packet)
        {
            if (IsHost)
            {
                foreach (var stream in ConnectedClients.Values)
                {
                    var _stream = stream.GetStream();
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(_stream, packet);
                }
            }
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ClientStream, packet);
            }
        }

        public static void RpcTo(ulong ID, string method, params object[] args)
        {
            if (IsHost)
            {
                Packet packet = new Packet() { Name = method, Params = args, SendTo = ID };
                var _stream = ConnectedClients[ClientIds[ID]].GetStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(_stream, packet);
            }
            else
            {
                Packet packet = new Packet() { Name = method, Params = args, SendTo = ID };
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ClientStream, packet);
            }
        }
        public static void RpcTo(ulong ID, Packet packet)
        {
            if (IsHost)
            {
                var _stream = ConnectedClients[ClientIds[ID]].GetStream();
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(_stream, packet);
            }
            else
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(ClientStream, packet);
            }
        }
    }
}