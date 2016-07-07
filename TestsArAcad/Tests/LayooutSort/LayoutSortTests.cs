using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;
using PIK_AR_Acad.LayoutSort;

namespace TestsArAcad.Tests.LayooutSort
{
    [TestFixture()]
    public class LayoutSortTests
    {
        [OneTimeSetUp]
        public void Init ()
        {
        }

        [Test(Description = "Показ всех листов в чертеже")]
        public void ShowAll ()
        {            
            LayoutSortService.Sort();
        }
    }
}
