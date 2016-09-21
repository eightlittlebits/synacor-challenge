using System;
using System.Diagnostics;
using System.IO;

namespace synacor
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length != 1)
            {
                Console.WriteLine("Usage: synacor <challenge binary>");
                return;
            }

            string filePath = args[0];

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File not found: {filePath}");
                return;
            }
            
            byte[] challengeDataRaw = File.ReadAllBytes(filePath);
            
            // convert raw challenge data to 16-bit unsigned ints
            ushort[] challengeData = new ushort[challengeDataRaw.Length / 2];
            Buffer.BlockCopy(challengeDataRaw, 0, challengeData, 0, challengeDataRaw.Length);

            SynacorVM vm = new SynacorVM();
            vm.Load(challengeData);
            vm.Run();

            if (Debugger.IsAttached)
            {
                Console.ReadLine();
            }
        }
    }
}
