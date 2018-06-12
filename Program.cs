using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

namespace TypeBuildingJsonDeserializer
{
    public interface IPerson
    {
        int Age { get; set; }
        string Name { get; }
        IEnumerable<ICar> Cars { get; }
    }

    public interface ICar
    {
        string LicensePlate { get; set; }
    }
    class Program
    {
        static void Main(string[] args)
        {
            IPerson p = JsonConvert.DeserializeObject<IPerson>(
                "{\"Age\": 18, \"Name\":\"Akos\", \"Cars\":[{ \"LicensePlate\":\"ABC-123\" },{ \"LicensePlate\":\"DEF-123\" }]}"
            , new TypeBuildingJsonConverter());
        }
    }
}
