using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Reflection;

namespace ProgramStateSaver
{
    internal class Person : Saveable
    {
        [Save]
        public string FirstName;

        public string LastName;

        [Save]
        public int Age { get; set; }

        public Person() {
            FirstName = "Default first name";
            LastName = "Default last name";
        }
        public Person(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
        }
    }
}
