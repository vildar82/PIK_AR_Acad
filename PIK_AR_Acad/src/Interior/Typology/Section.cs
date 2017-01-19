using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.DatabaseServices;
using Autodesk.AutoCAD.Geometry;

namespace PIK_AR_Acad.Interior.Typology
{
    public class Section
    {
        public ObjectId IdPlOrigin { get; set; }
        public Polyline Contour { get; set; }
        public int NumberFloors { get; set; }
        public double Area { get; set; }
        public Point3d Center { get; set; }
        public string Name { get { return name ?? "";}
            set { name = value; IsTower = value?.Contains("Башня", StringComparison.OrdinalIgnoreCase) ?? false; } }
        string name;
        public Extents3d Bounds { get; set; }
        public int TableRowIndex { get; internal set; }
        public bool Fail { get; internal set; }
        public bool IsTower { get; private set; }

        public Section(Polyline plContour, ObjectId idPlOrigin)
        {
            IdPlOrigin = idPlOrigin;
            Contour = plContour;
            if (plContour != null)
            {
                Area = plContour.Area;
                Bounds = plContour.Bounds.Value;
                Center = Bounds.Center();
            }
        }

        public void SetFloors (string textString)
        {
            NumberFloors = AcadLib.Regexes.RegexExt.StartInt(textString);
        }
    }
}
