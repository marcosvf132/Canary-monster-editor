using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Tibia.Protobuf.Staticdata;


namespace Canary_monster_editor
{
    public partial class ExportImportWindow : Window
    {
        public class CreatureExportData
        {
            public string Type { get; set; }
            public uint Id { get; set; }
            public string Name { get; set; }
            public uint LookType { get; set; }
            public uint LookTypeEx { get; set; }
            public uint Addon { get; set; }
            public uint LookHead { get; set; }
            public uint LookBody { get; set; }
            public uint LookLegs { get; set; }
            public uint LookFeet { get; set; }
        }

        public class CreatureSelectionItem
        {
            public string DisplayText { get; set; }
            public CreatureExportData Data { get; set; }
            public bool IsSelected { get; set; }
        }

        private int lastIndex = -1;

        public ExportImportWindow()
        {
            InitializeComponent();
            LoadCreatures(ListType.Monsters);
        }

        // Evento para mover a janela ao arrastar a barra de título
        private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void TypeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            if (MonsterRadio.IsChecked == true)
            {
                LoadCreatures(ListType.Monsters);
            }
            else if (BossRadio.IsChecked == true)
            {
                LoadCreatures(ListType.Bosses);
            }
        }

        private void LoadCreatures(ListType type)
        {
            if (Data.GlobalStaticData == null) return;

            CreatureListBox.Items.Clear();

            if (type == ListType.Monsters)
            {
                foreach (var monster in Data.GlobalStaticData.Monster)
                {
                    CreatureListBox.Items.Add(new CreatureSelectionItem
                    {
                        DisplayText = $"{monster.Name} (ID: {monster.Raceid})",
                        Data = new CreatureExportData
                        {
                            Type = "monster",
                            Id = monster.Raceid,
                            Name = monster.Name,
                            LookType = monster.AppearanceType?.Outfittype ?? 0,
                            LookTypeEx = monster.AppearanceType?.Itemtype ?? 0,
                            Addon = monster.AppearanceType?.Outfitaddon ?? 0,
                            LookHead = monster.AppearanceType?.Colors?.Lookhead ?? 0,
                            LookBody = monster.AppearanceType?.Colors?.Lookbody ?? 0,
                            LookLegs = monster.AppearanceType?.Colors?.Looklegs ?? 0,
                            LookFeet = monster.AppearanceType?.Colors?.Lookfeet ?? 0
                        },
                        IsSelected = false
                    });
                }
            }
            else
            {
                foreach (var boss in Data.GlobalStaticData.Boss)
                {
                    CreatureListBox.Items.Add(new CreatureSelectionItem
                    {
                        DisplayText = $"{boss.Name} (ID: {boss.Id})",
                        Data = new CreatureExportData
                        {
                            Type = "boss",
                            Id = boss.Id,
                            Name = boss.Name,
                            LookType = boss.AppearanceType?.Outfittype ?? 0,
                            LookTypeEx = boss.AppearanceType?.Itemtype ?? 0,
                            Addon = boss.AppearanceType?.Outfitaddon ?? 0,
                            LookHead = boss.AppearanceType?.Colors?.Lookhead ?? 0,
                            LookBody = boss.AppearanceType?.Colors?.Lookbody ?? 0,
                            LookLegs = boss.AppearanceType?.Colors?.Looklegs ?? 0,
                            LookFeet = boss.AppearanceType?.Colors?.Lookfeet ?? 0
                        },
                        IsSelected = false
                    });
                }
            }
        }

        private void CreatureListBox_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            var listBox = (ListBox)sender;
            var item = ItemsControl.ContainerFromElement(listBox, e.OriginalSource as DependencyObject) as ListBoxItem;
            
            if (item == null) return;

            int currentIndex = listBox.Items.IndexOf(item.DataContext);
            
            if (Keyboard.Modifiers == ModifierKeys.Shift && lastIndex != -1 && currentIndex != lastIndex)
            {
                int start = Math.Min(lastIndex, currentIndex);
                int end = Math.Max(lastIndex, currentIndex);
                
                for (int i = start; i <= end; i++)
                {
                    if (listBox.Items[i] is CreatureSelectionItem creature)
                    {
                        creature.IsSelected = true;
                    }
                }
                
                listBox.Items.Refresh();
                e.Handled = true;
            }
            else if (Keyboard.Modifiers != ModifierKeys.Control)
            {
                // Seleção única
                foreach (var listItem in listBox.Items)
                {
                    if (listItem is CreatureSelectionItem creature)
                    {
                        creature.IsSelected = false;
                    }
                }
                
                if (listBox.Items[currentIndex] is CreatureSelectionItem selectedCreature)
                {
                    selectedCreature.IsSelected = true;
                }
            }
            
            lastIndex = currentIndex;
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CreatureListBox.Items)
            {
                if (item is CreatureSelectionItem creature)
                {
                    creature.IsSelected = true;
                }
            }
            CreatureListBox.Items.Refresh();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in CreatureListBox.Items)
            {
                if (item is CreatureSelectionItem creature)
                {
                    creature.IsSelected = false;
                }
            }
            CreatureListBox.Items.Refresh();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = CreatureListBox.Items
                .Cast<CreatureSelectionItem>()
                .Where(item => item.IsSelected)
                .Select(item => item.Data)
                .ToList();

            if (!selectedItems.Any())
            {
                MessageBox.Show("Please select at least one creature", "Export", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = JsonConvert.SerializeObject(selectedItems, Formatting.Indented);
                    File.WriteAllText(saveFileDialog.FileName, json);
                    MessageBox.Show($"Successfully exported {selectedItems.Count} creatures", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Export failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                try
                {
                    string json = File.ReadAllText(openFileDialog.FileName);
                    var importedItems = JsonConvert.DeserializeObject<List<CreatureExportData>>(json);

                    if (importedItems == null || !importedItems.Any())
                    {
                        MessageBox.Show("No valid creatures found in file", "Import", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    int updatedCount = 0;
                    int createdCount = 0;
                    int skippedCount = 0;

                    foreach (var importedItem in importedItems)
                    {
                        if (importedItem.Type == "monster")
                        {
                            Monster monster = Data.GetMonsterByRaceId(importedItem.Id);
                            if (monster == null)
                            {
                                monster = new Monster
                                {
                                    Raceid = importedItem.Id,
                                    Name = importedItem.Name,
                                    AppearanceType = new Appearance_Type()
                                };
                                Data.GlobalStaticData.Monster.Add(monster);
                                createdCount++;
                            }
                            else
                            {
                                monster.Name = importedItem.Name;
                                updatedCount++;
                            }

                            if (monster.AppearanceType == null) 
                                monster.AppearanceType = new Appearance_Type();
                            
                            monster.AppearanceType.Outfittype = importedItem.LookType;
                            monster.AppearanceType.Itemtype = importedItem.LookTypeEx;
                            monster.AppearanceType.Outfitaddon = importedItem.Addon;
                            
                            if (monster.AppearanceType.Colors == null) 
                                monster.AppearanceType.Colors = new Colors();
                            
                            monster.AppearanceType.Colors.Lookhead = importedItem.LookHead;
                            monster.AppearanceType.Colors.Lookbody = importedItem.LookBody;
                            monster.AppearanceType.Colors.Looklegs = importedItem.LookLegs;
                            monster.AppearanceType.Colors.Lookfeet = importedItem.LookFeet;
                        }
                        else if (importedItem.Type == "boss")
                        {
                            Boss boss = Data.GetBossById(importedItem.Id);
                            if (boss == null)
                            {
                                boss = new Boss
                                {
                                    Id = importedItem.Id,
                                    Name = importedItem.Name,
                                    AppearanceType = Data.GlobalBossAppearancesObjects ? 
                                        new Appearance_Type() : null
                                };
                                Data.GlobalStaticData.Boss.Add(boss);
                                createdCount++;
                            }
                            else
                            {
                                boss.Name = importedItem.Name;
                                updatedCount++;
                            }

                            if (Data.GlobalBossAppearancesObjects)
                            {
                                if (boss.AppearanceType == null) 
                                    boss.AppearanceType = new Appearance_Type();
                                
                                boss.AppearanceType.Outfittype = importedItem.LookType;
                                boss.AppearanceType.Itemtype = importedItem.LookTypeEx;
                                boss.AppearanceType.Outfitaddon = importedItem.Addon;
                                
                                if (boss.AppearanceType.Colors == null) 
                                    boss.AppearanceType.Colors = new Colors();
                                
                                boss.AppearanceType.Colors.Lookhead = importedItem.LookHead;
                                boss.AppearanceType.Colors.Lookbody = importedItem.LookBody;
                                boss.AppearanceType.Colors.Looklegs = importedItem.LookLegs;
                                boss.AppearanceType.Colors.Lookfeet = importedItem.LookFeet;
                            }
                            else
                            {
                                skippedCount++;
                            }
                        }
                    }

                    string message = $"Import completed:\n" +
                                     $"- Updated: {updatedCount}\n" +
                                     $"- Created: {createdCount}\n";
                    
                    if (skippedCount > 0)
                    {
                        message += $"- Skipped (boss appearance not supported): {skippedCount}\n";
                    }

                    MessageBox.Show(message, "Import", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadCreatures(MonsterRadio.IsChecked == true ? ListType.Monsters : ListType.Bosses);
                    if (Application.Current.MainWindow is MainWindow mainWindow)
                    {
                        mainWindow.HasGlobalChangeMade = true;
                        mainWindow.ReloadMainListBox();
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Import failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}