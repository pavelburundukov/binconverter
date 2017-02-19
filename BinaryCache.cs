using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TSLab.DataSource;
using TSLab.Utils;

namespace GuiBinConverter
{
    class BinaryCache<T> where T : IStorable, new()
    {
        /// <summary>
        /// Загружает из файла, все находящиеся там данные. Формат файла должен соответствовать ожидаемому.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <param name="version">Версия данных.</param>
        /// <returns>Если файла нет, возврат null. Если данные более новой версии вернет пустой список.
        /// Если возникло исключение при чтении, вернет пустой список и пишет в трейс ошибку.</returns>
        public List<T> LoadCached(string path, int version)
        {
            // Проверим сущестсование файла.
            var file = new FileInfo(path);
            if (!file.Exists)
                return null;

            // Читаем файл.
            var fs = (FileStream)null;
            try
            {
                fs = file.OpenRead();
                var br = new BinaryReader(new BufferedStream(fs));

                // Проверим номер версии данных в файле.
                var fileVer = br.ReadInt32();
                if (fileVer > version)
                    return new List<T>();

                // Читаем все что есть в файле.
                var recCount = br.ReadInt32();
                var values = new List<T>(recCount);
                while (recCount-- > 0)
                {
                    var T = new T();
                    T.Restore(br, fileVer);
                    values.Add(T);
                }
                return values;
            }
            catch (Exception ex)
            {
                throw new IOException("Ошибка чтения данных из файла {0}".Put(Path.GetFileName(path)), ex);
                //Trace.WriteLine("Failed to deserialize. Reason: " + ex.Message);
                //return new List<T>();
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }

        /// <summary>
        /// Пишет в файл данные. 
        /// </summary>
        /// <param name="path"></param>
        /// <param name="data"></param>
        /// <param name="version"></param>
        public void SaveCached(string path, IList<T> data)
        {
            var file = new FileInfo(path);
            if (file.Exists)
                throw new InvalidOperationException("Нельзя писать в существующий файл.");

            var bs = new BufferedStream(file.Open(FileMode.Create, FileAccess.Write));
            var bw = new BinaryWriter(bs);
            try
            {
                // Вытащим из тслаба текущую версию кэша.
                // Так как пишет всегда в текущей версии, поэтому руками писать нехорошо. Можно записать не то и 
                // кэш поломается.
                var cInfo = new CacheInfoGetter("qwert");
                var version = cInfo.Version;
                // TODO: есть проблема с сохранением свечей. формат у них другой цифрой идет. А мы пишем 6
                bw.Write(version);
                bw.Write(data.Count);
                data.ForEach(obj => obj.Store(bw));
                bw.Flush();
                bs.Flush();
            }
            catch (Exception ex)
            {
                //Trace.WriteLine("Failed to serialize. Reason: " + exception0.Message);
                throw new IOException("Ошибка сохранения данных в файл {0}".Put(Path.GetFileName(path)), ex);
            }
            finally
            {
                bs.Close();
            }

        }

        /// <summary>
        /// Читает из файла данных номер версии формата файла.
        /// </summary>
        /// <param name="path">Путь к файлу.</param>
        /// <returns>Если файла нет, или ошибка чтения файла возвращает 0 и пишет ошибку в трейс. 
        /// Иначе вернет версию формата.</returns>
        public static int ReadVersion(string path)
        {
            // Проверим сущестсование файла.
            var file = new FileInfo(path);
            if (!file.Exists)
                return 0;

            // Читаем файл.
            var fs = (FileStream)null;
            try
            {
                fs = file.OpenRead();
                var br = new BinaryReader(fs);

                // Проверим номер версии данных в файле.
                var fileVer = br.ReadInt32();
                return fileVer;
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Failed to deserialize. Reason: " + ex.Message);
                return 0;
            }
            finally
            {
                if (fs != null)
                    fs.Close();
            }
        }
    }

    /// <summary>
    /// Собственно нужен только чтобы получать информацию о текущих параметрах кэша.
    /// Версии, формате даты для файлов кэша.
    /// Так как все запилено через protected то нужно наследоваться чтобы взять
    /// </summary>
    class CacheInfoGetter : TradeInfoCache
    {
        public int Version { get { return CacheVersion; } }

        public string CacheDateFormat { get { return m_cacheDateFormat; } }

        /// <summary>
        /// Конструктор чего то там не знаю чего.
        /// </summary>
        /// <param name="cacheName">Просто любая строка</param>
        public CacheInfoGetter(string cacheName) : base(cacheName)
        {
            
        }
    }
}
