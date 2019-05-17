using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SahaKuiskaaja
{
    class Program
    {
        private static int min_pituus = 250;
        private static int max_pituus = 2500;

        static void Main(string[] args)
        {
            var tavara = 4200;

            // luo testidataa
            var sauvat = LuoTestidataa();
            Console.WriteLine($"Sauvat sahaukseen: {string.Join(", ", sauvat)}");

            var optimoimaton = SahaaJarjestyksessa(sauvat, tavara);
            //Console.WriteLine($"Optimoimaton, hukka: {optimoimaton.LaskeHukka()}");
            
            var bruteforce = LuoIsoJoukko(sauvat, tavara);
            //Console.WriteLine($"Hullun tuurilla löytynyt hukka: {bruteforce.LaskeHukka()}");

            var hukka1 = optimoimaton.LaskeHukka();
            var hukka2 = bruteforce.LaskeHukka();
            Console.WriteLine(
                "| Tavara: {0,10} | Optimoimaton Hukka: {1,6} | Bruteforce hukka: {2,6} | Parruja: {3,6} | Säästö: {4,10} |",
                $"{tavara} mm.", hukka1, hukka2, bruteforce.Count, hukka1 - hukka2);
        }

        private static IList<Sahaus> LuoIsoJoukko(IList<Sauva> sauvat, int tavaranPituus)
        {
            IList<Sahaus> hullunTuuria = SahaaJarjestyksessa(sauvat, tavaranPituus);
            int pieninHukka = hullunTuuria.LaskeHukka();
            // foreach (var sahausSuunnitelma in testiJoukko)
            for(int i=0; i<5000; i++)
            {
                var sekoitettu = new List<Sauva>(sauvat);
                sekoitettu.Shuffle();
                var tamaKierros = SahaaJarjestyksessa(sekoitettu, tavaranPituus);
                var tamanKierroksenHukka = tamaKierros.LaskeHukka();
                if (tamanKierroksenHukka < pieninHukka)
                {
                    hullunTuuria = tamaKierros;
                    pieninHukka = tamanKierroksenHukka;
                }
                if(i%100==0)
                {
                    Console.Write(".");
                }
            }
            Console.WriteLine();
            // etsi tuurilla löytynyt pienin hukka
            return hullunTuuria;
        }



        private static IList<Sahaus> SahaaJarjestyksessa(IList<Sauva> sauvat, int tavaranPituus)
        {
            var optimoimaton = new List<Sahaus>();

            var kopio = new List<Sauva>(sauvat);
            var nykyinenSahaus = new Sahaus { tavara = tavaranPituus };
            while (0 != kopio.Count)
            {
                // kurkkaa
                bool loytyi = false;
                for (int indeksi = 0; indeksi < kopio.Count; indeksi++)
                {
                    var ehdokas = kopio.ElementAt(indeksi);
                    if (ehdokas.pituus > tavaranPituus) throw new Exception("Ei näitä voi sahata");
                    if (ehdokas.pituus <= nykyinenSahaus.Hukka())
                    {
                        // sahataan sauva tasta
                        nykyinenSahaus.LisaaPatka(ehdokas);
                        kopio.RemoveAt(indeksi);
                        loytyi = true;
                    }
                }

                if(!loytyi)
                {
                    // TODO: vois etsia sauvan joka viela voidaan sahata tasta
                    // aloita uusi parru
                    optimoimaton.Add(nykyinenSahaus);
                    nykyinenSahaus = new Sahaus { tavara = tavaranPituus };
                }
            }

            return optimoimaton;
        }

        private static IList<Sauva> LuoTestidataa()
        {
            var rng = new Random();
            var sauvat = new List<Sauva>(100);
            for(int i=0; i<100; i++)
            {
                sauvat.Add(new Sauva { pituus = rng.Next(min_pituus, max_pituus) } );
            }
            return sauvat;
        }
    }

    static class ListExtensions
    {
        public static void Shuffle<T>(this IList<T> list)
        {
            var rng = new Random();
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = rng.Next(n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static IList<T> Kopioi<T>(this IList<T> list)
        {
            var kopio = new List<T>(list.Count);
            foreach(T elem in list)
            {
                kopio.Add(elem);
            }
            return kopio;
        }

        public static int LaskeHukka(this IList<Sahaus> optimoimaton)
        {
            int kokonaisHukka = 0;
            foreach (var parru in optimoimaton)
            {
                kokonaisHukka += parru.Hukka();
            }
            return kokonaisHukka;
        }
    }

    class Sauva
    {
        public int pituus { get; set; }
        public override string ToString()
        {
            return $"{pituus} mm";
        }
    }

    class Sahaus
    {
        public int tavara { get; set; }
        private List<Sauva> _patkat;
        private List<Sauva> patkat
        {
            get
            {
                if (null == _patkat)
                {
                    _patkat = new List<Sauva>();
                }
                return _patkat;
            }
        }

        public bool LisaaPatka(Sauva patka)
        {
            if (patka.pituus <= Hukka())
            {
                patkat.Add(patka);
                return true;
            }
            return false;
        }

        public int Hukka()
        {
            return tavara - Hyoty();
        }

        private int Hyoty()
        {
            var hyotypituus = 0;
            foreach (var patka in patkat)
            {
                hyotypituus += patka.pituus;
            }
            return hyotypituus;
        }
    }
}
