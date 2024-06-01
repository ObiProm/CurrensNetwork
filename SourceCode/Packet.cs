using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Serialization;

namespace CurrensNetwork
{
    /// <summary>
    /// Data which CurrensNetwork uses for transpotring
    /// </summary>
    [Serializable]
    public class Packet
    {
        /// <summary>
        /// Name of method
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// Arguments of method
        /// </summary>
        public List<object> Params { get; set; }
        /// <summary>
        /// Id of user, which get data
        /// </summary>
        public ulong SendTo = 0;

        internal string Pack()
        {
            XmlSerializer serializer = new XmlSerializer(typeof(Packet));
            StringWriter sw = new StringWriter();
            serializer.Serialize(sw, this);
            var xmlContent = sw.ToString();
            return xmlContent;
        }

        public string ToString()
        {
            string _params = "";
            foreach (var param in this.Params)
                _params += param.ToString() + "(" + param.GetType() + ")";

            return "Method: " + this.Name + "\nParams: " + _params + "\nSendTo: " + this.SendTo;
        }
    }  
}