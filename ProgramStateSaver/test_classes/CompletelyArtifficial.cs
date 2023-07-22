using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramStateSaver
{
    internal class CompletelyArtifficial : Saveable
    {
        [Save]
        public Queue<SortedList<Dictionary<int, List<ArrayList>>, HashSet<SortedSet<int>>>> Queue {get; set;}

        public CompletelyArtifficial()
        {
            HashSet<SortedSet<int>> hashSet = new HashSet<SortedSet<int>> { new SortedSet<int> { 3, 1, 2}, new SortedSet<int> { 5, 6, 4 } };
            List<ArrayList> list = new List<ArrayList> { new ArrayList { "randomString", 5, 1.3 }, new ArrayList { (float)1.5, (decimal)2.7 } };
            Dictionary<int, List<ArrayList>> dictionary = new Dictionary<int, List<ArrayList>> { { 1 , list }, { 5, list } };
            SortedList<Dictionary<int, List<ArrayList>>, HashSet<SortedSet<int>>> sortedList =
                new SortedList<Dictionary<int, List<ArrayList>>, HashSet<SortedSet<int>>>();
            sortedList[dictionary] = hashSet;
            Queue = new Queue<SortedList<Dictionary<int, List<ArrayList>>, HashSet<SortedSet<int>>>>();
            Queue.Enqueue(sortedList);
        }

    }
}
