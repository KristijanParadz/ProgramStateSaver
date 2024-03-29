﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml;

namespace ProgramStateSaver
{
    using Getter = Func<object, object>;
    using Setter = Action<object, object>;
    public class Saveable
    {
        // properties for cacheing
        
        private Type ElementType { get; set; }

        private static Dictionary<Type,(Dictionary<string, (FieldInfo,Type, Getter, Setter)>, 
                                        Dictionary<string, (PropertyInfo, Type, Getter, Setter)>)> FieldsAndProperties;

        // Keywords from attributes are keys and their types are values
        private static Dictionary<string, Type> CachedTypes;

        private static Type SaveAttributeType { get; set; }

        static Saveable()
        {
            FieldsAndProperties = new Dictionary<Type, (Dictionary<string, (FieldInfo, Type, Getter, Setter)>,
                                                        Dictionary<string, (PropertyInfo, Type, Getter, Setter)>)>();
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
            // if it wasn't saved yet, cache fields and properties and their types
            var fields = ElementType.GetFields();
            var properties = ElementType.GetProperties();
            // string represents name of member which will be written in xml
            Dictionary<string, (FieldInfo, Type, Getter, Setter)> fieldDictionary = new Dictionary<string, (FieldInfo, Type, Getter, Setter)>();
            Dictionary<string, (PropertyInfo, Type, Getter, Setter)> propertyDictionary = new Dictionary<string, (PropertyInfo, Type, Getter, Setter)>();

            // prepare params for getters and setters
            ParameterExpression instanceParam = Expression.Parameter(typeof(object));
            UnaryExpression castedParam = Expression.Convert(instanceParam, ElementType);
            ParameterExpression valueParam = Expression.Parameter(typeof(object));

            foreach (var field in fields)
            {
                if (!field.IsDefined(SaveAttributeType))
                    continue;

                // save attribute is defined

                // if field has custom name cache that instead of Field.Name 
                var saveAttribute = field.CustomAttributes.Where(customAttr => customAttr.AttributeType == SaveAttributeType).First();
                bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 &&
                    Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                string nameToCache = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : field.Name;

                if (fieldDictionary.ContainsKey(nameToCache))
                    throw new Exception($"{nameToCache} is already used by some other field or property");

                Type fieldType = field.FieldType;

                Getter getter = CreateGetterForField(field,instanceParam,castedParam);
                Setter setter = CreateSetterForField(field, fieldType, instanceParam, castedParam, valueParam);

                fieldDictionary.Add(nameToCache, (field, fieldType, getter, setter));
            }

            foreach (var property in properties)
            {
                if (!property.IsDefined(SaveAttributeType))
                    continue;

                // save attribute is defined

                // if property has custom name cache that instead of Property.Name
                var saveAttribute = property.CustomAttributes.Where(customAttr => customAttr.AttributeType == SaveAttributeType).First();
                bool hasValidCustomName = saveAttribute.ConstructorArguments.Count > 0 &&
                    Regex.IsMatch(saveAttribute.ConstructorArguments[0].Value!.ToString()!, @"^[a-zA-Z_][a-zA-Z0-9_]*$");
                string nameToCache = hasValidCustomName ? saveAttribute.ConstructorArguments[0].Value!.ToString()! : property.Name;

                if (fieldDictionary.ContainsKey(nameToCache) || propertyDictionary.ContainsKey(nameToCache))
                    throw new Exception($"{nameToCache} is already used by some other field or property");

                Type propertyType = property.PropertyType;

                Getter getter = CreateGetterForProperty(property, instanceParam, castedParam);
                Setter setter = CreateSetterForProperty(property, propertyType, instanceParam, castedParam, valueParam);

                propertyDictionary.Add(nameToCache, (property, propertyType, getter, setter));
            }

            FieldsAndProperties[ElementType] = (fieldDictionary, propertyDictionary);
            
        }

        private Getter CreateGetterForField(FieldInfo fieldInfo, ParameterExpression instanceParam, UnaryExpression castedParam)
        {
            var fieldExpr = Expression.Field(castedParam, fieldInfo);
            var casted = Expression.Convert(fieldExpr, typeof(object));
            var getterLambda = Expression.Lambda<Getter>(casted, instanceParam);
            Getter getter = getterLambda.Compile();
            return getter;
        }

        private Getter CreateGetterForProperty(PropertyInfo propertyInfo, ParameterExpression instanceParam, UnaryExpression castedParam)
        {
            var propertyExpr = Expression.Property(castedParam, propertyInfo);
            var casted = Expression.Convert(propertyExpr, typeof(object));
            var getterLambda = Expression.Lambda<Getter>(casted, instanceParam);
            Getter getter = getterLambda.Compile();
            return getter;
        }

        private Setter CreateSetterForField(FieldInfo fieldInfo, Type fieldType, ParameterExpression instanceParam, UnaryExpression castedParam, ParameterExpression valueParam)
        {
            var castedValue = Expression.Convert(valueParam, fieldType);
            var fieldExpr = Expression.Field(castedParam, fieldInfo);
            BinaryExpression assignExpr = Expression.Assign(fieldExpr, castedValue);
            LambdaExpression lambdaExpr = Expression.Lambda<Setter>(assignExpr, instanceParam, valueParam);
            Setter setter = (Setter)lambdaExpr.Compile();
            return setter;
        }

        private Setter CreateSetterForProperty(PropertyInfo propertyInfo, Type propertyType, ParameterExpression instanceParam,
                                               UnaryExpression castedParam, ParameterExpression valueParam)
        {
            var castedValue = Expression.Convert(valueParam, propertyType);
            var propertyExpr = Expression.Property(castedParam, propertyInfo);
            BinaryExpression assignExpr = Expression.Assign(propertyExpr, castedValue);
            LambdaExpression lambdaExpr = Expression.Lambda<Setter>(assignExpr, instanceParam, valueParam);
            Setter setter = (Setter)lambdaExpr.Compile();
            return setter;
        }

        private bool IsSimple(Type type)
        {
            return (type.IsPrimitive || type == typeof(string) || type == typeof(decimal) || type == typeof(DateTime)) ? true : false;
        }


        private (Dictionary<string, (FieldInfo, Type, Getter, Setter)>, Dictionary<string, (PropertyInfo, Type, Getter, Setter)>) GetFieldsAndProperties(Type type)
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

        private void AddToCachedTypes(string keyword, Type type)
        {
            if (CachedTypes.ContainsKey(keyword))
                return;
            CachedTypes[keyword] = type;
        }

        private Type GetTypeFromKeyword(string keyword)
        {
            if(CachedTypes.ContainsKey(keyword))
                return CachedTypes[keyword];
            Type type = Type.GetType(keyword)!;
            CachedTypes[keyword] = type;
            return type;
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
            Type arrayType = type.GetElementType()!;
            writer.WriteAttributeString("type", arrayType.FullName);
            foreach (var element in array)
                WriteValue(element, writer, arrayType);
        }

        private void WriteNonGenericEnumerable(IEnumerable enumerable, XmlWriter writer)
        {
            foreach (var element in enumerable)
                WriteValue(element,writer, element.GetType(), "default", true);
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
                writer.WriteFullEndElement();
            }
        }

        private void WriteGenericDictionary(IDictionary dictionary, XmlWriter writer, Type type)
        {
            var genericArguments = type.GetGenericArguments();
            Type keyType = genericArguments[0];
            Type valueType = genericArguments[1];
            foreach (DictionaryEntry entry in dictionary)
            {
                writer.WriteStartElement("KeyValuePair");
                WriteValue(entry.Key, writer, keyType);
                WriteValue(entry.Value!, writer, valueType);
                writer.WriteFullEndElement();
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

        private void WriteSimple(XmlWriter writer, Type type, object value) 
        {
            if (type == typeof(bool))
                writer.WriteString(value.ToString()!.ToLower());
            else if (type == typeof(float))
            {
                float convertedValue = (float)value;
                writer.WriteString(convertedValue.ToString(CultureInfo.InvariantCulture));
            }
            else if (type == typeof(double))
            {
                double convertedValue = (double)value;
                writer.WriteString(convertedValue.ToString(CultureInfo.InvariantCulture));
            }
            else if (type == typeof(decimal))
            {
                decimal convertedValue = (decimal)value;
                writer.WriteString(convertedValue.ToString(CultureInfo.InvariantCulture));
            }
            else
                writer.WriteString(value.ToString());
        }

        private void WriteValue(object value, XmlWriter writer, Type type, string name = "default", bool hasAttribute = false )
        {
            // cache type for reading
            AddToCachedTypes(type.FullName!, type);

            // set proper name
            string realName = name;
            if (name == "default")
                realName = type.IsGenericType ? type.Name.Split('`')[0] : type.Name;

            writer.WriteStartElement(realName);

            if(hasAttribute)
                writer.WriteAttributeString("type", type.FullName);

            // type is simple
            if (IsSimple(type))
                WriteSimple(writer, type, value);

            // type is complex generic
            else if (type.IsGenericType)
                WriteGeneric(value, writer, type);
            // type is complex non-generic
            else
                WriteNonGeneric(value, writer, type);

            writer.WriteFullEndElement();
        }

        public void WriteXML(string filePath)
        {
            Type saveAttributeType = typeof(SaveAttribute);
            XmlWriterSettings settings = new XmlWriterSettings { Indent = true };

            using (XmlWriter writer = XmlWriter.Create(filePath,settings))
            {
                writer.WriteStartElement(ElementType.Name);
                (Dictionary<string, (FieldInfo, Type, Getter, Setter)> fieldDictionary, 
                 Dictionary<string, (PropertyInfo, Type, Getter, Setter)> propertyDictionary) = GetFieldsAndProperties(ElementType);       

                foreach (var entry in fieldDictionary)
                {
                    Getter getter = entry.Value.Item3;
                    var value = getter(this);
                    if (value == null) continue;
                    WriteValue(value, writer, entry.Value.Item2, entry.Key);
                }
                foreach (var entry in propertyDictionary)
                {
                    Getter getter = entry.Value.Item3;
                    var value = getter(this);
                    if (value == null) continue;
                    WriteValue(value, writer, entry.Value.Item2, entry.Key);
                }
                writer.WriteFullEndElement();
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

        private void ReadField(XmlReader reader, Type type, Setter setter) { 
            setter(this, ReadValue(reader,type));
        }

        private void ReadProperty(XmlReader reader, Type type, Setter setter)
        {
            setter(this, ReadValue(reader, type));
        }

        private void ReadFieldsAndProperties(XmlReader reader)
        {
            (Dictionary<string, (FieldInfo, Type, Getter, Setter)> fieldDictionary,
             Dictionary<string, (PropertyInfo, Type, Getter, Setter)> propertyDictionary) = GetFieldsAndProperties(ElementType);
            while (reader.NodeType != XmlNodeType.EndElement)
            {
                if (reader.NodeType != XmlNodeType.Element)
                {
                    Console.WriteLine($"Warning it is a {reader.NodeType}");
                    reader.Read();
                    continue;
                }

                string propertyOrFieldName = reader.LocalName;

                if(fieldDictionary.ContainsKey(propertyOrFieldName))
                    ReadField(reader, fieldDictionary[propertyOrFieldName].Item2, fieldDictionary[propertyOrFieldName].Item4);

                else if (propertyDictionary.ContainsKey(propertyOrFieldName))
                    ReadProperty(reader, propertyDictionary[propertyOrFieldName].Item2, propertyDictionary[propertyOrFieldName].Item4);

                else
                    throw new Exception("Invalid xml structure");
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
            (Dictionary<string, (FieldInfo, Type, Getter, Setter)> fieldDictionary,
             Dictionary<string, (PropertyInfo, Type, Getter, Setter)> propertyDictionary) = GetFieldsAndProperties(ElementType);
            foreach (var entry in fieldDictionary)
            {
                var field = entry.Value.Item1;
                Type type = entry.Value.Item2;
                Getter getter = entry.Value.Item3;
                var value = getter(this);

                if (IsSimple(type))
                    Console.WriteLine($"{field.Name}: {value}");
                else if(value is ITuple)
                {
                    Console.WriteLine(field.Name + " ");
                    var tuple = (ITuple)value;
                    for (int i = 0; i < tuple.Length; i++)
                        Console.Write(tuple[i]!.ToString() + " ");
                    Console.WriteLine("");
                }

                else if (type == typeof(Stack) || type  == typeof(Queue))
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

            foreach (var entry in propertyDictionary)
            {
                Getter getter = entry.Value.Item3;
                var value = getter(this);
                Console.WriteLine($"{entry.Key}: {value}");
            }
        }
    }
}
