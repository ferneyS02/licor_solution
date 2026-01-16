using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Licoreria.Desktop.Models;
using Licoreria.Desktop.Services;

namespace Licoreria.Desktop.Views;

public partial class MesasPage : Page
{
    private readonly ApiService _api = new();
    private readonly MesasLayoutStore _store = new();
    private MesasLayout _layout = new();

    private bool _editMode;
    private bool _dirty;

    private readonly Dictionary<int, Border> _mesaUi = new();
    private Border? _barraUi;

    // ✅ cajas del local
    private Border? _boliranaUi;
    private Border? _tvUi;
    private Border? _afueraUi;

    // Drag
    private UIElement? _dragElement;
    private Point _dragStartMouse;
    private double _dragStartLeft;
    private double _dragStartTop;

    private List<Mesa> _mesas = new();

    public MesasPage()
    {
        InitializeComponent();
        Loaded += async (_, __) => await LoadAsync();
    }

    private async Task LoadAsync()
    {
        try
        {
            _mesas = await _api.GetMesasAsync() ?? new();

            var canAdmin = Session.Rol.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                        || Session.Rol.Equals("Sistema", StringComparison.OrdinalIgnoreCase);

            Toolbar.Visibility = canAdmin ? Visibility.Visible : Visibility.Collapsed;
            _editMode = false;
            ChkEditar.IsChecked = false;
            BtnGuardar.IsEnabled = false;
            _dirty = false;

            _layout = _store.Load();
            RenderLayout();
        }
        catch (HttpRequestException ex)
        {
            MessageBox.Show(
                $"No pude conectarme a la API en: {ApiConfig.API}\n\n" +
                "1) Verifica que Licoreria.Api esté ejecutándose.\n" +
                "2) Verifica que esté escuchando en el puerto 5128.\n\n" +
                $"Detalle: {ex.Message}",
                "API no disponible");
        }
        catch (TaskCanceledException)
        {
            MessageBox.Show(
                $"Se agotó el tiempo intentando conectar a: {ApiConfig.API}\n\n" +
                "Asegúrate de iniciar Licoreria.Api.",
                "API no disponible");
        }
    }

    private void RenderLayout()
    {
        LayoutCanvas.Children.Clear();
        _mesaUi.Clear();

        // ✅ estructura detrás
        DrawEstructura();

        // ==================================================
        // ✅ cajas del local (mismo estilo que barra)
        // ==================================================
        var bolRect = _layout.Bolirana ?? new RectDto { X = 60, Y = 220, W = 60, H = 420 };
        _boliranaUi = BuildZonaBox("BOLIRANA", bolRect, "#9AE630", Brushes.Black);

        var tvRect = _layout.Tv ?? new RectDto { X = 520, Y = 40, W = 140, H = 60 };
        _tvUi = BuildZonaBox("TV", tvRect, "#CAD5E2", Brushes.Black);

        var afueraRect = _layout.Afuera ?? new RectDto { X = 900, Y = 40, W = 220, H = 60 };
        _afueraUi = BuildZonaBox("AFUERA", afueraRect, "#96F7E4", Brushes.Black);

        // ==================================================
        // ✅ Barra
        // ==================================================
        var barraRect = _layout.Barra ?? new RectDto { X = 420, Y = 520, W = 260, H = 80 };

        _barraUi = new Border
        {
            Width = barraRect.W,
            Height = barraRect.H,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FFDF20")),
            CornerRadius = new CornerRadius(6),
            Opacity = 0.9,
            Child = new Grid
            {
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = "BARRA",
                        Foreground = Brushes.Black,
                        FontWeight = FontWeights.Bold,
                        FontSize = 22,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    }
                }
            }
        };

        Canvas.SetLeft(_barraUi, barraRect.X);
        Canvas.SetTop(_barraUi, barraRect.Y);

        _barraUi.MouseLeftButtonDown += (s, e) => StartDragIfEdit(_barraUi, e);
        _barraUi.MouseMove += (s, e) => DragMove(e);
        _barraUi.MouseLeftButtonUp += (s, e) => EndDrag(e);

        LayoutCanvas.Children.Add(_barraUi);
        Panel.SetZIndex(_barraUi, 10);

        // ==================================================
        // Mesas
        // ==================================================
        for (int i = 0; i < _mesas.Count; i++)
        {
            var m = _mesas[i];
            var r = GetMesaRect(m.IdMesa, i);

            var mesa = BuildMesaOval(m);
            Canvas.SetLeft(mesa, r.X);
            Canvas.SetTop(mesa, r.Y);

            mesa.MouseLeftButtonDown += (s, e) =>
            {
                if (_editMode)
                {
                    StartDragIfEdit(mesa, e);
                    e.Handled = true;
                }
            };

            mesa.MouseMove += (s, e) => DragMove(e);

            mesa.MouseLeftButtonUp += async (s, e) =>
            {
                if (_editMode)
                {
                    EndDrag(e);
                    e.Handled = true;
                    return;
                }

                await OpenMesaAsync(m);
            };

            LayoutCanvas.Children.Add(mesa);
            Panel.SetZIndex(mesa, 10);
            _mesaUi[m.IdMesa] = mesa;
        }

        UpdateEditCursors();
    }

    // ✅ Caja tipo “Barra” para zonas.
    // ✅ Especial: BOLIRANA vertical con saltos de línea, MISMO tamaño de letra que las otras cajas.
    private Border BuildZonaBox(string text, RectDto rect, string bgHex, Brush fg)
    {
        const double zonaFontSize = 20;

        var isBolirana = text.Trim().Equals("BOLIRANA", StringComparison.OrdinalIgnoreCase);

        var displayText = isBolirana
            ? string.Join("\n", text.Trim().ToCharArray()) // B \n O \n L...
            : text;

        var box = new Border
        {
            Width = rect.W,
            Height = rect.H,
            Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(bgHex)),
            CornerRadius = new CornerRadius(6),
            Opacity = 0.9,
            Child = new Grid
            {
                IsHitTestVisible = false,
                Children =
                {
                    new TextBlock
                    {
                        Text = displayText,
                        Foreground = fg,
                        FontWeight = FontWeights.Bold,
                        FontSize = zonaFontSize,                 // ✅ igual que TV/Afuera
                        TextAlignment = TextAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center, // ✅ centrado
                        Padding = new Thickness(0),

                        LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                        LineHeight = zonaFontSize                // ✅ salto parejo
                    }
                }
            }
        };

        Canvas.SetLeft(box, rect.X);
        Canvas.SetTop(box, rect.Y);

        box.MouseLeftButtonDown += (s, e) => StartDragIfEdit(box, e);
        box.MouseMove += (s, e) => DragMove(e);
        box.MouseLeftButtonUp += (s, e) => EndDrag(e);

        LayoutCanvas.Children.Add(box);
        Panel.SetZIndex(box, 10);

        return box;
    }

    // =========================
    // Estructura del local (líneas blancas)
    // =========================
    private void DrawEstructura()
    {
        double yTop = 170;
        double xMid = 450;
        double xRight = 890;
        double yBottom = 870;

        AddWallLine(0, yTop, 1200, yTop, 4);
        AddWallLine(xMid, yTop, xMid, yBottom, 4);
        AddWallLine(xRight, yTop, xRight, yBottom, 4);
    }

    private void AddWallLine(double x1, double y1, double x2, double y2, double thickness)
    {
        x1 = Math.Round(x1); y1 = Math.Round(y1);
        x2 = Math.Round(x2); y2 = Math.Round(y2);

        var line = new Line
        {
            X1 = x1,
            Y1 = y1,
            X2 = x2,
            Y2 = y2,
            Stroke = Brushes.White,
            StrokeThickness = thickness,
            SnapsToDevicePixels = true,
            IsHitTestVisible = false,
            StrokeStartLineCap = PenLineCap.Round,
            StrokeEndLineCap = PenLineCap.Round
        };

        RenderOptions.SetEdgeMode(line, EdgeMode.Aliased);
        Panel.SetZIndex(line, 0);
        LayoutCanvas.Children.Add(line);
    }

    private RectDto GetMesaRect(int idMesa, int index)
    {
        if (_layout.Mesas.TryGetValue(idMesa, out var rect))
            return rect;

        var col = index % 4;
        var row = index / 4;

        return new RectDto
        {
            X = 60 + (col * 250),
            Y = 80 + (row * 160),
            W = 170,
            H = 110
        };
    }

    private Border BuildMesaOval(Mesa m)
    {
        var baseBg = (m.Estado == "Disponible")
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2ECC71"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#8B0000"));

        var hoverBg = (m.Estado == "Disponible")
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#6BFF9F"))
            : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF6B6B"));

        var baseBorder = Brushes.White;
        var hoverBorder = Brushes.Gold;

        var baseThickness = new Thickness(2);
        var hoverThickness = new Thickness(3);

        var oval = new Border
        {
            Width = 170,
            Height = 110,
            Background = baseBg,
            CornerRadius = new CornerRadius(80),
            BorderBrush = baseBorder,
            BorderThickness = baseThickness,
            Tag = m.IdMesa,
            Cursor = _editMode ? Cursors.SizeAll : Cursors.Hand
        };

        oval.MouseEnter += (_, __) =>
        {
            if (_editMode) return;
            oval.Background = hoverBg;
            oval.BorderBrush = hoverBorder;
            oval.BorderThickness = hoverThickness;
        };

        oval.MouseLeave += (_, __) =>
        {
            if (_editMode) return;
            oval.Background = baseBg;
            oval.BorderBrush = baseBorder;
            oval.BorderThickness = baseThickness;
        };

        var txt = new TextBlock
        {
            Text = $"{m.Nombre}\n{m.Estado}",
            Foreground = Brushes.Black,
            FontWeight = FontWeights.Bold,
            TextAlignment = TextAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            HorizontalAlignment = HorizontalAlignment.Center,
            IsHitTestVisible = false
        };

        oval.Child = new Grid { Children = { txt } };
        return oval;
    }

    private async Task OpenMesaAsync(Mesa m)
    {
        try
        {
            var idAbierta = await _api.GetOrdenAbiertaAsync(m.IdMesa);
            if (idAbierta.HasValue)
            {
                NavigationService?.Navigate(new OrdenPage(idAbierta.Value, m.Nombre));
                return;
            }

            var orden = await _api.AbrirOrdenAsync(m.IdMesa);
            if (orden != null)
                NavigationService?.Navigate(new OrdenPage(orden.IdOrden, orden.Mesa));
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message, "Error");
        }
    }

    private void ChkEditar_Checked(object sender, RoutedEventArgs e)
    {
        _editMode = true;
        UpdateEditCursors();
    }

    private void ChkEditar_Unchecked(object sender, RoutedEventArgs e)
    {
        _editMode = false;
        UpdateEditCursors();
    }

    private void UpdateEditCursors()
    {
        foreach (var kv in _mesaUi)
            kv.Value.Cursor = _editMode ? Cursors.SizeAll : Cursors.Hand;

        if (_barraUi != null)
            _barraUi.Cursor = _editMode ? Cursors.SizeAll : Cursors.Arrow;

        if (_boliranaUi != null) _boliranaUi.Cursor = _editMode ? Cursors.SizeAll : Cursors.Arrow;
        if (_tvUi != null) _tvUi.Cursor = _editMode ? Cursors.SizeAll : Cursors.Arrow;
        if (_afueraUi != null) _afueraUi.Cursor = _editMode ? Cursors.SizeAll : Cursors.Arrow;
    }

    private void StartDragIfEdit(UIElement element, MouseButtonEventArgs e)
    {
        if (!_editMode) return;

        _dragElement = element;
        _dragStartMouse = e.GetPosition(LayoutCanvas);
        _dragStartLeft = Canvas.GetLeft(element);
        _dragStartTop = Canvas.GetTop(element);

        element.CaptureMouse();
    }

    private void DragMove(MouseEventArgs e)
    {
        if (!_editMode) return;
        if (_dragElement == null) return;
        if (!_dragElement.IsMouseCaptured) return;

        var pos = e.GetPosition(LayoutCanvas);
        var dx = pos.X - _dragStartMouse.X;
        var dy = pos.Y - _dragStartMouse.Y;

        Canvas.SetLeft(_dragElement, _dragStartLeft + dx);
        Canvas.SetTop(_dragElement, _dragStartTop + dy);

        _dirty = true;
        BtnGuardar.IsEnabled = true;
    }

    private void EndDrag(MouseButtonEventArgs e)
    {
        if (!_editMode) return;
        if (_dragElement == null) return;

        _dragElement.ReleaseMouseCapture();
        _dragElement = null;
    }

    private void BtnGuardar_Click(object sender, RoutedEventArgs e)
    {
        if (_barraUi != null)
        {
            _layout.Barra = new RectDto
            {
                X = Canvas.GetLeft(_barraUi),
                Y = Canvas.GetTop(_barraUi),
                W = _barraUi.Width,
                H = _barraUi.Height
            };
        }

        if (_boliranaUi != null)
        {
            _layout.Bolirana = new RectDto
            {
                X = Canvas.GetLeft(_boliranaUi),
                Y = Canvas.GetTop(_boliranaUi),
                W = _boliranaUi.Width,
                H = _boliranaUi.Height
            };
        }

        if (_tvUi != null)
        {
            _layout.Tv = new RectDto
            {
                X = Canvas.GetLeft(_tvUi),
                Y = Canvas.GetTop(_tvUi),
                W = _tvUi.Width,
                H = _tvUi.Height
            };
        }

        if (_afueraUi != null)
        {
            _layout.Afuera = new RectDto
            {
                X = Canvas.GetLeft(_afueraUi),
                Y = Canvas.GetTop(_afueraUi),
                W = _afueraUi.Width,
                H = _afueraUi.Height
            };
        }

        foreach (var m in _mesas)
        {
            if (_mesaUi.TryGetValue(m.IdMesa, out var ui))
            {
                _layout.Mesas[m.IdMesa] = new RectDto
                {
                    X = Canvas.GetLeft(ui),
                    Y = Canvas.GetTop(ui),
                    W = ui.Width,
                    H = ui.Height
                };
            }
        }

        _store.Save(_layout);

        _dirty = false;
        BtnGuardar.IsEnabled = false;
        MessageBox.Show("Distribución guardada ✅", "OK", MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private async void BtnReset_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            "Esto borra la distribución guardada y vuelve a la automática.\n\n¿Continuar?",
            "Reset distribución",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm != MessageBoxResult.Yes) return;

        _store.Reset();
        await LoadAsync();
    }
}
