using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.EditorInput;
using Autodesk.AutoCAD.Geometry;
using AcadLib.Tables;

namespace PIK_AR_Acad.Interior.Typology
{
    /// <summary>
    /// Типология квартир в схеме дома
    /// </summary>
    public class ApartmentTypology
    {
        private const string modePIK1 = "PIK1";
        private const string modeChronology = "Chronology";
        private const string modeSection = "Section";
        private const string modeSU = "SU";
        private const string modeOptions = "Options";
        //private static string modeCurrent = "PIK1";

        public Document Doc { get; set; }
        public Database Db { get; set; }
        public Editor Ed { get; set; }

        public static Dictionary<ApartmentType, ApartmentLayer> ApartmentLayers = new Dictionary<ApartmentType, ApartmentLayer> {
            { ApartmentType.Studio, new ApartmentLayer (ApartmentType.Studio, "S_Квартиры_0_студия", Color.FromRgb(233,107,130)) },
            { ApartmentType.OneBedroom, new ApartmentLayer (ApartmentType.OneBedroom, "S_Квартиры_1", Color.FromRgb(139,168,222)) },
            { ApartmentType.TwoBedroom, new ApartmentLayer (ApartmentType.TwoBedroom, "S_Квартиры_2", Color.FromRgb(228,183,24)) },
            { ApartmentType.ThreeBedroom, new ApartmentLayer (ApartmentType.ThreeBedroom, "S_Квартиры_3", Color.FromRgb(148,183,28)) },
            { ApartmentType.FourBedroom, new ApartmentLayer (ApartmentType.FourBedroom, "S_Квартиры_4", Color.FromRgb(232,133,85)) },
            { ApartmentType.FiveBedroom, new ApartmentLayer (ApartmentType.FiveBedroom, "S_Квартиры_5", Color.FromRgb(1,1,1)) }
        };

        public ApartmentTypology (Document doc)
        {
            Doc = doc;
            Db = doc.Database;
            Ed = doc.Editor;
        }

        public void Start()
        {            
            // Выбор режима работы: PIK1, Crhronology, По секциям, СУ, Настройки
            var prOpt = new PromptKeywordOptions("");
            prOpt.Keywords.Add(modePIK1, "ПИК1");
            prOpt.Keywords.Add(modeChronology, "Хронология");
            prOpt.Keywords.Add(modeSection, "ПоСекциям");
            //prOpt.Keywords.Add(modeSU, "СУ");
            prOpt.Keywords.Add(modeOptions, "Настроки");
            prOpt.Keywords.Default = modePIK1;
            //try
            //{
            //    prOpt.Keywords.Default = modeCurrent;
            //}
            //catch { }
            var prRes = Ed.GetKeywords(prOpt);
            if (prRes.Status != PromptStatus.OK)
            {
                return;
            }
            //modeCurrent = prRes.StringResult;
            switch (prRes.StringResult)
            {
                case modePIK1:
                    Options.Instance.SortColumn = SortColumnEnum.PIK1;
                    CreateApartTable();
                    break;
                case modeChronology:
                    Options.Instance.SortColumn = SortColumnEnum.Chronology;
                    CreateApartTable();
                    break;
                case modeSection:
                    CreateSectionTable();
                    break;
                case modeOptions:
                    Options.PromptOptions();
                    break;                
            }
        }

        private void CreateApartTable()
        {            
            SchemeBlock scheme;
            var  groupApartments = GetApartments(out scheme);
            CreateTable(new TopologyTable(groupApartments, scheme, Db));            
        }

        private void CreateSectionTable()
        {
            SchemeBlock scheme;
            var groupApartments = GetApartments(out scheme);
            CreateTable(new TopologyTableSections(groupApartments, scheme, Db));
        }

        private void CreateTable(ICreateTable itable)
        {
            itable.CalcRows();
            var table = itable.Create();
            itable.Insert(table, Doc);
        }

        private List<IGrouping<ApartmentBlock, ApartmentBlock>> GetApartments(out SchemeBlock scheme)
        {            
            // Слои для квартир
            DefineApartmentLayers();

            var sel = Select();
            // Определение квартир
            var apartments = ApartmentBlock.GetApartments(sel, out scheme);
            Ed.WriteMessage($"\nОпределено блков квартир - {apartments.Count}");

            // группировка квартир
            List<IGrouping<ApartmentBlock, ApartmentBlock>>  groupApartments = null;
            if (Options.Instance.SortColumn == SortColumnEnum.PIK1)
            {
                groupApartments = apartments.GroupBy(g => g).OrderBy(o => o.Key.Type).ThenBy(o => o.Key).ToList();
            }
            else if (Options.Instance.SortColumn == SortColumnEnum.Chronology)
            {
                groupApartments = apartments.GroupBy(g => g).
                    OrderBy(o => o.Key.NameChronology, AcadLib.Comparers.AlphanumComparator.New).ToList();
            }
            return groupApartments;
        }

        private void CreateTableTypology ()
        {
            var sel = Select();

            // Слои для квартир
            DefineApartmentLayers();

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
                var tableService = new TopologyTable(groupApartments, scheme, Db);
                tableService.CalcRows();
                var table = tableService.Create();
                var scale = AcadLib.Scale.ScaleHelper.GetCurrentAnnoScale(Db);
                table.TransformBy(Matrix3d.Scaling(scale, table.Position));

                var tableServiceSections = new TopologyTableSections(groupApartments, scheme, Db);
                tableServiceSections.CalcRows();
                var tableSec = tableServiceSections.Create();
                tableSec.Position = new Point3d(table.Position.X, table.Position.Y - table.Height - 10 * scale, 0);
                tableSec.TransformBy(Matrix3d.Scaling(scale, tableSec.Position));

                ObjectId[] ids = new ObjectId[2];
                ids[0] = table.Id;
                ids[1] = tableSec.Id;

                AcadLib.Jigs.DragSel.Drag(Doc.Editor, ids, Point3d.Origin);

                t.Commit();
            }            
        }

        private void DefineApartmentLayers ()
        {
            using (var t = Db.TransactionManager.StartTransaction())
            {
                var lt = Db.LayerTableId.GetObject(OpenMode.ForRead) as LayerTable;

                foreach (var item in ApartmentLayers)
                {
                    var apartLay = item.Value;
                    LayerTableRecord lay;
                    if (lt.Has(item.Value.Name))
                    {
                        lay = lt[apartLay.Name].GetObject( OpenMode.ForWrite) as LayerTableRecord;                        
                    }
                    else
                    {
                        lt.UpgradeOpen();
                        lay = new LayerTableRecord();
                        lay.Name = apartLay.Name;
                        lt.Add(lay);
                        t.AddNewlyCreatedDBObject(lay, true);
                    }

                    if (lay.Color != apartLay.Color)                    
                        lay.Color = apartLay.Color;
                    apartLay.Id = lay.Id;                    
                }
                t.Commit();
            }
        }

        private IEnumerable<ObjectId> Select ()
        {
            var selOpt = new PromptSelectionOptions();
            
            //selOpt.Keywords.Add("Options");
            //var keys = selOpt.Keywords.GetDisplayString(true);
            selOpt.MessageForAdding = "\nВыбор блоков: ";// + keys;
            //selOpt.KeywordInput += SelOpt_KeywordInput;
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
