using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;
using AcadLib.Errors;

namespace PIK_AR_Acad.Interior.Typology
{
    public class TopologyTable : AcadLib.Tables.CreateTable
    {        
        private List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments;
        SchemeBlock scheme;

        public TopologyTable (List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments, SchemeBlock scheme, Database db) : base(db)
        {
            this.apartments = apartments;
            LwBold = LineWeight.ByLayer;
            this.scheme = scheme; 
        }

        public override void CalcRows ()
        {
            NumColumns = 6;
            NumRows = apartments.Count + 3;
            Title = $"Типология квартир - {scheme?.Name} : {DateTime.Now}";
        }        

        protected override void SetColumnsAndCap (ColumnsCollection columns)
        {
            // столбец № п/п
            var col = columns[0];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 24;
            col[1, 0].TextString = "№ п/п";
            
            // столбец Тип хрон
            col = columns[1];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 24;
            col[1, 1].TextString = "Тип хрон.";

            // столбец Тип квартир
            col = columns[2];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 24;
            col[1, 2].TextString = "Тип квартир";
            
            // столбец Имя в системе PIK 1
            col = columns[3];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 48;
            col[1, 3].TextString = "Имя";

            // столбец Схема
            col = columns[4];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 72;            
            col[1, 4].TextString = "Схема";

            // столбец Количество
            col = columns[5];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 30;
            col[1, 5].TextString = "Коли- чество";
        }

        protected override void FillCells (Table table)
        {
            //var cs = db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            //cs.AppendEntity(table);
            //db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(table, true);            

            table.Rows[0].Height = 36;
            table.Rows[1].Height = 30;
            table.Cells.TextHeight = 5;
            table.Cells[0, 0].TextHeight = 7;
            table.LineWeight = LineWeight.ByLayer;

            if (Options.Instance.SortColumn == SortColumnEnum.PIK1)
            {
                var groupApartments = apartments.GroupBy(g => g.Key.Type).OrderBy(o => o.Key).ToList();
                SetCells(table, groupApartments);

                // Объединение одинаковых хронологических марок
                MergeColCells(table, 1);
            }
            else if (Options.Instance.SortColumn == SortColumnEnum.Chronology)
            {
                var groupApartments = apartments.GroupBy(g => g.Key.NameChronology).
                    OrderBy(o => o.Key, AcadLib.Comparers.AlphanumComparator.New).ToList();
                SetCells(table, groupApartments);

                // Объединение одинаковых типов квартир
                MergeColCells(table, 1, 0);
                MergeColCells(table, 2, 90);
            }                       

            table.Columns[4].Borders.Bottom.Margin = 4;
            table.Columns[4].Borders.Top.Margin = 4;

            // итого    
            var lastRow = table.Rows.Count - 1;
            var mCells = CellRange.Create(table, lastRow, 0, lastRow, 4);
            table.MergeCells(mCells);
            var cell = table.Cells[lastRow, 0];
            cell.TextString = "Итого";
            cell = table.Cells[lastRow, 5];
            cell.TextString = apartments.Sum(s => s.Count()).ToString();
        }

        /// <summary>
        /// Заполнение строк квартир сгруппированным по типам квартир
        /// </summary>        
        private void SetCells (Table table, 
            List<IGrouping<ApartmentType, IGrouping<ApartmentBlock, ApartmentBlock>>> groupApartments)
        {
            int count = 1;
            int row = 2;
            Cell cell;
            CellRange mCells;
            foreach (var group in groupApartments)
            {
                if (group.Skip(1).Any())
                {
                    // Группировка строк Типов квартир
                    mCells = CellRange.Create(table, row, 2, row + group.Count() - 1, 2);
                    table.MergeCells(mCells);
                }

                cell = table.Cells[row, 2];
                cell.TextString = group.Key.Names;
                var first = group.First().Key;
                cell.BackgroundColor = first.Color;
                cell.Contents[0].Rotation = 90.0.ToRadians();

                foreach (var apart in group)
                {
                    table.Rows[row].Height = 20;
                    cell = table.Cells[row, 0];
                    cell.TextString = count++.ToString();

                    cell = table.Cells[row, 1];
                    cell.TextString = apart.Key.NameChronology;

                    cell = table.Cells[row, 3];
                    cell.TextString = apart.Key.Name;

                    cell = table.Cells[row, 4];
                    cell.BlockTableRecordId = GetApartCellBtr(apart);
                    var blockContent = cell.Contents[0];
                    blockContent.IsAutoScale = false;
                    blockContent.Scale = (1 / scale) * 0.4;
                    blockContent.ContentColor = first.Color;

                    cell = table.Cells[row, 5];
                    cell.TextString = apart.Count().ToString();

                    row++;
                }
            }
        }

        /// <summary>
        /// Заполнение строк квартир сгруппированным по хронологоическому номеру
        /// </summary>        
        private void SetCells (Table table, 
            List<IGrouping<string, IGrouping<ApartmentBlock, ApartmentBlock>>> groupApartments)
        {
            int count = 1;
            int row = 2;
            Cell cell;
            CellRange mCells;
            foreach (var group in groupApartments)
            {
                foreach (var apart in group)
                {
                    table.Rows[row].Height = 20;
                    cell = table.Cells[row, 0];
                    cell.TextString = count++.ToString();

                    cell = table.Cells[row, 1];
                    cell.TextString = apart.Key.NameChronology;

                    cell = table.Cells[row, 2];
                    cell.TextString = apart.Key.Type.Names;
                    cell.BackgroundColor = apart.Key.Color;

                    cell = table.Cells[row, 3];
                    cell.TextString = apart.Key.Name;

                    cell = table.Cells[row, 4];
                    cell.BlockTableRecordId = GetApartCellBtr(apart);
                    var blockContent = cell.Contents[0];
                    blockContent.IsAutoScale = false;
                    blockContent.Scale = (1 / scale) * 0.4;
                    blockContent.ContentColor = apart.Key.Color;

                    cell = table.Cells[row, 5];
                    cell.TextString = apart.Count().ToString();

                    row++;
                }
            }
        }

        private ObjectId GetApartCellBtr(IGrouping<ApartmentBlock, ApartmentBlock> apart)
        {
            // Если все блоки одинаковые, то берем блок, как в них, если нет, то дефолтный блок квартиры            
            if (apart.All(a=>a.IdBtrAnonym==apart.Key.IdBtrAnonym))
            {
                // Если они все не динамические
                if (apart.Key.IdBtrAnonym == ObjectId.Null)
                    return apart.Key.IdBtr;
                else
                    return apart.Key.IdBtrAnonym;
                            
            }
            else
            {
                // Разные динамические блоки одной квартиры - предупреждение
                Inspector.AddError($"Разные динамические блоки квартиры в схеме '{apart.Key.Name}'",
                    apart.Key.IdBlRef, System.Drawing.SystemIcons.Exclamation);
                return apart.Key.IdBtr;
            }
        }

        private void MergeColCells (Table table, int columnIndex, double rotate =0)
        {
            string lastText = null;
            int lastIndex = 0;
            for (int i = 2; i < table.Rows.Count; i++)
            {
                var cell = table.Cells[i, columnIndex];

                if (lastText != cell.TextString)
                {
                    if (lastText != null)
                    {
                        var mCells = CellRange.Create(table, lastIndex, columnIndex, i - 1, columnIndex);
                        table.MergeCells(mCells);
                        if (rotate !=0)
                        {
                            var cellMerged = table.Cells[lastIndex, columnIndex];
                            cellMerged.Contents[0].Rotation = rotate.ToRadians();
                        }
                    }
                    if (cell.TextString == "-" || string.IsNullOrWhiteSpace(cell.TextString))
                    {
                        lastIndex = 0;
                        lastText = null;
                    }
                    else
                    {
                        lastText = cell.TextString;
                        lastIndex = i;
                    }
                }
            }
        }

        public void Insert (ObjectId idTable, Document doc)
        {
            AcadLib.Jigs.DragSel.Drag(doc.Editor, new ObjectId[] { idTable }, Point3d.Origin);
        }
    }
}
