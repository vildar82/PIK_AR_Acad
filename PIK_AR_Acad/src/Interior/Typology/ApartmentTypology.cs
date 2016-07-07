using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;

namespace PIK_AR_Acad.Interior.Typology
{
    /// <summary>
    /// Типология квартир в схеме дома
    /// </summary>
    public class ApartmentTypology
    {
        public Document Doc { get; set; }
        public Database Db { get; set; }
        public Editor Ed { get; set; }

        public ApartmentTypology (Document doc)
        {
            Doc = doc;
            Db = doc.Database;
            Ed = doc.Editor;
        }

        public void CreateTableTypology ()
        {
            // Выбор блоков квартир (схема дома)
            var sel = Ed.SelectBlRefs("\nВыбор блоков квартир (схемы дома):");
            Ed.WriteMessage($"\nВыбрано блоков - {sel.Count}");

            // Определение квартир
            var apartments = ApartmentBlock.GetApartments (sel);
            Ed.WriteMessage($"\nОпределено блков квартир - {apartments.Count}");

            // группировка квартир по типам
            var groupApartments = apartments.GroupBy(g=>g).OrderBy(o=>o.Key.Type).ThenBy(o=>o.Key).ToList();

            TopologyTable tableService = new TopologyTable (groupApartments, Db);
            tableService.CalcRows();
            var table = tableService.Create();
            tableService.Insert(table, Doc);
        }
    }
}
