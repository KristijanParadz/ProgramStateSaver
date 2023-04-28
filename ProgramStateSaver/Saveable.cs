using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ProgramStateSaver
{
    public class Saveable
    {
        public void WriteXML()
        {
            // get all fields and properties of an object
            var fields = this.GetType().GetFields();
            var properties = this.GetType().GetProperties();

            // filter fields and properties that have custom attribute Save
            var fieldsToWrite = fields.Where(field => field.GetCustomAttributes(true).Any(attribute => attribute.GetType().Name == "SaveAttribute")).ToArray();
            var propertiesToWrite = properties.Where(property => property.GetCustomAttributes(true).Any(attribute => attribute.GetType().Name == "SaveAttribute")).ToArray();

            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string xmlElementName = this.GetType().Name;
            string filePath = Path.Combine(projectRoot, $"{xmlElementName}.xml");

            using (XmlWriter writer = XmlWriter.Create(filePath))
            {
                writer.WriteStartElement(xmlElementName);
                foreach (var field in fieldsToWrite)
                {
                    writer.WriteElementString(field.Name, field.GetValue(this).ToString());
                }
                foreach (var property in propertiesToWrite)
                {
                    writer.WriteElementString(property.Name, property.GetValue(this).ToString());
                }
                writer.WriteEndElement();
                writer.Flush();
            }
        }

        public void readXML()
        {
            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string xmlElementName = this.GetType().Name;
            string filePath = Path.Combine(projectRoot, $"{xmlElementName}.xml");
            XmlTextReader reader = new XmlTextReader(filePath);

            XmlDocument doc = new XmlDocument();
            reader.Read();
            doc.Load(reader);
            doc.Save(Console.Out);
        }
    }
}
