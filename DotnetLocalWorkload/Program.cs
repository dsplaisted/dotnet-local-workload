using System;
using System.Collections.Generic;
using System.CommandLine;

namespace DotnetLocalWorkload
{
    class Program
    {
        static void Main(string[] args)
        {
            var rootCommand = new RootCommand
            {
            };

            var installCommand = new Command("install");
            
            var workloadArgument = new Argument("workload")
            {
                Arity = ArgumentArity.OneOrMore,
            };
            installCommand.AddArgument(workloadArgument);
            rootCommand.Add(installCommand);

            var parseResult = rootCommand.Parse(args);

            if (parseResult.CommandResult.Command == installCommand)
            {
                var argumentResult = parseResult.ValueForArgument<List<string>>(workloadArgument);
                Console.WriteLine("Workloads to install: " + string.Join(' ', argumentResult));
            }
            else
            {
                Console.WriteLine("Unknown command");
            }
            

            
        }
    }
}
