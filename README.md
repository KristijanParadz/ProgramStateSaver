Kreirao sam console aplikaciju. Napravio sam jednostavnu klasu Person s dva fielda i jednim propertyjem.
Za početak sam samo u main dijelu (Program.cs) u console ispisao fields i properties klase Person pomocu refleksije.
Zatim sam napravio custom attribute Save koji se moze postaviti samo na field ili property.
Postavio sam Save attribute na jedan field i jedan property klase Person te sam u main dijelu za sve fields i properties ispisao sve custom atribute.
Napravio sam klasu Saveable koja ima metode writeXML i readXML. Klasa Person inherita klasu Saveable.
U writeXML sam napravio jednostavno zapisivanje u XML tako da sam iterirao po svim fields i properties
objekta na kojemu je pozvana funkcija koji imaju custom attribute Save i zapisao ih u XML file koji bi se trebao spremiti u isti folder gdje je i source code.
 U readXML() sam samo u console ispisao sadrzaj xml filea.
