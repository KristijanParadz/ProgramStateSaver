﻿using System.Collections.Generic;
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
        public int Number;

        
        [Save]
        public int Age { get; set; }

        [Save]
        public List<int> lista;

        [Save]
        public List<List<int>> matrix;

        [Save]
        public Stack<int> genericStack;

        [Save]
        public Queue<int> genericQueue;

        [Save]
        public HashSet<int> hashSet;

        [Save]
        public SortedSet<int> sortedSet;

        [Save]
        public Tuple<int, string> genericTuple;

        [Save]
        public Dictionary<int, List<int>> dictionary;

        [Save]
        public SortedList<int,string> genericSortedList;

        [Save]
        public ArrayList arrayLista;

        [Save]
        public int[] array = { 1, 2, 3 };

        [Save]
        public Stack nonGenericStack;

        [Save]
        public SortedList nonGenericSortedList;

        [Save]
        public Hashtable hashTable;



        public Person() {
            FirstName = "Default first name";
            LastName = "Default last name";
            Number = 0;
            Age = 0;
            this.lista = new List<int> { 1, 2, 3 };
            this.genericSortedList= new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } };
            this.nonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>(){ 1, 2 },
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } }};
            this.hashTable = new Hashtable() { { 1,3 }, { 2, "dsvg" }, { "dyfbdyfcb", true  } };
            dictionary = new Dictionary<int, List<int>>() { { 1, new List<int> { 1, 2 } }, { 2, new List<int> { 4, 3, 5 } } };
            hashSet = new HashSet<int>() { 1,2,3};
            sortedSet = new SortedSet<int>() { 3, 1, 2 };
            genericStack = new Stack<int>();
            genericStack.Push(1);
            genericStack.Push(2);
            genericQueue = new Queue<int>();
            genericQueue.Enqueue(1);
            genericQueue.Enqueue(2);
            nonGenericStack = new Stack();
            nonGenericStack.Push(1);
            nonGenericStack.Push("adfscv");
            Queue randomQueue = new Queue();
            randomQueue.Enqueue(1);
            randomQueue.Enqueue("bf");
            nonGenericStack.Push(randomQueue);
            genericTuple = Tuple.Create(1, "dsvgc");
        }
        public Person(string firstName, string lastName, int age)
        {
            FirstName = firstName;
            LastName = lastName;
            Number = 13;
            //Age = age;
            this.lista = new List<int> { 1, 2, 3 };
            this.matrix = new List<List<int>> { new List<int> { 1, 2, 3 }, new List<int> { 4, 5, 6 } };
            genericStack = new Stack<int>();
            genericStack.Push(1);
            genericStack.Push(2);
            genericQueue = new Queue<int>();
            genericQueue.Enqueue(1);
            genericQueue.Enqueue(2);
            hashSet = new HashSet<int>() { 1, 2, 3 };
            sortedSet = new SortedSet<int>() { 3, 1, 2 };
            genericTuple = Tuple.Create(1, "dsvgc");
            dictionary = new Dictionary<int, List<int>>() { { 1, new List<int> { 1, 2 } }, { 2, new List<int> { 4, 3, 5 } } };
            this.genericSortedList = new SortedList<int, string> { { 2, "adf" }, { 1, "dgbv" } };
            this.arrayLista = new ArrayList { "asfd", 1, true, new ArrayList { 1, 2, "sfdgv" }, new HashSet<int>() { 1, 2 } };
            nonGenericStack = new Stack();
            nonGenericStack.Push(1);
            nonGenericStack.Push("adfscv");
            Queue randomQueue = new Queue();
            randomQueue.Enqueue(1);
            randomQueue.Enqueue("bf");
            nonGenericStack.Push(randomQueue);
            this.nonGenericSortedList = new SortedList { { 2, "adf" }, { 1, "dgbv" } };
            new SortedList<int,string> { {2, "adf" }, { 1, "dgbv" } };
            this.hashTable = new Hashtable() { { 1, 3 }, { 2, "dsvg" }, { "dyfbdyfcb", true } };
        }
    }
}
