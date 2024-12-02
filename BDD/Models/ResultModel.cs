namespace BDD.Models
{
    public class Result
    {
        public int Id { get; set; } // Clé primaire
        public int ComputedResult { get; set; } // Le résultat
        public int val1 { get; set; }
        public int val2 { get; set; }
        public DateTime Timestamp { get; set; } // Date de sauvegarde


        public bool IsPair { get; set; }  // Indique si le résultat est pair
        public bool IsPremier { get; set; } // Indique si le résultat est un nombre premier
        public bool IsParfait { get; set; } // Indique si le résultat est un nombre parfait
    }
}
