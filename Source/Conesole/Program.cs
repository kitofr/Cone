﻿using Cone.Core;
using Cone.Runners;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Xml;

namespace Conesole
{
	public class ConesoleConfiguration
    {
		const string OptionPrefix = "--";
		readonly Regex OptionPattern = new Regex(string.Format("^{0}(?<option>.+)=(?<value>.+$)", OptionPrefix));

		public Predicate<IConeTest> IncludeTest;
		public Predicate<IConeSuite> IncludeSuite = _ => true;  

		public LoggerVerbosity Verbosity = LoggerVerbosity.Default;
		public bool IsDryRun;
		public bool XmlOutput;
		public bool TeamCityOutput;
        public bool Multicore;

        public static ConesoleConfiguration Parse(params string[] args) {
			var result = new ConesoleConfiguration();
			foreach(var item in args)
        		result.ParseOption(item);
			if(result.IncludeTest == null)
				result.IncludeTest = _ => true;
        	return result;
        }

		public static bool IsOption(string value) {
			return value.StartsWith(OptionPrefix);
		}

		void ParseOption(string item) {
			if(!IsOption(item))
				return;

			if(item == "--labels") {
				Verbosity = LoggerVerbosity.Labels;
				return;
			} 

			if(item == "--test-names") {
				Verbosity = LoggerVerbosity.TestNames;
				return;
			}
			
			if(item == "--dry-run") {
				IsDryRun = true;
				return;
			}

			if(item == "--xml-console") {
				XmlOutput = true;
				return;
			}

			if(item == "--teamcity") {
				TeamCityOutput  = true;
				return;
			}

            if (item == "--multicore") {
                Multicore = true;
                return;
            }
            
            var m = OptionPattern.Match(item);
			if(!m.Success)
				throw new ArgumentException("Unknown option:" + item);

			var option = m.Groups["option"].Value;
			var valueRaw =  m.Groups["value"].Value;
			if(option == "include-tests") {
				var suitePattern = "*";
				var parts = valueRaw.Split('.');

				if(parts.Length > 1)
					suitePattern = string.Join(".", parts, 0, parts.Length - 1);
					
				var testPatternRegex = CreatePatternRegex(suitePattern + "." + parts.Last());
				var suitePatternRegex = CreatePatternRegex(suitePattern);

				IncludeSuite = IncludeSuite.Or(x => suitePatternRegex.IsMatch(x.Name));
				IncludeTest = IncludeTest.Or(x => testPatternRegex.IsMatch(x.TestName.FullName));
			}
			else if(option == "categories") {
				var excluded = new HashSet<string>();
				var included = new HashSet<string>();
				foreach(var category in valueRaw.Split(','))
					if(category.StartsWith("!"))
						excluded.Add(category.Substring(1));
					else 
						included.Add(category);
				
				IncludeSuite = IncludeSuite.And(x => (included.IsEmpty() || x.Categories.Any(included.Contains)) && !x.Categories.Any(excluded.Contains));
				IncludeTest = IncludeTest.And(x => (included.IsEmpty() || x.Categories.Any(included.Contains)) && !x.Categories.Any(excluded.Contains));
			}
			else 
				throw new ArgumentException("Unknown option:" + item);
		}
		static Regex CreatePatternRegex(string pattern) {
			return new Regex("^" + pattern
				.Replace("\\", "\\\\")
				.Replace(".", "\\.")
				.Replace("*", ".*?"));
		}
    }

    class Program : MarshalByRefObject
    {
		public string[] AssemblyPaths;
		public string[] Options;

		static int Main(string[] args) {
            if(args.Length == 0)
            	return DisplayUsage();

			var assemblyPaths = args
				.Where(x => !ConesoleConfiguration.IsOption(x))
				.ToArray();

			return CrossDomainConeRunner.WithProxyInDomain<Program, int>(
				Path.GetDirectoryName(Path.GetFullPath(assemblyPaths.FirstOrDefault() ?? ".")), 
				assemblyPaths, 
				runner => {
					runner.AssemblyPaths = assemblyPaths;
					runner.Options = args;
					return runner.Execute();
			});
        }

		int Execute(){
            try {
				var config = ConesoleConfiguration.Parse(Options);
            	var results = CreateTestSession(config);

				if(config.IsDryRun) {
					results.GetResultCollector = _ => (test, result) => result.Success();
				}

                new SimpleConeRunner() {
                    Workers = config.Multicore ? Environment.ProcessorCount : 1,
                }.RunTests(results, CrossDomainConeRunner.LoadTestAssemblies(AssemblyPaths));

            } catch (ReflectionTypeLoadException tle) {
                foreach (var item in tle.LoaderExceptions)
                    Console.Error.WriteLine("{0}\n---", item);
				return -1;
			} catch(ArgumentException e) {
				Console.Error.WriteLine(e.Message);
				return DisplayUsage();
            } catch (Exception e) {
                Console.Error.WriteLine(e);
                return -1;
            }

			return 0;
		}

        static TestSession CreateTestSession(ConesoleConfiguration config) {
            ISessionLogger sessionLogger;
            if (config.XmlOutput) {
                var xml = new XmlSessionLogger(new XmlTextWriter(Console.Out){
                    Formatting = Formatting.Indented
                });

                sessionLogger = xml;
			} else if(config.TeamCityOutput) {
				sessionLogger = new TeamCityLogger(Console.Out);
            } else {
                var consoleLogger = new ConsoleSessionLogger();
                consoleLogger.Settings.Verbosity = config.Verbosity;
                if (config.IsDryRun)
                    consoleLogger.Settings.SuccessColor = ConsoleColor.DarkGreen;

                sessionLogger = consoleLogger;
            }

            return new TestSession(sessionLogger) {
                IncludeSuite = config.IncludeSuite,
                ShouldSkipTest = x => !config.IncludeTest(x)
            };;
		}

    	static int DisplayUsage() {
    		using(var reader = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Conesole.Usage.txt"))) {
    			Console.WriteLine(reader.ReadToEnd());
    		}
    		return -1;
    	}
    }
}
