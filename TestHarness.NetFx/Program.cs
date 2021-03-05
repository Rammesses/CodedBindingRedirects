using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TestHarness.NetFx
{
    class Program
    {
        [CodedBindingRedirects.AutoGenerateBindingRedirect]
        static void Main(string[] args)
        {
            CodedBindingRedirects.BindingRedirects.Apply();
        }
    }
}
