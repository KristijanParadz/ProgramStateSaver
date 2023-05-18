using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.Collections;

namespace ProgramStateSaver
{
    internal class Person : Saveable
    {
        [Save("Not satisfying first name")]
        public string FirstName;

        [Save("SatisfyingLastName")]
        public string LastName;

        [Save]
        public int Age { get; set; }

        [Save]
        public List<int> lista;

        [Save]
        public SortedList<int,string> genericSortedList;

        [Save]
        public SortedList nonGenericSortedList;

        [Save]
        public List<List<int>> matrix;

        [Save]
        public int[] array = { 1, 2, 3 };

        [Save]
        public ArrayList arrayLista;

        [Save]
        public Hashtable hashTable;

        [Save]
        public Dictionary<int, int> dictionary;

        [Save]
        public HashSet<int> hashSet { get; set; }

        public Person() {
            FirstName = "Default first name";
            LastName = "Default last name";
            this.lista = new List<int> { 1, 2, 3 };
            this.genericSortedList= new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } };
            this.nonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>(){ 1, 2 },
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } }};
            this.hashTable = new Hashtable() { { 1,3 }, { 2, "dsvg" }, { "dyfbdyfcb", true  } };
            dictionary = new Dictionary<int, int>() { { 1, 1 }, { 2,3 } };
            hashSet = new HashSet<int>() { 1,2,3};
        }
        public Person(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            this.lista = new List<int> { 1, 2, 3 };
            this.genericSortedList = new SortedList<int, string> { { 2, "adf" }, { 1, "dgbv" } };
            this.nonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>(){ 1, 2 },
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } }};
            this.hashTable = new Hashtable() { { 1, 3 }, { 2, "dsvg" }, { "dyfbdyfcb", true } };
            dictionary = new Dictionary<int, int>() { { 1, 1 }, { 2, 3 } };
            hashSet = new HashSet<int>() { 1, 2, 3 };
        }
    }
}
