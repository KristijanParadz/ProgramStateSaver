using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
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
                writer.WriteStartElement(name == "default" ? type.Name.Split('`')[0] : name);
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


            // type is generic dictionary or generic sorted list
            if (value is IDictionary && type.IsGenericType)
            {
                writer.WriteStartElement(name == "default" ? type.Name.Split('`')[0] : name);
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

            // type is non generic sorted list
            if (type == typeof(SortedList))
            {
                writer.WriteStartElement(name == "default" ? "SortedList" : name);
                foreach (DictionaryEntry entry in (SortedList)value)
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
            if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(HashSet<>))
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

        public void WriteXML(string filePath)
        {
            // get all fields and properties of an object
            Type elementType = this.GetType();
            var fields = elementType.GetFields();
            var properties = elementType.GetProperties();

            // filter fields and properties that have custom attribute Save
            Type saveAttributeType = typeof(SaveAttribute);
            var fieldsToWrite = fields.Where(field => field.CustomAttributes.Any(customAttr=>customAttr.AttributeType == saveAttributeType)).ToArray();
            var propertiesToWrite = properties.Where(property => property.CustomAttributes.Any(customAttr => customAttr.AttributeType == saveAttributeType)).ToArray();

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            using (XmlWriter writer = XmlWriter.Create(filePath,settings))
            {
                writer.WriteStartElement(elementType.Name);
                foreach (var field in fieldsToWrite)
                {
                    var value = field.GetValue(this);
                    if (value == null) continue;
                    Type type = field.FieldType;
                    var saveAttribute = field.CustomAttributes.Where(customAttr => customAttr.AttributeType == saveAttributeType).First();
                    bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 && 
                        Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                    string name = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : field.Name;
                    if (isSimple(type))
                    {

                        writer.WriteElementString(name, value.ToString());
                        continue;
                    }
                    //type is complex
                    writeComplex(value, writer, name);
                }
                foreach (var property in propertiesToWrite)
                {
                    var value = property.GetValue(this);
                    if (value == null) continue;
                    Type type = property.PropertyType;
                    var saveAttribute = property.CustomAttributes.Where(customAttr => customAttr.AttributeType == saveAttributeType).First();
                    bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 &&
                        Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                    string name = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : property.Name;
                    if (isSimple(type))
                    {

                        writer.WriteElementString(name, value.ToString());
                        continue;
                    }
                    //type is complex
                    writeComplex(value, writer, name);
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
