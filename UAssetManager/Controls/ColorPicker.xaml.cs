using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace UAssetManager.Controls;

public partial class ColorPicker : UserControl
{
	private bool _syncing;
	private bool _suppressNextTextLostFocusClose;

	private readonly DispatcherTimer _closeTimer;

	public ColorPicker()
	{
		InitializeComponent();
		SyncFromColor();

		_closeTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
		_closeTimer.Tick += (_, __) =>
		{
			_closeTimer.Stop();
			if (PART_Popup.IsOpen) PART_Popup.IsOpen = false;
		};
	}

	public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(
		nameof(Color), typeof(Color), typeof(ColorPicker),
		new FrameworkPropertyMetadata(Colors.Black, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));

	public Color Color { get => (Color)GetValue(ColorProperty); set => SetValue(ColorProperty, value); }

	public static readonly DependencyProperty ShowAlphaProperty = DependencyProperty.Register(
		nameof(ShowAlpha), typeof(bool), typeof(ColorPicker), new PropertyMetadata(true));

	public bool ShowAlpha { get => (bool)GetValue(ShowAlphaProperty); set => SetValue(ShowAlphaProperty, value); }

	public static readonly DependencyProperty RProperty = DependencyProperty.Register(nameof(R), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0));
	public static readonly DependencyProperty GProperty = DependencyProperty.Register(nameof(G), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0));
	public static readonly DependencyProperty BProperty = DependencyProperty.Register(nameof(B), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)0));
	public static readonly DependencyProperty AProperty = DependencyProperty.Register(nameof(A), typeof(byte), typeof(ColorPicker), new PropertyMetadata((byte)255));

	public byte R { get => (byte)GetValue(RProperty); set => SetValue(RProperty, value); }
	public byte G { get => (byte)GetValue(GProperty); set => SetValue(GProperty, value); }
	public byte B { get => (byte)GetValue(BProperty); set => SetValue(BProperty, value); }
	public byte A { get => (byte)GetValue(AProperty); set => SetValue(AProperty, value); }

	public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
		nameof(Text), typeof(string), typeof(ColorPicker),
		new FrameworkPropertyMetadata("", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

	/// <summary>Text form: r,g,b,a (bytes 0-255)</summary>
	public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }

	public static readonly DependencyProperty HtmlProperty = DependencyProperty.Register(
		nameof(Html), typeof(string), typeof(ColorPicker), new PropertyMetadata("#000000"));

	/// <summary>HTML form: #RRGGBB or #AARRGGBB (depending on ShowAlpha)</summary>
	public string Html { get => (string)GetValue(HtmlProperty); private set => SetValue(HtmlProperty, value); }

	protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
	{
		base.OnPropertyChanged(e);

		if (_syncing) return;

		if (e.Property == ColorProperty)
		{
			SyncFromColor();
			return;
		}

		if (e.Property == ShowAlphaProperty)
		{
			if (!ShowAlpha)
			{
				A = 255;
				Color = Color.FromArgb(255, Color.R, Color.G, Color.B);
			}
			SyncText();
			SyncHtml();
			return;
		}

		if (e.Property == RProperty || e.Property == GProperty || e.Property == BProperty || e.Property == AProperty)
		{
			var a = ShowAlpha ? A : (byte)255;
			Color = Color.FromArgb(a, R, G, B);
			SyncText();
			SyncHtml();
		}
	}

	private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		var ctl = (ColorPicker)d;
		if (ctl._syncing) return;
		if (ctl.TryApplyText((string?)e.NewValue)) ctl.SyncFromColor();
	}

	private void SyncFromColor()
	{
		_syncing = true;
		try
		{
			var c = Color;
			if (!ShowAlpha) c.A = 255;

			R = c.R;
			G = c.G;
			B = c.B;
			A = c.A;

			SyncText();
			SyncHtml();
		}
		finally
		{
			_syncing = false;
		}
	}

	private void SyncText()
	{
		var c = Color;
		if (!ShowAlpha) c.A = 255;
		var s = string.Format(CultureInfo.InvariantCulture, "{0},{1},{2},{3}", c.R, c.G, c.B, c.A);
		if (!string.Equals(Text, s, StringComparison.Ordinal)) Text = s;
	}

	private void SyncHtml()
	{
		var c = Color;
		if (!ShowAlpha) c.A = 255;
		Html = ShowAlpha
			? string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}{3:X2}", c.A, c.R, c.G, c.B)
			: string.Format(CultureInfo.InvariantCulture, "#{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B);
	}

	private bool TryApplyText(string? text)
	{
		if (string.IsNullOrWhiteSpace(text)) return false;

		var parts = text.Split(',');
		if (parts.Length != 4) return false;

		if (!byte.TryParse(parts[0].Trim(), out var r)) return false;
		if (!byte.TryParse(parts[1].Trim(), out var g)) return false;
		if (!byte.TryParse(parts[2].Trim(), out var b)) return false;
		if (!byte.TryParse(parts[3].Trim(), out var a)) return false;

		if (!ShowAlpha) a = 255;
		Color = Color.FromArgb(a, r, g, b);
		return true;
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

		SyncFromColor();
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
}