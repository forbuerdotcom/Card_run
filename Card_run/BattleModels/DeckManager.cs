using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;

namespace Card_run.BattleModels
{
    public static class DeckManager
    {
        private static readonly string DeckFilePath = "Data/PlayerDeck.json";

        public static List<Card> LoadDeck()
        {
            if (!File.Exists(DeckFilePath))
            {
                return new List<Card>();
            }

            try
            {
                var json = File.ReadAllText(DeckFilePath);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };
                // Возвращаем загруженную колоду или пустую, если файл был пуст
                return JsonSerializer.Deserialize<List<Card>>(json, options) ?? new List<Card>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось загрузить колоду: {ex.Message}", "Ошибка загрузки", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<Card>(); // В случае ошибки тоже начинаем с пустой колоды
            }
        }

        public static void SaveDeck(List<Card> deck)
        {
            try
            {
                var options = new JsonSerializerOptions { WriteIndented = true };
                var json = JsonSerializer.Serialize(deck, options);

                // Убедимся, что директория существует
                var directory = Path.GetDirectoryName(DeckFilePath);
                if (!Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                File.WriteAllText(DeckFilePath, json);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось сохранить колоду: {ex.Message}", "Ошибка сохранения", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}