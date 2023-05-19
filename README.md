Dodao sam mogucnost spremanja slozenih tipova. Za sada program podrzava spremanje List<T>, Array, ArrayList,
Dictionary<TKey,TValue>, Hashtable, SortedList<TKey, TValue>, SortedList, HashSet<T>, SortedSet<T>, Stack<T>,
Stack, Queue<int>, Queue. 
Spremanje ovakvih struktura radio sam pomocu rekurzije tako da podrzava i spremanje npr. List<List<int>>.
Kod struktura koje implementiraju IDictionary nije podrzano spremanje ako su Key ili Value slozeni tipovi jer 
cini mi se da je tako i u built-in XML serializeru.
Pretpostavljam da cu jos morati mijenjati spremanje prema tome kako mi bude trebalo za citanje.
Dodao sam mogucnost da korisnik sam odabere gdje zeli spremiti file.
Dodao sam mogucnost da pri zadavanju atributa korisnik moze fieldu ili propertyju dodijeliti custom ime.
Probao sam reducirati broj poziva na reflection koliko god je bilo moguce.
