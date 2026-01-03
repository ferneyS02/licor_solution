using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Licoreria.Desktop.Models;
using Licoreria.Desktop.Services;

namespace Licoreria.Desktop.Views;

public partial class ProductosWindow : Window
{
    private readonly ApiService _api = new();
    private readonly int _idOrden;

    // ✅ fallback icon (si no existe el png, usa un dibujo simple)
    private static readonly Lazy<ImageSource> _fallbackIcon = new(() =>
        TryLoadPackImage("pack://application:,,,/Licoreria.Desktop;component/Assets/shot_icon.png")
        ?? TryLoadPackImage("pack://application:,,,/Assets/shot_icon.png")
        ?? TryLoadPackImage("pack://application:,,,/assets/shot_icon.png")
        ?? CreateFallbackDrawing()
    );

    public ProductosWindow(int idOrden)
    {
        InitializeComponent();
        _idOrden = idOrden;
        Loaded += ProductosWindow_Loaded;
    }

    private async void ProductosWindow_Loaded(object sender, RoutedEventArgs e)
    {
        var cats = await _api.GetCategoriasAsync() ?? new();
        LstCategorias.ItemsSource = cats;

        // ✅ FIX: para que el ListBox muestre el nombre y no "Licoreria.Desktop.Models.Categoria"
        LstCategorias.DisplayMemberPath = "Nombre";

        if (cats.Count > 0) LstCategorias.SelectedIndex = 0;
    }

    private async void LstCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var cat = LstCategorias.SelectedItem as Categoria;
        if (cat == null) return;

        var productos = await _api.GetProductosPorCategoriaAsync(cat.IdCategoria) ?? new();
        WrapProductos.Children.Clear();

        foreach (var p in productos)
        {
            var card = new Border
            {
                Background = Brushes.Black,
                BorderBrush = Brushes.Goldenrod,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Width = 200
            };

            var panel = new StackPanel();

            var img = new Image
            {
                Width = 140,
                Height = 140,
                Stretch = Stretch.Uniform
            };

            // ✅ placeholder SIEMPRE primero
            var fallback = _fallbackIcon.Value;
            img.Source = fallback;

            if (!string.IsNullOrWhiteSpace(p.Imagen))
            {
                var url = ApiConfig.Img(p.Imagen);

                // ✅ útil para depurar: pasas el mouse y copias el url
                img.ToolTip = url;

                // ✅ carga robusta con fallback en DownloadFailed/DecodeFailed
                SetImageFromUrl(img, url, fallback);
            }

            var nombre = new TextBlock
            {
                Text = p.Nombre,
                Foreground = Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var precio = new TextBlock
            {
                Text = $"{p.PrecioActual:C0}",
                Foreground = Brushes.Goldenrod,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var qtyRow = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var tbQty = new TextBox { Width = 55, Text = "1" };

            var btnAdd = new Button
            {
                Content = "Agregar",
                Background = Brushes.DarkGreen,
                Foreground = Brushes.White,
                Margin = new Thickness(8, 0, 0, 0)
            };

            btnAdd.Click += async (_, __) =>
            {
                if (!int.TryParse(tbQty.Text, out var cant) || cant <= 0)
                {
                    MessageBox.Show("Cantidad inválida");
                    return;
                }

                var ok = await _api.AgregarProductoAsync(_idOrden, p.IdProducto, cant);
                if (ok) DialogResult = true;
                else MessageBox.Show("No se pudo agregar.");
            };

            qtyRow.Children.Add(tbQty);
            qtyRow.Children.Add(btnAdd);

            panel.Children.Add(img);
            panel.Children.Add(nombre);
            panel.Children.Add(precio);
            panel.Children.Add(qtyRow);

            card.Child = panel;
            WrapProductos.Children.Add(card);
        }
    }

    // =========================
    // Helpers
    // =========================

    private static void SetImageFromUrl(Image img, string url, ImageSource fallback)
    {
        // ✅ placeholder primero (nunca queda vacío)
        img.Source = fallback;

        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();

            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;

            // ✅ si falla la descarga/decodificación -> fallback
            bi.DownloadFailed += (_, __) => img.Dispatcher.Invoke(() => img.Source = fallback);
            bi.DecodeFailed += (_, __) => img.Dispatcher.Invoke(() => img.Source = fallback);

            bi.UriSource = new Uri(url, UriKind.Absolute);
            bi.EndInit();

            // ❗No hago Freeze aquí para no afectar eventos de descarga
            img.Source = bi;
        }
        catch
        {
            img.Source = fallback;
        }
    }

    private static ImageSource? TryLoadPackImage(string packUri)
    {
        try
        {
            var uri = new Uri(packUri, UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            if (info?.Stream == null) return null;

            var frame = BitmapFrame.Create(
                info.Stream,
                BitmapCreateOptions.IgnoreImageCache,
                BitmapCacheOption.OnLoad);

            frame.Freeze();
            return frame;
        }
        catch
        {
            return null;
        }
    }

    private static ImageSource CreateFallbackDrawing()
    {
        var g = new DrawingGroup();
        using (var dc = g.Open())
        {
            var pen = new Pen(Brushes.Goldenrod, 2);
            dc.DrawRectangle(Brushes.DimGray, pen, new Rect(4, 4, 132, 132));
            dc.DrawLine(pen, new Point(12, 12), new Point(128, 128));
            dc.DrawLine(pen, new Point(128, 12), new Point(12, 128));
        }
        g.Freeze();
        return new DrawingImage(g);
    }
}
