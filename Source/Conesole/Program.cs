﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cone;
using System.Reflection;

namespace Conesole
{
    class Program
    {
        static void Main(string[] args) {
            try {
                ConePad.RunTests(Console.Out, args.Select(x => Assembly.LoadFrom(x)));
            } catch(ReflectionTypeLoadException tle) {
                foreach(var item in tle.LoaderExceptions)
                    Console.Error.WriteLine("{0}\n---", item);
            } catch(Exception e) {
                Console.Error.WriteLine(e);
            }
        }
    }
}