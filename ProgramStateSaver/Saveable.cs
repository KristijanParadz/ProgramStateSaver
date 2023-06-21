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

        // Keywords from attributes are keys and their types are values
        private static Dictionary<string, Type> CachedTypes;

        private static Type SaveAttributeType { get; set; }


        static Saveable()
        {
            FieldsAndProperties = new Dictionary<Type, (FieldInfo[], PropertyInfo[])>();
            SaveAttributeType = typeof(SaveAttribute);
            CachedTypes = new Dictionary<string, Type>
            {
                {"System.Collections.Queue", typeof(Queue) },
                {"System.Collections.Stack", typeof(Stack) },
                {"System.Collections.ArrayList", typeof(ArrayList) },
                {"System.Collections.SortedList", typeof(SortedList) },
                {"System.Collections.Hashtable", typeof(Hashtable) }
            };
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

        private Type GetTypeFromKeyword(string keyword)
        {
            return CachedTypes.ContainsKey(keyword) ? CachedTypes[keyword] : Type.GetType(keyword)!;
        }


        private bool IsTuple(Type type)
        {
            Type[] tupleTypes = {typeof(Tuple<>), typeof(Tuple<,>), typeof(Tuple<,,>), typeof(Tuple<,,,>), typeof(Tuple<,,,,>), typeof(Tuple<,,,,,>),
            typeof(Tuple<,,,,,,>), typeof(Tuple<,,,,,,,>)};
            return tupleTypes.Any(tupleType => tupleType == type);
        }

        private object ArrayListToTuple(ArrayList list, Type tupleType)
        {
            switch (list.Count)
            {
                case 1:
                    return Activator.CreateInstance(tupleType, list[0])!;

                case 2:
                    return Activator.CreateInstance(tupleType, list[0], list[1])!;

                case 3:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2])!;

                case 4:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2], list[3])!;

                case 5:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2], list[3], list[4])!;

                case 6:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2], list[3], list[4], list[5])!;

                case 7:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2], list[3], list[4], list[5], list[6])!;

                default:
                    return Activator.CreateInstance(tupleType, list[0], list[1], list[2], list[3], list[4], list[5], list[6], list[7])!;
            }
        }

        private void WriteArray(Array array, XmlWriter writer, Type type)
        {
            if (array.Length == 0) return;
            Type arrayType = type.GetElementType()!;
            writer.WriteAttributeString("type", arrayType.FullName);
            foreach (var element in array)
                WriteValue(element, writer, arrayType);
        }

        private void WriteNonGenericEnumerable(IEnumerable enumerable, XmlWriter writer)
        {
            foreach (var element in enumerable)
                WriteValue(element,writer,element.GetType(), "default", true);
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
                WriteValue(entry.Key, writer, entry.Key.GetType(), "default", true);
                WriteValue(entry.Value!, writer, entry.Value!.GetType(), "default", true);
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

        private void WriteValue(object value, XmlWriter writer, Type type, string name = "default", bool hasAttribute = false )
        {
            // set proper name
            string realName = name;
            if (name == "default")
                realName = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;

            writer.WriteStartElement(realName);

            if(hasAttribute)
                writer.WriteAttributeString("type", type.FullName);

            // type is simple
            if (IsSimple(type))
                writer.WriteString(type == typeof(bool) ? value.ToString()!.ToLower() : value.ToString());

            // type is complex generic
            else if (type.IsGenericType)
                WriteGeneric(value, writer, type);
            // type is complex non-generic
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



        private object ReadTuple(XmlReader reader, Type[] genericArguments, Type genericTypeDefinition)
        {
            reader.ReadStartElement();
            ArrayList arrayList = new ArrayList();
            int i = 0;

            Type tupleType = genericTypeDefinition.MakeGenericType(genericArguments);

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                arrayList.Add(ReadValue(reader, genericArguments[i]));
                i++;
            }
            reader.ReadEndElement();
            return ArrayListToTuple(arrayList, tupleType);
        }

        private object ReadSet(XmlReader reader, Type genericType, Type genericTypeDefinition)
        {
            reader.ReadStartElement();
            Type genericSetType = genericTypeDefinition.MakeGenericType(genericType);
            var set = Activator.CreateInstance(genericSetType)!;

            MethodInfo addMethod = genericSetType.GetMethod("Add")!;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                addMethod.Invoke(set, new object[] { ReadValue(reader, genericType) });
            }
            reader.ReadEndElement();
            return set;
        }


        private object ReadGenericQueue(XmlReader reader, Type genericType)
        {
            reader.ReadStartElement();
            Type genericQueueType = typeof(Queue<>).MakeGenericType(genericType);
            var queue = Activator.CreateInstance(genericQueueType);

            MethodInfo pushMethod = genericQueueType.GetMethod("Enqueue")!;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                pushMethod.Invoke(queue, new object[] { ReadValue(reader, genericType) });
            }
            reader.ReadEndElement();
            return queue!;
        }

        private object ReadGenericStack(XmlReader reader, Type genericType)
        {
            reader.ReadStartElement();
            Type genericStackType = typeof(Stack<>).MakeGenericType(genericType);
            var stack= Activator.CreateInstance(genericStackType);

            MethodInfo pushMethod = genericStackType.GetMethod("Push")!;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                pushMethod.Invoke(stack, new object[] { ReadValue(reader, genericType) });
            }
            reader.ReadEndElement();
            return stack!;
        }

        private IList ReadList(XmlReader reader, Type genericType)
        {
            reader.ReadStartElement();
            Type genericListType = typeof(List<>).MakeGenericType(genericType);
            IList list = (IList)Activator.CreateInstance(genericListType)!;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                list.Add(ReadValue(reader, genericType));
            }
            reader.ReadEndElement();
            return list;
        }

        private (object, object) ReadGenericEntry(XmlReader reader, Type[] genericArguments)
        {
            reader.ReadStartElement();
            int i = 0;
            object? key = null;
            object? value = null;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                // reader is on key element
                if (i == 0)
                {
                    key = ReadValue(reader, genericArguments[0]);
                    i++;
                }
                // reader is on value element
                else
                    value = ReadValue(reader, genericArguments[1]);
            }
            reader.ReadEndElement();
            return (key!, value!);
        }

        private (object, object) ReadNonGenericEntry(XmlReader reader)
        {
            reader.ReadStartElement();
            int i = 0;
            object? key = null;
            object? value = null;
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                // reader is on key element
                reader.MoveToNextAttribute();
                string attributeValue= reader.Value;
                Type elementType = GetTypeFromKeyword(attributeValue);
                reader.MoveToElement();
                if (i == 0)
                {
                    key = ReadValue(reader, elementType);
                    i++;
                }
                // reader is on value element
                else
                    value = ReadValue(reader, elementType);
            }
            reader.ReadEndElement();
            return (key!, value!);
        }

        private IDictionary ReadDictionary(XmlReader reader, Type[] genericArguments, Type genericTypeDefinition)
        {
            reader.ReadStartElement();
            Type genericDictType = genericTypeDefinition.MakeGenericType(genericArguments);
            IDictionary dictionary= (IDictionary)Activator.CreateInstance(genericDictType)!;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                (object key, object value) = ReadGenericEntry(reader, genericArguments);
                dictionary.Add(key,value);
            }
            reader.ReadEndElement();
            return dictionary;
        }

        private object ReadArray(XmlReader reader)
        {
            reader.MoveToNextAttribute();
            // read type from attribute
            string attributeValue = reader.Value;
            Type arrayType = GetTypeFromKeyword(attributeValue);
            reader.ReadStartElement();
            Type genericListType = typeof(List<>).MakeGenericType(arrayType);
            IList list = (IList)Activator.CreateInstance(genericListType)!;

            while (reader.NodeType != XmlNodeType.EndElement)
            {
            if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                list.Add(ReadValue(reader, arrayType));
            }
            reader.ReadEndElement();
            Array array = Array.CreateInstance(arrayType, list.Count);
            list.CopyTo(array, 0);
            return array;
        }

        private ArrayList ReadArrayList(XmlReader reader)
        {
            ArrayList arrayList = new ArrayList();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                // read type from attribute
                reader.MoveToNextAttribute();
                string attributeValue = reader.Value;
                Type elementType = GetTypeFromKeyword(attributeValue);
                reader.MoveToElement();
                arrayList.Add(ReadValue(reader, elementType));
            }
            reader.ReadEndElement();
            return arrayList;
        }

        private Stack ReadNonGenericStack(XmlReader reader)
        {
            Stack stack= new Stack();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                // read type from attribute
                reader.MoveToNextAttribute();
                string attributeValue = reader.Value;
                Type elementType = GetTypeFromKeyword(attributeValue)!;
                reader.MoveToElement();
                stack.Push(ReadValue(reader, elementType));
            }
            reader.ReadEndElement();
            return stack;
        }

        private Queue ReadNonGenericQueue(XmlReader reader)
        {
            Queue queue= new Queue();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                // read type from attribute
                reader.MoveToNextAttribute();
                string attributeValue = reader.Value;
                Type elementType = GetTypeFromKeyword(attributeValue);
                reader.MoveToElement();
                queue.Enqueue(ReadValue(reader, elementType));
            }
            reader.ReadEndElement();
            return queue;
        }

        private SortedList ReadNonGenericSortedList(XmlReader reader)
        {
            SortedList sortedList= new SortedList();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                (object key, object value) = ReadNonGenericEntry(reader);
                sortedList.Add(key, value);
            }
            reader.ReadEndElement();
            return sortedList;
        }

        private Hashtable ReadHashtable(XmlReader reader)
        {
            Hashtable hashtable = new Hashtable();
            reader.ReadStartElement();
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }
                (object key, object value) = ReadNonGenericEntry(reader);
                hashtable.Add(key, value);
            }
            reader.ReadEndElement();
            return hashtable;
        }

        private object ReadGeneric(XmlReader reader, Type type)
        {
            Type genericTypeDefinition = type.GetGenericTypeDefinition();
            if (genericTypeDefinition == typeof(List<>))
                return ReadList(reader, type.GetGenericArguments()[0]);

            if (genericTypeDefinition == typeof(Stack<>))
                return ReadGenericStack(reader, type.GetGenericArguments()[0]);

            if (genericTypeDefinition == typeof(Queue<>))
                return ReadGenericQueue(reader, type.GetGenericArguments()[0]);

            if (genericTypeDefinition == typeof(HashSet<>) || genericTypeDefinition == typeof(SortedSet<>))
                return ReadSet(reader, type.GetGenericArguments()[0], genericTypeDefinition);

            if (IsTuple(genericTypeDefinition))
                return ReadTuple(reader, type.GetGenericArguments(), genericTypeDefinition);

            if (genericTypeDefinition == typeof(Dictionary<,>) || genericTypeDefinition == typeof(SortedList<,>))
                return ReadDictionary(reader, type.GetGenericArguments(), genericTypeDefinition);

            throw new Exception("Unsupported data type");
        }

        private object ReadNonGeneric(XmlReader reader, Type type)
        {
            if (type.IsArray)
                return ReadArray(reader);

            if (type == typeof(ArrayList))
                return ReadArrayList(reader);

            if (type == typeof(Stack))
                return ReadNonGenericStack(reader);

            if (type == typeof(Queue))
                return ReadNonGenericQueue(reader);

            if (type == typeof(SortedList))
                return ReadNonGenericSortedList(reader);

            if (type == typeof(Hashtable))
                return ReadHashtable(reader);

            throw new Exception("Unsupported data type");
        }

        private object ReadValue(XmlReader reader,Type type)
        {
            if (IsSimple(type))
            {
                return reader.ReadElementContentAs(type, null!);
            }

            // type is not simple
            bool isGeneric = type.IsGenericType;
            if (isGeneric)
                return ReadGeneric(reader, type);

            if (!isGeneric)
                return ReadNonGeneric(reader, type);

            throw new Exception("Unsupported data type");
        }

        private void ReadField(XmlReader reader, FieldInfo field) { 
            Type fieldType = field.FieldType;
            field.SetValue(this, ReadValue(reader,fieldType));
        }

        private void ReadProperty(XmlReader reader, PropertyInfo property)
        {
            Type propertyType = property.PropertyType;
            property.SetValue(this, ReadValue(reader, propertyType));
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
        }

        // function for testing reading xml
        public void PrintFieldsAndPropertiesAndValues()
        {
            (FieldInfo[] fields, PropertyInfo[] properties) = GetFieldsAndProperties(ElementType);
            foreach (FieldInfo field in fields)
            {
                object value = field.GetValue(this)!;
                if(IsSimple(field.FieldType))
                    Console.WriteLine($"{field.Name}: {field.GetValue(this)}");
                else if(value is ITuple)
                {
                    Console.WriteLine(field.Name + " ");
                    var tuple = (ITuple)value;
                    for (int i = 0; i < tuple.Length; i++)
                        Console.Write(tuple[i]!.ToString() + " ");
                    Console.WriteLine("");
                }

                else if (field.FieldType == typeof(Stack) || field.FieldType == typeof(Queue))
                {
                    Console.WriteLine(field.Name + " ");
                    foreach (var elem in (Stack)value)
                        Console.Write(elem.ToString() + " ");
                    Console.WriteLine("");
                }

                else if (value is IDictionary)
                {
                    Console.WriteLine(field.Name + " ");
                    foreach (DictionaryEntry elem in (IDictionary)value)
                    {
                        Console.WriteLine(elem.Key + " " + elem.Value);
                    }
                    Console.WriteLine("");
                }

                else
                {
                    Console.WriteLine(field.Name + " ");
                    foreach (object elem in (IEnumerable)value)
                        Console.Write(elem.ToString() + " ");
                    Console.WriteLine("");
                }
            }

            foreach (PropertyInfo property in properties)
                Console.WriteLine($"{property.Name}: {property.GetValue(this)}");
        }
    }
}
