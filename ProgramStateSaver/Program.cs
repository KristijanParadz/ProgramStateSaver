using ProgramStateSaver;


Person person = new Person("John", "King", 28);
Person person2 = new Person("John", "Doe", 25);
string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
string filePath = Path.Combine(projectRoot, "person.xml");

//person.WriteXML(filePath);
person.ReadXML(filePath);

Console.WriteLine("");
person.PrintFieldsAndPropertiesAndValues();
Console.WriteLine("");

//var hashset = (HashSet<int>)person.arrayLista[4]!;
//foreach (int i in hashset)
//    Console.WriteLine(i);

//foreach (var list in person.matrix )
  //  foreach (var item in list)
    //    Console.WriteLine(item.ToString());