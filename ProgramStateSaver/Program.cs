using ProgramStateSaver;


Person person = new Person("John", "King", 28);

Console.WriteLine("Properties:");
foreach (var property in typeof(Person).GetProperties())
{
    Console.WriteLine($"\tProperty name: {property.Name}, Property type: {property.PropertyType.Name}, value: {property.GetValue(person)} ");
    Console.WriteLine("\tCustom attributes: ");
    foreach (var attribute in property.GetCustomAttributes(true))
    {
        Console.WriteLine("\t\t" + attribute.GetType().Name);
    }
}


Console.WriteLine("\nFields:");
foreach (var field in typeof(Person).GetFields())
{
    Console.WriteLine($"\tField name: {field.Name}, Field type: {field.FieldType.Name}, value: {field.GetValue(person)} ");
    Console.WriteLine("\tCustom attributes: ");
    foreach (var attribute in field.GetCustomAttributes(true))
    {
        Console.WriteLine("\t\t"+attribute.GetType().Name);
    }
}

Console.WriteLine("");

person.WriteXML();
person.readXML();