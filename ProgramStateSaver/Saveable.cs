using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ProgramStateSaver
{
    public class Saveable
    {
        // properties for cacheing 
        private Type ElementType { get; set; }

        private static Dictionary<Type,(FieldInfo[], PropertyInfo[])> FieldsAndProperties;

        private static Type SaveAttributeType { get; set; }


        static Saveable()
        {
            FieldsAndProperties = new Dictionary<Type, (FieldInfo[], PropertyInfo[])>();
            SaveAttributeType = typeof(SaveAttribute);
        }
        protected Saveable()
        {
            ElementType = this.GetType();
            // if type of this object has been saved before, it already has fields and properties cached 
            if (FieldsAndProperties.ContainsKey(ElementType))
                return;
            // if it wasn't saved yet, cache fields and properties
            var fields = ElementType.GetFields().Where(field => field.IsDefined(SaveAttributeType)).ToArray();
            var properties = ElementType.GetProperties().Where(property => property.IsDefined(SaveAttributeType)).ToArray();
            FieldsAndProperties[ElementType] = (fields, properties);
            
        }

        private bool IsSimple(Type type)
        {
            return (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)) ? true : false;
        }

        private bool IsGenericWithOneArgument(Type genericTypeDefinition, object value)
        {
            if (genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(SortedSet<>) ||
                genericTypeDefinition == typeof(Stack<>) || genericTypeDefinition == typeof(Queue<>) || value is IList) 
            {
                return true;
            }
            return false;
        }

        private void WriteSimple(object value, XmlWriter writer, string name)
        {
            writer.WriteElementString(name, value.ToString());
        }

        private void WriteArray(Array array, XmlWriter writer, Type type)
        {
            if (array.Length == 0) return;
            Type arrayType = type.GetElementType()!;
            foreach (var element in array)
                WriteValue(element, writer, arrayType);
        }

        private void WriteNonGenericEnumerable(IEnumerable enumerable, XmlWriter writer)
        {
            foreach (var element in enumerable)
                WriteValue(element,writer,element.GetType());
        }

        private void WriteGenericEnumerable(IEnumerable enumerable, XmlWriter writer, Type type)
        {
            Type genericType = type.GetGenericArguments()[0];
            foreach (var element in enumerable)
                WriteValue(element, writer, genericType);
        }

        private void WriteNonGenericDictionary(IDictionary dictionary, XmlWriter writer)
        {
            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WriteStartElement("KeyValuePair");
                WriteValue(entry.Key, writer, entry.Key.GetType());
                WriteValue(entry.Value!, writer, entry.Value!.GetType());
                writer.WriteEndElement();
            }
        }

        private void WriteGenericDictionary(IDictionary dictionary, XmlWriter writer, Type type)
        {
            if (dictionary.Count == 0) return;
            var genericArguments = type.GetGenericArguments();
            Type keyType = genericArguments[0];
            Type valueType = genericArguments[1];
            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WriteStartElement("KeyValuePair");
                WriteValue(entry.Key, writer, keyType);
                WriteValue(entry.Value!, writer, valueType);
                writer.WriteEndElement();
            }
        }

        private void WriteTuple(ITuple tuple, XmlWriter writer)
        {
            for (int i = 0; i<tuple.Length; i++)
                WriteValue(tuple[i]!, writer, tuple[i]!.GetType());
        }

        private void WriteNonGeneric(object value, XmlWriter writer, Type type)
        {
            // type is Array
            if (type.IsArray)
                WriteArray((Array)value, writer, type);

            // type is non-generic ArrayList, Stack or Queue
            else if (type == typeof(ArrayList) || type == typeof(Stack) || type == typeof(Queue))
                WriteNonGenericEnumerable((IEnumerable)value, writer);

            // type is non-generic Hashtable or non-generic SortedList
            else if (type == typeof(Hashtable) || type == typeof(SortedList))
                WriteNonGenericDictionary((IDictionary)value, writer);
        }

        private void WriteGeneric(object value, XmlWriter writer, Type type)
        {
            // type implements IDictionary (Dictionary, SortedList)
            if (value is IDictionary)
                WriteGenericDictionary((IDictionary)value, writer, type);

            // type is List, HashSet, SortedSet, Stack, Queue
            else if (IsGenericWithOneArgument(type.GetGenericTypeDefinition(), value))
                WriteGenericEnumerable((IEnumerable)value, writer, type);

            else if (value is ITuple)
                WriteTuple((ITuple)value, writer);
        }

        private void WriteValue(object value, XmlWriter writer, Type type, string name = "default")
        {
            // set proper name
            string realName = name;
            if (name == "default")
                realName = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;

            // type is simple
            if (IsSimple(type))
            {
                WriteSimple(value, writer, realName);
                return;
            }

            // type is complex
            writer.WriteStartElement(realName);

            if (type.IsGenericType)
                WriteGeneric(value, writer, type);
            else
                WriteNonGeneric(value, writer, type);

            writer.WriteEndElement();
        }

        public void WriteXML(string filePath)
        {
            Type saveAttributeType = typeof(SaveAttribute);
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };

            using (XmlWriter writer = XmlWriter.Create(filePath,settings))
            {
                writer.WriteStartElement(ElementType.Name);
                FieldInfo[] fields = FieldsAndProperties[ElementType].Item1;
                PropertyInfo[] properties = FieldsAndProperties[ElementType].Item2;

                foreach (var field in fields)
                {
                    var value = field.GetValue(this);
                    if (value == null) continue;
                    Type type = field.FieldType;
                    var saveAttribute = field.CustomAttributes.Where(customAttr => customAttr.AttributeType == saveAttributeType).First();
                    bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 && 
                        Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                    string name = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : field.Name;

                    WriteValue(value, writer, type, name);
                }
                foreach (var property in properties)
                {
                    var value = property.GetValue(this);
                    if (value == null) continue;
                    Type type = property.PropertyType;
                    var saveAttribute = property.CustomAttributes.Where(customAttr => customAttr.AttributeType == saveAttributeType).First();
                    bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 &&
                        Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                    string name = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : property.Name;

                    WriteValue(value, writer, type, name);
                }
                writer.WriteEndElement();
                writer.Flush();
            }
        }


        public void readXML(string filePath)
        {
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                reader.ReadStartElement();
                // ovo odvojit u novu fju
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    if (reader.NodeType == XmlNodeType.Element)
                    {
                        string propertyOrFieldName = reader.LocalName;
                        MemberInfo[] members = ElementType.GetMember(propertyOrFieldName); // ovo drugacije
                        if (members.Length == 0)
                        {
                            reader.Skip();
                            continue;
                        }
                        

                    }
                    reader.Read();
                }
            }
            // XmlTextReader reader_for_console = new XmlTextReader(filePath);
            // XmlDocument doc = new XmlDocument();
            // doc.Load(reader_for_console);
            // doc.Save(Console.Out);
        }
    }
}
