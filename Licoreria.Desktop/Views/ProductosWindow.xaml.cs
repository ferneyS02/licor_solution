using System;
using System.Collections.Generic;
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

    private int _idCategoriaActual;
    private List<Producto> _productos = new();
    private bool _modoOrganizar;
    private bool _ordenCambiado;

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
        LstCategorias.DisplayMemberPath = "Nombre";

        // ✅ Solo Admin/Sistema pueden organizar
        var canAdmin = Session.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                    || Session.Rol.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

        ChkOrganizar.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
        BtnGuardarOrden.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;

        if (cats.Count > 0) LstCategorias.SelectedIndex = 0;
    }

    private async void LstCategorias_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var cat = LstCategorias.SelectedItem as Categoria;
        if (cat == null) return;

        _idCategoriaActual = cat.IdCategoria;

        var productos = await _api.GetProductosPorCategoriaAsync(cat.IdCategoria) ?? new();
        // ya vienen ordenados por API (Orden)
        _productos = productos.ToList();

        _ordenCambiado = false;
        BtnGuardarOrden.IsEnabled = false;

        RenderProductos();
    }

    private void ChkOrganizar_Checked(object sender, RoutedEventArgs e)
    {
        _modoOrganizar = true;
        RenderProductos();
    }

    private void ChkOrganizar_Unchecked(object sender, RoutedEventArgs e)
    {
        _modoOrganizar = false;
        RenderProductos();
    }

    private int GetCols()
    {
        // Card: 200px + márgenes aprox. -> 220
        var w = SvProductos.ActualWidth;
        if (double.IsNaN(w) || w < 260) return 1;
        var cols = (int)((w - 25) / 220);
        return Math.Max(1, cols);
    }

    private void MoveBy(int idProducto, int delta)
    {
        var i = _productos.FindIndex(p => p.IdProducto == idProducto);
        if (i < 0) return;

        var to = i + delta;
        if (to < 0 || to >= _productos.Count) return;

        var item = _productos[i];
        _productos.RemoveAt(i);
        _productos.Insert(to, item);

        _ordenCambiado = true;
        BtnGuardarOrden.IsEnabled = true;

        RenderProductos();
    }

    private void RenderProductos()
    {
        WrapProductos.Children.Clear();

        foreach (var p in _productos)
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

            var fallback = _fallbackIcon.Value;
            img.Source = fallback;

            if (!string.IsNullOrWhiteSpace(p.Imagen))
            {
                var url = ApiConfig.Img(p.Imagen);
                img.ToolTip = url;
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

            // --- fila cantidad + agregar ---
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

            // ✅ Controles de orden (solo en modo organizar)
            if (_modoOrganizar)
            {
                var cols = GetCols();

                var rowMove = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Margin = new Thickness(0, 8, 0, 0)
                };

                Button mk(string txt)
                    => new Button { Content = txt, Width = 36, Height = 28, Margin = new Thickness(0, 0, 6, 0) };

                var btnLeft = mk("←");
                var btnRight = mk("→");
                var btnUp = mk("↑");
                var btnDown = mk("↓");

                btnLeft.Click += (_, __) => MoveBy(p.IdProducto, -1);
                btnRight.Click += (_, __) => MoveBy(p.IdProducto, +1);
                btnUp.Click += (_, __) => MoveBy(p.IdProducto, -cols);
                btnDown.Click += (_, __) => MoveBy(p.IdProducto, +cols);

                rowMove.Children.Add(btnLeft);
                rowMove.Children.Add(btnRight);
                rowMove.Children.Add(btnUp);
                rowMove.Children.Add(btnDown);

                panel.Children.Add(rowMove);
            }

            panel.Children.Add(qtyRow);

            card.Child = panel;
            WrapProductos.Children.Add(card);
        }
    }

    private async void BtnGuardarOrden_Click(object sender, RoutedEventArgs e)
    {
        if (!_ordenCambiado) return;

        var ids = _productos.Select(x => x.IdProducto).ToList();
        var (ok, error) = await _api.GuardarOrdenProductosAsync(_idCategoriaActual, ids);

        if (!ok)
        {
            MessageBox.Show(error ?? "No se pudo guardar el orden.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        _ordenCambiado = false;
        BtnGuardarOrden.IsEnabled = false;
        MessageBox.Show("Orden guardado ✅", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    // =========================
    // Helpers imagen
    // =========================
    private static void SetImageFromUrl(Image img, string url, ImageSource fallback)
    {
        img.Source = fallback;
        try
        {
            var bi = new BitmapImage();
            bi.BeginInit();
            bi.CacheOption = BitmapCacheOption.OnLoad;
            bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            bi.DownloadFailed += (_, __) => img.Dispatcher.Invoke(() => img.Source = fallback);
            bi.DecodeFailed += (_, __) => img.Dispatcher.Invoke(() => img.Source = fallback);
            bi.UriSource = new Uri(url, UriKind.Absolute);
            bi.EndInit();
            img.Source = bi;
        }
        catch { img.Source = fallback; }
    }

    private static ImageSource? TryLoadPackImage(string packUri)
    {
        try
        {
            var uri = new Uri(packUri, UriKind.Absolute);
            var info = Application.GetResourceStream(uri);
            if (info?.Stream == null) return null;

            var frame = BitmapFrame.Create(info.Stream, BitmapCreateOptions.IgnoreImageCache, BitmapCacheOption.OnLoad);
            frame.Freeze();
            return frame;
        }
        catch { return null; }
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
