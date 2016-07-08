using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using AcadLib;
using Autodesk.AutoCAD.ApplicationServices;

namespace PIK_AR_Acad.Interior.Typology
{
    [Serializable]
    public class Options
    {
        static string FileXml = Path.Combine(AutoCAD_PIK_Manager.Settings.PikSettings.ServerShareSettingsFolder, @"АР\Interior\AI_ApartmentsTypology.xml");
        const string DictNod = "AI_ApartmentsTypology";
        //const string RecAbsoluteZero = "AbsoluteZero";        

        [Category("Общие")]
        [DisplayName("Хронологические марки квартир")]
        [Description("Имена квартир новые (PIK) и соответствующие им хронологические имена (старые).")]        
        public AcadLib.UI.Properties.XmlSerializableDictionary<string> ApartmentsChronology { get; set; }

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

        public Options PromptOptions ()
        {
            Options resVal = this;
            //Запрос начальных значений
            AcadLib.UI.FormProperties formProp = new AcadLib.UI.FormProperties();
            Options thisCopy = (Options)resVal.MemberwiseClone();
            formProp.propertyGrid1.SelectedObject = thisCopy;
            if (Application.ShowModalDialog(formProp) != System.Windows.Forms.DialogResult.OK)
            {
                throw new Exception(General.CanceledByUser);
            }
            try
            {
                resVal = thisCopy;
                Save();
            }
            catch (Exception ex)
            {
                Logger.Log.Error(ex, "Не удалось сохранить стартовые параметры.");
            }
            return resVal;
        }

        private static Options Load ()
        {
            Options options = null;
            if (File.Exists(FileXml))
            {
                try
                {
                    // Загрузка настроек таблицы из файла XML
                    options = Options.LoadFromXml();
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
            ApartmentsChronology = new AcadLib.UI.Properties.XmlSerializableDictionary<string>();
            foreach (var item in ApartmentBlock.dictNameChronologyDefault)
            {
                ApartmentsChronology.Add(item.Key, item.Value);
            }            
        }

        private static Options LoadFromXml ()
        {
            AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(FileXml);
            return ser.DeserializeXmlFile<Options>();
        }

        public void Save ()
        {
            SaveToNOD();
            AcadLib.Files.SerializerXml ser = new AcadLib.Files.SerializerXml(FileXml);
            ser.SerializeList(this);
        }

        private void SaveToNOD ()
        {
            //var nod = new DictNOD(DictNod, true);
            //nod.Save(AbsoluteZero, RecAbsoluteZero);            
        }

        private void LoadFromNOD ()
        {
            //var nod = new DictNOD(DictNod, true);
            //AbsoluteZero = nod.Load(RecAbsoluteZero, AbsoluteZero);            
        }
    }
}
