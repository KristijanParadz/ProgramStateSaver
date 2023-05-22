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
                Type arrayType = type.GetElementType()!;
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

            // type is non-generic ArrayList or non-generic stack or non-generic queue
            if(type == typeof(ArrayList) || type == typeof(Stack) || type == typeof(Queue))
            {
                writer.WriteStartElement(name == "default" ? type.Name : name);
                foreach (var element in (IEnumerable)value)
                    writeComplex(element, writer);
                writer.WriteEndElement();
                return;
            }

            // type implements IDictionary (Dictionary, SortedList, Hashtable)
            if (value is IDictionary)
            {
                writer.WriteStartElement(name == "default" ? (type.IsGenericType ? type.Name.Split('`')[0] : type.Name) : name);
                foreach (DictionaryEntry entry in (IDictionary)value)
                {
                    writer.WriteStartElement("KeyValuePair");
                    writer.WriteElementString("Key", entry.Key.ToString());
                    writer.WriteElementString("Value", entry.Value!.ToString());
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                return;
            }

            if (!type.IsGenericType) return;

            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            // type is generic HashSet or generic SortedSet or generic Stack or generic Queue or generic List
            if (type.IsGenericType && (genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(SortedSet<>) ||
                genericTypeDefinition == typeof(Stack<>) || genericTypeDefinition == typeof(Queue<>) || value is IList))
            {
                Type genericType = type.GetGenericArguments()[0];
                writer.WriteStartElement(name == "default" ? type.Name.Split('`')[0] : name);
                if (isSimple(genericType))
                {
                    foreach (var element in (IEnumerable)value)
                        writer.WriteElementString(genericType.Name, element.ToString());
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
            var fieldsToWrite = fields.Where(field => field.IsDefined(saveAttributeType));
            var propertiesToWrite = properties.Where(property => property.IsDefined(saveAttributeType));

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
