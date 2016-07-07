using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib;

namespace PIK_AR_Acad.Interior.Typology
{
    public class TopologyTable : AcadLib.Tables.CreateTable
    {        
        private List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments;
        
        public TopologyTable (List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments, Database db) : base(db)
        {
            this.apartments = apartments;            
        }

        public override void CalcRows ()
        {
            NumColumns = 5;
            NumRows = apartments.Count + 3;
            Title = "Типология квартир " + DateTime.Now;            
        }        

        protected override void SetColumnsAndCap (ColumnsCollection columns)
        {
            // столбец № п/п
            var col = columns[0];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 10;
            col[1, 0].TextString = "№ п/п";
            
            // столбец Тип квартир
            col = columns[1];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 10;
            col[1, 1].TextString = "Тип квартир";
            
            // столбец Имя в системе PIK 1
            col = columns[2];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 30;
            col[1, 2].TextString = "Имя в системе PIK 1";

            // столбец Схема
            col = columns[3];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 25;
            col[1, 3].TextString = "Схема";

            // столбец Количество
            col = columns[4];
            col.Alignment = CellAlignment.MiddleCenter;
            col.Width = 25;
            col[1, 4].TextString = "Количество";
        }

        protected override void FillCells (Table table)
        {
            int count = 1;
            int row = 2;
            Cell cell;
            CellRange mCells;

            var groupApartments = apartments.GroupBy(g=>g.Key.Type);            

            foreach (var group in groupApartments)
            {
                if (group.Skip(1).Any())
                { 
                    // Группировка строк Типов квартир
                    mCells = CellRange.Create(table, row, 1, row+group.Count()-1, 1);
                    table.MergeCells(mCells);                    
                }

                cell = table.Cells[row, 1];
                cell.TextString = group.Key.Names;
                cell.BackgroundColor = group.Key.Color;
                cell.Contents[0].Rotation = 90.0.ToRadians();

                foreach (var apart in group)
                {
                    cell = table.Cells[row, 0];
                    cell.TextString = count++.ToString();
                    cell.BackgroundColor = group.Key.Color;

                    //cell = table.Cells[row, 1];
                    //cell.TextString = group.Key.Type.Names;
                    //cell.BackgroundColor = group.Key.Type.Color;

                    cell = table.Cells[row, 2];
                    cell.TextString = apart.Key.Name;
                    cell.BackgroundColor = group.Key.Color;

                    cell = table.Cells[row, 3];
                    cell.BlockTableRecordId = apart.Key.IdBtr;
                    var blockContent = cell.Contents[0];
                    blockContent.IsAutoScale = false;
                    blockContent.Scale = (1 / scale) * 0.5;
                    blockContent.ContentColor = group.Key.Color;

                    cell = table.Cells[row, 4];
                    cell.TextString = apart.Count().ToString();

                    row++;
                }
            }

            // итого    
            mCells = CellRange.Create(table, row, 0, row, 3);
            table.MergeCells(mCells);
            cell = table.Cells[row, 0];
            cell.TextString = "Итого";
            cell = table.Cells[row, 4];
            cell.TextString = apartments.Sum(s=>s.Count()).ToString();

            table.Columns[1].Width = 10;
        }
    }
}
