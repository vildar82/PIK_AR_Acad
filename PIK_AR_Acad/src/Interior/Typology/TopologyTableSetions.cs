using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib;
using Autodesk.AutoCAD.Colors;

namespace PIK_AR_Acad.Interior.Typology
{
    public class TopologyTableSetions : AcadLib.Tables.CreateTable
    {
        Color badColor = Color.FromColorIndex( ColorMethod.ByAci, 1);
        int rowNumberPP = 1;
        int rowType = 2;
        int rowMarkPik1 = 3;
        int rowMarkChrono = 4;
        int rowScheme = 5;
        int rowCountInFloor;
        int rowTotalInFloorByType;
        List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments;
        SchemeBlock scheme;

        public TopologyTableSetions (List<IGrouping<ApartmentBlock, ApartmentBlock>> apartments, SchemeBlock scheme, Database db) : base(db)
        {
            this.apartments = apartments;
            LwBold = LineWeight.ByLayer;
            this.scheme = scheme;
        }        

        public override void CalcRows ()
        {
            NumColumns = apartments.Count + 3;            
            NumRows = 8 + (scheme.Sections.Count >1? scheme.Sections.Count: 0);
            Title = $"Типология квартир на этаже - {scheme?.Name} : {DateTime.Now}";
        }

        protected override void SetColumnsAndCap (ColumnsCollection columns)
        {
            Cell cell;
            var col = columns[0];
            var table = col.ParentTable;           

            table.SetRowHeight(27);
            table.SetColumnWidth(65);
            table.Cells.Alignment = CellAlignment.MiddleCenter;
                        
            col.Width = 45;
            CellRange mCells = CellRange.Create(table, 1, 0, 1, 1);
            table.MergeCells(mCells);
            col[1, 0].TextString = "№ п/п";
            
            mCells = CellRange.Create(table,2, 0, 2, 1);
            table.MergeCells(mCells);
            col[2, 0].TextString = "Кол-во комнат";

            mCells = CellRange.Create(table, 3, 0, 3, 1);
            table.MergeCells(mCells);
            col[3, 0].TextString = "Марка PIK1";

            mCells = CellRange.Create(table, 4, 0, 4, 1);
            table.MergeCells(mCells);
            col[4, 0].TextString = "Марка хронол.";

            mCells = CellRange.Create(table, 5, 0, 5, 1);
            table.MergeCells(mCells);
            col[5, 0].TextString = "Схема";

            col = columns[1];            
            col.Width = 60;

            int rowSec = 6;
            if (scheme.Sections.Count > 1)
            {
                scheme.Sections.Sort((s1, s2) => AcadLib.Comparers.AlphanumComparator.New.Compare(s1.Name, s2.Name));                
                foreach (var item in scheme.Sections)
                {
                    item.TableRowIndex = rowSec;
                    col[rowSec++, 1].TextString = $"{item.Name}\n({item.NumberFloors}эт.)";
                }
                mCells = CellRange.Create(table, 6, 0, rowSec - 1, 0);
                table.MergeCells(mCells);
                cell = table.Cells[6, 0];
                cell.TextString = "Кол-во квартир по секциям на этаже, шт.";
                cell.Contents[0].Rotation = 90.0.ToRadians();
            }

            col = columns[0];
            rowCountInFloor = rowSec;
            mCells = CellRange.Create(table, rowCountInFloor, 0, rowCountInFloor, 1);
            table.MergeCells(mCells);
            col[rowCountInFloor, 0].TextString = "Кол-во на этаже";
            rowTotalInFloorByType = rowCountInFloor + 1;
            mCells = CellRange.Create(table, rowTotalInFloorByType, 0, rowTotalInFloorByType, 1);
            table.MergeCells(mCells);
            col[rowTotalInFloorByType, 0].TextString = "Всего на этаже";

            // Последний столбец - всего квартир в секции на этаже
            int colLast = table.Columns.Count - 1;
            mCells = CellRange.Create(table, 1, colLast, rowScheme, colLast);
            table.MergeCells(mCells);
            cell = table.Cells[1, colLast];            
            cell.TextString = "ВСЕГО НА ЭТАЖЕ:";
            cell.Contents[0].Rotation = 90.0.ToRadians();

            var groupSec = apartments.SelectMany(s => s).GroupBy(g => g.Section);
            foreach (var item in groupSec)
            {
                cell = table.Cells[item.Key.TableRowIndex, colLast];
                cell.TextString = item.Count().ToString();
            }

            // всего квартир
            mCells = CellRange.Create(table, rowCountInFloor, colLast, rowTotalInFloorByType, colLast);
            table.MergeCells(mCells);
            cell = table.Cells[rowCountInFloor, colLast];
            cell.TextString = apartments.Sum(a=>a.Count()).ToString();
        }

        protected override void FillCells (Table table)
        {
            var cs = db.CurrentSpaceId.GetObject(OpenMode.ForWrite) as BlockTableRecord;
            cs.AppendEntity(table);
            db.TransactionManager.TopTransaction.AddNewlyCreatedDBObject(table, true);

            table.Cells.TextHeight = 8;
            table.Rows[0].TextHeight = 12;

            Cell cell;
            CellRange mCells;
            var groupApartments = apartments.GroupBy(g => g.Key.Type).OrderBy(o=>o.Key);

            int curCol = 2;            

            foreach (var group in groupApartments)
            {
                int groupCount = group.Count();
                mCells = CellRange.Create(table, rowType, curCol, rowType, curCol + groupCount - 1);
                table.MergeCells(mCells);
                table.Cells[rowType, curCol].TextString = group.Key.Names;

                mCells = CellRange.Create(table, rowTotalInFloorByType, curCol, rowTotalInFloorByType, curCol + groupCount - 1);
                table.MergeCells(mCells);
                table.Cells[rowTotalInFloorByType, curCol].TextString = (group.Sum(s=>s.Count())).ToString();

                foreach (var apart in group)
                {
                    cell = table.Cells[rowNumberPP, curCol];
                    cell.TextString = (curCol - 1).ToString();

                    cell = table.Cells[rowMarkPik1, curCol];
                    cell.TextString = apart.Key.Name;

                    cell = table.Cells[rowMarkChrono, curCol];
                    cell.TextString = apart.Key.NameChronology;

                    cell = table.Cells[rowScheme, curCol];
                    cell.BlockTableRecordId = apart.Key.IdBtr;
                    var blockContent = cell.Contents[0];
                    blockContent.IsAutoScale = false;
                    blockContent.Scale = (1 / scale) * 0.5;
                    blockContent.ContentColor = group.Key.Color;

                    var groupBySec = apart.GroupBy(g => g.Section);
                    if (scheme.Sections.Count > 1)
                    {
                        foreach (var item in scheme.Sections)
                        {
                            cell = table.Cells[item.TableRowIndex, curCol];
                            cell.TextString = "-";
                        }
                                                
                        foreach (var item in groupBySec)
                        {
                            cell = table.Cells[item.Key.TableRowIndex, curCol];
                            cell.TextString = item.Count().ToString();
                        }
                    }

                    cell = table.Cells[rowCountInFloor, curCol];
                    cell.TextString = apart.Count().ToString();

                    if (apart.Count()!= groupBySec.Sum(s=>s.Count()))
                    {
                        cell.BackgroundColor = badColor;
                    }

                    curCol++;                        
                }                
            }
            // Объединение одинаковых хронологических марок
            MergeChronoCells(table);
        }

        private void MergeChronoCells (Table table)
        {
            string lastChrono = null;
            int lastIndex = 0;
            for (int i = 2; i < table.Columns.Count; i++)
            {
                var cell = table.Cells[rowMarkChrono, i];
                
                if (lastChrono != cell.TextString)
                {   
                    if (lastChrono != null)
                    {
                        var mCells = CellRange.Create(table, rowMarkChrono, lastIndex, rowMarkChrono, i-1);
                        table.MergeCells(mCells);
                    }
                    if (cell.TextString == "-" || string.IsNullOrWhiteSpace(cell.TextString))
                    {
                        lastIndex = 0;
                        lastChrono = null;                        
                    }
                    else
                    {
                        lastChrono = cell.TextString;
                        lastIndex = i;
                    }
                }
            }
        }
    }
}
