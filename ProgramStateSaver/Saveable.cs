﻿using System;
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

        private (FieldInfo[], PropertyInfo[]) GetFieldsAndProperties(Type type)
        {
            return (FieldsAndProperties[type].Item1, FieldsAndProperties[type].Item2);
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
                (FieldInfo[] fields, PropertyInfo[] properties) = GetFieldsAndProperties(ElementType);       

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

        private IList ReadList(XmlReader reader, Type listType)
        {
            reader.ReadStartElement();
            Type genericListType = typeof(List<>).MakeGenericType(listType);
            IList list = (IList)Activator.CreateInstance(genericListType)!;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                list.Add(ReadValue(reader, listType));
            }
            reader.ReadEndElement();
            return list;
        }

        private object ReadGeneric(XmlReader reader, Type type)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(List<>))
                return ReadList(reader, type.GetGenericArguments()[0]);
            return "";
        }

        private object ReadValue(XmlReader reader,Type type)
        {
            if (IsSimple(type))
            {
                string value = reader.ReadElementContentAsString();
                return Convert.ChangeType(value, type);
            }

            // type is not simple
            if (type.IsGenericType)
                return ReadGeneric(reader, type);

            return "";
        }

        private void ReadField(XmlReader reader, FieldInfo field) { 
            Console.WriteLine(field.Name);
            Type fieldType = field.FieldType;
            field.SetValue(this, ReadValue(reader,fieldType));
        }

        private void ReadProperty(XmlReader reader, PropertyInfo property)
        {
            Console.WriteLine(property.Name);
            reader.Read();
        }

        private void ReadFieldsAndProperties(XmlReader reader)
        {
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }

                string propertyOrFieldName = reader.LocalName;
                (FieldInfo[] fields, PropertyInfo[] properties) = GetFieldsAndProperties(ElementType);
                FieldInfo? field = Array.Find(fields, e => e.Name == propertyOrFieldName); 
                PropertyInfo? property = Array.Find(properties, e => e.Name == propertyOrFieldName);
                if (field != null)
                    ReadField(reader,field);

                else if (property != null)
                    ReadProperty(reader,property);

                else
                {
                    reader.Read();
                    Console.WriteLine($"Warning, element is not f or p it is {reader.Name}");
                }
            }
            reader.ReadEndElement();

        }

        public void ReadXML(string filePath)
        {
            using (XmlReader reader = XmlReader.Create(filePath))
            {
                reader.ReadStartElement();
                ReadFieldsAndProperties(reader);
            }
            // XmlTextReader reader_for_console = new XmlTextReader(filePath);
            // XmlDocument doc = new XmlDocument();
            // doc.Load(reader_for_console);
            // doc.Save(Console.Out);
        }

        public void PrintFieldsAndPropertiesAndValues()
        {
            (FieldInfo[] fields, PropertyInfo[] properties) = GetFieldsAndProperties(ElementType);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(this)!;
                if(IsSimple(field.FieldType))
                    Console.WriteLine($"{field.Name}: {field.GetValue(this)}");
                else
                {
                    Console.WriteLine(field.Name + " ");
                    foreach (object elem in (IEnumerable)value)
                        Console.Write(elem.ToString() + " ");
                }
            }

            foreach (PropertyInfo property in properties)
                Console.WriteLine($"{property.Name}: {property.GetValue(this)}");
        }
    }
}
