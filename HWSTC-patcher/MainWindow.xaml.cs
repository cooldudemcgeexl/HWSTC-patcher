using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.IO;
using Microsoft.Win32;

namespace HWSTC_patcher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string gameLocation;
        private string mainGameExecutable;

        private Dictionary<string, byte[]> fovBytes = new Dictionary<string, byte[]>();
        private Dictionary<string, byte[]> aspectRatioBytes = new Dictionary<string, byte[]>();

        private string[] aspectRatioList = { "5:4", "25:6", "16:10", "15:9", "16:9", "21:9" };

        private string selectedAspectRatio = "16:9";

        private int HorizRes;
        private int VertRes;

        private const string registryPath = @"HKEY_LOCAL_MACHINE\SOFTWARE\WOW6432Node\THQ\HotWheelsSTC\display settings";

        private enum HexOffsets
        {
            fovOffset = 0x001FAD17,
            aspectRatioOffset = 0x0036E734 // Game's aspect ratio is stored as the inverse for some reason...
        }

        

        public MainWindow()
        {
            InitializeComponent();
            initializeDicts();
            gameLocation = Properties.Settings.Default.ExecutableLocation;
            tbResX.Text = "1920";
            tbResY.Text = "1080";
            aspectRatioComboBox.ItemsSource = aspectRatioList;
            aspectRatioComboBox.SelectedItem = "16:9";
            selectedAspectRatio = aspectRatioComboBox.Text;

        }

        private void initializeDicts()
        {
            // Using values from AuToMaNiAk005's tutorial: https://youtu.be/nnpnmJvG9BI
            // If I can figure out how these were calculated, this function can probably be removed
            fovBytes.Add("5:4", new byte[] { 0x00, 0x00, 0x70, 0x3F });
            aspectRatioBytes.Add("5:4", new byte[] { 0xCD, 0xCC, 0x4C, 0x3F });

            fovBytes.Add("25:16", new byte[] { 0x00, 0x00, 0x96, 0x3F });
            aspectRatioBytes.Add("25:16", new byte[] { 0x0A, 0xD7, 0x23, 0x3F });

            fovBytes.Add("16:10", new byte[] { 0x9A, 0x99, 0x99, 0x3F });
            aspectRatioBytes.Add("16:10", new byte[] { 0x00, 0x00, 0x20, 0x2F });

            fovBytes.Add("15:9", new byte[] { 0x00, 0x00, 0xA0, 0x3F });
            aspectRatioBytes.Add("15:9", new byte[] { 0x9A, 0x99, 0x19, 0x3F });
            
            fovBytes.Add("16:9", new byte[] { 0xAB, 0xAA, 0xAA, 0x3F });
            aspectRatioBytes.Add("16:9", new byte[] { 0x00, 0x00, 0x10, 0x3F });

            fovBytes.Add("21:9", new byte[] { 0x39, 0x8E, 0xE3, 0x3F });
            aspectRatioBytes.Add("21:9", new byte[] { 0x00, 0x00, 0xD8, 0x3E });



        }

        private void setGameDir()
        {
            FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog();
            folderBrowserDialog.ShowDialog();
            gameLocation = folderBrowserDialog.SelectedPath;
            GameDir.Text = gameLocation;
            patchBtn.IsEnabled = true;
        }


        private void patchGame()
        {
            if (string.IsNullOrEmpty(gameLocation))
            {
                System.Windows.MessageBox.Show("Please select the directory of Hot Wheels Stunt Track Challenge.", "No Directory Chosen!", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                return;
            }

            mainGameExecutable = File.Exists($"{gameLocation}\\hwstc.exe") ? $"{gameLocation}\\hwstc.exe" : String.Empty;

            if (!string.IsNullOrEmpty(mainGameExecutable))
            {
                // TODO: Add better error handling
                try
                {
                    Properties.Settings.Default.ExecutableLocation = gameLocation;
                    Properties.Settings.Default.Save();
                    File.Copy(mainGameExecutable, $"{mainGameExecutable}.bak", true);
                    hexEditFile(mainGameExecutable, HexOffsets.fovOffset, fovBytes[selectedAspectRatio]);
                    hexEditFile(mainGameExecutable, HexOffsets.aspectRatioOffset, aspectRatioBytes[selectedAspectRatio]);
                    setRegistryValue("x", HorizRes);
                    setRegistryValue("y", VertRes);
                    System.Windows.MessageBox.Show("Successfully patched!", "It worked!", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch
                {
                    System.Windows.MessageBox.Show("Something went wrong", "CALAMITY!", MessageBoxButton.OK, MessageBoxImage.Error);
                    File.Copy($"{mainGameExecutable}.bak", mainGameExecutable,  true);
                }
            }
        }

        /// <summary>
        /// Hex edit the HWSTC exe for FOV and AR fixes
        /// </summary>
        /// <param name="filePath"></param>
        /// <param name="offset"></param>
        /// <param name="bytesToWrite"></param>
        private void hexEditFile(string filePath, HexOffsets offset, byte[] bytesToWrite)
        {
            using (FileStream fileStream = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite))
            {
                fileStream.Seek((long)offset, SeekOrigin.Begin);

                foreach (byte byteToWrite in bytesToWrite)
                {
                    Console.WriteLine(byteToWrite);
                    fileStream.WriteByte(byteToWrite);
                }

            }
        }

        /// <summary>
        /// Set registry values for horiz and vertical resolution
        /// </summary>
        /// <param name="regKey"></param>
        /// <param name="val"></param>
        private void setRegistryValue(string regVal, int val)
        {
            Registry.SetValue(registryPath, regVal, val, RegistryValueKind.DWord);
        }

        private void chooseGameDir_Click(object sender, RoutedEventArgs e)
        {
            setGameDir();
        }



        private void patchBtn_Click(object sender, RoutedEventArgs e)
        {
            patchGame();
        }

        private void aspectRatioComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedAspectRatio = aspectRatioComboBox.Text;
        }

        private void tbResX_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(tbResX.Text,out HorizRes);
        }

        private void tbResY_TextChanged(object sender, TextChangedEventArgs e)
        {
            int.TryParse(tbResY.Text, out VertRes);
        }



        private void tbResX_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out int _);
        }

        private void tbResY_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !int.TryParse(e.Text, out int _);
        }
    }
}
