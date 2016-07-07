using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;

namespace PIK_AR_Acad.LayoutSort
{
    public static class LayoutSortService
    {
        public static Document Doc { get; set; }

        public static void Sort()
        {
            Doc = Application.DocumentManager.MdiActiveDocument;
        }
    }
}
