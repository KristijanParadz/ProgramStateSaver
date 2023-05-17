using ProgramStateSaver;


Person person = new Person("John", "King", 28);
string projectRoot = Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.FullName;
string filePath = Path.Combine(projectRoot, "person.xml");

person.WriteXML(filePath);
person.readXML();