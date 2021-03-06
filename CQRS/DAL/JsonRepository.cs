using System;
using System.Configuration;
using System.IO;
using System.Reflection;
using Newtonsoft.Json;

namespace CQRS.DAL
{
    public class JsonRepository : IRepository
    {
        public static string GetJsonFilePath(Type type)
        {
            var codeBase = Assembly.GetExecutingAssembly().CodeBase;
            var uri = new UriBuilder(codeBase);
            var path = Uri.UnescapeDataString(uri.Path);
            var res = Path.GetDirectoryName(path);

            var pathFromConfig = ConfigurationManager.AppSettings["jsonRepositoryPath"];
            if (string.IsNullOrWhiteSpace(pathFromConfig))
            {
                throw new ArgumentException(
                    "� .config ����� �� ����� ���� jsonRepositoryPath � ������������� ����� � �����������");
            }
            else
            {
                return String.Format("{0}" + pathFromConfig + "\\{1}", res, type.Name + ".json");
            }
        }

        public T[] Get<T>()
        {
            var filePath = GetJsonFilePath(typeof (T[])).Replace("[", "").Replace("]", "");
            return JsonConvert.DeserializeObject<T[]>(File.ReadAllText(filePath));
        }

        public void Set<T>(T[] items)
        {
            var filePath = GetJsonFilePath(typeof(T[])).Replace("[", "").Replace("]", "");
            File.WriteAllText(filePath, JsonConvert.SerializeObject(items));
        }
    }
}