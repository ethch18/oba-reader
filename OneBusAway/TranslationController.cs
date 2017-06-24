using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;

namespace OneBusAway
{
    public class Runner
    {
        public static void Main(string[] args)
        {
            TranslationController c = new TranslationController();
            c.Get("university"); // cache miss
            c.Get("university"); // cache hit
            c.Get("salisbury");
            Console.ReadLine();
        }
    }

    class TranslationController
    {
        private SortedDictionary<string, WeakReference> cache;
        private Mapping rawData;
        
        public TranslationController()
        {
            this.cache = new SortedDictionary<WeightedPhrase, WeakReference>();
            TranslationController.Import(this.cache);
        }

        public string Translate(string original)
        {
            using (StringBuilder sb = new StringBuilder())
            {
                
            }
        }

        // private string Get(string original)
        // {
        //     if (this.cache.ContainsKey(original))
        //     {
        //         // result found
        //         return (string) this.cache[original].Target;
        //     }
        //     var output = this.rawData.GetType().GetProperty(original);
        //     if (output != null) 
        //     {
        //         string result = (string)  output.GetValue(this.rawData);
        //         // in data store
        //         Console.WriteLine(result);
        //         this.cache[original] = new WeakReference(result);
        //         return result;
        //     }
        //     // no result found
        //     return null;
        // }

        private void Import(SortedDictionary<WeightedPhrase, WeakReference> cache)
        {
            string raw;
            using (StreamReader sr = new StreamReader("data.json"))
            {
                raw = sr.ReadToEnd();
            }
            if (raw != null)
            {
                Mapping data = JsonConvert.DeserializeObject<Mapping>(raw);
                foreach (var prop in data.GetType().GetProperties())
                {
                    WeightedPhrase key = new WeightedPhrase(prop.Name, prop.Name.Length);
                    string value = (string) prop.GetValue(data);
                    cache[key] = new WeakReference(value);
                }
            }

        }
    }

    class WeightedPhrase : IComparable<WeightedPhrase>
    {
        public int weight {get; set;};
        public string value {get; set;};
        public WeightedPhrase(string val, int weight)
        {
            this.weight = weight;
            this.value = val;
        }
        public int CompareTo(WeightedPhrase other)
        {
            return Int32.Compare(this.weight, other.weight);
        }
    }
    
    class Mapping
    {
        public string university {get; set;}
    }
}