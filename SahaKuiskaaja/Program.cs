using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Threading.Tasks;
using CommandLine;
using System.Diagnostics;

namespace SahaKuiskaaja
{
    class Program
    {
        private static int min_pituus = 250;
        private static int max_pituus = 2500;
        private static int lukumaara = 100;

        public class SahaOptions
        {
            [Option('t', "tavara", Required = true, HelpText = "Aseta sahatavaran mitta millimetreina.")]
            public int Tavara { get; set; }
            [Option('m', "mitat", Required = false, HelpText = "Anna sahattavien kappaleiden mitat listattuna (space valiin).")]
            public IEnumerable<int> Mitat { get; set; }
        }

        static void Main(string[] args)
        {
            bool testiMoodi = false;
            IList<Sauva> sauvat = null;
            int tavara = 0;
            // katso parametrit
            Parser.Default.ParseArguments<SahaOptions>(args)
                   .WithParsed<SahaOptions>(o =>
                   {
                       if (null == o.Mitat)
                       {
                           Console.WriteLine($"Lahdetaan laskemaan TESTIMOODISSA, luoden {lukumaara} kpl kappaleita valilla {min_pituus}..{max_pituus} mm.");
                           Console.WriteLine($"Paina 'Y' jatkaaksesi.");
                           // luo testidataa
                           sauvat = LuoTestidataa();
                       }
                       else
                       {
                           sauvat = new List<Sauva>(o.Mitat.Count());
                           foreach (var mitta in o.Mitat)
                           {
                               sauvat.Add(new Sauva { pituus = mitta });
                           }
                       }
                       tavara = o.Tavara;
                   });

            Console.WriteLine($"Lahtee {sauvat.Count} sauvaa sahaukseen: {string.Join(", ", sauvat)}");

            var optimoimaton = SahaaJarjestyksessa(sauvat, tavara);

            var bruteforce = LuoIsoJoukko(sauvat, tavara);

            //var puupakkaaja = BinaaripuuMenetelma(sauvat, tavara);

            var hukka1 = optimoimaton.LaskeHukka();
            var hukka2 = bruteforce.LaskeHukka();

            //var hukka3 = puupakkaaja.LaskeHukka();
            Raportti(tavara, "Optimoimaton", optimoimaton, optimoimaton);
            Raportti(tavara, "Generatiivinen", bruteforce, optimoimaton);
            //Raportti(tavara, "BinaariPuu", puupakkaaja, hukka1, optimoimaton.Count);

            // valintamenettely
            IList<Sahaus> tulos = null;
            if (optimoimaton.Count < bruteforce.Count || (optimoimaton.Count == bruteforce.Count && hukka1 < hukka2))
            {
                Console.WriteLine("Optimointi ei tuottanut tulosta :(");
                tulos = optimoimaton;
            }
            else
            {
                Console.WriteLine("Valitaan generatiivinen tulos..");
                tulos = bruteforce;
            }

            // tulos tiedostoon
            var fileName = GetTempFileName(".txt");
            using (var file = new System.IO.StreamWriter(fileName))
            {
                file.WriteLine($"Tavara: {tavara} mm.");
                file.WriteLine();
                int laskuri = 0;
                foreach(var sahausInfo in tulos)
                {
                    var hukka = sahausInfo.Hukka();
                    file.WriteLine($"PARRU {++laskuri} (hukka: {hukka} mm):");
                    var jarjestyksessa = sahausInfo.patkat;
                    jarjestyksessa.Sort(delegate (Sauva c1, Sauva c2) { return c2.pituus.CompareTo(c1.pituus); });
                    file.WriteLine(string.Join(", ", jarjestyksessa));
                    file.WriteLine();
                }
                file.Flush();
            }

            // avaa notepadissa
            var process = Process.Start("notepad.exe", fileName);
            Console.WriteLine("Ohjelma odottaa Notepadin sulkemista.");
            process.WaitForExit();
            File.Delete(fileName);
        }

        internal static string GetTempFileName(string extension)
        {
            int attempt = 0;
            while (true)
            {
                string fileName = Path.GetRandomFileName();
                fileName = Path.ChangeExtension(fileName, extension);
                var hakemisto = Path.GetTempPath();
                fileName = Path.Combine(hakemisto, fileName);

                try
                {
                    using (new FileStream(fileName, FileMode.CreateNew)) { }
                    return fileName;
                }
                catch (IOException ex)
                {
                    if (++attempt == 10)
                        throw new IOException($"Tiedoston luonti ei onnistu, siivoa hakemistoa {hakemisto}.", ex);
                }
            }
        }


        private static void Raportti(int tavara, string nimi, IList<Sahaus> parruja, IList<Sahaus> referenssiHukka)
        {
            var hukka = parruja.LaskeHukka();
            var parruLkm = parruja.Count;
            var refParruLkm = referenssiHukka?.Count ?? parruLkm;
            var kokonaiset = tavara * (parruLkm - refParruLkm);
            var saasto = referenssiHukka?.LaskeHukka() - hukka - kokonaiset;
            Console.WriteLine(
                "| Menetelma: {0,15} | Tavara: {1,10} | hukka (yht): {2,6} | Parruja: {3,6} | Säästö: {4,14} |",
                nimi, $"{tavara} mm.", Metrit(hukka), parruja.Count, Metrit(saasto));
        }

        private static string Metrit(int? millit)
        {
            if (null == millit) return "";
            return $"{((double)millit.Value / 1000).ToString("n2")} m.";
        }

        private static IList<Sahaus> LuoIsoJoukko(IList<Sauva> sauvat, int tavaranPituus)
        {
            IList<Sahaus> hullunTuuria = SahaaJarjestyksessa(sauvat, tavaranPituus);
            int pieninHukka = hullunTuuria.LaskeHukka();
            int pieninParrut = hullunTuuria.Count;
            var _lock = new Object();
            Console.Write("Lasketaan kiivaasti");
            // foreach (var sahausSuunnitelma in testiJoukko)
            Parallel.For(0, 25000, i =>
            {
                var sekoitettu = new List<Sauva>(sauvat);
                sekoitettu.Shuffle();
                var tamaKierros = SahaaJarjestyksessa(sekoitettu, tavaranPituus);
                var tamanKierroksenHukka = tamaKierros.LaskeHukka();
                var tamanKierroksenParrut = tamaKierros.Count;
                lock (_lock)
                {
                    if (tamanKierroksenParrut <= pieninParrut && tamanKierroksenHukka < pieninHukka)
                    {
                        hullunTuuria = tamaKierros;
                        pieninHukka = tamanKierroksenHukka;
                        pieninParrut = tamanKierroksenParrut;
                    }
                }
                if (i % 100 == 0)
                {
                    Console.Write(".");
                }
            });
            Console.WriteLine();
            // etsi tuurilla löytynyt pienin hukka
            return hullunTuuria;
        }

        private static IList<Sahaus> BinaaripuuMenetelma(IList<Sauva> sauvat, int tavaranPituus)
        {
            // sorttaa isommasta laskevaksi
            var laskevatMitat = new List<Sauva>(sauvat);
            laskevatMitat.Sort();
            laskevatMitat.Reverse();

            // latele ensimmaiseen mihin mahtuu O(n^2)
            var sahaukset = new List<Sahaus> { new Sahaus(tavaranPituus) };
            foreach(var sauva in sauvat)
            {
                if (sauva.pituus > tavaranPituus) throw new Exception("Ei naita voi sahata");
                bool eiMahtunut = true;
                foreach(var sahaus in sahaukset)
                {
                    if(sauva.pituus <= sahaus.Hukka())
                    {
                        sahaus.LisaaPatka(sauva);
                        eiMahtunut = false;
                    }
                }
                if (eiMahtunut)
                {
                    var uusiTavara = new Sahaus(tavaranPituus);
                    uusiTavara.LisaaPatka(sauva);
                    sahaukset.Add(uusiTavara);
                }
            }

            return sahaukset;
        }

        private static IList<Sahaus> SahaaJarjestyksessa(IList<Sauva> sauvat, int tavaranPituus)
        {
            var optimoimaton = new List<Sahaus>();

            var kopio = new List<Sauva>(sauvat);
            var nykyinenSahaus = new Sahaus(tavaranPituus);
            while (0 != kopio.Count)
            {
                // kurkkaa
                bool loytyi = false;
                for (int indeksi = 0; indeksi < kopio.Count; indeksi++)
                {
                    var ehdokas = kopio.ElementAt(indeksi);
                    if (ehdokas.pituus > tavaranPituus) throw new Exception("Ei naita voi sahata");
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
                    nykyinenSahaus = new Sahaus(tavaranPituus);
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

    public class Sauva : IComparable
    {
        public int pituus { get; set; }
        public override string ToString()
        {
            return $"{pituus} mm";
        }

        public int CompareTo(object other)
        {
            return pituus.CompareTo(((Sauva)other).pituus);
        }
    }

    public class Sahaus
    {
        private int _tavara { get; set; }
        private List<Sauva> _patkat;

        public Sahaus(int tavara)
        {
            _tavara = tavara;
        }

        internal List<Sauva> patkat
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
            return _tavara - Hyoty();
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
