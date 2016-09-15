using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using PIK_AR_Acad.Interior.Typology;

namespace PIK_AR_Acad.Interior.Typology
{
    public class ApartmentLayer
    {
        public ApartmentType Type { get; set; }
        public string Name { get; set; }
        public Color Color { get; set; }
        public ObjectId Id { get; set; }

        public ApartmentLayer(ApartmentType type, string layName, Color color)
        {
            Type = type;
            Name = layName;
            Color = color;
        }            
    }
}
