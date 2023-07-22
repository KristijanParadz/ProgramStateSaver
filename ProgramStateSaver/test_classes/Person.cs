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
        [Save("PatternMatchName")]
        public string FirstName;

        [Save(".NotMatch")]
        public string LastName;

        
        [Save]
        public int Age { get; set; }

        [Save]
        public List<string> Hobbies { get; set; }

        [Save]
        public List<List<int>> FavouriteNumbers { get; set; } = new List<List<int>> { new List<int> { 1, 2, 3 },
                                                                                      new List<int> { 4, 5, 6, 7 },
                                                                                      new List<int> { 8, 9, 10 }
                                                                                    };

        public Dictionary<string,string> Details { get; set; } = new Dictionary<string, string>();

        public Person(string firstName, string lastName, int age, List<string> hobbies)
        {
            FirstName = firstName;
            LastName = lastName;
            Age = age;
            Hobbies = hobbies;
            Details["gender"] = "Male";
            Details["occupation"] = "Software Enginner";
        }

    }
}
