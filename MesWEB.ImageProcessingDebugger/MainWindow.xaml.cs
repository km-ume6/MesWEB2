using Microsoft.Extensions.Logging;
using Microsoft.Win32;
using MesWEB.ImageCapture.Services;
using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;

namespace MesWEB.ImageProcessingDebugger
{
    public partial class MainWindow : Window
    {
        private string? _currentImagePath;
        private string? _debugDir;
        private readonly ILoggerFactory _loggerFactory;

        public MainWindow()
        {
            InitializeComponent();

            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.AddConsole();
                builder.SetMinimumLevel(LogLevel.Information);
            });
        }

        private void BtnLoad_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog()
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff|All files|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                _currentImagePath = dlg.FileName;
                ImgOriginal.Source = LoadBitmapImage(_currentImagePath);
                ImgDeskew.Source = null;
                ImgFinal.Source = null;
                LstDebug.ItemsSource = null;
            }
        }

        private void BtnProcess_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrEmpty(_currentImagePath) || !File.Exists(_currentImagePath))
            {
                MessageBox.Show(this, "画像を読み込んでください。", "エラー", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _debugDir = Path.Combine(Path.GetTempPath(), "mesweb_debug_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_debugDir);

                var logger = _loggerFactory.CreateLogger<ImageProcessingService>();
                var svc = new ImageProcessingService(logger, saveDebugImages: true);

                // 1) Deskew
                using (var srcBmp = new System.Drawing.Bitmap(_currentImagePath))
                {
                    using var deskewed = svc.DeskewImage(srcBmp);
                    var deskewPath = Path.Combine(_debugDir, "01_deskewed.png");
                    deskewed.Save(deskewPath, System.Drawing.Imaging.ImageFormat.Png);
                    ImgDeskew.Source = LoadBitmapImage(deskewPath);
                }

                // 2) Preprocess (透視補正など含む)
                var finalPath = Path.Combine(_debugDir, "03_preprocessed.png");
                svc.PreprocessForOcr(_currentImagePath, finalPath, isRoi: false, debugDir: _debugDir);

                if (File.Exists(finalPath))
                {
                    ImgFinal.Source = LoadBitmapImage(finalPath);
                }

                // 3) 列挙して ListBox に表示
                var files = Directory.GetFiles(_debugDir).OrderBy(n => n).Select(Path.GetFileName).ToArray();
                LstDebug.ItemsSource = files;
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "処理中にエラーが発生しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private BitmapImage? LoadBitmapImage(string path)
        {
            try
            {
                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.UriSource = new Uri(path);
                bmp.EndInit();
                bmp.Freeze();
                return bmp;
            }
            catch
            {
                return null;
            }
        }

        private void LstDebug_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LstDebug.SelectedItem is string fileName && !string.IsNullOrEmpty(_debugDir))
            {
                var path = Path.Combine(_debugDir, fileName);
                if (File.Exists(path))
                {
                    // 選択したデバッグ画像を Final エリアに表示
                    ImgFinal.Source = LoadBitmapImage(path);
                }
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (LstDebug.SelectedItem is not string fileName || string.IsNullOrEmpty(_debugDir))
            {
                MessageBox.Show(this, "保存するデバッグ画像を選択してください。", "情報", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var sourcePath = Path.Combine(_debugDir, fileName);
            if (!File.Exists(sourcePath))
            {
                MessageBox.Show(this, "ファイルが見つかりません。", "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var dlg = new SaveFileDialog()
            {
                FileName = fileName,
                Filter = "PNG Image|*.png|All files|*.*"
            };

            if (dlg.ShowDialog(this) == true)
            {
                try
                {
                    File.Copy(sourcePath, dlg.FileName, overwrite: true);
                    MessageBox.Show(this, "保存しました。", "完了", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(this, "保存に失敗しました: " + ex.Message, "エラー", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
