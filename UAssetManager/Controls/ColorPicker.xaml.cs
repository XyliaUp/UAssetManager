using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using UAssetAPI.UnrealTypes;

namespace UAssetManager.Controls;

public partial class ColorPicker : UserControl
{
    #region Constructor
    private bool _syncing;
    private bool _suppressNextTextLostFocusClose;

    private readonly DispatcherTimer _closeTimer;

    public ColorPicker()
    {
        InitializeComponent();

        _closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
        _closeTimer.Tick += (_, __) =>
        {
            _closeTimer.Stop();
            if (PART_Popup.IsOpen) PART_Popup.IsOpen = false;
        };
    }
    #endregion

    #region DependencyProperty
    public event EventHandler ColorChanged;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(FVector), typeof(ColorPicker),
        new FrameworkPropertyMetadata(default(FVector), FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    public FVector Value
    {
        get => (FVector)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
        nameof(Color), typeof(Color), typeof(ColorPicker),
        new FrameworkPropertyMetadata(Colors.White, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnColorChanged));

    public Color Color
    {
        get => (Color)GetValue(ColorProperty);
        set => SetValue(ColorProperty, value);
    }

    public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
        nameof(ShowAlpha), typeof(bool), typeof(ColorPicker),
        new PropertyMetadata(false, OnShowAlphaChanged));

    public bool ShowAlpha
    {
        get => (bool)GetValue(ShowAlphaProperty);
        set => SetValue(ShowAlphaProperty, value);
    }

    public static readonly DependencyProperty RProperty = DependencyProperty.Register(
        nameof(R), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentChanged));

    public static readonly DependencyProperty GProperty = DependencyProperty.Register(
        nameof(G), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentChanged));

    public static readonly DependencyProperty BProperty = DependencyProperty.Register(
        nameof(B), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0, OnComponentChanged));

    public static readonly DependencyProperty AProperty = DependencyProperty.Register(
        nameof(A), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)255, OnComponentChanged));

    public byte R
    {
        get => (byte)GetValue(RProperty);
        set => SetValue(RProperty, value);
    }

    public byte G
    {
        get => (byte)GetValue(GProperty);
        set => SetValue(GProperty, value);
    }

    public byte B
    {
        get => (byte)GetValue(BProperty);
        set => SetValue(BProperty, value);
    }

    public byte A
    {
        get => (byte)GetValue(AProperty);
        set => SetValue(AProperty, value);
    }

    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
        nameof(Text), typeof(string), typeof(ColorPicker),
        new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    /// <summary>Text form: r,g,b,a (bytes 0-255)</summary>
    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register(
        nameof(Html), typeof(string), typeof(ColorPicker), new PropertyMetadata("#000000"));

    /// <summary>HTML form: #RRGGBB or #AARRGGBB (depending on ShowAlpha)</summary>
    public string Html
    {
        get => (string)GetValue(HtmlProperty);
        private set => SetValue(HtmlProperty, value);
    }
    #endregion

    #region Methods
    private static void OnColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (ColorPicker)d;
        if (ctl._syncing) return;

        ctl.OnColorChanged((Color)e.NewValue);
    }

    private void OnColorChanged(Color c)
    {
        _syncing = true;
        try
        {
            if (!ShowAlpha) c.A = 255;

            R = c.R;
            G = c.G;
            B = c.B;
            A = c.A;

            Text = ShowAlpha
                ? string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", c.R, c.G, c.B, c.A)
                : string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}", c.R, c.G, c.B);

            Html = ShowAlpha
                ? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B)
                : string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);

            // Update FVector Value while preserving per-channel HDR scale.
            UpdateValueFromColor();

            ColorChanged?.Invoke(this, EventArgs.Empty);
        }
        finally
        {
            _syncing = false;
        }
    }

    private static void OnComponentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (ColorPicker)d;
        if (ctl._syncing) return;

        var a = ctl.ShowAlpha ? ctl.A : (byte)255;
        ctl.Color = Color.FromArgb(a, ctl.R, ctl.G, ctl.B);
    }

    private static void OnShowAlphaChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (ColorPicker)d;
        if (ctl._syncing) return;

        var alpha = (bool)e.NewValue ? ctl.A : (byte)255;
        ctl.Color = Color.FromArgb(alpha, ctl.Color.R, ctl.Color.G, ctl.Color.B);
    }

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (ColorPicker)d;
        if (ctl._syncing) return;

        ctl.OnValueChanged((FVector)e.NewValue);
    }

    private void OnValueChanged(FVector v)
    {
        _syncing = true;
        try
        {
			// Display color: per-channel clamp of FVector components to [0,1].
			double dr = Math.Clamp(v.X, 0f, 1f);
			double dg = Math.Clamp(v.Y, 0f, 1f);
			double db = Math.Clamp(v.Z, 0f, 1f);

            byte r8 = (byte)(dr * 255f);
            byte g8 = (byte)(dg * 255f);
            byte b8 = (byte)(db * 255f);

            // Call instance handler directly so that R/G/B/Text/Html are updated consistently.
            Color = Color.FromRgb(r8, g8, b8);
			OnColorChanged(Color);
        }
        finally
        {
            _syncing = false;
        }
    }

    private void UpdateValueFromColor()
    {
        // New display color (0..1) from current Color
        float drNew = Color.R / 255.0f;
        float dgNew = Color.G / 255.0f;
        float dbNew = Color.B / 255.0f;

        // Old FVector value (contains HDR information)
        var old = Value;

        // Old display color (0..1) derived from old value
        double drOld = Math.Clamp(old.X, 0f, 1f);
		double dgOld = Math.Clamp(old.Y, 0f, 1f);
		double dbOld = Math.Clamp(old.Z, 0f, 1f);

		// Per-channel HDR scale factor: original value / original display color.
		// If original display color is zero, use scale = 1 (no HDR boost).
		double factorR = drOld > 0f ? old.X / drOld : 1f;
		double factorG = dgOld > 0f ? old.Y / dgOld : 1f;
		double factorB = dbOld > 0f ? old.Z / dbOld : 1f;

		// New FVector: keep per-channel HDR factor, only change base color.
		double newX = drNew * factorR;
		double newY = dgNew * factorG;
		double newZ = dbNew * factorB;

        Value = new FVector(newX, newY, newZ);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var ctl = (ColorPicker)d;
        if (ctl._syncing) return;

        var text = (string?)e.NewValue;
        if (string.IsNullOrWhiteSpace(text)) return;

        var parts = text.Split(',');
        if (parts.Length < 3) return;

        _ = byte.TryParse(parts[0].Trim(), out var r);
        _ = byte.TryParse(parts[1].Trim(), out var g);
        _ = byte.TryParse(parts[2].Trim(), out var b);
        _ = byte.TryParse(parts.ElementAtOrDefault(3)?.Trim(), out var a);

        if (!ctl.ShowAlpha) a = 255;
        ctl.Color = Color.FromArgb(a, r, g, b);
    }

    private void Popup_Opened(object sender, EventArgs e)
    {
        _closeTimer.Stop();
    }

    private void Popup_Closed(object sender, EventArgs e)
    {
        _closeTimer.Stop();

        // Restore focus to the TextBox for a natural editing flow.
        Dispatcher.BeginInvoke(new Action(() =>
        {
            PART_Text?.Focus();
        }), DispatcherPriority.Input);
    }

    private void Text_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Opening the popup will move focus away from the TextBox; prevent that from instantly closing the popup.
        _suppressNextTextLostFocusClose = true;
        PART_Popup.IsOpen = true;
    }

    private void Text_LostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
    {
        // Desired behavior (B): click outside closes the popup.
        // But when we open the popup, the TextBox loses focus first; we suppress that one close.
        if (_suppressNextTextLostFocusClose)
        {
            _suppressNextTextLostFocusClose = false;
            return;
        }

        // If focus moves into the popup, don't close.
        if (PART_PopupRoot != null && PART_PopupRoot.IsKeyboardFocusWithin) return;

        if (PART_Popup.IsOpen) PART_Popup.IsOpen = false;
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        _closeTimer.Stop();
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        // Close shortly after mouse leaves popup (helps prevent accidental closes when moving between elements)
        if (PART_Popup.IsOpen) _closeTimer.Start();
    }
    #endregion
}