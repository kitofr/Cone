﻿using System;
using System.Linq;
using System.Reflection;
using Cone;
using Cone.Runners;

namespace Conesole
{
    class Program
    {
        static void Main(string[] args) {
            try {
                new SimpleConeRunner().RunTests(new ConePad.ConsoleLogger(), args.Select(x => Assembly.LoadFrom(x)));
            } catch(ReflectionTypeLoadException tle) {
                foreach(var item in tle.LoaderExceptions)
                    Console.Error.WriteLine("{0}\n---", item);
            } catch(Exception e) {
                Console.Error.WriteLine(e);
            }
        }
    }
}
