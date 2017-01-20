using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;
using System.Collections;
using AcadLib.UI.Properties;
using System.Drawing.Design;
using AcadLib.XData;
using Autodesk.AutoCAD.DatabaseServices;
using AcadLib.Errors;

namespace PIK_AR_Acad.Interior.Typology
{
    public enum SortApartment
    {
        Имя,
        Хронология
    }

    [Serializable]
    public class Options : ITypedDataValues, IExtDataSave
    {
        static string FileXml = Path.Combine(AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder, @"АР\Interior\AI_ApartmentsTypology.xml");
        const string DictNod = "AI_ApartmentsTypology";
        const string RecSortColumn = "SortColumn";
        const string RecSortApart = "SortApart";

        //[Category("Общие")]
        //[DisplayName("Хронологические марки квартир")]
        //[Description("Имена квартир новые (PIK) и соответствующие им хронологические имена (старые).")]        
        //[XmlIgnore]
        //public AcadLib.UI.Properties.XmlSerializableDictionary<string> ApartmentsChronology { get; set; }        

        [XmlIgnore]
        [Browsable(false)]
        [Category("Сортировка")]
        [DisplayName("По столбцу")]
        [Description("Выбор столбца для сотрировки квартир в таблице.")]
        public SortColumnEnum SortColumn { get; set; } = SortColumnEnum.PIK1;

        [Category("Квартиры")]
        [DisplayName("Сортировка коллекции")]
        [Description("Сортировка квартир в редактрое.")]
        public SortApartment SortApartment { get { return sortApartment; } set { sortApartment = value; SortApartments(); } }
        SortApartment sortApartment = SortApartment.Имя;

        [Category("Квартиры")]
        [DisplayName("Квартиры")]
        [Description("Редактирование хронологических номеров квартир.")]
        [ReadOnly(false)]
        [Editor(typeof(AcadLib.UI.Designer.GenericDictionaryEditor<string, string>), typeof(UITypeEditor))]
        [AcadLib.UI.Designer.GenericDictionaryEditor(Title ="Редактор хронологических имен квартир",
            KeyDisplayName = "Квартира", ValueDisplayName = "Хронологическое имя")]
        public XmlSerializableDictionary <string> Apartments { get; set; }
                
        private static Options _instance;
        public static Options Instance {
            get {
                if (_instance == null)
                {
                    _instance = Load();
                }
                return _instance;                          
            }
        }

        public Options()
        {
        }

        public static void PromptOptions ()
        {
            Options resVal = Instance;
            //Запрос начальных значений
            var formProp = new AcadLib.UI.FormProperties();
            var thisCopy = (Options)resVal.MemberwiseClone();
            formProp.propertyGrid1.SelectedObject = thisCopy;
            if (Application.ShowModalDialog(formProp) != System.Windows.Forms.DialogResult.OK)
            {
                return;
            }
            try
            {
                resVal = thisCopy;
                resVal.Save();
                _instance = resVal;
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex, "Не удалось сохранить стартовые параметры.");
            }            
        }

        private static Options Load ()
        {
            Options options = null;
            if (File.Exists(FileXml))
            {
                try
                {
                    // Загрузка настроек таблицы из файла XML
                    options = LoadFromXml();
                    if (options.Apartments == null || options.Apartments.Count==0)
                    {
                        options.Apartments = DefaultApartments();
                    }
                    else
                    {
                        //Сортировка квартир
                        options.SortApartments();
                    }
                }
                catch (Exception ex)
                {
                    var errMsg = $"Ошибка при попытке загрузки настроек таблицы из XML файла '{FileXml}'. {ex.Message}";
                    Logger.Log.Error(ex, errMsg);
                    Inspector.AddError(errMsg);
                }
            }
            if (options == null)
            {
                // Создать дефолтные
                options = new Options();
                options.SetDefault();
                //// Сохранение дефолтных настроек 
                //try
                //{
                //    options.Save();
                //}
                //catch (Exception exSave)
                //{
                //    Logger.Log.Error(exSave, $"Попытка сохранение настроек в файл {FileXml}");
                //}
            }
            // Загрузка начтроек чертежа
            //options.LoadFromNOD();            

            return options;
        }       

        private void SetDefault ()
        {                     
            Apartments = DefaultApartments();
        }

        private static Options LoadFromXml ()
        {
            var ser = new AcadLib.Files.SerializerXml(FileXml);
            return ser.DeserializeXmlFile<Options>();
        }

        public void Save ()
        {
            //SaveToNOD();
            var ser = new AcadLib.Files.SerializerXml(FileXml);
            ser.SerializeList(this);
        }

        public void SaveToNOD ()
        {
            var nod = new DictNOD("AR", true);            
            nod.Save(GetExtDic(null));           
        }

        private void LoadFromNOD ()
        {
            var nod = new DictNOD("AR", true);
            var dic = nod.LoadED(DictNod);
            SetExtDic(dic, null);            
        }

        private static XmlSerializableDictionary<string> DefaultApartments()
        {
            var aparts = new XmlSerializableDictionary<string>(); 
            foreach (var item in ApartmentBlock.DictNameChronologyDefault)
            {
                aparts.Add(item.Key, item.Value);
            }
            return aparts;
        }

        private void SortApartments()
        {
            if (Apartments == null) return;
            var sortAparts = Apartments.OrderBy(a => a.Key.Substring(0,a.Key.IndexOf('_')))
                .ThenBy(o =>
                {
                    if (SortApartment == SortApartment.Имя)
                        return o.Key;
                    else
                        return o.Value;
                }, AcadLib.Comparers.AlphanumComparator.New);

            //var sortAparts = Apartments.OrderBy(o=> o.Key, AcadLib.Comparers.AlphanumComparator.New).ToList();
            Apartments = new XmlSerializableDictionary<string>();
            foreach (var item in sortAparts)
            {
                Apartments.Add(item.Key, item.Value);
            }
        }

        public List<TypedValue> GetDataValues(Document doc)
        {
            var tvs = new TypedValueExtKit();
            tvs.Add("SortApartment", SortApartment);
            return tvs.Values;            
        }

        public void SetDataValues(List<TypedValue> values, Document doc)
        {            
            var dictValues = values.ToDictionary();
            SortApartment = dictValues.GetValue("SortApartment", SortApartment.Имя);
        }

        public DicED GetExtDic(Document doc)
        {
            var dic = new DicED(DictNod);
            dic.AddRec("OptionsRec", GetDataValues(null));
            return dic;
        }

        public void SetExtDic(DicED dicEd, Document doc)
        {
            SetDataValues(dicEd?.GetRec(DictNod)?.Values, doc);
        }
    }   
}
