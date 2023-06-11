using ProgramStateSaver;


Person person = new Person("John", "King", 28);
Person person2 = new Person("John", "Doe", 25);
string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
string filePath = Path.Combine(projectRoot, "person.xml");

person.WriteXML(filePath);
person.readXML(filePath);