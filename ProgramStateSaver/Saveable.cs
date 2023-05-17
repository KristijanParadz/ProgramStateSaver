using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace ProgramStateSaver
{
    public class Saveable
    {
        private bool isSimple(Type type)
        {
            return (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)) ? true : false;
        }

        private void writeComplex(object value, XmlWriter writer, string name = "default" ) {
            if (value == null) return;
            Type type = value.GetType();

            // type is simple
            if (isSimple(type))
            {
                writer.WriteElementString( name=="default" ? type.Name : name, value.ToString());
                return;
            }

            // type is complex

            //type is Array
            if (type.IsArray)
            {
                Type arrayType = type.GetElementType();
                Array array = (Array)value;
                writer.WriteStartElement(name == "default" ?  "Array"  : name);
                if (isSimple(arrayType))
                {
                    foreach (var element in array)
                        writer.WriteElementString(arrayType.Name, element.ToString());
                }
                else
                {
                    foreach (var element in array)
                        writeComplex(element, writer);
                }
                writer.WriteEndElement();
                return;
            }

            // type is non-generic ArrayList
            if(type == typeof(ArrayList))
            {
                writer.WriteStartElement(name == "default" ? "ArrayList" : name);
                ArrayList arrayList = (ArrayList)value;
                foreach (var element in arrayList)
                    writeComplex(element, writer);
                writer.WriteEndElement();
                return;
            }

            // type is generic List
            if (value is IList && type.IsGenericType)
            {
                Type listType = type.GetGenericArguments()[0];
                writer.WriteStartElement(name == "default" ? "List" : name);
                if (isSimple(listType))
                {
                    foreach (var element in (IList)value)
                        writer.WriteElementString(listType.Name, element.ToString());
                }
                else
                {
                    foreach (var element in (IList)value)
                        writeComplex(element, writer);
                }
                writer.WriteEndElement();
                return;
            }

            // type is non-generic Hashtable
            if(type == typeof(Hashtable))
            {
                Hashtable hashTable = (Hashtable)value;
                writer.WriteStartElement(name == "default" ? "Hashtable" : name);
                foreach (DictionaryEntry entry in hashTable)
                {
                    writer.WriteStartElement("KeyValuePair");
                    writer.WriteElementString("Key", entry.Key.ToString());
                    writer.WriteElementString("Value", entry.Value.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                return;
            }


            // type is generic dictionary
            if (value is IDictionary && type.IsGenericType)
            {
                writer.WriteStartElement(name == "default" ? "Dictionary" : name);
                foreach (DictionaryEntry entry in (IDictionary)value)
                {
                    writer.WriteStartElement("KeyValuePair");
                    writer.WriteElementString("Key", entry.Key.ToString());
                    writer.WriteElementString("Value", entry.Value.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                return;
            }

            // type is generic Hashset
            if(type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
            {
                Type setType = type.GetGenericArguments()[0];
                writer.WriteStartElement(name == "default" ? "Hashset" : name);
                if (isSimple(setType))
                {
                    foreach (var element in (IEnumerable)value)
                        writer.WriteElementString(setType.Name, element.ToString());
                }
                else
                {
                    foreach (var element in (IEnumerable)value)
                        writeComplex(element, writer);
                }
                writer.WriteEndElement();
                return;
            }

        }

        public void WriteXML()
        {
            // get all fields and properties of an object
            var fields = this.GetType().GetFields();
            var properties = this.GetType().GetProperties();

            // filter fields and properties that have custom attribute Save
            Type saveAttributeType = typeof(SaveAttribute);
            var fieldsToWrite = fields.Where(field => field.IsDefined(saveAttributeType,false)).ToArray();
            var propertiesToWrite = properties.Where(property => property.IsDefined(saveAttributeType, false)).ToArray();

            string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
            string xmlElementName = this.GetType().Name;
            string filePath = Path.Combine(projectRoot, $"{xmlElementName}.xml");

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(filePath,settings))
            {
                writer.WriteStartElement(xmlElementName);
                foreach (var field in fieldsToWrite)
                {
                    var value = field.GetValue(this);
                    if (value == null) continue;
                    Type type = field.FieldType;
                    if (isSimple(type))
                    {
                        writer.WriteElementString(field.Name, value.ToString());
                        continue;
                    }
                    //type is complex
                    writeComplex(value, writer, field.Name);
                }
                foreach (var property in propertiesToWrite)
                {
                    var value = property.GetValue(this);
                    if (value == null) continue;
                    writer.WriteElementString(property.Name, value.ToString());
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
