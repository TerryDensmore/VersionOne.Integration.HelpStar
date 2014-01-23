using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Reflection;

namespace VersionOne.Integration.HelpStar
{
    public class IntegrationConfigurationHandler : IConfigurationSectionHandler
    {
        public object Create(object parent, object configContext, System.Xml.XmlNode section)
        {
            return new IntegrationConfiguration(section);
        }
    }

    public class IntegrationConfiguration
    {
        public struct ConnectionInfo
        {
            public string Url { get; set; }
            public string Username { get; set; }
            public string Password { get; set; }
            public bool UseWindowsAuthentication { get; set; }
            public int ConnectAttempts { get; set; }
        }


        public ConnectionInfo V1Connection = new ConnectionInfo();

        public IntegrationConfiguration(XmlNode section)
        {
            //Convert the XmlNode to an XDocument (for LINQ).
            XDocument xmlDoc = XDocument.Parse(section.OuterXml);

            // **********************************
            // * V1 connection settings.
            // **********************************
            var v1Data = from item in xmlDoc.Descendants("V1Connection")
                         select new ConnectionInfo
                         {
                             Url = item.Element("Url").Value,
                             Username = string.IsNullOrEmpty(item.Element("Username").Value) ? string.Empty : item.Element("Username").Value,
                             Password = string.IsNullOrEmpty(item.Element("Password").Value) ? string.Empty : item.Element("Password").Value,
                             UseWindowsAuthentication = System.Convert.ToBoolean(item.Attribute("useWindowsAuthentication").Value),
                             ConnectAttempts = System.Convert.ToInt32(item.Attribute("connectAttempts").Value),
                         };
            if (v1Data.Count() == 0)
                throw new ConfigurationErrorsException("Missing V1Connection information in application config file.");
            else
                V1Connection = v1Data.First();
        }
    }
}
