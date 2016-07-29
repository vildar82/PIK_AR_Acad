using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib.Blocks;
using AcadLib.Errors;
using Autodesk.AutoCAD.Colors;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Geometry;
using Autodesk.AutoCAD.Geometry;

namespace PIK_AR_Acad.Interior.Typology
{
    /// <summary>
    /// Блок квартиры - интерьеров (Гули)
    /// </summary>
    public class ApartmentBlock : BlockBase, IEquatable<ApartmentBlock>, IComparable<ApartmentBlock>
    {
        public static Dictionary<string, string> DictNameChronologyDefault = new Dictionary<string, string> ()
        {
            { "PIK1_1NS1_AO", "9" }, { "PIK1_1NS1_ZO", "9" },
            { "PIK1_1KL1_AO", "4" }, { "PIK1_1KL1_ZO", "4" },{ "PIK1_1KL1_AO_(m)", "4" },
            { "PIK1_1KS1_AO", "10" }, { "PIK1_1KS1_ZO", "10" }, { "PIK1_1KS1_AO_(A)", "10" },
            { "PIK1_1NM1_AO", "29" }, { "PIK1_1NM1_ZO", "29" },
            { "PIK1_1NM2_AO", "6" }, { "PIK1_1NM2_ZO", "6" },
            { "PIK1_1NM3_AO", "2" }, { "PIK1_1NM3_ZO", "2" },
            { "PIK1_2KL1_AO", "17" }, { "PIK1_2KL1_ZO", "17" },{ "PIK1_2KL1_AL", "17" },{ "PIK1_2KL1_ZL", "17" },
            { "PIK1_2KL2_AO", "23" }, { "PIK1_2KL2_ZO", "23" },
            { "PIK1_2KL3_AO", "5" }, { "PIK1_2KL3_ZO", "5" },
            { "PIK1_2KL3_AL", "5" }, { "PIK1_2KL3_ZL", "5" },{ "PIK1_2KL3_ZO_(А)", "5" },
            { "PIK1_2NM1_AO", "3" }, { "PIK1_2NM1_ZO", "3" },
            { "PIK1_2NM2_AO", "18" },
            { "PIK1_2NS1_AO", "7" }, { "PIK1_2NS1_ZO", "7" },
            { "PIK1_2KL_AO_T_(A)", "26" },
            { "PIK1_3KL1_AO", "14" }, { "PIK1_3KL1_ZO", "14" },
            { "PIK1_3KL1_AL", "14" }, { "PIK1_3KL1_ZL", "14" },
            { "PIK1_3KL2_AO", "15" }, { "PIK1_3KL2_ZO", "15" }, { "PIK1_3KL2_ZL", "15" },
            { "PIK1_3NL1_AO", "1" }, { "PIK1_3NL1_ZO", "1" },
            { "PIK1_3NL2_AO", "8" }, { "PIK1_3NL2_ZO", "8" },
            { "PIK1_3NL3_AO", "11" }, { "PIK1_3NL3_ZO", "11" },
            { "PIK1_3KL_AO_T_(A)", "27" },
            { "PIK1_4KL2_AO", "16" },{ "PIK1_4KL2_ZO", "16" },{ "PIK1_4KL2_AL", "16" },
            { "PIK1_4NL1_AO", "11" }, { "PIK1_4NL1_ZO", "11" },
            { "PIK1_4NL2_AO", "13" }, { "PIK1_4NL2_AL", "13" }
        };


        public const string BlockNamePrefix = "PIK1_";
        public override Color Color { get; set; }
        public ApartmentType Type { get; set; }
        public string Name { get; set; }
        public string NameChronology { get; set; } = "-";
        public Point3d Center { get; set; }
        public Section Section { get; set; }
        public SchemeBlock Scheme { get; set; }

        public ApartmentBlock (BlockReference blRef, string blName) : base(blRef, blName)
        {
            Center = Bounds.Value.Center();
            NameChronology = GetNameChronology(blName);
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

        public static List<ApartmentBlock> GetApartments (IEnumerable<ObjectId> sel, out SchemeBlock scheme)
        {
            scheme = null;
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
                    else if (SchemeBlock.IsSchemeBlock(blName))
                    {
                        scheme = new SchemeBlock(blRef, blName);
                    }
                    else
                    {
                        Inspector.AddError($"Пропущен блок '{blName}'", blRef, System.Drawing.SystemIcons.Warning);
                    }
                }

                // Определение секции для каждой квартиры                
                defineSections(apartments,ref scheme);                

                t.Commit();
            }
            return apartments;
        }

        private static void defineSections (List<ApartmentBlock> apartments,ref SchemeBlock scheme)
        {
            var secNull = new Section(null, ObjectId.Null);
            secNull.Name = "Неопределенная";

            if (scheme == null)
            {
                Inspector.AddError($"Не определен блок схемы", System.Drawing.SystemIcons.Error);

                scheme = new SchemeBlock(null, null);                
            }
            else if (scheme.Sections.Count == 0)
            {
                Inspector.AddError($"Не определены секции в схеме", scheme.IdBlRef, System.Drawing.SystemIcons.Error);                
            }

            foreach (var apart in apartments)
            {
                foreach (var sec in scheme.Sections)
                {
                    if (sec.Contour.IsPointInsidePolygon(apart.Center))
                    {
                        apart.Section = sec;
                        apart.Scheme = scheme;
                        break;
                    }
                }
                if (apart.Section == null)
                {
                    apart.Section = secNull;
                }
            }

            CheckSecNull(apartments, scheme, secNull);
        }

        private static void CheckSecNull (List<ApartmentBlock> apartments, SchemeBlock scheme, Section secNull)
        {
            if (apartments.Any(a => a.Section == secNull))
            {
                scheme.Sections.Add(secNull);
                var apartsSecNull = apartments.Where(s => s.Section == secNull);
                Extents3d extSecNull = new Extents3d();
                foreach (var item in apartsSecNull)
                {
                    extSecNull.AddExtents(item.Bounds.Value);
                }                
                Inspector.AddError($"Неопределенная секция", extSecNull, Matrix3d.Identity, System.Drawing.SystemIcons.Error);
            }
        }

        public static bool IsApartmentBlock (string blName)
        {
            var res = blName.StartsWith(BlockNamePrefix, StringComparison.OrdinalIgnoreCase);
            return res;
        }

        private string GetNameChronology (string blName)
        {
            string chrono;
            if (!DictNameChronologyDefault.TryGetValue(blName, out chrono))            
                chrono = "-";
            return chrono;            
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
