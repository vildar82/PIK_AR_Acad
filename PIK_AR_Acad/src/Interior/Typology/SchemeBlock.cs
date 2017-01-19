using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using AcadLib.Errors;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PIK_AR_Acad.Interior.Typology
{
    public class SchemeBlock : BlockBase
    {
        static Tolerance toleranceCenterSections = new Tolerance(0.2, 100);

        public string Name { get; set; }
        public List<Section> Sections { get; set; } = new List<Section>();

        public SchemeBlock (BlockReference blRef, string blName, string schemeName = null) : base(blRef, blName)
        {
            if (blRef == null)
            {
                Name = schemeName ?? "Неопределенная схема";
                return;
            }
            defineSections(blRef);
        }

        private void defineSections(BlockReference blRef)
        {
            var btr = blRef.BlockTableRecord.GetObject(OpenMode.ForRead) as BlockTableRecord;

            List<DBText> textSections = new List<DBText>();
            List<DBText> textFloors = new List<DBText>();

            foreach (var idEnt in btr)
            {
                var ent = idEnt.GetObject(OpenMode.ForRead);
                if (ent is Polyline)
                {
                    var pl = (Polyline)ent;
                    if ((int)pl.LineWeight >= 30)
                    {
                        var plContour = (Polyline)pl.Clone();
                        plContour.TransformBy(Transform);
                        Section sec = new Section(plContour, ent.Id);

                        if (!Sections.Any(s => s.Center.IsEqualTo(sec.Center, toleranceCenterSections)))
                        {
                            Sections.Add(sec);
                        }
                    }
                }
                else if (ent is DBText)
                {
                    var text = (DBText)ent;
                    if (text.TextString.StartsWith("%%U"))
                    {
                        Name = text.TextString.Replace("%%U", "");
                        Inspector.AddError($"Имя объекта - '{Name}'", GetEntityExtentsInModel(text),
                            Matrix3d.Identity, SystemIcons.Information);
                    }
                    else if (text.TextString.Contains("Секция", StringComparison.OrdinalIgnoreCase))
                    {
                        textSections.Add(text);
                    }
                    else if (text.TextString.Contains("Башня", StringComparison.OrdinalIgnoreCase))
                    {
                        textSections.Add(text);
                    }
                    else if (text.TextString.Contains("этаж", StringComparison.OrdinalIgnoreCase))
                    {
                        textFloors.Add(text);
                    }
                }
            }

            //// Удаление секции с максимально площадью - если эта площадь блольше суммы всех остальных площадей секций
            //var secMax = Sections.OrderByDescending(o => o.Area).First();
            //var areaOthers = Sections.Where(s => s != secMax).Sum(s => s.Area);
            //if (secMax.Area> areaOthers)
            //{
            //    Sections.Remove(secMax);
            //}

            // Определение номера секции и этажности
            var inversTrans = Transform.Inverse();
            foreach (var item in Sections)
            {
                var ptItem = item.Center.TransformBy(inversTrans);
                var textSec = findNearest(ptItem, textSections);
                if (textSec == null)
                {
                    Inspector.AddError($"Не определена секция в схеме.", item.IdPlOrigin, Transform,
                        SystemIcons.Error);
                    //item.Fail = true;
                }
                else
                {
                    item.Name = textSec.TextString;
                    textSections.Remove(textSec);
                }

                var textFloor = findNearest(ptItem, textFloors);
                if (textFloor == null)
                {
                    Inspector.AddError($"Не определена этажность секции в схеме.", item.Bounds, Matrix3d.Identity,
                        SystemIcons.Error);
                    //item.Fail = true;
                }
                else
                {
                    item.SetFloors(textFloor.TextString);
                    textFloors.Remove(textFloor);
                }
            }
            Sections.RemoveAll(s => s.Fail);
            if (Sections.Any(s=>s.IsTower) && Sections.Count>1)
            {
                var tower = Sections.FirstOrDefault(s=>s.IsTower);
                Inspector.AddError($"В схеме есть Башня и другие секции. Башня должна быть одна в схеме!", 
                    tower.Bounds, Matrix3d.Identity, SystemIcons.Error);
            }
            foreach (var item in Sections)
            {
                if (!string.IsNullOrEmpty(item.Name) && item.NumberFloors != 0)
                {
                    Inspector.AddError($"{item.Name}, {item.NumberFloors} этажная", item.Bounds, item.IdPlOrigin,
                        System.Drawing.SystemIcons.Information);
                }
            }
        }
    
        private Extents3d GetEntityExtentsInModel(Entity ent)
        {
            try
            {
                var ext = ent.GeometricExtents;
                ext.TransformBy(Transform);
                return ext;
            }
            catch { }
            return ExtentsToShow;
        }

        private DBText findNearest (Point3d ptItem, List<DBText> textSections)
        {
            var res = textSections.Select(s=>new { text = s, len = (ptItem - s.Position).Length }).OrderBy(o => o.len).FirstOrDefault();            
            return res?.len < 30000 ? res.text : null;
        }

        public static bool IsSchemeBlock (string blName)
        {
            return blName.StartsWith("Схема_", StringComparison.OrdinalIgnoreCase);
        }
    }
}
