using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NitroBolt.CodeGeneration
{
    class Program
    {
        static void Main(string[] args)
        {
            switch (args.FirstOrDefault())
            {
                case "--monitor":
                    CodeChangeMonitor.Monitor(args.ElementAtOrDefault(1));
                    break;
                case "--process":
                    CodeChangeMonitor.ProcessAll(args.ElementAtOrDefault(1));
                    break;
            }
        }
    }
}
