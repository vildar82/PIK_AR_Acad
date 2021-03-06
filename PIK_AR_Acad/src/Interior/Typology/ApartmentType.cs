﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib;
using Autodesk.AutoCAD.Colors;

namespace PIK_AR_Acad.Interior.Typology
{
    /// <summary>
    ///  Тип квартиры - Однокомнатная, 2, 3 , 4, Студия, 
    /// </summary>
    public class ApartmentType : IEquatable<ApartmentType>, IComparable<ApartmentType>
    {
        public static ApartmentType Studio = new ApartmentType(0) { Name = "Студия", Names = "Студии" };
        public static ApartmentType OneBedroom = new ApartmentType(1) { Name = "Однокомнатная", Names = "Однокомнатные" };        
        public static ApartmentType TwoBedroom = new ApartmentType(2) { Name = "Двухкомнатная", Names = "Двухкомнатные" };
        public static ApartmentType ThreeBedroom = new ApartmentType(3) { Name = "3-комнатная", Names = "3-комнатные" };
        public static ApartmentType FourBedroom = new ApartmentType(4) { Name = "4-комнатная", Names = "4-комнатные" };
        public static ApartmentType FiveBedroom = new ApartmentType(5) { Name = "5-комнатная", Names = "5-комнатные" };

        private int index;
        public string Name { get; set; }
        public string Names { get; set; }
        //public Color Color { get; set; }

        private ApartmentType(int index)
        {
            this.index = index;            
        }

        public static ApartmentType GetType (ApartmentBlock apartment)
        {            
            ApartmentType type = null;
            string apartCode = apartment.Name.Substring(apartment.Name.IndexOf('_', 1)+1);
            var firstIndex = apartCode[0];
            switch (firstIndex)
            {
                case '1':
                    if (apartCode.Substring(1).StartsWith("N"))
                        type = Studio;                    
                    else
                        type = OneBedroom;                    
                    break;
                case '2':
                    type = TwoBedroom;
                    break;
                case '3':
                    type = ThreeBedroom;
                    break;
                case '4':
                    type = FourBedroom;
                    break;
                case '5':
                    type = FiveBedroom;
                    break;                
            }            
            return type;
        }

        public bool Equals (ApartmentType other)
        {
            if (other == null) return false;
            if (ReferenceEquals(this, other)) return true;
            return index == other.index;
        }

        public int CompareTo (ApartmentType other)
        {
            if (other == null) return -1;
            if (ReferenceEquals(this, other)) return 0;
            return index.CompareTo(other.index);
        }

        public override int GetHashCode ()
        {
            return index.GetHashCode();
        }
    }
}
