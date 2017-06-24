using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;
using System.Text;

namespace OneBusAway
{
    //public class Runner
    //{
    //    //public static void Run(string[] args)
    //    //{
    //    //    TranslationController c = new TranslationController();
    //    //    c.Get("university"); // cache miss
    //    //    c.Get("university"); // cache hit
    //    //    c.Get("salisbury");
    //    //    Console.ReadLine();
    //    //}
    //}

    public class TranslationController
    {
        private SortedDictionary<WeightedPhrase, string> memory;
        private Dictionary<string, WeakReference<string>> tlb;

        public TranslationController()
        {
            this.memory = new SortedDictionary<WeightedPhrase, string>();
            this.tlb = new Dictionary<string, WeakReference<string>>();

        }

        private static void Import(SortedDictionary<WeightedPhrase, string> destination)
        {
            string raw;
            using (StreamReader sr = new StreamReader("data.json"))
            {
                raw = sr.ReadToEnd();
            }
            if (raw != null)
            {
                Mapping data = JsonConvert.DeserializeObject<Mapping>(raw);
                foreach (PropertyInfo prop in data.GetType().GetProperties())
                {
                    WeightedPhrase key = new WeightedPhrase(prop.Name, prop.Name.Length);
                    string value = (string) prop.GetValue(data);
                    destination[key] = value;
                }
            }
        }

        public string Get(string original)
        {
            string result = null;
            if (this.tlb.ContainsKey(original) && tlb[original].TryGetTarget(out result))
            {
                // tlb hit
                return result;
            }
            foreach (WeightedPhrase w in this.memory.Keys)
            {
                result = original.Replace(w.value, this.memory[w]);
            }

            if (result != original) // successful translation
            {
#if debug
                foreach (char c in result)
                {
                    if ((c >= 65 && c <= 90) || (c >= 97 && c <= 122))
                    {
                        Log(original, result);
                        break;
                    }
                }
#endif          
                this.tlb[original] = new WeakReference<string>(result);
            }
            else
            {
                Log(original, result);
            }
            return result;
        }

        private void Log(string original, string result)
        {
            using (StreamWriter sr = new StreamWriter("log.out", true))
            {
                sr.WriteLine("{0}\t{1}", original, result);
            }
        }
    }

    class WeightedPhrase : IComparable<WeightedPhrase>
    {
        public int weight { get; set; }
        public string value { get; set; }
        public WeightedPhrase(string val, int weight)
        {
            this.weight = weight;
            this.value = val;
        }
        public int CompareTo(WeightedPhrase other)
        {
            // higher weights are earlier, so we go counter to int comparison
            return other.weight.CompareTo(this.weight);
        }
    }
}