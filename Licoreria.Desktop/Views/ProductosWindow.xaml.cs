using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Licoreria.Desktop.Models;
using Licoreria.Desktop.Services;

namespace Licoreria.Desktop.Views;

public partial class ProductosWindow : Window
{
    private readonly ApiService _api = new();
    private readonly int _idOrden;

    private readonly string _shotIconPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Assets", "shot_icon.png");

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
                Background = System.Windows.Media.Brushes.Black,
                BorderBrush = System.Windows.Media.Brushes.Goldenrod,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(10),
                Padding = new Thickness(10),
                Width = 200
            };

            var panel = new StackPanel();

            var img = new Image { Width = 140, Height = 140, Stretch = System.Windows.Media.Stretch.Uniform };

            // carga imagen desde API si existe; si no, shot icon
            try
            {
                if (!string.IsNullOrWhiteSpace(p.Imagen))
                {
                    var url = ApiConfig.Img(p.Imagen);
                    var bi = new BitmapImage();
                    bi.BeginInit();
                    bi.UriSource = new Uri(url, UriKind.Absolute);
                    bi.CacheOption = BitmapCacheOption.OnLoad;
                    bi.EndInit();
                    img.Source = bi;
                }
                else
                {
                    img.Source = new BitmapImage(new Uri(_shotIconPath, UriKind.Absolute));
                }
            }
            catch
            {
                img.Source = new BitmapImage(new Uri(_shotIconPath, UriKind.Absolute));
            }

            var nombre = new TextBlock
            {
                Text = p.Nombre,
                Foreground = System.Windows.Media.Brushes.White,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 8, 0, 0)
            };

            var precio = new TextBlock
            {
                Text = $"{p.PrecioActual:C0}",
                Foreground = System.Windows.Media.Brushes.Goldenrod,
                Margin = new Thickness(0, 4, 0, 0)
            };

            var qtyRow = new StackPanel { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 8, 0, 0) };
            var tbQty = new TextBox { Width = 55, Text = "1" };
            var btnAdd = new Button
            {
                Content = "Agregar",
                Background = System.Windows.Media.Brushes.DarkGreen,
                Foreground = System.Windows.Media.Brushes.White,
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
                if (ok)
                {
                    DialogResult = true; // para que OrdenPage refresque
                }
                else
                {
                    MessageBox.Show("No se pudo agregar (revisa stock si estás usando stock).");
                }
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
}
