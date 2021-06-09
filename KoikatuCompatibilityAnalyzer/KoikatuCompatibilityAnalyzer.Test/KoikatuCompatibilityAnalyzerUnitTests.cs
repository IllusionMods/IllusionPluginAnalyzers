using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CSharp.Testing;
//using VerifyCS = KoikatuCompatibilityAnalyzer.Test.CSharpCodeFixVerifier<
//    KoikatuCompatibilityAnalyzer.KoikatuCompatibilityAnalyzerAnalyzer,
//    KoikatuCompatibilityAnalyzer.KoikatuCompatibilityAnalyzerCodeFixProvider>;
using VerifyA = KoikatuCompatibilityAnalyzer.Test.CSharpAnalyzerVerifier<KoikatuCompatibilityAnalyzer.KoikatuCompatibilityAnalyzer>;
namespace KoikatuCompatibilityAnalyzer.Test
{
    [TestClass]
    public class KoikatuCompatibilityAnalyzerUnitTest
    {
        //No diagnostics expected to show up
        [TestMethod]
        public async Task TestMethod1()
        {
            var test = @"";

            //await VerifyCS.VerifyAnalyzerAsync(test);
        }

        //Diagnostic and CodeFix both triggered and checked for
        [TestMethod]
        public async Task TestMethod2()
        {
            //VerifyA.Diagnostic()
            var test = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class {|#0:TypeName|}
        {   
            void test()
{
var xa= nameof(Encoding.ASCII);
var asd = Encoding.ASCII; 
//var a = Task.Delay(100, CancellationToken.None);
//var xa= nameof(H3PDarkHoushi.AfterProc);
}
        }
    }";

            var fixtest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using System.Diagnostics;

    namespace ConsoleApplication1
    {
        class TYPENAME
        {   
        }
    }";
            await VerifyA.VerifyAnalyzerAsync(test);
            //var expected = VerifyCS.Diagnostic("KoikatuCompatibilityAnalyzer").WithLocation(0).WithArguments("TypeName");
            //await VerifyCS.VerifyCodeFixAsync(test, expected, fixtest);
        }
    }
}
