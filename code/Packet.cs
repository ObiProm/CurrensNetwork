using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace CurrensNetwork
{
    /// <summary>
    /// Data which CurrensNetwork uses for transpotring
    /// </summary>
    public class Packet
    {
        /// <summary>
        /// Name of method
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Args of method
        /// </summary>
        public object[] Params { get; set; }
        internal short paramsCount = 0;
        /// <summary>
        /// Id of user, which get data
        /// </summary>
        public ulong SendTo = 0;

        internal byte[] Pack()
        {
            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter writer = new BinaryWriter(memoryStream);
            paramsCount = (short)Params.Length;

            writer.Write(Name);
            writer.Write(paramsCount);

            foreach (object param in Params)
                writer.Write(param.ToString());

            writer.Write(SendTo);

            byte[] data = memoryStream.ToArray();
            return data;
        }
        internal static Packet Unpack(NetworkStream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            Packet packet = new Packet();
            packet.Name = reader.ReadString();

            List<object> paramsList = new List<object>();
            int paramsCount = reader.ReadInt16();

            for (int i = 0; i < paramsCount; i++)
                paramsList.Add(reader.ReadString());

            packet.Params = paramsList.ToArray();

            packet.SendTo = reader.ReadUInt64();

            return packet;
        }
    }
}