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

namespace PIK_AR_Acad.Interior.Typology
{
    [Serializable]
    public class Options
    {
        static string FileXml = Path.Combine(AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder, @"АР\Interior\AI_ApartmentsTypology.xml");
        const string DictNod = "AI_ApartmentsTypology";
        const string RecSortColumn = "SortColumn";

        //[Category("Общие")]
        //[DisplayName("Хронологические марки квартир")]
        //[Description("Имена квартир новые (PIK) и соответствующие им хронологические имена (старые).")]        
        //[XmlIgnore]
        //public AcadLib.UI.Properties.XmlSerializableDictionary<string> ApartmentsChronology { get; set; }        

        [Category("Сортировка")]
        [DisplayName("По столбцу")]
        [Description("Выбор столбца для сотрировки квартир в таблице.")]
        public SortColumnEnum SortColumn { get; set; } = SortColumnEnum.PIK1;

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
                    //else
                    //{
                        // Сортировка квартир по хронологическому номеру
                        //options.SortApartments();
                    //}
                }
                catch (Exception ex)
                {
                    Logger.Log.Error(ex, $"Ошибка при попытке загрузки настроек таблицы из XML файла {FileXml}");
                }
            }
            if (options == null)
            {
                // Создать дефолтные
                options = new Options();
                options.SetDefault();
                // Сохранение дефолтных настроек 
                try
                {
                    options.Save();
                }
                catch (Exception exSave)
                {
                    Logger.Log.Error(exSave, $"Попытка сохранение настроек в файл {FileXml}");
                }
            }
            // Загрузка начтроек чертежа
            options.LoadFromNOD();            

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
            SaveToNOD();
            var ser = new AcadLib.Files.SerializerXml(FileXml);
            ser.SerializeList(this);
        }

        private void SaveToNOD ()
        {
            var nod = new DictNOD(DictNod, true);
            nod.Save((int)this.SortColumn, RecSortColumn);            
        }

        private void LoadFromNOD ()
        {
            var nod = new DictNOD(DictNod, true);
            var sortColInt = nod.Load(RecSortColumn, 0);
            SortColumn = (SortColumnEnum)sortColInt;
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
            var sortAparts = Apartments.OrderBy(o=> o.Value, AcadLib.Comparers.AlphanumComparator.New).ToList();
            Apartments = new XmlSerializableDictionary<string>();
            foreach (var item in sortAparts)
            {
                Apartments.Add(item.Key, item.Value);
            }
        }
    }   
}
