using ProgramStateSaver;


Artifical artificial = new Artifical("John", "King", 28);
string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory())!.Parent!.Parent!.FullName;
string filePath = Path.Combine(projectRoot, "xml/artificial.xml");

artificial.WriteXML(filePath);
artificial.ReadXML(filePath);

Console.WriteLine("");
artificial.PrintFieldsAndPropertiesAndValues();
Console.WriteLine("");

Complex complex = new Complex();
filePath = Path.Combine(projectRoot, "xml/complex.xml");
complex.WriteXML(filePath);
complex.ReadXML(filePath);


Person person = new Person("John", "Doe", 27, new List<string> { "Football", "Chess", "Basketball" });
filePath = Path.Combine(projectRoot, "xml/person.xml");
person.WriteXML(filePath);
person.ReadXML(filePath);