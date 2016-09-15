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
            { "PIK1_1NS1_A0", "9" }, { "PIK1_1NS1_Z0", "9" },
            { "PIK1_1KL1_A0", "4" }, { "PIK1_1KL1_Z0", "4" },{ "PIK1_1KL1_A0_(m)", "4" },
            { "PIK1_1KS1_A0", "10" }, { "PIK1_1KS1_Z0", "10" }, { "PIK1_1KS1_A0_(A)", "10" },
            { "PIK1_1NM1_A0", "29" }, { "PIK1_1NM1_Z0", "29" },
            { "PIK1_1NM2_A0", "6" }, { "PIK1_1NM2_Z0", "6" },
            { "PIK1_1NM3_A0", "2" }, { "PIK1_1NM3_Z0", "2" },
            { "PIK1_1NS2_A0", "9" },
            { "PIK1_2KL1_A0", "17" }, { "PIK1_2KL1_Z0", "17" },{ "PIK1_2KL1_AL", "17" },{ "PIK1_2KL1_ZL", "17" },
            { "PIK1_2KL2_A0", "23" }, { "PIK1_2KL2_Z0", "23" },
            { "PIK1_2KL3_A0", "5" }, { "PIK1_2KL3_Z0", "5" },
            { "PIK1_2KL3_AL", "5" }, { "PIK1_2KL3_ZL", "5" },{ "PIK1_2KL3_Z0_(А)", "5" },
            { "PIK1_2NM1_A0", "3" }, { "PIK1_2NM1_Z0", "3" },
            { "PIK1_2NM2_A0", "18" },
            { "PIK1_2NS1_A0", "7" }, { "PIK1_2NS1_Z0", "7" },
            { "PIK1_2KL_A0_T_(A)", "26" },
            { "PIK1_3KL1_A0", "14" }, { "PIK1_3KL1_Z0", "14" },
            { "PIK1_3KL1_AL", "14" }, { "PIK1_3KL1_ZL", "14" },
            { "PIK1_3KL2_A0", "15" }, { "PIK1_3KL2_Z0", "15" }, { "PIK1_3KL2_ZL", "15" },
            { "PIK1_3KL3_A0", "21" }, { "PIK1_3KL3_Z0", "21" },
            { "PIK1_3KL_Z0_T_(B)", "28" }, { "PIK1_3KL_A0_T_(B)", "28" },
            { "PIK1_3NL1_A0", "1" }, { "PIK1_3NL1_Z0", "1" },
            { "PIK1_3NL2_A0", "8" }, { "PIK1_3NL2_Z0", "8" },
            { "PIK1_3NL3_A0", "11" }, { "PIK1_3NL3_Z0", "11" },
            { "PIK1_3KL_A0_T_(A)", "27" },
            { "PIK1_4KL2_A0", "16" },{ "PIK1_4KL2_Z0", "16" },{ "PIK1_4KL2_AL", "16" },
            { "PIK1_4NL1_A0", "11" }, { "PIK1_4NL1_Z0", "11" },
            { "PIK1_4NL2_A0", "13" }, { "PIK1_4NL2_AL", "13" }
        };


        public const string BlockNamePrefix = "flat_";
        public new Color Color { get; set; }
        public ApartmentType Type { get; set; }
        public string Name { get; set; }
        public string NameChronology { get; set; } = "-";
        public Point3d Center { get; set; }
        public Section Section { get; set; }
        public SchemeBlock Scheme { get; set; }

        public ApartmentBlock (BlockReference blRef, string blName) : base(blRef, blName)
        {
            Center = Bounds.Value.Center();                        
            Name = blName.Substring(BlockNamePrefix.Length);
            NameChronology = GetNameChronology(Name);
            Type = ApartmentType.GetType(this);
            if (Type == null)
            {
                AddError($"Не определен тип квартиры по блоку - {blName}");
            }
            else
            {
                // проверка слоя
                CheckApartmentLayer(blRef);
            }
            //Color = GetColor(blRef);            
        }

        private void CheckApartmentLayer (BlockReference blRef)
        {
            var apartLay = ApartmentTypology.ApartmentLayers[Type];
            if (!BlLayer.Equals(apartLay.Name, StringComparison.OrdinalIgnoreCase))
            {
                blRef.UpgradeOpen();
                blRef.LayerId = apartLay.Id;
                blRef.DowngradeOpen();
            }
            if (!blRef.Color.IsByLayer)
            {
                blRef.UpgradeOpen();
                blRef.Color = Color.FromColorIndex(ColorMethod.ByLayer, 256);
            }
            Color = apartLay.Color;
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
                // Переименование блоков PIK1
                RenameOldblocksApartmentsPIK1ToNewFlat(db, t);

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

        private static void RenameOldblocksApartmentsPIK1ToNewFlat (Database db, Transaction t)
        {
            var bt = db.BlockTableId.GetObject(OpenMode.ForRead) as BlockTable;
            foreach (var idBlk in bt)
            {
                var block = idBlk.GetObject(OpenMode.ForRead) as BlockTableRecord;
                if (block.Name.StartsWith("PIK1_", StringComparison.OrdinalIgnoreCase))
                {
                    block.UpgradeOpen();
                    block.Name = BlockNamePrefix + block.Name;
                }
            }
        }

        private static void defineSections (List<ApartmentBlock> apartments,ref SchemeBlock scheme)
        {
            var secNull = new Section(null, ObjectId.Null);
            secNull.Name = "Неопределенная";

            if (scheme == null || scheme.Sections == null)
            {
                string help = "Схема дома - блок, имя начинается с 'Схема_'. В блоке схемы: однострочный текст начинающийся с подчеркивания - имя схемы. " + 
                     "Секция - полилиния с толщиной >=30, рядом с которой однострочные тексты 'Секция #' и '# этажей'.";
                Inspector.AddError($"Не определен блок схемы.\n" + help , System.Drawing.SystemIcons.Error);
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

        private string GetNameChronology (string name)
        {
            string chrono;
            if (!DictNameChronologyDefault.TryGetValue(name, out chrono))            
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
