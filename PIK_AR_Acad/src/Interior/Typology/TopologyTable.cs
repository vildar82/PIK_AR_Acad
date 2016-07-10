using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;
using Autodesk.AutoCAD.Geometry;

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
            col[1, 3].TextString = "Имя в системе PIK 1";

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
            var cs = db.CurrentSpaceId.GetObject( OpenMode.ForWrite) as BlockTableRecord;
            cs.AppendEntity(table);
            db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(table, true);                

            int count = 1;
            int row = 2;
            Cell cell;
            CellRange mCells;

            table.Rows[0].Height = 36;
            table.Rows[1].Height = 30;
            table.Cells.TextHeight = 5;
            table.Cells[0, 0].TextHeight = 7;
            table.LineWeight = LineWeight.ByLayer;

            var groupApartments = apartments.GroupBy(g=>g.Key.Type).OrderBy(o=>o.Key);            

            foreach (var group in groupApartments)
            {
                if (group.Skip(1).Any())
                { 
                    // Группировка строк Типов квартир
                    mCells = CellRange.Create(table, row, 2, row+group.Count()-1, 2);
                    table.MergeCells(mCells);                    
                }

                cell = table.Cells[row, 2];
                cell.TextString = group.Key.Names;
                cell.BackgroundColor = group.Key.Color;
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
                    cell.BlockTableRecordId = apart.Key.IdBtr;
                    var blockContent = cell.Contents[0];
                    blockContent.IsAutoScale = false;
                    blockContent.Scale = (1 / scale) * 0.4;
                    blockContent.ContentColor = group.Key.Color;                                        

                    cell = table.Cells[row, 5];
                    cell.TextString = apart.Count().ToString();

                    row++;
                }
            }

            table.Columns[4].Borders.Bottom.Margin = 4;
            table.Columns[4].Borders.Top.Margin = 4;

            // итого    
            mCells = CellRange.Create(table, row, 0, row, 4);
            table.MergeCells(mCells);
            cell = table.Cells[row, 0];
            cell.TextString = "Итого";
            cell = table.Cells[row, 5];
            cell.TextString = apartments.Sum(s=>s.Count()).ToString();            
        }

        public void Insert (ObjectId idTable, Document doc)
        {
            AcadLib.Jigs.DragSel.Drag(doc.Editor, new ObjectId[] { idTable }, Point3d.Origin);
        }
    }
}
