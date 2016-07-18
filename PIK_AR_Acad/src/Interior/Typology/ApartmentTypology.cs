using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;

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
            var sel = Select();

            // Определение квартир
            SchemeBlock scheme;
            var apartments = ApartmentBlock.GetApartments(sel, out scheme);
            Ed.WriteMessage($"\nОпределено блков квартир - {apartments.Count}");

            // группировка квартир
            List<IGrouping<ApartmentBlock, ApartmentBlock>> groupApartments = null;
            if (Options.Instance.SortColumn == SortColumnEnum.PIK1)
            {
                groupApartments = apartments.GroupBy(g => g).OrderBy(o => o.Key.Type).ThenBy(o => o.Key).ToList();
            }
            else if (Options.Instance.SortColumn == SortColumnEnum.Chronology)
            {
                groupApartments = apartments.GroupBy(g => g).
                    OrderBy(o => o.Key.NameChronology, AcadLib.Comparers.AlphanumComparator.New).ToList();
            }

            using (var t = Db.TransactionManager.StartTransaction())
            {
                TopologyTable tableService = new TopologyTable(groupApartments, scheme, Db);
                tableService.CalcRows();
                var table = tableService.Create();
                var scale = AcadLib.Scale.ScaleHelper.GetCurrentAnnoScale(Db);
                table.TransformBy(Matrix3d.Scaling(scale, table.Position));

                TopologyTableSetions tableServiceSections = new TopologyTableSetions(groupApartments, scheme, Db);
                tableServiceSections.CalcRows();
                var tableSec = tableServiceSections.Create();
                tableSec.Position = new Point3d(table.Position.X, table.Position.Y - table.Height - 10 * scale, 0);
                tableSec.TransformBy(Matrix3d.Scaling(scale, tableSec.Position));

                ObjectId[] ids = new ObjectId[2];
                ids[0] = table.Id;
                ids[1] = tableSec.Id;

                AcadLib.Jigs.DragSel.Drag(Doc.Editor, ids.ToArray(), Point3d.Origin);

                t.Commit();
            }
        }

        private IEnumerable<ObjectId> Select ()
        {
            var selOpt = new PromptSelectionOptions();
            
            selOpt.Keywords.Add("Options");
            var keys = selOpt.Keywords.GetDisplayString(true);
            selOpt.MessageForAdding = "\nВыбор блоков: " + keys;
            selOpt.KeywordInput += SelOpt_KeywordInput;
            var selRes = Ed.GetSelection(selOpt);

            if (selRes.Status!= PromptStatus.OK)
            {
                throw new Exception(AcadLib.General.CanceledByUser);
            }

            var selIds = selRes.Value.GetObjectIds();
                        
            Ed.WriteMessage($"\nВыбрано блоков - {selIds.Length}");
            return selIds;
        }

        private void SelOpt_KeywordInput (object sender, SelectionTextInputEventArgs e)
        {
            Options.PromptOptions();
        }
    }
}
