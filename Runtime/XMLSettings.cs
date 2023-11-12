using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Threading;
using UnityEngine;
using XMLSystem.Xml;

namespace XMLSystem.Settings
{
    public delegate void ChangeSetting(string group, string key);
    /// <summary>
    /// Статический класс создает файл настроек в формате XML
    /// </summary>
    public class XMLSettings
    {

        public static UserXMLSettings instance;
        public static event ChangeSetting OnChangeSetting;
        /// <summary>
        /// Имя файла по умолчанию
        /// </summary>
        public static string FileName { get { return instance.FileName; } }
        /// <summary>
        /// Имя группы (основного узла) по умолчанию
        /// </summary>
        public static string DefaultGroup
        {
            get { return instance.DefaultGroup; }
        }
        /// <summary>
        /// Экземпляр документа файла настроек
        /// </summary>
        public static XmlDocument XML { get { return instance.XML; } }

        static XMLSettings()
        {
            CultureInfo ci = new CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
#if !UNITY_ANDROID && !UNITY_IOS && !UNITY_STANDALONE_OSX
            instance = ReloadXML("settings.xml"); 
#else
            instance = ReloadXML(Path.Combine(Application.persistentDataPath, "settings.xml"));
#endif

#if CHUVI_EXTENSIONS && CHUVI_SETTINGS
            UnityEngine.GameObject go = UnityEngine.Resources.Load<UnityEngine.GameObject>("Settings");
            var inst = UnityEngine.GameObject.Instantiate(go);
            var contr = inst.GetComponent<ISettingsController>();
            contr.Settings = instance; 
#endif
        }

        /// <summary>
        /// Перезагружает файл настроек
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static UserXMLSettings ReloadXML(string filename)
        {
            if (instance == null)
            {
                var _instance = new UserXMLSettings();
                _instance.OnChangeSetting += _instance_OnChangeSetting;
                _instance.Load(filename);
                return _instance;
            }
            else
            {
                instance.OnChangeSetting -= _instance_OnChangeSetting;
                instance.OnChangeSetting += _instance_OnChangeSetting;
                instance.Load(filename);
                return instance;
            }
        }

        private static void _instance_OnChangeSetting(string group, string key)
        {
            OnChangeSetting?.Invoke(group, key);
        }

        /// <summary>
        /// Определяет существование указанной группы
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public static bool Exists(string group)
        {
            return instance.Exists(group);
        }
        /// <summary>
        /// Определяет существование указанной группы с ключем
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public static bool Exists(string group, string key)
        {
            return instance.Exists(group, key);
        }

        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>В случае отсутствия узлов с указанными параметрами, возвращается значение по умолчанию</returns>
        public static T GetValue<T>(string key, UserXMLSettings.TypeValidationAction typeValidationAction = null)
        {
            return instance.GetValue<T>(key, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <returns>
        /// В случае отсутствия узлов с указанными параметрами, 
        /// возвращается значение по умолчанию
        /// </returns>
        public static T GetValue<T>(string group, string key)
        {
            return instance.GetValue<T>(group, key);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>
        /// В случае отсутствия узлов с указанными параметрами, 
        /// возвращается значение по умолчанию
        /// </returns>
        public static T GetValue<T>(string group, string key, UserXMLSettings.TypeValidationAction typeValidationAction)
        {
            return instance.GetValue<T>(group, key, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public static bool TryGetValue<T>(string key, out T val, UserXMLSettings.TypeValidationAction typeValidationAction = null)
        {
            return instance.TryGetValue<T>(key, out val, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public static bool TryGetValue<T>(string group, string key, out T val)
        {
            return instance.TryGetValue<T>(group, key, out val);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public static bool TryGetValue<T>(string group, string key, out T val, UserXMLSettings.TypeValidationAction typeValidationAction)
        {
            return instance.TryGetValue<T>(group, key, out val, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public static T GetValueWithAdd<T>(string key, T defaultValue, UserXMLSettings.TypeValidationAction typeValidationAction = null)
        {
            return instance.GetValueWithAdd<T>(key, defaultValue, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public static T GetValueWithAdd<T>(string group, string key, T defaultValue)
        {
            return instance.GetValueWithAdd<T>(group, key, defaultValue);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public static T GetValueWithAdd<T>(string group, string key, T defaultValue, UserXMLSettings.TypeValidationAction typeValidationAction)
        {
            return instance.GetValueWithAdd<T>(group, key, defaultValue, typeValidationAction);
        }
        /// <summary>
        /// Добавляет значение в документ
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isComment">Добавляет как коментарий</param>
        /// <param name="save">Автоматически сохраняет файл после добавления</param>
        public static void SetValue(string key, object value, bool isComment = false, bool save = false)
        {
            instance.SetValue(key, value, isComment, save);
        }
        /// <summary>
        /// Добавляет значение в документ
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isComment">Добавляет как коментарий</param>
        /// <param name="save">Автоматически сохраняет файл после добавления</param>
        public static void SetValue(string group, string key, object value, bool isComment = false, bool save = false)
        {
            instance.SetValue(group, key, value, isComment, save);
        }

        /// <summary>
        /// Сохранить изменения проведенные после операции SetValue
        /// </summary>
        public static void Save()
        {
            instance.Save(FileName);
        }
        /// <summary>
        /// Сохранить изменения проведенные после операции SetValue
        /// </summary>
        /// <param name="fileName"></param>
        public static void Save(string fileName)
        {
            instance.Save(fileName);
        }
    }
    /// <summary>
    /// Класс создает файл настроек в формате XML
    /// </summary>
    public class UserXMLSettings
#if CHUVI_EXTENSIONS
        : ISettings 
#endif
    {

        public event ChangeSetting OnChangeSetting;

        /// <summary>
        /// Делегат проверки типа данных значения
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="typeName"></param>
        /// <param name="isValid"></param>
        public delegate void TypeValidationAction(string group, string key, string typeName, bool isValid);
        ///// <summary>
        ///// Делегат оповещения об изменении значения в файле под указанной группой и ключем
        ///// </summary>
        ///// <param name="group"></param>
        ///// <param name="key"></param>
        //public delegate void ChangeEvent(string group, string key);

        private XmlDocument doc;
        private string defaultGroup = "DEFAULT";

        private string fileName = "settings.xml";

        /// <summary>
        /// Экземпляр документа файла настроек
        /// </summary>
        public XmlDocument XML { get { return doc; } }
#if CHUVI_EXTENSIONS
        object ISettings.RawData => XML; 
#endif
        /// <summary>
        /// Имя группы (основного узла) по умолчанию
        /// </summary>
        /// 
        public string DefaultGroup
        {
            get { return defaultGroup; }
        }
        /// <summary>
        /// Имя файла по умолчанию
        /// </summary>
        public string FileName 
        {
            get 
            {
#if UNITY_ANDROID
                return Path.Combine(UnityEngine.Application.persistentDataPath, fileName); //ANDROID
#elif UNITY_IOS
                return Path.Combine(UnityEngine.Application.persistentDataPath, fileName); //iOS
#elif UNITY_STANDALONE_OSX
                return Path.Combine(UnityEngine.Application.persistentDataPath, fileName); //MAC
#else
                return fileName;
#endif
            } 
            set 
            { 
                fileName = value; 
            } 
        }
        /// <summary>
        /// Создает экземпляр класса настроек
        /// </summary>
        /// <param name="filename"></param>
        public UserXMLSettings(string filename)
        {
            Load(filename);
        }
        public UserXMLSettings()
        {
            doc = new XmlDocument();
            doc.ChangeNode += Doc_ChangeNode;
            doc.SetMainNode(XmlDocument.CreateNode("SETTINGS"));
        }

        public UserXMLSettings(byte[] bytes)
        {
            doc = new XmlDocument();
            doc.Load(bytes);
            if (string.IsNullOrEmpty(doc.Name)) // если файл есть, но он пустой
                doc.SetMainNode(XmlDocument.CreateNode("SETTINGS"));
        }
        /// <summary>
        /// Загружает файл настроек
        /// </summary>
        /// <param name="filename"></param>
        /// <returns></returns>
        public void Load(string filename)
        {
            doc = new XmlDocument();
            doc.ChangeNode += Doc_ChangeNode;
            FileName = filename;

            if (!File.Exists(FileName))
            {
                doc.SetMainNode(XmlDocument.CreateNode("SETTINGS"));
                return;
            }
            doc.Load(File.ReadAllText(FileName));
            if (string.IsNullOrEmpty(doc.Name)) // если файл есть, но он пустой
                doc.SetMainNode(XmlDocument.CreateNode("SETTINGS"));
        }

        private void Doc_ChangeNode(IXmlDocumentNode obj)
        {
            //if(obj.Parent != null)
                OnChangeSetting?.Invoke(obj.Parent.Name, obj.Name);
        }

        /// <summary>
        /// Определяет существование указанной группы
        /// </summary>
        /// <param name="group"></param>
        /// <returns></returns>
        public bool Exists(string group)
        {
            XmlDocumentNode node;
            return doc.TrySelectNode(string.Format("*/{0}", group), out node);
        }
        /// <summary>
        /// Определяет существование указанной группы с ключем
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        public bool Exists(string group, string key)
        {
            XmlDocumentNode node;
            return doc.TrySelectNode(string.Format("*/{0}/{1}", group, key), out node);
        }

        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>
        /// В случае отсутствия узлов с указанными параметрами, 
        /// возвращается значение по умолчанию
        /// </returns>
        public T GetValue<T>(string group, string key, TypeValidationAction typeValidationAction)
        {
            T val;
            TryGetValue<T>(group, key, out val, typeValidationAction);
            return val;
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <returns>
        /// В случае отсутствия узлов с указанными параметрами, 
        /// возвращается значение по умолчанию
        /// </returns>
        public T GetValue<T>(string group, string key)
        {
            T val;
            TryGetValue<T>(group, key, out val);
            return val;
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>В случае отсутствия узлов с указанными параметрами, возвращается значение по умолчанию</returns>
        public T GetValue<T>(string key, TypeValidationAction typeValidationAction = null)
        {
            T val;
            TryGetValue<T>(defaultGroup, key, out val, typeValidationAction);
            return val;
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public bool TryGetValue<T>(string key, out T val, TypeValidationAction typeValidationAction = null)
        {
            return TryGetValue<T>(defaultGroup, key, out val, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public bool TryGetValue<T>(string group, string key, out T val)
        {
            return TryGetValue<T>(group, key, out val, null);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="val"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает false, если значение не найдено</returns>
        public bool TryGetValue<T>(string group, string key, out T val, TypeValidationAction typeValidationAction)
        {
            XmlDocumentNode node;
            if (doc.TrySelectNode(string.Format("*/{0}/{1}", group, key), out node))
            {
                val = node.GetInnerTextValue<T>();
                if (typeValidationAction != null && node.Attributes["type"] != null)
                    typeValidationAction(group, key, node.Attributes["type"].Value, node.Attributes["type"].Value == typeof(T).Name);
                return true;
            }
            else
            {
                var _val = default(T);
                _SetValue(group, key, _val == null ? "null" : _val.ToString(), true, true, false);
            }
            val = default(T);
            return false;
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public T GetValueWithAdd<T>(string key, T defaultValue, TypeValidationAction typeValidationAction = null)
        {
            return GetValueWithAdd<T>(defaultGroup, key, defaultValue, typeValidationAction);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public T GetValueWithAdd<T>(string group, string key, T defaultValue)
        {
            return GetValueWithAdd<T>(group, key, defaultValue, null);
        }
        /// <summary>
        /// Извлекает значение из файла
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <param name="typeValidationAction"></param>
        /// <returns>Возвращает 'defaultValue', если значение не найдено</returns>
        public T GetValueWithAdd<T>(string group, string key, T defaultValue, TypeValidationAction typeValidationAction)
        {
            T val = defaultValue;
            XmlDocumentNode node;
            if (doc.TrySelectNode(string.Format("*/{0}/{1}", group, key), out node))
            {
                val = node.GetInnerTextValue<T>();
                if (typeValidationAction != null && node.Attributes["type"] != null)
                    typeValidationAction(group, key, node.Attributes["type"].Value, node.Attributes["type"].Value == typeof(T).Name);
            }
            else
            {
                _SetValue(group, key, val, false, true, false);
            }
            return val;
        }

        private string TypeToString<T>(T val)
        {
            if (typeof(T).IsPrimitive)
                return (string)System.Convert.ChangeType(val, typeof(string), new CultureInfo("en-US"));
            else return val.ToString();
        }

        /// <summary>
        /// Добавляет значение в документ
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isComment">Добавляет как коментарий</param>
        /// <param name="save">Автоматически сохраняет файл после добавления</param>
        public void SetValue(string key, object value, bool isComment = false, bool save = false)
        {
            SetValue(defaultGroup, key, value, isComment, save);
        }
        /// <summary>
        /// Добавляет значение в документ
        /// </summary>
        /// <param name="group"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="isComment">Добавляет как коментарий</param>
        /// <param name="save">Автоматически сохраняет файл после добавления</param>
        public void SetValue(string group, string key, object value, bool isComment = false, bool save = false)
        {
            _SetValue(group, key, value, isComment, save, true);
        }
        private void _SetValue(string group, string key, object value, bool isComment = false, bool save = false, bool checkFullPath = false)
        {
            XmlDocumentNode node;
            if (checkFullPath)
            {
                if (doc.TrySelectNode(string.Format("*/{0}/{1}", group, key), out node))
                {
                    node.InnerText = TypeToString(value);

                    var attr = node.Attributes["type"];
                    if (attr == null) node.AddAttribute("type", value.GetType().Name);
                    else attr.Value = value.GetType().Name;
                    
                    if (isComment)
                        node.NodeType = XNodeType.Coment;
                    if (save)
                        Save(FileName);
                    return;
                } 
            }
            if (doc.TrySelectNode(string.Format("*/{0}", group), out node))
            {
                XmlDocumentNode nodeKey = XmlDocument.CreateNode(key);
                nodeKey.InnerText = TypeToString(value);// val.ToString();
                nodeKey.AddAttribute("type", value.GetType().Name);
                if (isComment)
                    nodeKey.NodeType = XNodeType.Coment;
                node.AddChildNode(nodeKey);
            }
            else
            {
                node = XmlDocument.CreateNode(group);
                XmlDocumentNode nodeKey = XmlDocument.CreateNode(key);
                nodeKey.InnerText = TypeToString(value);
                nodeKey.AddAttribute("type", value.GetType().Name);
                if (isComment)
                    nodeKey.NodeType = XNodeType.Coment;
                node.AddChildNode(nodeKey);
                doc.AddChildNode(node);
            }

            if (value is Enum && value != null)
            {
                XmlDocumentNode nodeEnumGroup;// = XmlDocument.CreateNode("EnumValues");
                if (!doc.TrySelectNode("*/EnumValues", out nodeEnumGroup))
                {
                    nodeEnumGroup = XmlDocument.CreateNode("EnumValues");
                    doc.AddChildNode(nodeEnumGroup);
                }
                XmlDocumentNode nodeKey;
                if (!nodeEnumGroup.TrySelectNode(value.GetType().Name, out nodeKey))
                {
                    nodeKey = XmlDocument.CreateNode(value.GetType().Name);
                    nodeEnumGroup.AddChildNode(nodeKey);

                    string[] values = Enum.GetNames(value.GetType());
                    //nodeKey.InnerText = string.Join("&#10;\n", values);
                    for (int i = 0; i < values.Length; i++)
                    {
                        var val_node = XmlDocument.CreateNode("EnumValue");
                        val_node.AddAttribute("Value", values[i]);
                        val_node.AddAttribute("IntValue", (int)Enum.Parse(value.GetType(), values[i]));
                        nodeKey.AddChildNode(val_node);
                    }
                }
            }
            if(save)
                Save(FileName);
        }

        /// <summary>
        /// Сохранить изменения проведенные после операции SetValue
        /// </summary>
        public void Save()
        {
            Save(FileName);
        }
        /// <summary>
        /// Сохранить изменения проведенные после операции SetValue.
        /// Не изменяет значение свойства <see cref="FileName"/>
        /// </summary>
        /// <param name="fileName">Полный путь к файлу с учетом платформы</param>
        public void Save(string fileName)
        {
            doc.Save(fileName);
        }

#if CHUVI_EXTENSIONS
        ISettingsData[] ISettings.GetData()
        {
            List<ISettingsData> data = new List<ISettingsData>();
            foreach (var group in doc)
            {
                foreach (var key in group)
                {
                    if (string.IsNullOrEmpty(key.InnerText))
                        continue;

                    XmlData idata = new XmlData(group.Name, key.Name, this);
                    data.Add(idata);
                }
            }
            return data.ToArray();
        }

        private class XmlData : ISettingsData
        {
            string group, key;
            UserXMLSettings settings;
            string ISettingsData.Group => group;
            string ISettingsData.Key => key;

            public XmlData(string _group, string _key, UserXMLSettings _settings)
            {
                group = _group;
                key = _key;
                settings = _settings;
            }

            T ISettingsData.GetData<T>()
            {
                return settings.GetValue<T>(group, key);
            }
            Type ISettingsData.GetDataType()
            {
                XmlDocumentNode node;
                if (settings.XML.TrySelectNode(string.Format("*/{0}/{1}", group, key), out node))
                {
                    XmlDocumentAttribute attr = node.Attributes["type"];
                    if (attr != null)
                    {
                        Type currentType = Type.GetType(attr.value);
                        if (currentType == null)
                        {
                            currentType = System.Type.GetType("System." + attr.Value);
                        }
                        if (currentType == null)
                        {
#if UNITY_EDITOR
                            string assm_path = "Library/ScriptAssemblies/Assembly-CSharp.dll";
#else
                            string assm_path = $"{UnityEngine.Application.productName}_Data/Managed/Assembly-CSharp.dll";
#endif
                            var assm_ex = System.Reflection.Assembly.LoadFrom(assm_path);
                            var uetypes = assm_ex.GetTypes();
                            currentType = System.Array.Find(uetypes, type => type.Name == attr.Value);
                        }

                        if (currentType == null)
                        {
                            var types = GetType().Assembly.GetTypes();
                            currentType = System.Array.Find(types, type => type.Name == attr.Value);
                        }
                        if (currentType == null)
                        {
                            return typeof(string);
                        }
                        return currentType;
                    }
                }
                return typeof(string);
            }

            void ISettingsData.SetData<T>(T value)
            {
                settings.SetValue(group, key, value, false);
            }
        } 
#endif
    }
}
