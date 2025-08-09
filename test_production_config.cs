using System;
using LegacyModernization.Core.Configuration;

namespace TestProductionConfig
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Testing Production Configuration (Strict Environment Variables)");
            Console.WriteLine("================================================================");
            
            try
            {
                var config = PipelineConfiguration.CreateFromEnvironment();
                Console.WriteLine("✅ Production configuration loaded successfully!");
                Console.WriteLine(config.ToString());
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine("❌ Production configuration failed (as expected):");
                Console.WriteLine(ex.Message);
            }
        }
    }
}
