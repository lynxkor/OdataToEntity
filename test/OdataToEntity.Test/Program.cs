﻿using System;
using System.Linq.Expressions;

namespace OdataToEntity.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            //new BatchTest().Add().Wait();
            Console.WriteLine();
            new SelectTest(new DbFixtureInitDb()).KeyComposition().Wait();

            Console.ReadLine();
        }
    }
}
