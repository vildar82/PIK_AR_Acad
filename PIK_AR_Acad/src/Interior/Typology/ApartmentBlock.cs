using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using AcadLib.Errors;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;

namespace PIK_AR_Acad.Interior.Typology
{
    /// <summary>
    /// Блок квартиры - интерьеров (Гули)
    /// </summary>
    public class ApartmentBlock : BlockBase, IEquatable<ApartmentBlock>, IComparable<ApartmentBlock>
    {
        public const string BlockNamePrefix = "PIK1_";
        public override Color Color { get; set; }
        public ApartmentType Type { get; set; }
        public string Name { get; set; }

        public ApartmentBlock (BlockReference blRef, string blName) : base(blRef, blName)
        {
            Color = GetColor(blRef);        
            Name = blName;
            Type = ApartmentType.GetType(this);
            if (Type == null)
            {
                AddError($"Не определен тип квартиры по блоку - {blName}");
            }
        }

        private Color GetColor (BlockReference blRef)
        {
            var layer = blRef.LayerId.GetObject( OpenMode.ForRead) as LayerTableRecord;
            return layer.Color;
        }

        public static List<ApartmentBlock> GetApartments (List<ObjectId> sel)
        {
            if (!sel.Any()) return null;

            var apartments = new List<ApartmentBlock>();
            Database db = sel.First().Database;

            using (var t = db.TransactionManager.StartTransaction())
            {
                foreach (var item in sel)
                {
                    var blRef = item.GetObject( OpenMode.ForRead) as BlockReference;
                    if (blRef == null) continue;

                    string blName = blRef.GetEffectiveName();

                    if (IsApartmentBlock(blName))
                    {
                        var apartment = new ApartmentBlock(blRef, blName);
                        if (apartment.Error != null)
                        {
                            Inspector.AddError(apartment.Error);
                            continue;
                        }
                        apartments.Add(apartment);
                    }
                }
                t.Commit();
            }
            return apartments;
        }

        public static bool IsApartmentBlock (string blName)
        {
            var res = blName.StartsWith(BlockNamePrefix, StringComparison.OrdinalIgnoreCase);
            return res;
        }

        public bool Equals (ApartmentBlock other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;

            var res = Name == other.Name;
            return res;
        }

        public int CompareTo (ApartmentBlock other)
        {
            if (other == null) return 1;
            if (ReferenceEquals(this, other)) return 0;

            var res = AcadLib.Comparers.AlphanumComparator.New.Compare(Name, other.Name);
            return res;
        }

        public override int GetHashCode ()
        {
            return Name.GetHashCode();
        }
    }
}
