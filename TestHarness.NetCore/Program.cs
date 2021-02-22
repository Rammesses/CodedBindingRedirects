using System;
using System.Threading.Tasks;

namespace CodingBindingRedirects.TestHarness
{
    class Program
    {
        static async Task Main(string[] args)
        {
            CodedBindingRedirects.BindingRedirects.Apply();

            await Task.Delay(1000);

            Console.WriteLine("\nPress ENTER to quit.");
        }
    }
}
