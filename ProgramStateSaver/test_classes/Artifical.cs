using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.Reflection;
using System.Collections;

namespace ProgramStateSaver
{
    internal class Artifical : Saveable
    {
        [Save("PatternMatchName")]
        public string FirstName;

        [Save(".NotMatch")]
        public string LastName;

        [Save]
        public int Number;

        
        [Save]
        public int Age { get; set; }

        [Save]
        public List<int> ListOfInt;

        [Save]
        public List<List<int>> Matrix;

        [Save]
        public Stack<int> GenericStack;

        [Save]
        public Queue<int> GenericQueue;

        [Save]
        public HashSet<int> HashSet;

        [Save]
        public SortedSet<int> SortedSet;

        [Save]
        public Tuple<int, string> GenericTuple;

        [Save]
        public Dictionary<int, List<int>> Dictionary;

        [Save]
        public SortedList<int,string> GenericSortedList;

        [Save]
        public ArrayList ArrayList;

        [Save]
        public int[] Array = { 1, 2, 3 };

        [Save]
        public Stack NonGenericStack;

        [Save]
        public SortedList NonGenericSortedList;

        [Save]
        public Hashtable HashTable;



        public Artifical() {
            FirstName = "Default first name";
            LastName = "Default last name";
            Number = 0;
            Age = 0;
            this.ListOfInt = new List<int> { 1, 2, 3 };
            this.GenericSortedList= new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } };
            this.NonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            this.Matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            this.ArrayList= new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>(){ 1, 2 },
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } }};
            this.HashTable = new Hashtable() { { 1,3 }, { 2, "dsvg" }, { "dyfbdyfcb", true  } };
            Dictionary = new Dictionary<int, List<int>>() { { 1, new List<int> { 1, 2 } }, { 2, new List<int> { 4, 3, 5 } } };
            HashSet = new HashSet<int>() { 1,2,3};
            SortedSet = new SortedSet<int>() { 3, 1, 2 };
            GenericStack = new Stack<int>();
            GenericStack.Push(1);
            GenericStack.Push(2);
            GenericQueue = new Queue<int>();
            GenericQueue.Enqueue(1);
            GenericQueue.Enqueue(2);
            NonGenericStack = new Stack();
            NonGenericStack.Push(1);
            NonGenericStack.Push("adfscv");
            Queue randomQueue = new Queue();
            randomQueue.Enqueue(1);
            randomQueue.Enqueue("bf");
            NonGenericStack.Push(randomQueue);
            GenericTuple = Tuple.Create(1, "dsvgc");
        }
        public Artifical(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Number = 13;
            Age = age;
            this.ListOfInt = new List<int> { 1, 2, 3 };
            this.Matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            GenericStack = new Stack<int>();
            GenericStack.Push(1);
            GenericStack.Push(2);
            GenericQueue = new Queue<int>();
            GenericQueue.Enqueue(1);
            GenericQueue.Enqueue(2);
            HashSet = new HashSet<int>() { 1, 2, 3 };
            SortedSet = new SortedSet<int>() { 3, 1, 2 };
            GenericTuple = Tuple.Create(1, "dsvgc");
            Dictionary = new Dictionary<int, List<int>>() { { 1, new List<int> { 1, 2 } }, { 2, new List<int> { 4, 3, 5 } } };
            this.GenericSortedList = new SortedList<int, string> { { 2, "adf" }, { 1, "dgbv" } };
            this.ArrayList = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>() { 1, 2 } };
            NonGenericStack = new Stack();
            NonGenericStack.Push(1);
            NonGenericStack.Push("adfscv");
            Queue randomQueue = new Queue();
            randomQueue.Enqueue(1);
            randomQueue.Enqueue("bf");
            NonGenericStack.Push(randomQueue);
            this.NonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } };
            this.HashTable = new Hashtable() { { 1, 3 }, { 2, "dsvg" }, { "dyfbdyfcb", true } };
        }
    }
}
