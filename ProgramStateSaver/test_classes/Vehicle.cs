using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProgramStateSaver
{
    internal class Vehicle : Saveable
    {
        [Save]
        public string Manufacturer { get; set; } = "Audi";
        [Save]
        public string Model { get; set; } = "A3";
        [Save]
        public Stack<string> Colors { get; set; } = new Stack<string>();
        [Save]
        public double CurrentSpeed { get; private set; } = 87.5;
        [Save]
        public bool EngineStarted { get; private set; } = true;
        [Save]
        public decimal EngineStrength { get; private set; } = 120.93M;
        [Save]
        public int PassengerCapacity { get; set; } = 5;
        [Save]
        public Hashtable RandomInformation { get; set; } = new Hashtable();
        public Vehicle()
        {
            Colors.Push("Blue");
            Colors.Push("Red");
            Colors.Push("Black");
            Colors.Push("Yellow");
            RandomInformation["FuelCapacity"] = (float)60.3;
            RandomInformation["Sold"] = true;
            RandomInformation["Year"] = 2017;
            RandomInformation["Engine"] = "Some engine";
        }
    }
}
