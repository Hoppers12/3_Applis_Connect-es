namespace BDD.Models
{
    public class Result
    {
        public int Id { get; set; } // Clé primaire
        public int ComputedResult { get; set; } // Le résultat
        public DateTime Timestamp { get; set; } // Date de sauvegarde
    }
}
