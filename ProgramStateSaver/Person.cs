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
        [Save]
        public string FirstName;

        public string LastName;

        [Save]
        public int Age { get; set; }

        [Save]
        public List<int> lista;

        [Save]
        public List<List<int>> matrix;

        [Save]
        public int[] array = { 1, 2, 3 };

        [Save]
        public ArrayList arrayLista;

        public Person() {
            FirstName = "Default first name";
            LastName = "Default last name";
            this.lista = new List<int> { 1, 2, 3 };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" } };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
        }
        public Person(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            this.lista = new List<int> { 1, 2, 3 };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" } };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 } , new List<int> { 4, 5, 6 } };
        }
    }
}
