using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Reflection;

namespace Card_run.BattleModels
{
    public static class DataLoader
    {
        private static List<Card> _allCards;

        public static List<Card> GetAllCards()
        {
            if (_allCards == null)
            {
                var assembly = Assembly.GetExecutingAssembly();
                var resourceName = "Card_run.Data.cards.json";

                using (Stream stream = assembly.GetManifestResourceStream(resourceName))
                using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
                {
                    string json = reader.ReadToEnd();
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    _allCards = JsonSerializer.Deserialize<List<Card>>(json, options);
                }
            }
            return _allCards;
        }
    }
}