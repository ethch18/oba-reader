using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using Newtonsoft.Json;

namespace OneBusAway
{
    public class TranslationController
    {
        private SortedDictionary<WeightedPhrase, string> memory;
        private Dictionary<string, WeakReference<string>> tlb;

        public TranslationController()
        {
            this.memory = new SortedDictionary<WeightedPhrase, string>();
            this.tlb = new Dictionary<string, WeakReference<string>>();
            Import(this.memory);

        }

        private static void Import(SortedDictionary<WeightedPhrase, string> destination)
        {
            string raw;
            using (StreamReader sr = File.OpenText("db.json"))
            {
                raw = sr.ReadToEnd();
            }
            if (raw != null)
            {
                Mapping data = JsonConvert.DeserializeObject<Mapping>(raw);
                foreach (PropertyInfo prop in data.GetType().GetProperties())
                {
                    string propName = prop.Name.Replace("__", " ").Replace("_", "-");
                    WeightedPhrase key = new WeightedPhrase(propName, propName.Length);
                    string value = (string) prop.GetValue(data);
                    destination[key] = value;
                }
            }
        }

        public string Get(string original)
        {
            string result;
            if (this.tlb.ContainsKey(original) && tlb[original].TryGetTarget(out result))
            {
                // tlb hit
                return result;
            }
            result = original;
            foreach (WeightedPhrase w in this.memory.Keys)
            {
                result = result.Replace(w.value, this.memory[w]);
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
                return result;
            }
            else
            {
                Log(original, result);
                return original;
            }
        }

        private void Log(string original, string result)
        {
            // TODO: logging
            //using (StreamWriter sr = File.OpenWrite("log.out"))
            //{
            //    sr.WriteLine("{0}\t{1}", original, result);
            //}
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
            int result = other.weight.CompareTo(this.weight);
            if (result != 0)
            {
                return result;
            }
            return this.value.CompareTo(other.value);
        }
    }
}